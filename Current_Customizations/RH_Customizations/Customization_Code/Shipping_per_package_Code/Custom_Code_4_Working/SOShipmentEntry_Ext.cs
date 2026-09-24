using System.Collections;
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

                // ========================================================================
                // CRITICAL: Log Base.IsDirty state - if false, Save won't actually save!
                // ========================================================================
                PXTrace.WriteWarning("[SHIP-PACKAGES-OVERRIDE] Base.IsDirty BEFORE Save.Press = {0}", Base.IsDirty);

                if (Base.IsDirty)
                {
                    PXTrace.WriteInformation("[SHIP-PACKAGES-OVERRIDE] Graph is dirty, pressing Save");
                    Base.Save.Press();
                    PXTrace.WriteInformation("[SHIP-PACKAGES-OVERRIDE] Save.Press() called");
                }
                else
                {
                    PXTrace.WriteWarning("[SHIP-PACKAGES-OVERRIDE] ⚠️ Graph is NOT dirty after restore - Save.Press() will NOT persist changes!");
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
            }
            else
            {
                PXTrace.WriteWarning("[SHIP-PACKAGES-OVERRIDE] No packages were captured for preservation - tracking may be overwritten");
            }

            PXTrace.WriteInformation("[SHIP-PACKAGES-OVERRIDE] EXITING ShipPackages override");
        }
    }
}