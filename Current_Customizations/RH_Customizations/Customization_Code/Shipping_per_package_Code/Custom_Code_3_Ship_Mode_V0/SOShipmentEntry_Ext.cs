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

            var svc = new PackageCarrierLabelService(Base);

            // Capture tracking values for packages that already have labels
            var preserved = svc.CaptureTrackingForPackagesWithExistingLabels(shiporder.ShipmentNbr);

            // Let native Acumatica shipping run
            baseMethod(shiporder);

            // Restore tracking values only for packages we want preserved
            if (preserved.Count > 0)
            {
                Base.Document.Current = Base.Document.Search<SOShipment.shipmentNbr>(shiporder.ShipmentNbr);

                svc.RestoreTrackingForPackages(shiporder.ShipmentNbr, preserved);

                if (Base.IsDirty)
                    Base.Save.Press();
            }
        }
    }
}