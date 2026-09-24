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
    /// WMS Ship Mode - UCC Carrier Label Scan Interception
    /// ========================================================================
    /// 
    /// Extension: PXGraphExtension on PickPackShip.ShipMode.Logic
    /// 
    /// Purpose:
    /// Intercepts barcode processing in Ship Mode BEFORE CommandOrShipmentOnlyState
    /// rejects non-command/non-shipment barcodes.
    /// 
    /// When user enters Ship Mode, loads a shipment, and scans a package UCC barcode
    /// (matching SOPackageDetailEx.TCSOPackageDetailExt.UsrTCUCC128), this extension:
    /// 1. Detects the UCC match
    /// 2. Finds the corresponding package
    /// 3. Checks for duplicate tracking/label
    /// 4. Queues carrier label generation in PXLongOperation
    /// 5. Returns true to consume the scan
    /// 
    /// If barcode does NOT match a package UCC, delegates to base implementation.
    /// 
    /// Hook Point:
    /// ShipMode.Logic.HandleScan() - runs before state machine processes barcode
    /// 
    /// Design Pattern:
    /// Uses PXOverride on HandleScan(string barcode, Func<string, bool> base_HandleScan)
    /// Same pattern as WarehouseManagementSystem.HandleScan override
    /// </summary>
    public class ShipMode_UccCarrierLabelScanExt : PXGraphExtension<PickPackShip.ShipMode.Logic, PickPackShip.Host>
    {
        public static bool IsActive() => true;

        public delegate bool HandleScanDelegate(string barcode);

        [PXOverride]
        public virtual bool HandleScan(string barcode, Func<string, bool> base_HandleScan)
        {
            PickPackShip basis = Base.WMS;

            PXTrace.WriteInformation("[SHIP-MODE-UCC] HandleScan called with barcode: '{0}'", barcode ?? "null");

            // Only process in Ship Mode with a loaded shipment
            if (basis.Header?.Mode == PickPackShip.ShipMode.Value &&
                !string.IsNullOrWhiteSpace(basis.RefNbr))
            {
                PXTrace.WriteInformation("[SHIP-MODE-UCC] In Ship Mode with shipment: {0}", basis.RefNbr);

                // Try to handle as UCC carrier label scan
                if (TryHandleUccCarrierLabelScan(basis, barcode))
                {
                    PXTrace.WriteInformation("[SHIP-MODE-UCC] ✅ UCC scan handled successfully");
                    return true;
                }

                PXTrace.WriteInformation("[SHIP-MODE-UCC] Barcode is not a UCC match, delegating to base");
            }

            // Not in Ship Mode, or UCC not matched - use base implementation
            return base_HandleScan(barcode);
        }

        /// <summary>
        /// Attempt to handle barcode as a package UCC and generate carrier label.
        /// Returns true if handled, false if not a matching UCC.
        /// </summary>
        private bool TryHandleUccCarrierLabelScan(PickPackShip basis, string rawBarcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawBarcode))
                {
                    PXTrace.WriteWarning("[SHIP-MODE-UCC] Barcode is empty");
                    return false;
                }

                string normalizedBarcode = NormalizeBarcode(rawBarcode);
                PXTrace.WriteInformation("[SHIP-MODE-UCC] Normalized barcode: '{0}'", normalizedBarcode);

                // Get current shipment from WMS context
                if (string.IsNullOrWhiteSpace(basis.RefNbr))
                {
                    PXTrace.WriteWarning("[SHIP-MODE-UCC] No shipment loaded");
                    return false;
                }

                SOShipment shipment = SOShipment.PK.Find(basis.Graph, basis.RefNbr);
                if (shipment == null)
                {
                    PXTrace.WriteWarning("[SHIP-MODE-UCC] Shipment {0} not found", basis.RefNbr);
                    return false;
                }

                PXTrace.WriteInformation("[SHIP-MODE-UCC] Current shipment: {0}", shipment.ShipmentNbr);

                // Use fresh graph for safe package lookup
                var lookupGraph = PXGraph.CreateInstance<SOShipmentEntry>();
                var lookupShipment = SOShipment.PK.Find(lookupGraph, shipment.ShipmentNbr);
                if (lookupShipment == null)
                {
                    PXTrace.WriteWarning("[SHIP-MODE-UCC] Shipment {0} not found on lookup graph", shipment.ShipmentNbr);
                    return false;
                }
                lookupGraph.Document.Current = lookupShipment;

                // Find package by UCC
                SOPackageDetailEx package = FindPackageByUcc(lookupGraph, shipment.ShipmentNbr, normalizedBarcode);
                if (package == null)
                {
                    PXTrace.WriteInformation("[SHIP-MODE-UCC] No package found with UCC '{0}' in shipment {1}", normalizedBarcode, shipment.ShipmentNbr);
                    return false; // Not a UCC match - let base implementation handle it
                }

                PXTrace.WriteInformation("[SHIP-MODE-UCC] Found package line {0} with UCC '{1}'", package.LineNbr, normalizedBarcode);

                // Check for existing tracking/label (duplicate protection)
                if (HasExistingTrackingOrLabel(lookupGraph, package))
                {
                    PXTrace.WriteWarning("[SHIP-MODE-UCC] Package line {0} already has tracking or label", package.LineNbr);
                    basis.ReportInfo($"Package line {package.LineNbr} already has a carrier label or tracking number.");
                    return true; // Consume scan
                }

                // Queue label generation
                QueueCarrierLabelGeneration(basis, shipment, package);
                return true; // Consumed the scan
            }
            catch (Exception ex)
            {
                PXTrace.WriteError("[SHIP-MODE-UCC] Exception in TryHandleUccCarrierLabelScan: {0}", ex.Message);
                PXTrace.WriteError("[SHIP-MODE-UCC] Stack: {0}", ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Normalize barcode by trimming and removing GS1 separators (0x1D).
        /// </summary>
        private static string NormalizeBarcode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            return raw.Trim().Replace("\u001D", "");
        }

        /// <summary>
        /// Find package by matching UsrTCUCC128 field value.
        /// Uses case-insensitive comparison after normalization.
        /// </summary>
        private static SOPackageDetailEx FindPackageByUcc(SOShipmentEntry graph, string shipmentNbr, string normalizedUcc)
        {
            if (string.IsNullOrWhiteSpace(shipmentNbr) || string.IsNullOrWhiteSpace(normalizedUcc))
                return null;

            try
            {
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

                        string pkgUcc = NormalizeBarcode(ext.UsrTCUCC128);
                        return string.Equals(pkgUcc, normalizedUcc, StringComparison.OrdinalIgnoreCase);
                    });

                return result;
            }
            catch (Exception ex)
            {
                PXTrace.WriteWarning("[SHIP-MODE-UCC] Exception during package lookup: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Check if package already has tracking number or carrier label file.
        /// </summary>
        private static bool HasExistingTrackingOrLabel(SOShipmentEntry graph, SOPackageDetailEx package)
        {
            if (package == null)
                return false;

            // Check tracking number
            if (!string.IsNullOrWhiteSpace(package.TrackNumber))
            {
                PXTrace.WriteWarning("[SHIP-MODE-UCC] Package line {0} has TrackNumber: {1}", package.LineNbr, package.TrackNumber);
                return true;
            }

            // Check for attached label file
            FileInfo existingFile = TryGetExistingCarrierLabel(graph, package);
            if (existingFile != null)
            {
                PXTrace.WriteWarning("[SHIP-MODE-UCC] Package line {0} has label file: {1}", package.LineNbr, existingFile.Name);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if carrier label file is already attached to package.
        /// </summary>
        private static FileInfo TryGetExistingCarrierLabel(SOShipmentEntry graph, SOPackageDetailEx package)
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
                PXTrace.WriteWarning("[SHIP-MODE-UCC] Error checking for existing label file: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Queue PXLongOperation to generate carrier label for package.
        /// Uses fresh graph to avoid WMS context contamination.
        /// </summary>
        private static void QueueCarrierLabelGeneration(PickPackShip basis, SOShipment shipment, SOPackageDetailEx package)
        {
            string shipmentNbr = shipment.ShipmentNbr;
            int? packageLineNbr = package.LineNbr;

            if (packageLineNbr == null)
            {
                PXTrace.WriteError("[SHIP-MODE-UCC] Package has no LineNbr");
                return;
            }

            PXTrace.WriteInformation("[SHIP-MODE-UCC] Queueing label generation for package {0}", packageLineNbr);
            basis.ReportInfo($"Generating carrier label for package {packageLineNbr}...");

            PXLongOperation.StartOperation(basis.Graph, delegate()
            {
                PXTrace.WriteInformation("[SHIP-MODE-UCC-LONGOP] Long operation started for package {0}", packageLineNbr);

                // ========================================================================
                // CRITICAL: Activate filter scope BEFORE creating freshGraph
                // This ensures Initialize() sees the active scope and replaces the view
                // ========================================================================
                using (CarrierPackageFilterScope.Activate(shipmentNbr, packageLineNbr))
                {
                    PXTrace.WriteInformation("[SHIP-MODE-UCC-LONGOP] [CarrierPkgFilter] Activated filter scope. Shipment={0}, LineNbr={1}", shipmentNbr, packageLineNbr);

                    try
                    {
                        // Create fresh graph INSIDE filter scope
                        // Initialize() will see active scope and replace Packages view
                        SOShipmentEntry freshGraph = PXGraph.CreateInstance<SOShipmentEntry>();
                        PXTrace.WriteInformation("[SHIP-MODE-UCC-LONGOP] [CarrierPkgFilter] Fresh SOShipmentEntry created with active filter scope");

                        // Reload shipment
                        SOShipment reloadedShipment = SOShipment.PK.Find(freshGraph, shipmentNbr);
                        if (reloadedShipment == null)
                        {
                            PXTrace.WriteError("[SHIP-MODE-UCC-LONGOP] Shipment {0} not found after reload", shipmentNbr);
                            throw new PXException($"Shipment '{shipmentNbr}' could not be reloaded in the label generation operation.");
                        }
                        freshGraph.Document.Current = reloadedShipment;

                        // Reload package
                        SOPackageDetailEx reloadedPackage = ReloadPackage(freshGraph, shipmentNbr, packageLineNbr.Value);
                        if (reloadedPackage == null)
                        {
                            PXTrace.WriteError("[SHIP-MODE-UCC-LONGOP] Package {0} not found after reload", packageLineNbr);
                            throw new PXException($"Package line {packageLineNbr} could not be reloaded in the label generation operation.");
                        }

                        PXTrace.WriteInformation("[SHIP-MODE-UCC-LONGOP] Shipment and package reloaded successfully");

                        // DUPLICATE PROTECTION CHECK #2
                        // Another user/process might have generated the label between scan and operation start
                        if (!string.IsNullOrWhiteSpace(reloadedPackage.TrackNumber))
                        {
                            PXTrace.WriteWarning("[SHIP-MODE-UCC-LONGOP] Package {0} now has tracking {1}, skipping generation", packageLineNbr, reloadedPackage.TrackNumber);
                            return;
                        }

                        if (TryGetExistingCarrierLabel(freshGraph, reloadedPackage) != null)
                        {
                            PXTrace.WriteWarning("[SHIP-MODE-UCC-LONGOP] Package {0} now has label file attached, skipping generation", packageLineNbr);
                            return;
                        }

                        // Log package count BEFORE filter execution
                        var beforePackageCount = freshGraph.Packages.Select().RowCast<SOPackageDetailEx>().Count();
                        PXTrace.WriteInformation("[SHIP-MODE-UCC-LONGOP] [CarrierPkgFilter-BEFORE] Total packages visible: {0}", beforePackageCount);
                        foreach (SOPackageDetailEx pkg in freshGraph.Packages.Select().RowCast<SOPackageDetailEx>())
                        {
                            PXTrace.WriteInformation(
                                "[SHIP-MODE-UCC-LONGOP] [CarrierPkgFilter-BEFORE] LineNbr={0}, Confirmed={1}, TrackNumber={2}",
                                pkg.LineNbr, pkg.Confirmed, pkg.TrackNumber ?? "(empty)");
                        }

                        // Log package count AFTER filter has been applied (via Initialize on graph creation)
                        var afterPackageCount = freshGraph.Packages.Select().RowCast<SOPackageDetailEx>().Count();
                        PXTrace.WriteInformation("[SHIP-MODE-UCC-LONGOP] [CarrierPkgFilter-AFTER] visible package count = {0}", afterPackageCount);
                        foreach (SOPackageDetailEx pkg in freshGraph.Packages.Select().RowCast<SOPackageDetailEx>())
                        {
                            PXTrace.WriteInformation(
                                "[SHIP-MODE-UCC-LONGOP] [CarrierPkgFilter-AFTER] LineNbr={0}, Confirmed={1}, TrackNumber={2}",
                                pkg.LineNbr, pkg.Confirmed, pkg.TrackNumber ?? "(empty)");
                        }

                        // Generate carrier label
                        var service = new PackageCarrierLabelService(freshGraph);

                        PXTrace.WriteInformation("[SHIP-MODE-UCC-LONGOP] [CarrierPkgFilter] Calling GenerateCarrierLabelForPackage with filtered Packages view");

                        try
                        {
                            FileInfo generatedFile = service.GenerateCarrierLabelForPackage(reloadedShipment, reloadedPackage);

                            if (generatedFile == null)
                            {
                                PXTrace.WriteWarning("[SHIP-MODE-UCC-LONGOP] GenerateCarrierLabelForPackage returned null");
                                return;
                            }

                            PXTrace.WriteInformation("[SHIP-MODE-UCC-LONGOP] ✅ Label generated: {0}", generatedFile.Name);

                            // Output the generated file
                            OutputSingleGeneratedLabelFile(generatedFile);

                            PXTrace.WriteInformation("[SHIP-MODE-UCC-LONGOP] ✅ Label file output completed");
                        }
                        catch (PXException pxEx)
                        {
                            PXTrace.WriteError("[SHIP-MODE-UCC-LONGOP] [CarrierPkgFilter-ERROR] BuildRequest failed with PXException: {0}", pxEx.Message);
                            PXTrace.WriteError("[SHIP-MODE-UCC-LONGOP] [CarrierPkgFilter-ERROR] Stack: {0}", pxEx.StackTrace);
                            throw;
                        }

                        PXTrace.WriteInformation("[SHIP-MODE-UCC-LONGOP] [CarrierPkgFilter] Filter scope ending (will restore)");
                    }
                    catch (Exception longOpEx)
                    {
                        PXTrace.WriteError("[SHIP-MODE-UCC-LONGOP] Exception in label generation: {0}", longOpEx.Message);
                        PXTrace.WriteError("[SHIP-MODE-UCC-LONGOP] Stack: {0}", longOpEx.StackTrace);
                        throw;
                    }
                }

                PXTrace.WriteInformation("[SHIP-MODE-UCC-LONGOP] [CarrierPkgFilter] Filter scope exited - Packages view restored");
            });

            PXTrace.WriteInformation("[SHIP-MODE-UCC] ✅ Long operation queued for package {0}", packageLineNbr);
        }

        /// <summary>
        /// Reload package by shipment + line number.
        /// </summary>
        private static SOPackageDetailEx ReloadPackage(SOShipmentEntry graph, string shipmentNbr, int lineNbr)
        {
            try
            {
                return PXSelect<
                    SOPackageDetailEx,
                    Where<
                        SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>,
                        And<SOPackageDetailEx.lineNbr, Equal<Required<SOPackageDetailEx.lineNbr>>>>>
                    .Select(graph, shipmentNbr, lineNbr)
                    .RowCast<SOPackageDetailEx>()
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                PXTrace.WriteWarning("[SHIP-MODE-UCC] Error reloading package {0}: {1}", lineNbr, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Output the generated label file to user.
        /// Currently uses PXRedirectToFileException for browser download.
        /// Future: Replace with DeviceHub or print-job logic.
        /// </summary>
        private static void OutputSingleGeneratedLabelFile(FileInfo fileInfo)
        {
            if (fileInfo == null)
                throw new PXException("No label file was generated for output.");

            PXTrace.WriteInformation("[SHIP-MODE-UCC-LONGOP] Redirecting to label file: {0}", fileInfo.Name);

            throw new PXRedirectToFileException(fileInfo, true);
        }
    }
}
