using System;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;
using PX.Objects.SO.GraphExtensions.SOShipmentEntryExt;

namespace iStarShippingRestrictionsCustomizations
{
    public class ConfirmShipmentExtension_Extension
        : PXGraphExtension<ConfirmShipmentExtension, SOShipmentEntry>
    {
        public static bool IsActive() => true;

        private static readonly HashSet<string> BypassCustomerCDs =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DKOHL"
            };

        public delegate void ValidateShipmentDelegate(SOShipment shiporder);

        [PXOverride]
        public virtual void ValidateShipment(
            SOShipment shiporder,
            ValidateShipmentDelegate baseMethod)
        {
            baseMethod?.Invoke(shiporder);

            SOShipment shipment = Base.Document.Current ?? shiporder;
            if (shipment == null)
                return;

            if (ShouldBypassShippingValidation(shipment))
            {
                PXTrace.WriteInformation(
                    $"Shipping restriction validation bypassed for shipment {shipment.ShipmentNbr}, customer ID {shipment.CustomerID}.");

                return;
            }

            // Validation 1: shipment must have package contents
            PXResultset<SOShipLineSplitPackage> packageContents =
                PXSelect<
                    SOShipLineSplitPackage,
                    Where<
                        SOShipLineSplitPackage.shipmentNbr,
                        Equal<Required<SOShipLineSplitPackage.shipmentNbr>>>>
                .Select(Base, shipment.ShipmentNbr);

            if (packageContents.Count == 0)
            {
                throw new PXException("Please add contents to the package before confirming shipment.");
            }

            // Validation 2: every package must have GS1-128 / UCC128
            PXResultset<SOPackageDetailEx> packages =
                PXSelect<
                    SOPackageDetailEx,
                    Where<
                        SOPackageDetailEx.shipmentNbr,
                        Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                .Select(Base, shipment.ShipmentNbr);

            foreach (SOPackageDetailEx package in packages)
            {
                string ucc128 = Base.Packages.Cache.GetValue(package, "UsrTCUCC128") as string;

                if (string.IsNullOrWhiteSpace(ucc128))
                {
                    throw new PXException("GS1-128 is required for all packages before confirming shipment.");
                }
            }
        }

        private bool ShouldBypassShippingValidation(SOShipment shipment)
        {
            if (shipment?.CustomerID == null)
                return false;

            Customer customer = Customer.PK.Find(Base, shipment.CustomerID);

            string customerCD = customer?.AcctCD?.Trim();

            if (string.IsNullOrWhiteSpace(customerCD))
                return false;

            return BypassCustomerCDs.Contains(customerCD);
        }
    }
}