using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.CS;
using PX.SM;
using PX.Objects.CR;

namespace PX.Objects.SO
{
    public class SOShipmentEntry_Ext : PXGraphExtension<SOShipmentEntry>
    {
        public static bool IsActive() => true;

        public PXAction<SOShipment> PrintSelectedPackageLabel;

        [PXButton(CommitChanges = true)]
        [PXUIField(DisplayName = "Generate/Print Selected Package Label", Visible = true, Enabled = true)]
        protected virtual IEnumerable printSelectedPackageLabel(PXAdapter adapter)
        {
            SOShipment shipment = Base.Document.Current;
            SOPackageDetailEx package = Base.Packages.Current;

            if (shipment == null || package == null)
            {
                throw new PXException("Shipment and package must be selected to generate labels.");
            }

            PXTrace.WriteInformation(
                "[MANUAL-PRINT] Starting manual package label generation. Shipment={0}, LineNbr={1}",
                shipment.ShipmentNbr,
                package.LineNbr);

            // ========================================================================
            // Variables to capture state from inside filter scope
            // These will be used OUTSIDE the scope to avoid context leakage
            // ========================================================================
            FileInfo queuedFile = null;
            Guid? queuedPrinterID = null;
            FileInfo fallbackFile = null;

            // ========================================================================
            // CRITICAL: Activate filter scope for manual button path
            // This ensures CarrierRates.GetPackages override filters packages
            // ========================================================================
            using (CarrierPackageFilterScope.Activate(shipment.ShipmentNbr, package.LineNbr))
            {
                PXTrace.WriteInformation(
                    "[MANUAL-PRINT] [CarrierPkgFilter] Activated filter scope. Shipment={0}, LineNbr={1}",
                    shipment.ShipmentNbr,
                    package.LineNbr);

                try
                {
                    var svc = new PackageCarrierLabelService(Base);
                    svc.ValidatePackageForGeneration(shipment, package);

                    FileInfo existingFile = svc.TryGetExistingCarrierLabel(package);
                    if (existingFile != null)
                    {
                        PXTrace.WriteInformation(
                            "[MANUAL-PRINT] Using existing label file: {0}",
                            existingFile.Name);
                        
                        // Capture existing file for use outside scope
                        if (existingFile.UID.HasValue)
                        {
                            queuedFile = existingFile;
                            queuedPrinterID = ResolveDeviceHubPrinter();
                            if (!queuedPrinterID.HasValue)
                            {
                                // No printer - use fallback download
                                fallbackFile = existingFile;
                                queuedFile = null;
                            }
                        }
                        else
                        {
                            // No UID - use fallback download
                            fallbackFile = existingFile;
                        }
                        
                        // Exit scope to use captured state
                        // IMPORTANT: This return happens INSIDE the using block, but values are captured
                        // actual queueing happens outside scope
                    }
                    else
                    {
                        FileInfo generatedFile = svc.GenerateCarrierLabelForPackage(shipment, package);
                        if (generatedFile != null)
                        {
                            PXTrace.WriteInformation(
                                "[MANUAL-PRINT] ✅ Label generated: {0}",
                                generatedFile.Name);

                            // ========================================================================
                            // Simplify refresh: just request refresh, avoid heavy cache clearing
                            // ========================================================================
                            PXTrace.WriteInformation(
                                "[MANUAL-PRINT] Requesting package grid refresh after generation");

                            Base.Packages.View.RequestRefresh();

                            // Capture generated file for use outside scope
                            if (generatedFile.UID.HasValue)
                            {
                                queuedFile = generatedFile;
                                queuedPrinterID = ResolveDeviceHubPrinter();
                                if (!queuedPrinterID.HasValue)
                                {
                                    // No printer - use fallback download
                                    fallbackFile = generatedFile;
                                    queuedFile = null;
                                }
                            }
                            else
                            {
                                throw new PXException("Generated label file does not have a valid UID for printing.");
                            }
                        }
                        else
                        {
                            throw new PXException($"No label could be found or generated for package line {package.LineNbr}.");
                        }
                    }
                }
                catch (PXException pxEx)
                {
                    PXTrace.WriteError(
                        "[MANUAL-PRINT] [CarrierPkgFilter-ERROR] PXException in manual print: {0}",
                        pxEx.Message);
                    PXTrace.WriteError(
                        "[MANUAL-PRINT] [CarrierPkgFilter-ERROR] Stack: {0}",
                        pxEx.StackTrace);
                    throw;
                }
                finally
                {
                    PXTrace.WriteInformation(
                        "[MANUAL-PRINT] [CarrierPkgFilter] Filter scope exiting");
                }
            }

            // ========================================================================
            // FIX #1: CRITICAL - Print job queueing happens OUTSIDE filter scope
            // This prevents CarrierPackageFilterScope context from leaking into async operation
            // ========================================================================
            if (queuedFile != null && queuedPrinterID.HasValue)
            {
                PXTrace.WriteInformation(
                    "[MANUAL-PRINT] Queueing DeviceHub print job for file {0} (OUTSIDE scope)",
                    queuedFile.UID.Value);
                QueueFilePrintJob(queuedFile.UID.Value, queuedPrinterID.Value);

                // ====================================================================
                // OPTIONAL: Also prompt user to download file after print job is queued
                // 
                // Set to true for testing/debugging, false for production.
                // This should later become a user preference/configuration setting.
                // Default production behavior: queue print only, no download prompt.
                // ====================================================================
                bool alsoDownloadAfterPrint = true; // TODO: Make this configurable per user preference

                if (alsoDownloadAfterPrint)
                {
                    PXTrace.WriteInformation(
                        "[MANUAL-PRINT] Also offering file download after print job queued");
                    var svc = new PackageCarrierLabelService(Base);
                    svc.PrintSingleFile(queuedFile);
                    // NOTE: PrintSingleFile throws PXRedirectToFileException, which stops execution
                    // This is intentional - the print job was already queued before the exception
                }

                return adapter.Get();
            }
            else if (fallbackFile != null)
            {
                // FIX #6: Fallback file download happens OUTSIDE scope
                PXTrace.WriteWarning(
                    "[MANUAL-PRINT] No printer configured, using file download fallback (OUTSIDE scope)");
                var svc = new PackageCarrierLabelService(Base);
                svc.PrintSingleFile(fallbackFile);
                return adapter.Get();
            }
            else
            {
                throw new PXException("Label file could not be queued for printing.");
            }
        }

        protected virtual void _(Events.RowSelected<SOShipment> e)
        {
            if (e.Row == null)
                return;

            PrintSelectedPackageLabel.SetVisible(true);
            PrintSelectedPackageLabel.SetEnabled(true);
        }

        /// <summary>
        /// Resolve DeviceHub printer for carrier label printing.
        /// 
        /// Uses Acumatica's native notification utility to find printer
        /// by PrintLabels report.
        /// 
        /// Returns null if no printer is configured.
        /// </summary>
        protected virtual Guid? ResolveDeviceHubPrinter()
        {
            try
            {
                var notificationUtility = new NotificationUtility(Base);
                Guid? printerID = notificationUtility.SearchPrinter(
                    SONotificationSource.Customer,
                    SOReports.PrintLabels,
                    Base.Accessinfo.BranchID);

                if (printerID.HasValue && printerID.Value != Guid.Empty)
                {
                    PXTrace.WriteInformation(
                        "[PRINT-RESOLVE] Printer resolved. PrinterID={0}",
                        printerID.Value);
                    return printerID;
                }

                PXTrace.WriteWarning(
                    "[PRINT-RESOLVE] No printer resolved for PrintLabels");
                return null;
            }
            catch (Exception ex)
            {
                PXTrace.WriteError(
                    "[PRINT-RESOLVE] Exception resolving printer: {0}",
                    ex);
                return null;
            }
        }

        /// <summary>
        /// Queue raw ZPL label file to DeviceHub printer via LongOperationManager.
        /// 
        /// Purpose:
        /// Creates async print job so file is sent to printer, not downloaded.
        /// Uses LongOperationManager.StartAsyncOperation for proper Acumatica pattern.
        /// 
        /// Parameters:
        /// - fileID: The persisted file UID
        /// - printerID: The DeviceHub printer GUID
        /// 
        /// Why LongOperationManager:
        /// - Proper async context in Acumatica
        /// - Avoids graph lifetime issues
        /// - Integrates with UI progress/feedback
        /// </summary>
        protected virtual void QueueFilePrintJob(Guid fileID, Guid printerID)
        {
            if (fileID == Guid.Empty)
                throw new PXException("File ID is required for queuing print job.");

            if (printerID == Guid.Empty)
                throw new PXException("Printer ID is required for queuing print job.");

            PXTrace.WriteInformation(
                "[MANUAL-PRINT] Queuing print job for file {0} to printer {1}",
                fileID, printerID);

            // ====================================================================
            // Queue async print job using LongOperationManager
            // Creates fresh graph inside async context
            // ====================================================================
            Base.LongOperationManager.StartAsyncOperation(ct =>
            {
                // TRACE #3: Async operation start
                PXTrace.WriteInformation(
                    "[PRINT-ASYNC] Starting async DeviceHub operation for file {0}",
                    fileID);

                // Create fresh graph instance inside async context
                SOShipmentEntry freshGraph = PXGraph.CreateInstance<SOShipmentEntry>();
                PackageCarrierLabelService svc = new PackageCarrierLabelService(freshGraph);

                // Call async method that uses fresh graph
                return svc.SendRawFileToPrinterAsync(fileID, printerID, ct);
            });

            PXTrace.WriteInformation(
                "[MANUAL-PRINT] Print job queued successfully");
        }

        public delegate void ShipPackagesDelegate(SOShipment shiporder);

        [PXOverride]
        public virtual void ShipPackages(SOShipment shiporder, ShipPackagesDelegate baseMethod)
        {
            if (shiporder == null)
            {
                baseMethod(shiporder);
                return;
            }

            // ========================================================================
            // DIAGNOSTIC TRACE: PHASE 1 - CONFIRM SHIPMENT BEHAVIOR
            // ========================================================================
            PXTrace.WriteInformation("[SHIP-PACKAGES-OVERRIDE] ENTERED ShipPackages for shipment {0}", shiporder.ShipmentNbr);

            // Log BEFORE state - all packages before baseMethod
            PXTrace.WriteInformation("[SHIP-PACKAGES-OVERRIDE] ========== BEFORE baseMethod ==========");
            foreach (SOPackageDetailEx pkg in PXSelect<
                SOPackageDetailEx,
                Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                .Select(Base, shiporder.ShipmentNbr))
            {
                FileInfo lblFile = null;
                try
                {
                    lblFile = new PackageCarrierLabelService(Base).TryGetExistingCarrierLabel(pkg);
                }
                catch { }

                PXTrace.WriteInformation(
                    "[SHIP-PACKAGES-OVERRIDE] BEFORE - LineNbr={0}, TrackNumber={1}, TrackUrl={2}, TrackData={3}, HasLabelFile={4}, Confirmed={5}",
                    pkg.LineNbr,
                    pkg.TrackNumber ?? "(empty)",
                    pkg.TrackUrl ?? "(empty)",
                    pkg.TrackData ?? "(empty)",
                    lblFile != null ? "YES" : "NO",
                    pkg.Confirmed);
            }

            var svc = new PackageCarrierLabelService(Base);

            // Capture tracking values for packages that already have labels
            var preserved = svc.CaptureTrackingForPackagesWithExistingLabels(shiporder.ShipmentNbr);

            PXTrace.WriteInformation("[SHIP-PACKAGES-OVERRIDE] Captured {0} packages for preservation", preserved.Count);
            foreach (var kvp in preserved)
            {
                PXTrace.WriteInformation(
                    "[SHIP-PACKAGES-OVERRIDE] Preserved - LineNbr={0}, TrackNumber={1}, TrackUrl={2}, TrackData={3}",
                    kvp.Key,
                    kvp.Value.TrackNumber ?? "(empty)",
                    kvp.Value.TrackUrl ?? "(empty)",
                    kvp.Value.TrackData ?? "(empty)");
            }

            // CRITICAL: Log filter scope state before calling baseMethod
            PXTrace.WriteInformation(
                "[SHIP-PACKAGES-OVERRIDE] CarrierPackageFilterScope.IsActive={0} (about to call baseMethod)",
                CarrierPackageFilterScope.IsActive);

            // ========================================================================
            // PREVENTIVE FIX: Activate ConfirmShipmentCarrierFilterScope
            // This signals to GetPackages to filter out already-tracked packages
            // ========================================================================
            using (ConfirmShipmentCarrierFilterScope.Activate(shiporder.ShipmentNbr))
            {
                PXTrace.WriteInformation(
                    "[SHIP-PACKAGES-OVERRIDE] ConfirmShipmentCarrierFilterScope activated");

                // Let native Acumatica shipping run (with filter scope active)
                baseMethod(shiporder);

                PXTrace.WriteInformation(
                    "[SHIP-PACKAGES-OVERRIDE] ConfirmShipmentCarrierFilterScope exiting");
            }

            // ========================================================================
            // DIAGNOSTIC TRACE: AFTER baseMethod - Log state after native processing
            // ========================================================================
            PXTrace.WriteInformation("[SHIP-PACKAGES-OVERRIDE] ========== AFTER baseMethod ==========");
            foreach (SOPackageDetailEx pkg in PXSelect<
                SOPackageDetailEx,
                Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                .Select(Base, shiporder.ShipmentNbr))
            {
                FileInfo lblFile = null;
                try
                {
                    lblFile = new PackageCarrierLabelService(Base).TryGetExistingCarrierLabel(pkg);
                }
                catch { }

                PXTrace.WriteInformation(
                    "[SHIP-PACKAGES-OVERRIDE] AFTER - LineNbr={0}, TrackNumber={1}, TrackUrl={2}, TrackData={3}, HasLabelFile={4}, Confirmed={5}",
                    pkg.LineNbr,
                    pkg.TrackNumber ?? "(empty)",
                    pkg.TrackUrl ?? "(empty)",
                    pkg.TrackData ?? "(empty)",
                    lblFile != null ? "YES" : "NO",
                    pkg.Confirmed);
            }

            // Restore tracking values only for packages we want preserved
            if (preserved.Count > 0)
            {
                PXTrace.WriteInformation("[SHIP-PACKAGES-OVERRIDE] Attempting restore for {0} packages", preserved.Count);

                Base.Document.Current = Base.Document.Search<SOShipment.shipmentNbr>(shiporder.ShipmentNbr);

                svc.RestoreTrackingForPackages(shiporder.ShipmentNbr, preserved);

                // ========================================================================
                // CRITICAL: Log AFTER restore to verify restore actually worked
                // ========================================================================
                PXTrace.WriteInformation("[SHIP-PACKAGES-OVERRIDE] ========== AFTER RestoreTracking ==========");
                foreach (SOPackageDetailEx pkg in PXSelect<
                    SOPackageDetailEx,
                    Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                    .Select(Base, shiporder.ShipmentNbr))
                {
                    PXTrace.WriteInformation(
                        "[SHIP-PACKAGES-OVERRIDE] RESTORED - LineNbr={0}, TrackNumber={1}, TrackUrl={2}, TrackData={3}",
                        pkg.LineNbr,
                        pkg.TrackNumber ?? "(empty)",
                        pkg.TrackUrl ?? "(empty)",
                        pkg.TrackData ?? "(empty)");
                }
            }
            else
            {
                PXTrace.WriteWarning("[SHIP-PACKAGES-OVERRIDE] No packages were captured for preservation - tracking may be overwritten");
            }

            // ========================================================================
            // NEW FIX: Restore ShippedViaCarrier flag when all packages already tracked
            // This must be called AFTER restore so we inspect the correct package state
            // ========================================================================
            bool headerChanged = EnsureShippedViaCarrierWhenAllPackagesAlreadyTracked(shiporder);

            // ========================================================================
            // CRITICAL: Log Base.IsDirty state and save if needed
            // ========================================================================
            PXTrace.WriteWarning("[SHIP-PACKAGES-OVERRIDE] Base.IsDirty BEFORE Save.Press = {0}", Base.IsDirty);

            if (Base.IsDirty || headerChanged)
            {
                PXTrace.WriteInformation("[SHIP-PACKAGES-OVERRIDE] Graph is dirty or header changed, pressing Save");
                Base.Save.Press();
                PXTrace.WriteInformation("[SHIP-PACKAGES-OVERRIDE] Save.Press() called");
            }
            else
            {
                PXTrace.WriteWarning("[SHIP-PACKAGES-OVERRIDE] ⚠️ Graph is NOT dirty and header not changed - nothing to save");
            }

            // Log final state AFTER Save attempt
            PXTrace.WriteInformation("[SHIP-PACKAGES-OVERRIDE] ========== AFTER Save.Press ==========");
            foreach (SOPackageDetailEx pkg in PXSelect<
                SOPackageDetailEx,
                Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                .Select(Base, shiporder.ShipmentNbr))
            {
                PXTrace.WriteInformation(
                    "[SHIP-PACKAGES-OVERRIDE] FINAL - LineNbr={0}, TrackNumber={1}",
                    pkg.LineNbr,
                    pkg.TrackNumber ?? "(empty)");
            }

            PXTrace.WriteInformation("[SHIP-PACKAGES-OVERRIDE] EXITING ShipPackages override");
        }

        /// <summary>
        /// Ensure ShippedViaCarrier flag is set when all packages already have tracking numbers.
        /// Returns true if the flag was changed, false otherwise.
        /// 
        /// Purpose:
        /// When ConfirmShipmentCarrierFilterScope filters out already-tracked packages,
        /// Acumatica's native ShipPackages does not call cs.Ship(cr), so it may not set
        /// the ShippedViaCarrier flag. However, Correct Shipment depends on this flag to
        /// properly clear tracking numbers and delete label files.
        /// 
        /// This helper restores the native-equivalent state without reintroducing FedEx
        /// regeneration.
        /// 
        /// Only called during Confirm Shipment (when ConfirmShipmentCarrierFilterScope was active).
        /// Does NOT affect manual print or WMS scan print.
        /// </summary>
        private bool EnsureShippedViaCarrierWhenAllPackagesAlreadyTracked(SOShipment shiporder)
        {
            if (shiporder == null || string.IsNullOrWhiteSpace(shiporder.ShipmentNbr))
                return false;

            PXTrace.WriteInformation(
                "[CONFIRM-CARRIER-FIX] Checking ShippedViaCarrier state after confirm carrier filtering. Shipment={0}",
                shiporder.ShipmentNbr);

            // Query all packages for this shipment
            List<SOPackageDetailEx> packages = PXSelect<
                SOPackageDetailEx,
                Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                .Select(Base, shiporder.ShipmentNbr)
                .RowCast<SOPackageDetailEx>()
                .ToList();

            int packageCount = packages.Count;
            int packagesWithTracking = packages.Count(p => !string.IsNullOrWhiteSpace(p.TrackNumber));

            PXTrace.WriteInformation(
                "[CONFIRM-CARRIER-FIX] Package count={0}, PackagesWithTracking={1}",
                packageCount,
                packagesWithTracking);

            // Check if all packages have tracking
            bool allHaveTracking = packageCount > 0 && packagesWithTracking == packageCount;

            if (!allHaveTracking)
            {
                PXTrace.WriteInformation(
                    "[CONFIRM-CARRIER-FIX] Not setting ShippedViaCarrier. Reason=Not all packages have tracking.");
                return false;
            }

            // Re-query current shipment state
            SOShipment shipment = Base.Document.Search<SOShipment.shipmentNbr>(shiporder.ShipmentNbr);

            if (shipment == null)
            {
                PXTrace.WriteWarning(
                    "[CONFIRM-CARRIER-FIX] Shipment not found after search. Cannot set ShippedViaCarrier.");
                return false;
            }

            PXTrace.WriteInformation(
                "[CONFIRM-CARRIER-FIX] Shipment ShippedViaCarrier before={0}",
                shipment.ShippedViaCarrier);

            // Only set if not already true
            if (shipment.ShippedViaCarrier == true)
            {
                PXTrace.WriteInformation(
                    "[CONFIRM-CARRIER-FIX] Not setting ShippedViaCarrier. Reason=Already true.");
                return false;
            }

            // Set ShippedViaCarrier = true
            PXTrace.WriteInformation(
                "[CONFIRM-CARRIER-FIX] All packages already tracked. Setting ShippedViaCarrier=true so Correct Shipment can clear packages later.");

            shipment.ShippedViaCarrier = true;
            Base.Document.Update(shipment);

            PXTrace.WriteInformation(
                "[CONFIRM-CARRIER-FIX] Shipment ShippedViaCarrier after=True");

            return true;  // Flag was changed
        }
    }
}