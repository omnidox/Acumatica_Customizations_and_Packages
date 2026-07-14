using System;
using System.Collections;
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
        private const string TracePrefix = "[SelectedPackageSkipState]";
        private const string UsrSkippedStatusField = "UsrSkippedStatus";

        private const string SkippedText = "Skipped";
        private const string ActiveText = "Active";

        public static bool IsSkipped(PXGraph graph, string shipmentNbr, int? packageLineNbr, int? shipmentSplitLineNbr)
        {
            if (graph == null || string.IsNullOrEmpty(shipmentNbr) || packageLineNbr == null || shipmentSplitLineNbr == null)
                return false;

            WmsPlan row =
                PXSelectReadonly<
                    WmsPlan,
                    Where<
                        WmsPlan.shipmentNbr, Equal<Required<WmsPlan.shipmentNbr>>,
                        And<WmsPlan.packageLineNbr, Equal<Required<WmsPlan.packageLineNbr>>,
                        And<WmsPlan.shipmentSplitLineNbr, Equal<Required<WmsPlan.shipmentSplitLineNbr>>>>>>
                .Select(graph, shipmentNbr, packageLineNbr, shipmentSplitLineNbr)
                .RowCast<WmsPlan>()
                .FirstOrDefault();

            if (row == null)
                return false;

            PXCache cache = graph.Caches<WmsPlan>();

            if (!HasField(cache, UsrSkippedStatusField))
            {
                PXTrace.WriteWarning($"{TracePrefix} Field not found in cache: {UsrSkippedStatusField}");
                return false;
            }

            string status = cache.GetValue(row, UsrSkippedStatusField) as string;

            return string.Equals(status, SkippedText, StringComparison.OrdinalIgnoreCase);
        }

        public static void Skip(PXGraph graph, string shipmentNbr, int? packageLineNbr, int? shipmentSplitLineNbr)
        {
            if (graph == null || string.IsNullOrEmpty(shipmentNbr) || packageLineNbr == null || shipmentSplitLineNbr == null)
                return;

            PXDatabase.Update<WmsPlan>(
                new PXDataFieldAssign(UsrSkippedStatusField, SkippedText),
                new PXDataFieldRestrict(nameof(WmsPlan.ShipmentNbr), shipmentNbr),
                new PXDataFieldRestrict(nameof(WmsPlan.PackageLineNbr), packageLineNbr),
                new PXDataFieldRestrict(nameof(WmsPlan.ShipmentSplitLineNbr), shipmentSplitLineNbr)
            );

            PXTrace.WriteInformation(
                $"{TracePrefix} DB skip saved. ShipmentNbr={shipmentNbr}, PackageLineNbr={packageLineNbr}, ShipmentSplitLineNbr={shipmentSplitLineNbr}");
        }

        public static void ClearPackage(PXGraph graph, string shipmentNbr, int? packageLineNbr)
        {
            if (graph == null || string.IsNullOrEmpty(shipmentNbr) || packageLineNbr == null)
                return;

            PXDatabase.Update<WmsPlan>(
                new PXDataFieldAssign(UsrSkippedStatusField, ActiveText),
                new PXDataFieldRestrict(nameof(WmsPlan.ShipmentNbr), shipmentNbr),
                new PXDataFieldRestrict(nameof(WmsPlan.PackageLineNbr), packageLineNbr)
            );

            PXTrace.WriteInformation(
                $"{TracePrefix} DB skipped rows cleared. ShipmentNbr={shipmentNbr}, PackageLineNbr={packageLineNbr}");
        }

        private static bool HasField(PXCache cache, string fieldName)
        {
            return cache != null &&
                   cache.Fields.Any(f => string.Equals(f, fieldName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class SOShipmentEntryExt_SelectedPackageSort
        : PXGraphExtension<WmsShipmentExt, SOShipmentEntry>
    {
        private const string TracePrefix = "[SelectedPackageSort]";
        private const string Version = "2026-06-18-DB-SKIPPED-STATUS-01";

        private const string UsrRemainingQtyField = "UsrRemainingQty";
        private const string UsrSkippedStatusField = "UsrSkippedStatus";
        private const string UsrCompletedSortOrderField = "UsrCompletedSortOrder";
        private const string UsrSkipSortOrderField = "UsrSkipSortOrder";

        public static bool IsActive() => true;

        public PXSelect<
            WmsPlan,
            Where<
                WmsPlan.shipmentNbr, Equal<Current<SOPackageDetailEx.shipmentNbr>>,
                And<WmsPlan.packageLineNbr, Equal<Current<SOPackageDetailEx.lineNbr>>>>
        > SelectedPackageContentsView;

        protected virtual IEnumerable selectedPackageContentsView()
        {
            PXTrace.WriteInformation($"{TracePrefix} VERSION {Version}");

            SOPackageDetailEx package = Base.Packages.Current;

            if (package == null || package.ShipmentNbr == null || package.LineNbr == null)
            {
                PXTrace.WriteInformation($"{TracePrefix} No valid current package. Returning no rows.");
                yield break;
            }

            if (ShouldBypassTopRowWorkflow())
            {
                List<RowCalc> allRows = GetCalculatedRows(package)
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

            PXTrace.WriteInformation(
                $"{TracePrefix} Live incomplete-only calculated values complete. Returned={sortedRows.Count}");

            foreach (RowCalc item in sortedRows)
            {
                ApplyCalculatedValues(item);
                yield return item.Row;
            }
        }

        private void ApplyCalculatedValues(RowCalc item)
        {
            PXCache cache = Base.Caches<WmsPlan>();

            SetValueSafe(cache, item.Row, UsrRemainingQtyField, item.RemainingQty);
            SetValueSafe(cache, item.Row, UsrSkippedStatusField, GetSkipStatusText(item.SkipSortOrder));
            SetValueSafe(cache, item.Row, UsrCompletedSortOrderField, item.CompletedSortOrder);
            SetValueSafe(cache, item.Row, UsrSkipSortOrderField, item.SkipSortOrder);
        }

        private void SetValueSafe(PXCache cache, WmsPlan row, string fieldName, object value)
        {
            if (cache == null || row == null || string.IsNullOrEmpty(fieldName))
                return;

            if (!cache.Fields.Any(f => string.Equals(f, fieldName, StringComparison.OrdinalIgnoreCase)))
            {
                PXTrace.WriteWarning($"{TracePrefix} Custom field not found in cache: {fieldName}");
                return;
            }

            cache.SetValue(row, fieldName, value);
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

                result.Add(new RowCalc
                {
                    Row = row,
                    RealShipmentSplitLineNbr = realShipmentSplitLineNbr,
                    RemainingQty = remainingQty,
                    CompletedSortOrder = completed ? 1 : 0,
                    SkipSortOrder = skipped ? 1 : 0
                });

                PXTrace.WriteInformation(
                    $"{TracePrefix} Row calculated: RecordID={row.RecordID}, InventoryID={row.InventoryID}, RealSplitLineNbr={realShipmentSplitLineNbr}, Expected={expectedQty}, Actual={actualQty}, Remaining={remainingQty}, Completed={completed}, Skipped={skipped}, SkipStatus={GetSkipStatusText(skipped ? 1 : 0)}");
            }

            PXTrace.WriteInformation(
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
                PXTrace.WriteInformation($"{TracePrefix} Could not refresh: WMS extension/view not found. Reason={reason}");
                return;
            }

            wmsExt.SelectedPackageContentsView.Cache.Clear();
            wmsExt.SelectedPackageContentsView.View.Clear();
            wmsExt.SelectedPackageContentsView.View.RequestRefresh();

            PXTrace.WriteInformation($"{TracePrefix} Refresh requested. Reason={reason}");
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