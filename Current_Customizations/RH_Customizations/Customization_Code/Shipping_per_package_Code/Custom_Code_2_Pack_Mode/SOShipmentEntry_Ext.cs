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

            var svc = new PackageCarrierLabelService(Base);
            svc.ValidatePackageForGeneration(shipment, package);

            FileInfo existingFile = svc.TryGetExistingCarrierLabel(package);
            if (existingFile != null)
            {
                svc.PrintSingleFile(existingFile);
                return adapter.Get();
            }

            FileInfo generatedFile = svc.GenerateCarrierLabelForPackage(shipment, package);
            if (generatedFile != null)
            {
                svc.PrintSingleFile(generatedFile);
                return adapter.Get();
            }

            throw new PXException($"No label could be found or generated for package line {package.LineNbr}.");
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