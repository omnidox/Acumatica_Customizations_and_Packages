using System;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.SO;
using PX.Objects.SO.WMS;

namespace IStar.ScanPerformance
{
    /// <summary>
    /// Preserves the standard SOShipment_RowSelected behavior on the
    /// PickPackShip host while replacing the full Transactions view load used
    /// only to test for an empty shipment with a TOP 1 existence query.
    /// </summary>
    public class PickPackShipShipmentRowSelectedOptimization
        : PXGraphExtension<PickPackShip.Host>
    {
        public static bool IsActive()
        {
            return true;
        }

        public delegate void SOShipmentRowSelectedDelegate(
            PXCache sender,
            PXRowSelectedEventArgs e);

        [PXOverride]
        public virtual void SOShipment_RowSelected(
            PXCache sender,
            PXRowSelectedEventArgs e,
            SOShipmentRowSelectedDelegate baseMethod)
        {
            // Do not call baseMethod here. The standard implementation loads
            // every SOShipLine through Transactions.Select() merely to test
            // whether at least one line exists. The remaining standard logic
            // is reproduced below.
            if (e.Row == null)
            {
                return;
            }

            SOShipment shipment = (SOShipment)e.Row;
            bool isTransfer = shipment.ShipmentType == "T";
            bool isNotConfirmed = shipment.Confirmed == false;
            bool hasNoWorksheet = shipment.CurrentWorksheetNbr == null;
            bool canEditPackages = hasNoWorksheet || shipment.Picked.GetValueOrDefault();
            bool allowUpdate = isNotConfirmed;

            PXUIFieldAttribute.SetVisible<SOShipment.curyID>(
                sender,
                e.Row,
                PXAccess.FeatureInstalled<FeaturesSet.multicurrency>() && !isTransfer);

            PXUIFieldAttribute.SetEnabled<SOShipment.curyID>(
                sender,
                e.Row,
                allowUpdate);

            PXUIFieldAttribute.SetEnabled<SOShipment.curyFreightAmt>(
                sender,
                e.Row,
                allowUpdate && shipment.OverrideFreightAmount.GetValueOrDefault());

            PXUIFieldAttribute.SetEnabled<SOShipment.overrideFreightAmount>(
                sender,
                e.Row,
                AllowChangingOverrideFreightAmount(shipment));

            sender.AllowInsert = true;
            sender.AllowUpdate = isNotConfirmed;
            sender.AllowDelete = allowUpdate && hasNoWorksheet;

            Base.Transactions.Cache.AllowInsert = false;
            Base.Transactions.Cache.AllowUpdate = allowUpdate && hasNoWorksheet;
            Base.Transactions.Cache.AllowDelete = allowUpdate && hasNoWorksheet;

            Base.splits.Cache.AllowInsert = allowUpdate && hasNoWorksheet;
            Base.splits.Cache.AllowUpdate = allowUpdate && hasNoWorksheet;
            Base.splits.Cache.AllowDelete = allowUpdate && hasNoWorksheet;

            Base.Packages.Cache.AllowInsert = isNotConfirmed && canEditPackages;
            Base.Packages.Cache.AllowUpdate = isNotConfirmed && canEditPackages;
            Base.Packages.Cache.AllowDelete = isNotConfirmed && canEditPackages;

            PXUIFieldAttribute.SetVisible<SOShipment.controlQty>(
                sender,
                e.Row,
                Base.sosetup.Current.RequireShipmentTotal.Value);

            bool shipmentHasNoLines =
                PXSelect<
                    SOShipLine,
                    Where<
                        SOShipLine.shipmentNbr,
                        Equal<Required<SOShipLine.shipmentNbr>>>>
                .SelectWindowed(Base, 0, 1, shipment.ShipmentNbr)
                .TopFirst == null;

            bool canEditShipmentKeys = sender.AllowUpdate && shipmentHasNoLines;

            PXUIFieldAttribute.SetEnabled<SOShipment.shipmentType>(
                sender,
                e.Row,
                canEditShipmentKeys && sender.GetStatus(e.Row) == PXEntryStatus.Inserted);

            PXUIFieldAttribute.SetEnabled<SOShipment.operation>(
                sender,
                e.Row,
                canEditShipmentKeys);

            PXUIFieldAttribute.SetEnabled<SOShipment.customerID>(
                sender,
                e.Row,
                canEditShipmentKeys);

            PXUIFieldAttribute.SetEnabled<SOShipment.customerLocationID>(
                sender,
                e.Row,
                canEditShipmentKeys);

            PXUIFieldAttribute.SetEnabled<SOShipment.siteID>(
                sender,
                e.Row,
                canEditShipmentKeys);

            PXUIFieldAttribute.SetEnabled<SOShipment.destinationSiteID>(
                sender,
                e.Row,
                canEditShipmentKeys && isTransfer);

            Base.validateAddresses.SetEnabled(
                allowUpdate &&
                Base.FindAllImplementations<IAddressValidationHelper>()
                    .RequiresValidation());

            if (shipment.ShipVia != null)
            {
                Carrier carrier = Carrier.PK.Find(
                    Base,
                    shipment.ShipVia,
                    PKFindOptions.None);

                if (carrier != null)
                {
                    PXUIFieldAttribute.SetEnabled<SOShipment.curyFreightCost>(
                        sender,
                        e.Row,
                        carrier.CalcMethod == "M" && allowUpdate);
                }

                string freightCostError =
                    PXUIFieldAttribute.GetErrorOnly<SOShipment.curyFreightCost>(
                        sender,
                        shipment);

                if (carrier != null &&
                    carrier.IsExternal.GetValueOrDefault() &&
                    string.IsNullOrEmpty(freightCostError))
                {
                    PXUIFieldAttribute.SetWarning<SOShipment.curyFreightCost>(
                        sender,
                        e.Row,
                        shipment.FreightCostIsValid == false && allowUpdate
                            ? "The freight cost is not up to date."
                            : null);
                }
            }

            PXUIFieldAttribute.SetVisible<SOShipment.groundCollect>(
                sender,
                e.Row,
                CanUseGroundCollect(shipment));

            PXUIFieldAttribute.SetVisible<SOShipment.customerID>(
                sender,
                e.Row,
                !isTransfer);

            PXUIFieldAttribute.SetVisible<SOShipment.customerLocationID>(
                sender,
                e.Row,
                !isTransfer);

            PXUIFieldAttribute.SetVisible<SOShipment.destinationSiteID>(
                sender,
                e.Row,
                isTransfer);

            PXUIFieldAttribute.SetVisible<SOShipLine.isFree>(
                Base.Transactions.Cache,
                null,
                !isTransfer);

            PXUIFieldAttribute.SetRequired<SOShipment.destinationSiteID>(
                sender,
                true);

            bool shipmentFreightAmount =
                shipment.FreightAmountSource.IsIn(null, "S");

            PXUIFieldAttribute.SetVisible<SOShipment.curyFreightAmt>(
                sender,
                e.Row,
                shipmentFreightAmount);

            PXUIFieldAttribute.SetVisible<SOShipment.overrideFreightAmount>(
                sender,
                e.Row,
                shipmentFreightAmount);
        }

        private static bool AllowChangingOverrideFreightAmount(
            SOShipment shipment)
        {
            return shipment.Confirmed == false &&
                shipment.FreightAmountSource.IsIn(null, "S");
        }

        private bool CanUseGroundCollect(SOShipment shipment)
        {
            if (string.IsNullOrEmpty(shipment.ShipVia))
            {
                return false;
            }

            Carrier carrier = Carrier.PK.Find(
                Base,
                shipment.ShipVia,
                PKFindOptions.None);

            return carrier != null &&
                carrier.IsExternal.GetValueOrDefault() &&
                !string.IsNullOrEmpty(carrier.CarrierPluginID) &&
                CarrierPluginMaint.GetCarrierPluginAttributes(
                    Base,
                    carrier.CarrierPluginID)
                .Contains("COLLECT");
        }
    }
}
