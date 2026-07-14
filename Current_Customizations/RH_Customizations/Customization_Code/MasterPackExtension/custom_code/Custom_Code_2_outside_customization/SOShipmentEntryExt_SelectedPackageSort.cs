using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.SO;

using WmsShipmentExt = WMS.SOShipmentEntryExt;
using WmsPlan = WMS.SelectedPackageContents;

namespace CustomWMS
{
    public sealed class SelectedPackageContentsExt : PXCacheExtension<WmsPlan>
    {
        public static bool IsActive() => true;

        #region UsrCompletedSortOrder
        public abstract class usrCompletedSortOrder : PX.Data.BQL.BqlInt.Field<usrCompletedSortOrder> { }

        [PXInt]
        [PXUIField(DisplayName = "Completed Sort Order", Visible = false, Enabled = false)]
        public int? UsrCompletedSortOrder { get; set; }
        #endregion
          
        #region UsrActualPackedQty
        public abstract class usrActualPackedQty : PX.Data.BQL.BqlDecimal.Field<usrActualPackedQty> { }
        
        [PXDecimal]
        [PXUIField(DisplayName = "Scanned Qty", Enabled = false)]
        public decimal? UsrActualPackedQty { get; set; }
        #endregion
          
    }

    public class SOShipmentEntryExt_SelectedPackageSort
        : PXGraphExtension<WmsShipmentExt, SOShipmentEntry>
    {
        private const string TracePrefix = "[SelectedPackageSort]";

        public static bool IsActive() => true;

        public PXSelect<
            WmsPlan,
            Where<
                WmsPlan.shipmentNbr, Equal<Current<SOPackageDetailEx.shipmentNbr>>,
                And<WmsPlan.packageLineNbr, Equal<Current<SOPackageDetailEx.lineNbr>>>>,
            OrderBy<
                Asc<SelectedPackageContentsExt.usrCompletedSortOrder,
                Asc<WmsPlan.defaultIssueFrom,
                Asc<WmsPlan.orderNbr,
                Asc<WmsPlan.storeNbr,
                Asc<WmsPlan.inventoryID,
                Asc<WmsPlan.lotSerialNbr>>>>>>>
        > SelectedPackageContentsView;

        protected virtual IEnumerable selectedPackageContentsView()
        {
            PXTrace.WriteInformation($"{TracePrefix} VERSION 2026-05-27-HYBRID-LIVE-SORT");

            SOPackageDetailEx package = Base.Packages.Current;

            if (package == null || package.ShipmentNbr == null || package.LineNbr == null)
            {
                PXTrace.WriteInformation($"{TracePrefix} No valid current package. Returning no rows.");
                yield break;
            }

            List<WmsPlan> plannedRows =
                PXSelectReadonly<
                    WmsPlan,
                    Where<
                        WmsPlan.shipmentNbr, Equal<Required<WmsPlan.shipmentNbr>>,
                        And<WmsPlan.packageLineNbr, Equal<Required<WmsPlan.packageLineNbr>>>>>
                .Select(Base, package.ShipmentNbr, package.LineNbr)
                .RowCast<WmsPlan>()
                .ToList();

            List<SOShipLineSplitPackage> actualRows =
                PXSelectReadonly<
                    SOShipLineSplitPackage,
                    Where<
                        SOShipLineSplitPackage.shipmentNbr, Equal<Required<SOShipLineSplitPackage.shipmentNbr>>,
                        And<SOShipLineSplitPackage.packageLineNbr, Equal<Required<SOShipLineSplitPackage.packageLineNbr>>>>>
                .Select(Base, package.ShipmentNbr, package.LineNbr)
                .RowCast<SOShipLineSplitPackage>()
                .ToList();

            Dictionary<int?, decimal> actualQtyBySplit =
                actualRows
                    .GroupBy(x => x.ShipmentSplitLineNbr)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(x => x.PackedQty ?? 0m)
                    );

            var sortedRows =
                plannedRows
                    .Select(row =>
                    {
                        decimal expectedQty = row.PackedQty ?? 0m;
                        decimal actualQty = 0m;

                        actualQtyBySplit.TryGetValue(row.ShipmentSplitLineNbr, out actualQty);

                        bool completed = expectedQty > 0m && actualQty >= expectedQty;

                        SelectedPackageContentsExt rowExt = row.GetExtension<SelectedPackageContentsExt>();

                        rowExt.UsrActualPackedQty = actualQty;
                        rowExt.UsrCompletedSortOrder = completed ? 1 : 0;

                        if (completed)
                        {
                            PXTrace.WriteInformation(
                                $"{TracePrefix} Completed row moved down: RecordID={row.RecordID}, InventoryID={row.InventoryID}, SplitLineNbr={row.ShipmentSplitLineNbr}, Expected={expectedQty}, Actual={actualQty}");
                        }

                        return new
                        {
                            Row = row,
                            CompletedSortOrder = completed ? 1 : 0
                        };
                    })
                    .OrderBy(x => x.CompletedSortOrder)
                    .ThenBy(x => x.Row.DefaultIssueFrom)
                    .ThenBy(x => x.Row.OrderNbr)
                    .ThenBy(x => x.Row.StoreNbr)
                    .ThenBy(x => x.Row.InventoryID)
                    .ThenBy(x => x.Row.LotSerialNbr)
                    .ToList();

            PXTrace.WriteInformation(
                $"{TracePrefix} Live sort complete. Planned={plannedRows.Count}, Actual={actualRows.Count}, Returned={sortedRows.Count}");

            foreach (var item in sortedRows)
            {
                yield return item.Row;
            }
        }

        protected virtual void _(Events.RowInserted<SOShipLineSplitPackage> e)
        {
            RequestEstimatedContentRefresh("Packed row inserted");
        }

        protected virtual void _(Events.RowUpdated<SOShipLineSplitPackage> e)
        {
            RequestEstimatedContentRefresh("Packed row updated");
        }

        protected virtual void _(Events.RowDeleted<SOShipLineSplitPackage> e)
        {
            RequestEstimatedContentRefresh("Packed row deleted");
        }

        private void RequestEstimatedContentRefresh(string reason)
        {
            WmsShipmentExt wmsExt = Base.GetExtension<WmsShipmentExt>();

            if (wmsExt?.SelectedPackageContentsView == null)
            {
                PXTrace.WriteInformation($"{TracePrefix} Could not refresh: WMS extension/view not found. Reason={reason}");
                return;
            }

            wmsExt.SelectedPackageContentsView.Cache.Clear();
            wmsExt.SelectedPackageContentsView.View.Clear();
            wmsExt.SelectedPackageContentsView.View.RequestRefresh();

            PXTrace.WriteInformation($"{TracePrefix} Refresh requested. Reason={reason}");
        }
    }
}