using System;
using System.Linq;
using PX.Data;
using PX.BarcodeProcessing;
using PX.Objects.SO;
using PX.Objects.SO.WMS;
using PX.Objects.CS;
using PX.SM;

namespace PX.Objects.SO.WMS
{
    /// <summary>
    /// ========================================================================
    /// WMS Pack Mode - UCC Carrier Label Scan Integration
    /// ========================================================================
    /// 
    /// Purpose:
    /// When a warehouse user scans a package's custom UCC value (UsrTCUCC128)
    /// in WMS Pack Mode, this extension detects that barcode and automatically
    /// generates and prints the carrier label for only that package.
    /// 
    /// Hook Strategy:
    /// - Extends PickPackShip.PackMode.Logic (the existing WMS infrastructure)
    /// - Overrides InjectItemAbsenceHandlingByBox to append custom UCC handling
    /// - Preserves native Acumatica box auto-confirm behavior by calling base first
    /// - Appends custom logic to InventoryItemState.HandleAbsence intercept
    /// - When unrecognized barcode is scanned, checks if it matches package UCC
    /// - If match found, validates no duplicate tracking/label, queues carrier generation
    /// - Uses PXLongOperation for isolated carrier service execution
    /// 
    /// Why InjectItemAbsenceHandlingByBox (not DecorateScanState):
    /// - InjectItemAbsenceHandlingByBox is the existing decoration method in PackMode.Logic
    /// - Called during DecorateScanState for InventoryItemState decoration
    /// - Allows us to append logic without duplicating the state machine hook
    /// - Preserves clean separation of concerns (base + custom appends)
    /// 
    /// Why HandleAbsence (not SettleAndConfirmPackage):
    /// - Fires DURING scan processing, before package confirmation
    /// - Allows interception of raw barcode and match to package UCC
    /// - Returns AbsenceHandling.Done to consume the scan if matched
    /// - Avoids confusion with confirmation-triggered Asgard label printing
    /// - Clean separation: Asgard labels = checkbox-driven per-package print
    ///                    Carrier labels = scan-driven per-package shipment
    /// </summary>
    public class PackMode_UccCarrierLabelScanExt : PXGraphExtension<PickPackShip.PackMode.Logic, PickPackShip.Host>
    {
        public static bool IsActive() => true;

        /// <summary>
        /// Override InjectItemAbsenceHandlingByBox to append UCC carrier label scan handling.
        /// This method is called during InventoryItemState decoration in PackMode.Logic.DecorateScanState.
        /// We call the base method first to preserve native box auto-confirm behavior,
        /// then append our custom UCC handling logic.
        /// </summary>
        public delegate void InjectItemAbsenceHandlingByBoxDelegate(InventoryItemState itemState);

        [PXOverride]
        public virtual void InjectItemAbsenceHandlingByBox(
            InventoryItemState itemState,
            InjectItemAbsenceHandlingByBoxDelegate base_InjectItemAbsenceHandlingByBox)
        {
            // Call base first to preserve native Acumatica box auto-confirm behavior
            base_InjectItemAbsenceHandlingByBox(itemState);

            // Append our custom UCC carrier label scan handling
            PXTrace.WriteInformation("[UCC-SCAN] Appending UCC carrier label handling to InventoryItemState.HandleAbsence");

            itemState.Intercept.HandleAbsence.ByAppend((basis, barcode) =>
            {
                PXTrace.WriteInformation("[UCC-SCAN] Checking if barcode '{0}' is a package UCC", barcode ?? "null");

                return TryHandleUccCarrierLabelScan(basis, barcode)
                    ? AbsenceHandling.Done
                    : AbsenceHandling.Skipped;
            });
        }

        /// <summary>
        /// Main entry point for UCC barcode scan handling.
        /// Normalizes barcode, finds matching package, validates conditions, queues generation.
        /// Uses Basis (passed from the intercept) to access the WMS context safely.
        /// </summary>
        private bool TryHandleUccCarrierLabelScan(PickPackShip.Host basis, string rawBarcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawBarcode))
                {
                    PXTrace.WriteInformation("[UCC-SCAN] Barcode is empty, skipping UCC handling");
                    return false;
                }

                string normalizedBarcode = NormalizeBarcode(rawBarcode);
                PXTrace.WriteInformation("[UCC-SCAN] Normalized barcode: '{0}'", normalizedBarcode);

                // Access shipment through Basis.Graph (the WMS basis context)
                // basis.RefNbr is the shipment number from the WMS state
                SOShipment shipment = SOShipment.PK.Find(basis.Graph, basis.RefNbr);
                if (shipment == null)
                {
                    PXTrace.WriteWarning("[UCC-SCAN] No shipment currently loaded");
                    return false;
                }

                PXTrace.WriteInformation("[UCC-SCAN] Current shipment: {0}", shipment.ShipmentNbr);

                // Use a fresh graph for the lookup (safe approach to avoid cross-graph contamination)
                var lookupGraph = PXGraph.CreateInstance<SOShipmentEntry>();
                var lookupShipment = SOShipment.PK.Find(lookupGraph, shipment.ShipmentNbr);
                if (lookupShipment == null)
                {
                    PXTrace.WriteWarning("[UCC-SCAN] Shipment {0} could not be loaded on lookup graph", shipment.ShipmentNbr);
                    return false;
                }
                lookupGraph.Document.Current = lookupShipment;

                // Find package matching this UCC in the current shipment
                SOPackageDetailEx package = FindPackageByUcc(lookupGraph, shipment.ShipmentNbr, normalizedBarcode);
                if (package == null)
                {
                    PXTrace.WriteInformation("[UCC-SCAN] No package found with UCC '{0}' in shipment {1}", normalizedBarcode, shipment.ShipmentNbr);
                    return false;
                }

                PXTrace.WriteInformation("[UCC-SCAN] Found package line {0} with UCC '{1}'", package.LineNbr, normalizedBarcode);

                // Check for existing tracking/label (duplicate protection before queuing)
                if (HasExistingTrackingOrLabel(lookupGraph, package))
                {
                    PXTrace.WriteWarning("[UCC-SCAN] Package line {0} already has tracking or label attached, skipping generation", package.LineNbr);
                    
                    // Consume the scan (return true) but silently skip
                    // In production, could add WMS message here if available
                    return true;
                }

                // Queue the carrier label generation in a long operation
                QueueCarrierLabelGeneration(basis, shipment, package);
                return true;
            }
            catch (Exception ex)
            {
                PXTrace.WriteError("[UCC-SCAN] Exception in TryHandleUccCarrierLabelScan: {0}", ex.Message);
                PXTrace.WriteError("[UCC-SCAN] Stack: {0}", ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Normalize barcode by trimming whitespace and handling GS1 formatting.
        /// Enhanced to handle UCC-128 barcodes with GS1 separators (ASCII 0x1D).
        /// </summary>
        private string NormalizeBarcode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            // Trim whitespace and remove GS1 group separator (FNC1 / 0x1D)
            return raw.Trim().Replace("\u001D", "");
        }

        /// <summary>
        /// Find a package in the current shipment by matching its UsrTCUCC128 field.
        /// Uses reflection on extension to match barcode to package UCC.
        /// Applies normalization to both scanned barcode and stored UCC value for safe comparison.
        /// </summary>
        private SOPackageDetailEx FindPackageByUcc(SOShipmentEntry graph, string shipmentNbr, string normalizedUcc)
        {
            if (string.IsNullOrWhiteSpace(shipmentNbr) || string.IsNullOrWhiteSpace(normalizedUcc))
                return null;

            try
            {
                // Query all packages for this shipment and match UCC via extension reflection
                var result = PXSelect<
                    SOPackageDetailEx,
                    Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                    .Select(graph, shipmentNbr)
                    .RowCast<SOPackageDetailEx>()
                    .FirstOrDefault(pkg =>
                    {
                        var ext = pkg.GetExtension<TCAddon.TCSOPackageDetailExt>();
                        if (ext == null)
                            return false;

                        // Normalize BOTH scanned barcode and stored UCC for safe comparison
                        string pkgUcc = NormalizeBarcode(ext.UsrTCUCC128);
                        
                        // Case-insensitive comparison for UCC-128 barcodes
                        return string.Equals(pkgUcc, normalizedUcc, StringComparison.OrdinalIgnoreCase);
                    });

                return result;
            }
            catch (Exception ex)
            {
                PXTrace.WriteWarning("[UCC-SCAN] Exception during package lookup: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Check if package already has a tracking number or carrier label file attached.
        /// Prevents duplicate carrier generation.
        /// </summary>
        private bool HasExistingTrackingOrLabel(SOShipmentEntry graph, SOPackageDetailEx package)
        {
            if (package == null)
                return false;

            // Check for existing tracking number
            if (!string.IsNullOrWhiteSpace(package.TrackNumber))
            {
                PXTrace.WriteWarning("[UCC-SCAN] Package line {0} already has TrackNumber: {1}", package.LineNbr, package.TrackNumber);
                return true;
            }

            // Check for existing attached file
            FileInfo existingFile = TryGetExistingCarrierLabel(graph, package);
            if (existingFile != null)
            {
                PXTrace.WriteWarning("[UCC-SCAN] Package line {0} already has label file attached: {1}", package.LineNbr, existingFile.Name);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if a carrier label file is already attached to the package.
        /// Looks for .pdf, .zpl, .zplii, .epl files in package notes.
        /// 
        /// Note: This is a conservative check. In production, you may want to add additional
        /// validation for carrier-label-specific naming patterns to avoid treating unrelated
        /// package files as carrier labels.
        /// </summary>
        private FileInfo TryGetExistingCarrierLabel(SOShipmentEntry graph, SOPackageDetailEx package)
        {
            if (package == null)
                return null;

            try
            {
                Guid[] fileNotes = PXNoteAttribute.GetFileNotes(graph.Packages.Cache, package);
                if (fileNotes == null || fileNotes.Length == 0)
                    return null;

                UploadFileMaintenance upload = PXGraph.CreateInstance<UploadFileMaintenance>();
                string[] allowed = { ".zpl", ".zplii", ".epl", ".pdf" };

                return fileNotes
                    .Select(id => upload.GetFile(id))
                    .Where(f => f != null && !string.IsNullOrEmpty(f.Name))
                    .FirstOrDefault(f => allowed.Any(ext => f.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
            }
            catch (Exception ex)
            {
                PXTrace.WriteWarning("[UCC-SCAN] Error checking for existing label file: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Queue a PXLongOperation to generate and print the carrier label for the package.
        /// Uses fresh graph to avoid state contamination from WMS context.
        /// Uses basis.Graph as the context for PXLongOperation (same pattern as Asgard extension).
        /// </summary>
        private void QueueCarrierLabelGeneration(PickPackShip.Host basis, SOShipment shipment, SOPackageDetailEx package)
        {
            string shipmentNbr = shipment.ShipmentNbr;
            int? packageLineNbr = package.LineNbr;

            if (packageLineNbr == null)
            {
                PXTrace.WriteError("[UCC-SCAN] Package has no LineNbr, cannot queue generation");
                return;
            }

            PXTrace.WriteInformation("[UCC-SCAN] Queueing carrier label generation for package {0} in shipment {1}", packageLineNbr, shipmentNbr);

            // Use basis.Graph as the context (matching Asgard extension pattern)
            PXLongOperation.StartOperation(basis.Graph, delegate()
            {
                PXTrace.WriteInformation("[UCC-SCAN-LONGOP] Long operation started for package {0}", packageLineNbr);

                try
                {
                    // Create fresh graph inside long operation
                    SOShipmentEntry freshGraph = PXGraph.CreateInstance<SOShipmentEntry>();

                    // Reload shipment
                    SOShipment reloadedShipment = SOShipment.PK.Find(freshGraph, shipmentNbr);
                    if (reloadedShipment == null)
                    {
                        PXTrace.WriteError("[UCC-SCAN-LONGOP] Shipment {0} not found after reload", shipmentNbr);
                        throw new PXException($"Shipment '{shipmentNbr}' could not be reloaded in the label generation operation.");
                    }

                    freshGraph.Document.Current = reloadedShipment;

                    // Reload package
                    SOPackageDetailEx reloadedPackage = ReloadPackage(freshGraph, shipmentNbr, packageLineNbr.Value);
                    if (reloadedPackage == null)
                    {
                        PXTrace.WriteError("[UCC-SCAN-LONGOP] Package {0} not found after reload", packageLineNbr);
                        throw new PXException($"Package line {packageLineNbr} could not be reloaded in the label generation operation.");
                    }

                    PXTrace.WriteInformation("[UCC-SCAN-LONGOP] Shipment and package reloaded successfully");

                    // DUPLICATE PROTECTION (second check after reload)
                    // Another user/process could have generated the label after scan but before operation ran
                    if (!string.IsNullOrWhiteSpace(reloadedPackage.TrackNumber))
                    {
                        PXTrace.WriteWarning("[UCC-SCAN-LONGOP] Package {0} now has tracking number {1}, skipping generation", packageLineNbr, reloadedPackage.TrackNumber);
                        return;
                    }

                    if (TryGetExistingCarrierLabel(freshGraph, reloadedPackage) != null)
                    {
                        PXTrace.WriteWarning("[UCC-SCAN-LONGOP] Package {0} now has label attached, skipping generation", packageLineNbr);
                        return;
                    }

                    // Generate carrier label using service
                    var service = new PackageCarrierLabelService(freshGraph);

                    PXTrace.WriteInformation("[UCC-SCAN-LONGOP] Calling GenerateCarrierLabelForPackage for package {0}", packageLineNbr);

                    FileInfo generatedFile = service.GenerateCarrierLabelForPackage(reloadedShipment, reloadedPackage);

                    if (generatedFile == null)
                    {
                        PXTrace.WriteWarning("[UCC-SCAN-LONGOP] GenerateCarrierLabelForPackage returned null");
                        return;
                    }

                    PXTrace.WriteInformation("[UCC-SCAN-LONGOP] ✅ Carrier label generated: {0}", generatedFile.Name);

                    // Output/print the single generated label file
                    OutputSingleGeneratedLabelFile(freshGraph, generatedFile);

                    PXTrace.WriteInformation("[UCC-SCAN-LONGOP] ✅ Label file output completed");
                }
                catch (Exception longOpEx)
                {
                    PXTrace.WriteError("[UCC-SCAN-LONGOP] Exception in label generation long operation: {0}", longOpEx.Message);
                    PXTrace.WriteError("[UCC-SCAN-LONGOP] Stack: {0}", longOpEx.StackTrace);
                    throw;
                }
            });

            PXTrace.WriteInformation("[UCC-SCAN] ✅ Long operation queued for package {0}", packageLineNbr);
        }

        /// <summary>
        /// Reload a specific package from the database by shipment + line number.
        /// </summary>
        private SOPackageDetailEx ReloadPackage(SOShipmentEntry graph, string shipmentNbr, int lineNbr)
        {
            try
            {
                SOPackageDetailEx package = PXSelect<
                    SOPackageDetailEx,
                    Where<
                        SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>,
                        And<SOPackageDetailEx.lineNbr, Equal<Required<SOPackageDetailEx.lineNbr>>>>>
                    .Select(graph, shipmentNbr, lineNbr)
                    .FirstOrDefault() as SOPackageDetailEx;

                return package;
            }
            catch (Exception ex)
            {
                PXTrace.WriteWarning("[UCC-SCAN] Error reloading package {0}: {1}", lineNbr, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Output (print/download) the single generated label file.
        /// 
        /// Current implementation uses PXRedirectToFileException for browser download/viewing.
        /// NOTE: This is for testing (browser output).
        /// Future implementation may replace this with DeviceHub or print-job logic.
        /// 
        /// This method signature accepts SOShipmentEntry graph so that
        /// it can be overridden to use alternative output methods (DeviceHub direct printing,
        /// print job queuing, etc.) without requiring changes to the core logic.
        /// </summary>
        protected virtual void OutputSingleGeneratedLabelFile(SOShipmentEntry graph, FileInfo fileInfo)
        {
            if (fileInfo == null)
                throw new PXException("No label file was generated for output.");

            PXTrace.WriteInformation("[UCC-SCAN-LONGOP] Redirecting to generated label file: {0}", fileInfo.Name);

            throw new PXRedirectToFileException(fileInfo, true);
        }
    }
}
