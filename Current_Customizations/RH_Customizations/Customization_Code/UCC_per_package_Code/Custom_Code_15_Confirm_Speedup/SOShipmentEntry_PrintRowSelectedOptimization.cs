using PX.Common;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.SO;

namespace AA.Objects.AL.Integration.PerPackage
{
    /// <summary>
    /// ========================================================================
    /// PRINT-ONLY SOShipment_RowSelected OPTIMIZATION
    /// ========================================================================
    ///
    /// Every Asgard label context creates a fresh SOShipmentEntry graph and sets
    /// the shipment as current, which raises SOShipment_RowSelected. The standard
    /// handler loads every SOShipLine through Transactions.Select() only to test
    /// whether the shipment has at least one line (1,808 rows on the profiled
    /// shipment, about 60 ms of SQL each time).
    ///
    /// While ALPackagesFilterScope is active (label model resolution and label
    /// printing only), the handler is replaced with the same logic using a TOP 1
    /// existence query. Everywhere else, including the Shipments screen, the
    /// standard handler runs unchanged.
    ///
    /// The replacement body is the same as
    /// IStarScanPerformance PickPackShipShipmentRowSelectedOptimization, which
    /// applies it to PickPackShip.Host. Keep the two in sync after upgrades.
    /// </summary>
    public class SOShipmentEntry_PrintRowSelectedOptimization
        : PXGraphExtension<SOShipmentEntry>
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
            if (!ALPackagesFilterScope.IsActive)
            {
                baseMethod(sender, e);
                return;
            }

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
