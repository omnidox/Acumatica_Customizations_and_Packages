using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.IN;

using WmsShipmentExt = WMS.SOShipmentEntryExt;
using WmsPlan = WMS.SelectedPackageContents;

namespace CustomWMS
{
    public static class SelectedPackageSkipState
    {
        private static readonly ConcurrentDictionary<string, bool> SkippedRows =
            new ConcurrentDictionary<string, bool>();

        public static bool IsSkipped(PXGraph graph, string shipmentNbr, int? packageLineNbr, int? shipmentSplitLineNbr)
        {
            if (string.IsNullOrEmpty(shipmentNbr) || packageLineNbr == null || shipmentSplitLineNbr == null)
                return false;

            return SkippedRows.ContainsKey(BuildKey(graph, shipmentNbr, packageLineNbr, shipmentSplitLineNbr));
        }

        public static void Skip(PXGraph graph, string shipmentNbr, int? packageLineNbr, int? shipmentSplitLineNbr)
        {
            if (string.IsNullOrEmpty(shipmentNbr) || packageLineNbr == null || shipmentSplitLineNbr == null)
                return;

            SkippedRows[BuildKey(graph, shipmentNbr, packageLineNbr, shipmentSplitLineNbr)] = true;
        }

        public static void ClearPackage(PXGraph graph, string shipmentNbr, int? packageLineNbr)
        {
            if (string.IsNullOrEmpty(shipmentNbr) || packageLineNbr == null)
                return;

            string prefix = BuildPackagePrefix(graph, shipmentNbr, packageLineNbr);

            foreach (string key in SkippedRows.Keys)
            {
                if (key.StartsWith(prefix))
                    SkippedRows.TryRemove(key, out _);
            }
        }

        private static string BuildKey(PXGraph graph, string shipmentNbr, int? packageLineNbr, int? shipmentSplitLineNbr)
        {
            return $"{BuildPackagePrefix(graph, shipmentNbr, packageLineNbr)}|Split={shipmentSplitLineNbr}";
        }

        private static string BuildPackagePrefix(PXGraph graph, string shipmentNbr, int? packageLineNbr)
        {
            string company = graph?.Accessinfo?.CompanyName ?? string.Empty;
            string user = graph?.Accessinfo?.UserName ?? string.Empty;

            return $"Company={company}|User={user}|Shipment={shipmentNbr}|Package={packageLineNbr}";
        }
    }

    public sealed class SelectedPackageContentsExt : PXCacheExtension<WmsPlan>
    {
        public static bool IsActive() => true;

        #region RemainingQty
        public abstract class remainingQty : PX.Data.BQL.BqlDecimal.Field<remainingQty> { }

        [PXDecimal]
        [PXUIField(DisplayName = "Remaining Qty", Enabled = false)]
        public decimal? RemainingQty { get; set; }
        #endregion

        #region SkippedStatus
        public abstract class skippedStatus : PX.Data.BQL.BqlBool.Field<skippedStatus> { }

        [PXBool]
        [PXUIField(DisplayName = "Skipped Status", Enabled = false)]
        public bool? SkippedStatus { get; set; }
        #endregion

        #region UsrCompletedSortOrder
        public abstract class usrCompletedSortOrder : PX.Data.BQL.BqlInt.Field<usrCompletedSortOrder> { }

        [PXInt]
        [PXUIField(DisplayName = "Completed Sort Order", Visible = false, Enabled = false)]
        public int? UsrCompletedSortOrder { get; set; }
        #endregion

        #region UsrSkipSortOrder
        public abstract class usrSkipSortOrder : PX.Data.BQL.BqlInt.Field<usrSkipSortOrder> { }

        [PXInt]
        [PXUIField(DisplayName = "Skip Sort Order", Visible = false, Enabled = false)]
        public int? UsrSkipSortOrder { get; set; }
        #endregion
    }

    public class SOShipmentEntryExt_SelectedPackageSort
        : PXGraphExtension<WmsShipmentExt, SOShipmentEntry>
    {
        private const string TracePrefix = "[SelectedPackageSort]";
        private const string Version = "2026-06-26-CACHE-SETVALUEEXT-REMAINING-QTY-SKIPPED-STATUS-01";

        public static bool IsActive() => true;

        public PXSelect<
            WmsPlan,
            Where<
                WmsPlan.shipmentNbr, Equal<Current<SOPackageDetailEx.shipmentNbr>>,
                And<WmsPlan.packageLineNbr, Equal<Current<SOPackageDetailEx.lineNbr>>>>,
            OrderBy<
                Asc<SelectedPackageContentsExt.usrCompletedSortOrder,
                Asc<SelectedPackageContentsExt.usrSkipSortOrder,
                Asc<WmsPlan.defaultIssueFrom,
                Asc<WmsPlan.orderNbr,
                Asc<WmsPlan.storeNbr,
                Asc<WmsPlan.inventoryID,
                Asc<WmsPlan.lotSerialNbr>>>>>>>>
        > SelectedPackageContentsView;

        protected virtual IEnumerable selectedPackageContentsView()
        {
            WmsDebugTrace.Info($"{TracePrefix} VERSION {Version}");

            SOPackageDetailEx package = Base.Packages.Current;

            if (package == null || package.ShipmentNbr == null || package.LineNbr == null)
            {
                WmsDebugTrace.Info($"{TracePrefix} No valid current package. Returning no rows.");
                yield break;
            }

            if (ShouldBypassTopRowWorkflow())
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Top-row workflow bypassed. Returning all rows with RemainingQty and SkippedStatus.");

              
                List<RowCalc> allRows = GetCalculatedRows(package)
                    .Where(x => x.RemainingQty > 0m)
                    .OrderBy(x => x.Row.DefaultIssueFrom)
                    .ThenBy(x => x.Row.OrderNbr)
                    .ThenBy(x => x.Row.StoreNbr)
                    .ThenBy(x => GetInventoryCD(x.Row.InventoryID))
                    .ThenBy(x => x.Row.LotSerialNbr)
                    .ToList();

                foreach (RowCalc item in allRows)
                {
                    ApplyCalculatedValues(item);
                    yield return item.Row;
                }

                yield break;
            }

            List<RowCalc> sortedRows = GetCalculatedRows(package)
                .Where(x => x.CompletedSortOrder == 0)
                .OrderBy(x => x.SkipSortOrder)
                .ThenBy(x => x.Row.DefaultIssueFrom)
                .ThenBy(x => x.Row.OrderNbr)
                .ThenBy(x => x.Row.StoreNbr)
                .ThenBy(x => GetInventoryCD(x.Row.InventoryID))
                .ThenBy(x => x.Row.LotSerialNbr)
                .ToList();

            WmsDebugTrace.Info(
                $"{TracePrefix} Live incomplete-only RemainingQty and SkippedStatus complete. Returned={sortedRows.Count}");

            foreach (RowCalc item in sortedRows)
            {
                ApplyCalculatedValues(item);
                yield return item.Row;
            }
        }

        private void ApplyCalculatedValues(RowCalc item)
        {
            if (item?.Row == null)
                return;

            PXCache cache = Base.Caches<WmsPlan>();

            cache.SetValueExt<SelectedPackageContentsExt.remainingQty>(
                item.Row,
                item.RemainingQty);

            cache.SetValueExt<SelectedPackageContentsExt.skippedStatus>(
                item.Row,
                item.SkipSortOrder == 1);

            cache.SetValueExt<SelectedPackageContentsExt.usrCompletedSortOrder>(
                item.Row,
                item.CompletedSortOrder);

            cache.SetValueExt<SelectedPackageContentsExt.usrSkipSortOrder>(
                item.Row,
                item.SkipSortOrder);

            WmsDebugTrace.Info(
                $"{TracePrefix} Applied values: RecordID={item.Row.RecordID}, RemainingQty={item.RemainingQty}, SkippedStatus={item.SkipSortOrder == 1}");
        }

        private bool ShouldBypassTopRowWorkflow()
        {
            SOShipment shipment = Base.Document.Current;

            return CustomerWorkflowBypass.ShouldBypassTopRowWorkflow(Base, shipment);
        }

        private string GetSkipStatusText(int skipSortOrder)
        {
            return skipSortOrder == 1 ? "Skipped" : "Active";
        }

        private List<RowCalc> GetCalculatedRows(SOPackageDetailEx package)
        {
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

            List<RowCalc> result = new List<RowCalc>();

            foreach (WmsPlan row in plannedRows)
            {
                int? realShipmentSplitLineNbr = row.ShipmentSplitLineNbr;

                decimal expectedQty = row.PackedQty ?? 0m;
                decimal actualQty = 0m;

                actualQtyBySplit.TryGetValue(realShipmentSplitLineNbr, out actualQty);

                decimal remainingQty = expectedQty - actualQty;
                if (remainingQty < 0m)
                    remainingQty = 0m;

                bool completed = expectedQty > 0m && actualQty >= expectedQty;

                bool skipped = SelectedPackageSkipState.IsSkipped(
                    Base,
                    row.ShipmentNbr,
                    row.PackageLineNbr,
                    realShipmentSplitLineNbr);

                WmsDebugTrace.Info(
                    $"{TracePrefix} Row calculated: RecordID={row.RecordID}, InventoryID={row.InventoryID}, RealSplitLineNbr={realShipmentSplitLineNbr}, Expected={expectedQty}, Actual={actualQty}, Remaining={remainingQty}, Completed={completed}, Skipped={skipped}, SkipStatus={GetSkipStatusText(skipped ? 1 : 0)}");

                result.Add(new RowCalc
                {
                    Row = row,
                    RealShipmentSplitLineNbr = realShipmentSplitLineNbr,
                    RemainingQty = remainingQty,
                    CompletedSortOrder = completed ? 1 : 0,
                    SkipSortOrder = skipped ? 1 : 0
                });
            }

            WmsDebugTrace.Info(
                $"{TracePrefix} Calculated rows complete. Planned={plannedRows.Count}, Actual={actualRows.Count}, Calculated={result.Count}");

            return result;
        }

        private string GetInventoryCD(int? inventoryID)
        {
            if (inventoryID == null)
                return string.Empty;

            InventoryItem item =
                PXSelectReadonly<
                    InventoryItem,
                    Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>
                .Select(Base, inventoryID)
                .RowCast<InventoryItem>()
                .FirstOrDefault();

            return item?.InventoryCD?.Trim() ?? string.Empty;
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
                WmsDebugTrace.Info($"{TracePrefix} Could not refresh: WMS extension/view not found. Reason={reason}");
                return;
            }

            wmsExt.SelectedPackageContentsView.Cache.Clear();
            wmsExt.SelectedPackageContentsView.View.Clear();
            wmsExt.SelectedPackageContentsView.View.RequestRefresh();

            WmsDebugTrace.Info($"{TracePrefix} Refresh requested. Reason={reason}");
        }

        private sealed class RowCalc
        {
            public WmsPlan Row { get; set; }
            public int? RealShipmentSplitLineNbr { get; set; }
            public decimal RemainingQty { get; set; }
            public int CompletedSortOrder { get; set; }
            public int SkipSortOrder { get; set; }
        }
    }
}