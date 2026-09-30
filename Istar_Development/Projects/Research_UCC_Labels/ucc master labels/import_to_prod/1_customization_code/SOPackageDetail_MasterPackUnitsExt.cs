using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.SO;

using WmsPackageExt = WMS.SOPackageDetailExt;
using WmsPlan = WMS.SelectedPackageContents;

namespace CustomWMS
{
    /*
     * Master Pack Units
     *
     * Read-only quantity for a master pack carton: the total planned
     * quantity of the child cartons assigned to it.
     *
     * A child carton points to its master through
     * SOPackageDetailExt.UsrSelectedParentBox, which holds the master's
     * UsrCartonNbr. The planned contents of each carton are the
     * WMS.SelectedPackageContents rows for its package line.
     *
     * The value is calculated when package rows are read; nothing is
     * stored, so it cannot drift from the package assignments. Regular
     * (non-master) cartons leave the field empty and cost no query.
     *
     * Used by the Asgard master pack labels (Packages.UsrMasterPackUnits).
     */
    public sealed class SOPackageDetailEx_MasterPackUnitsExt
        : PXCacheExtension<SOPackageDetailEx>
    {
        public static bool IsActive()
        {
            return true;
        }

        #region UsrMasterPackUnits

        public abstract class usrMasterPackUnits
            : PX.Data.BQL.BqlDecimal.Field<usrMasterPackUnits>
        {
        }

        [PXDecimal(0)]
        [PXUIField(
            DisplayName = "Master Pack Units",
            Enabled = false)]
        public decimal? UsrMasterPackUnits { get; set; }

        #endregion
    }

    public class SOShipmentEntryExt_MasterPackUnits
        : PXGraphExtension<SOShipmentEntry>
    {
        public static bool IsActive()
        {
            return true;
        }

        /*
         * RowSelecting runs after the package's own fields (including the
         * WMS extension fields) are populated, so the carton number and
         * master flag are available here. Only master cartons query.
         */
        protected virtual void _(Events.RowSelecting<SOPackageDetailEx> e)
        {
            SOPackageDetailEx row = e.Row;

            if (row == null)
            {
                return;
            }

            WmsPackageExt wmsExt = row.GetExtension<WmsPackageExt>();

            string cartonNbr = wmsExt?.UsrCartonNbr?.Trim();

            if (wmsExt?.UsrIsParentBox != true
                || string.IsNullOrEmpty(cartonNbr)
                || string.IsNullOrEmpty(row.ShipmentNbr))
            {
                return;
            }

            decimal units;

            using (new PXConnectionScope())
            {
                units = GetMasterPackUnits(row.ShipmentNbr, cartonNbr);
            }

            e.Cache.SetValue<SOPackageDetailEx_MasterPackUnitsExt.usrMasterPackUnits>(
                row,
                units);
        }

        /*
         * Sum of the child cartons' planned contents, in base units.
         */
        private decimal GetMasterPackUnits(
            string shipmentNbr,
            string masterCartonNbr)
        {
            WmsPlan total =
                SelectFrom<WmsPlan>
                    .InnerJoin<SOPackageDetail>
                        .On<SOPackageDetail.shipmentNbr.IsEqual<WmsPlan.shipmentNbr>
                            .And<SOPackageDetail.lineNbr.IsEqual<WmsPlan.packageLineNbr>>>
                    .Where<WmsPlan.shipmentNbr.IsEqual<@P.AsString>
                        .And<WmsPackageExt.usrSelectedParentBox.IsEqual<@P.AsString>>>
                    .AggregateTo<Sum<WmsPlan.basePackedQty>>
                    .View.ReadOnly
                    .Select(Base, shipmentNbr, masterCartonNbr);

            return total?.BasePackedQty ?? 0m;
        }
    }
}
