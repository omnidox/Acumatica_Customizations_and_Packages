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
         *
         * Deliberately summed in C#, not via .AggregateTo<Sum<>>. Acumatica's
         * contract-API export optimizer (EnableExportOptimization in
         * Web.config) tracks RowSelecting-computed fields and tries to fold
         * their underlying query into its own already-aggregate-based export
         * SQL. A SUM() here collided with that, producing "Cannot perform an
         * aggregate function on an expression containing an aggregate or a
         * subquery" for any contract-API read of a master-carton package
         * (e.g. rs_carton_packer's completeCarton, GET .../Shipment?$expand=
         * Packages). Fetching the raw rows and summing them here instead
         * keeps this query free of SQL-level aggregates, so there is nothing
         * for the optimizer to nest illegally — without touching
         * EnableExportOptimization at all. Same numeric result, same single
         * round trip; only the aggregation moved from SQL to C#. Mirrors the
         * pattern SOShipmentEntry_MasterPackStores.cs already uses (raw rows,
         * summarized in C#) rather than introducing a new one.
         */
        private decimal GetMasterPackUnits(
            string shipmentNbr,
            string masterCartonNbr)
        {
            PXResultset<WmsPlan> rows =
                SelectFrom<WmsPlan>
                    .InnerJoin<SOPackageDetail>
                        .On<SOPackageDetail.shipmentNbr.IsEqual<WmsPlan.shipmentNbr>
                            .And<SOPackageDetail.lineNbr.IsEqual<WmsPlan.packageLineNbr>>>
                    .Where<WmsPlan.shipmentNbr.IsEqual<@P.AsString>
                        .And<WmsPackageExt.usrSelectedParentBox.IsEqual<@P.AsString>>>
                    .View.ReadOnly
                    .Select(Base, shipmentNbr, masterCartonNbr);

            decimal total = 0m;

            foreach (WmsPlan row in rows)
            {
                total += row.BasePackedQty ?? 0m;
            }

            return total;
        }
    }
}
