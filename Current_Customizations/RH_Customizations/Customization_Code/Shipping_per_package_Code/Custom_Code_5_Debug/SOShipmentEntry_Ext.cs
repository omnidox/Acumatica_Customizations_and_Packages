using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.SO;
using PX.SM;

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
                        svc.PrintSingleFile(existingFile);
                        return adapter.Get();
                    }

                    FileInfo generatedFile = svc.GenerateCarrierLabelForPackage(shipment, package);
                    if (generatedFile != null)
                    {
                        PXTrace.WriteInformation(
                            "[MANUAL-PRINT] ✅ Label generated: {0}",
                            generatedFile.Name);

                        // ========================================================================
                        // CRITICAL: Refresh UI cache before redirect to print
                        // File download/print redirect interrupts normal screen response, so we must
                        // clear caches and re-query the package before printing to ensure the grid
                        // shows updated tracking number when user returns or refreshes
                        // ========================================================================
                        PXTrace.WriteInformation(
                            "[MANUAL-PRINT] Refreshing package grid cache before print redirect");

                        // Request a refresh and clear caches
                        Base.Packages.View.RequestRefresh();
                        Base.Packages.Cache.Clear();
                        Base.Packages.View.Clear();

                        // Re-query the shipment to get fresh state
                        Base.Document.Current = Base.Document.Search<SOShipment.shipmentNbr>(shipment.ShipmentNbr);

                        // Re-query the specific package to get the updated tracking number
                        SOPackageDetailEx refreshedPackage = PXSelect<
                            SOPackageDetailEx,
                            Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>,
                                And<SOPackageDetailEx.lineNbr, Equal<Required<SOPackageDetailEx.lineNbr>>>>>
                            .Select(Base, shipment.ShipmentNbr, package.LineNbr);

                        if (refreshedPackage != null)
                        {
                            Base.Packages.Current = refreshedPackage;
                            PXTrace.WriteInformation(
                                "[MANUAL-PRINT] Package grid refreshed. Current package: LineNbr={0}, TrackNumber={1}",
                                refreshedPackage.LineNbr,
                                refreshedPackage.TrackNumber ?? "(empty)");
                        }
                        else
                        {
                            PXTrace.WriteWarning(
                                "[MANUAL-PRINT] Package not found after refresh. Grid may not show updated tracking.");
                        }

                        svc.PrintSingleFile(generatedFile);
                        return adapter.Get();
                    }

                    throw new PXException($"No label could be found or generated for package line {package.LineNbr}.");
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
        }

        protected virtual void _(Events.RowSelected<SOShipment> e)
        {
            if (e.Row == null)
                return;

            PrintSelectedPackageLabel.SetVisible(true);
            PrintSelectedPackageLabel.SetEnabled(true);
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