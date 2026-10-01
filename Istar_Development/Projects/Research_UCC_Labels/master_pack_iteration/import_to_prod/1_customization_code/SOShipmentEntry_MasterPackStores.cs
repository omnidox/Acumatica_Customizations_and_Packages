using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.SO;

using WmsPackageExt = WMS.SOPackageDetailExt;
using WmsPlan = WMS.SelectedPackageContents;

namespace CustomWMS
{
    /*
     * Master Pack Stores
     *
     * One read-only row per (master carton, store) on the current shipment:
     * the distinct stores of the child cartons assigned to each master
     * carton.
     *
     * A child carton points to its master through
     * SOPackageDetailExt.UsrSelectedParentBox, which holds the master's
     * UsrCartonNbr. The store comes from the child's planned contents
     * (WMS.SelectedPackageContents.StoreNbr); a child carton without a
     * stored content store falls back to its own UsrStoreNbr.
     *
     * Nothing is stored; the rows are rebuilt from the package assignments
     * every time the view is read.
     *
     * Used by the Asgard iterator sub-model
     * iStar-5A-MasterPackingCartonLabel-Iterator, whose print rule keeps
     * only the rows of the master carton being printed:
     *     MasterPackStores.MasterCartonNbr == Packages.UsrCartonNbr
     * This is a separate DAC (not SOPackageDetailEx) on purpose: Asgard
     * picks the Packages row by matching result sets of the same type, so
     * a package-typed list here would make Packages point at the wrong
     * carton while the rule runs.
     */
    [PXHidden]
    public class MasterPackStore : PXBqlTable, IBqlTable
    {
        #region MasterCartonNbr

        public abstract class masterCartonNbr
            : PX.Data.BQL.BqlString.Field<masterCartonNbr>
        {
        }

        [PXString(30, IsUnicode = true, IsKey = true)]
        [PXUIField(DisplayName = "Master Carton Nbr.", Enabled = false)]
        public virtual string MasterCartonNbr { get; set; }

        #endregion

        #region StoreNbr

        public abstract class storeNbr
            : PX.Data.BQL.BqlString.Field<storeNbr>
        {
        }

        [PXString(30, IsUnicode = true, IsKey = true)]
        [PXUIField(DisplayName = "Store Nbr.", Enabled = false)]
        public virtual string StoreNbr { get; set; }

        #endregion
    }

    public class SOShipmentEntryExt_MasterPackStores
        : PXGraphExtension<SOShipmentEntry>
    {
        public static bool IsActive()
        {
            return true;
        }

        [PXVirtualDAC]
        [PXCopyPasteHiddenView]
        public SelectFrom<MasterPackStore>.View.ReadOnly MasterPackStores;

        /*
         * Sorted by master carton, then store number.
         */
        protected virtual IEnumerable masterPackStores()
        {
            string shipmentNbr = Base.Document.Current?.ShipmentNbr;

            if (string.IsNullOrEmpty(shipmentNbr))
            {
                return Enumerable.Empty<MasterPackStore>();
            }

            var pairs = new SortedSet<(string Master, string Store)>();

            foreach (PXResult<SOPackageDetail, WmsPlan> result in
                SelectFrom<SOPackageDetail>
                    .LeftJoin<WmsPlan>
                        .On<WmsPlan.shipmentNbr.IsEqual<SOPackageDetail.shipmentNbr>
                            .And<WmsPlan.packageLineNbr.IsEqual<SOPackageDetail.lineNbr>>>
                    .Where<SOPackageDetail.shipmentNbr.IsEqual<@P.AsString>
                        .And<WmsPackageExt.usrSelectedParentBox.IsNotNull>>
                    .View.ReadOnly
                    .Select(Base, shipmentNbr))
            {
                SOPackageDetail child = result;
                WmsPlan plan = result;

                WmsPackageExt wmsExt = child.GetExtension<WmsPackageExt>();

                string master = wmsExt?.UsrSelectedParentBox?.Trim();

                string store = plan?.StoreNbr?.Trim();

                if (string.IsNullOrEmpty(store))
                {
                    store = wmsExt?.UsrStoreNbr?.Trim();
                }

                if (string.IsNullOrEmpty(master) || string.IsNullOrEmpty(store))
                {
                    continue;
                }

                pairs.Add((master, store));
            }

            return pairs
                .Select(pair => new MasterPackStore
                {
                    MasterCartonNbr = pair.Master,
                    StoreNbr = pair.Store
                })
                .ToList();
        }
    }
}
