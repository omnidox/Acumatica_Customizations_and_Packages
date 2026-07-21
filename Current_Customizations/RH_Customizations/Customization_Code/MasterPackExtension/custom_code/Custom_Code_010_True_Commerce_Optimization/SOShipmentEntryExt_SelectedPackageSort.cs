using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;

using WmsPlan = WMS.SelectedPackageContents;

namespace CustomWMS
{
    public static class SelectedPackageSkipState
    {
        private static readonly ConcurrentDictionary<string, bool>
            SkippedRows =
                new ConcurrentDictionary<string, bool>();

        public static bool IsSkipped(
            PXGraph graph,
            string shipmentNbr,
            int? packageLineNbr,
            int? shipmentSplitLineNbr)
        {
            if (string.IsNullOrEmpty(shipmentNbr) ||
                packageLineNbr == null ||
                shipmentSplitLineNbr == null)
            {
                return false;
            }

            return SkippedRows.ContainsKey(
                BuildKey(
                    graph,
                    shipmentNbr,
                    packageLineNbr,
                    shipmentSplitLineNbr));
        }

        public static void Skip(
            PXGraph graph,
            string shipmentNbr,
            int? packageLineNbr,
            int? shipmentSplitLineNbr)
        {
            if (string.IsNullOrEmpty(shipmentNbr) ||
                packageLineNbr == null ||
                shipmentSplitLineNbr == null)
            {
                return;
            }

            string key =
                BuildKey(
                    graph,
                    shipmentNbr,
                    packageLineNbr,
                    shipmentSplitLineNbr);

            SkippedRows[key] = true;
        }

        public static void ClearPackage(
            PXGraph graph,
            string shipmentNbr,
            int? packageLineNbr)
        {
            if (string.IsNullOrEmpty(shipmentNbr) ||
                packageLineNbr == null)
            {
                return;
            }

            string prefix =
                BuildPackagePrefix(
                    graph,
                    shipmentNbr,
                    packageLineNbr);

            foreach (string key in SkippedRows.Keys)
            {
                if (!key.StartsWith(prefix))
                {
                    continue;
                }

                bool removed;

                SkippedRows.TryRemove(
                    key,
                    out removed);
            }
        }

        private static string BuildKey(
            PXGraph graph,
            string shipmentNbr,
            int? packageLineNbr,
            int? shipmentSplitLineNbr)
        {
            return
                $"{BuildPackagePrefix(graph, shipmentNbr, packageLineNbr)}" +
                $"|Split={shipmentSplitLineNbr}";
        }

        private static string BuildPackagePrefix(
            PXGraph graph,
            string shipmentNbr,
            int? packageLineNbr)
        {
            string company =
                graph?.Accessinfo?.CompanyName
                ?? string.Empty;

            string user =
                graph?.Accessinfo?.UserName
                ?? string.Empty;

            return
                $"Company={company}" +
                $"|User={user}" +
                $"|Shipment={shipmentNbr}" +
                $"|Package={packageLineNbr}";
        }
    }

    public sealed class SelectedPackageContentsExt
        : PXCacheExtension<WmsPlan>
    {
        public static bool IsActive()
        {
            return true;
        }

        #region RemainingQty

        public abstract class remainingQty
            : PX.Data.BQL.BqlDecimal.Field<remainingQty>
        {
        }

        [PXDecimal]
        [PXUIField(
            DisplayName = "Remaining Qty",
            Enabled = false)]
        public decimal? RemainingQty { get; set; }

        #endregion

        #region SkippedStatus

        public abstract class skippedStatus
            : PX.Data.BQL.BqlBool.Field<skippedStatus>
        {
        }

        [PXBool]
        [PXUIField(
            DisplayName = "Skipped Status",
            Enabled = false)]
        public bool? SkippedStatus { get; set; }

        #endregion

        #region UsrCompletedSortOrder

        public abstract class usrCompletedSortOrder
            : PX.Data.BQL.BqlInt.Field<usrCompletedSortOrder>
        {
        }

        [PXInt]
        [PXUIField(
            DisplayName = "Completed Sort Order",
            Visible = false,
            Enabled = false)]
        public int? UsrCompletedSortOrder { get; set; }

        #endregion

        #region UsrSkipSortOrder

        public abstract class usrSkipSortOrder
            : PX.Data.BQL.BqlInt.Field<usrSkipSortOrder>
        {
        }

        [PXInt]
        [PXUIField(
            DisplayName = "Skip Sort Order",
            Visible = false,
            Enabled = false)]
        public int? UsrSkipSortOrder { get; set; }

        #endregion
    }

    /*
     * This is a standard SOShipmentEntry graph extension.
     *
     * It deliberately does not inherit from:
     *
     *     PXGraphExtension<WMS.SOShipmentEntryExt, SOShipmentEntry>
     *
     * Avoiding that second-level extension dependency preserves the
     * working Persist override order between Vadym's WMS package and
     * TrueCommerce.
     *
     * This extension does not override Persist.
     */
    public class SOShipmentEntryExt_SelectedPackageSort
        : PXGraphExtension<SOShipmentEntry>
    {
        private const string TracePrefix =
            "[SelectedPackageSort]";

        private const string Version =
            "2026-07-21-STANDARD-EXT-POST-COMMIT-REFRESH-01";

        public static bool IsActive()
        {
            return true;
        }

        /*
         * The existing view name is retained so the existing grid and
         * ASPX DataMember do not need to be changed.
         */
        public PXSelect<
            WmsPlan,
            Where<
                WmsPlan.shipmentNbr,
                Equal<
                    Current<
                        SOPackageDetailEx.shipmentNbr>>,
                And<
                    WmsPlan.packageLineNbr,
                    Equal<
                        Current<
                            SOPackageDetailEx.lineNbr>>>>,
            OrderBy<
                Asc<
                    SelectedPackageContentsExt.usrCompletedSortOrder,
                    Asc<
                        SelectedPackageContentsExt.usrSkipSortOrder,
                        Asc<
                            WmsPlan.defaultIssueFrom,
                            Asc<
                                WmsPlan.orderNbr,
                                Asc<
                                    WmsPlan.storeNbr,
                                    Asc<
                                        WmsPlan.inventoryID,
                                        Asc<
                                            WmsPlan.lotSerialNbr
                                        >
                                    >
                                >
                            >
                        >
                    >
                >
            >
        > SelectedPackageContentsView;

        protected virtual IEnumerable
            selectedPackageContentsView()
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} VERSION {Version}");

            SOPackageDetailEx package =
                Base.Packages.Current;

            if (package == null ||
                string.IsNullOrEmpty(
                    package.ShipmentNbr) ||
                package.LineNbr == null)
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} No valid current package. " +
                    $"Returning no rows.");

                yield break;
            }

            List<RowCalc> calculatedRows =
                GetCalculatedRows(package);

            Dictionary<int?, string> inventoryCodes =
                GetInventoryCodes(calculatedRows);

            IEnumerable<RowCalc> result;

            if (ShouldBypassTopRowWorkflow())
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Top-row workflow bypassed. " +
                    $"Returning incomplete rows.");

                result =
                    calculatedRows
                        .Where(item =>
                            item.RemainingQty > 0m)
                        .OrderBy(item =>
                            item.Row.DefaultIssueFrom)
                        .ThenBy(item =>
                            item.Row.OrderNbr)
                        .ThenBy(item =>
                            item.Row.StoreNbr)
                        .ThenBy(item =>
                            GetInventoryCD(
                                inventoryCodes,
                                item.Row.InventoryID))
                        .ThenBy(item =>
                            item.Row.LotSerialNbr);
            }
            else
            {
                result =
                    calculatedRows
                        .Where(item =>
                            item.CompletedSortOrder == 0)
                        .OrderBy(item =>
                            item.SkipSortOrder)
                        .ThenBy(item =>
                            item.Row.DefaultIssueFrom)
                        .ThenBy(item =>
                            item.Row.OrderNbr)
                        .ThenBy(item =>
                            item.Row.StoreNbr)
                        .ThenBy(item =>
                            GetInventoryCD(
                                inventoryCodes,
                                item.Row.InventoryID))
                        .ThenBy(item =>
                            item.Row.LotSerialNbr);
            }

            List<RowCalc> sortedRows =
                result.ToList();

            WmsDebugTrace.Info(
                $"{TracePrefix} Returning calculated display rows. " +
                $"Count={sortedRows.Count}");

            foreach (RowCalc item in sortedRows)
            {
                WmsPlan displayRow =
                    CreateDisplayRow(item);

                if (displayRow != null)
                {
                    yield return displayRow;
                }
            }
        }

        /*
         * Creates a detached copy for display.
         *
         * The original WmsPlan record in the graph cache is not
         * modified. No SetValueExt calls are used.
         */
        private WmsPlan CreateDisplayRow(
            RowCalc item)
        {
            if (item?.Row == null)
            {
                return null;
            }

            WmsPlan displayRow =
                PXCache<WmsPlan>.CreateCopy(
                    item.Row);

            SelectedPackageContentsExt extension =
                displayRow.GetExtension<
                    SelectedPackageContentsExt>();

            extension.RemainingQty =
                item.RemainingQty;

            extension.SkippedStatus =
                item.SkipSortOrder == 1;

            extension.UsrCompletedSortOrder =
                item.CompletedSortOrder;

            extension.UsrSkipSortOrder =
                item.SkipSortOrder;

            WmsDebugTrace.Info(
                $"{TracePrefix} Created display copy. " +
                $"RecordID={displayRow.RecordID}, " +
                $"RemainingQty={item.RemainingQty}, " +
                $"SkippedStatus={item.SkipSortOrder == 1}");

            return displayRow;
        }

        private bool ShouldBypassTopRowWorkflow()
        {
            SOShipment shipment =
                Base.Document.Current;

            return CustomerWorkflowBypass
                .ShouldBypassTopRowWorkflow(
                    Base,
                    shipment);
        }

        private string GetSkipStatusText(
            int skipSortOrder)
        {
            return skipSortOrder == 1
                ? "Skipped"
                : "Active";
        }

        private List<RowCalc> GetCalculatedRows(
            SOPackageDetailEx package)
        {
            /*
             * Load the expected rows assigned to the currently selected
             * package.
             */
            List<WmsPlan> plannedRows =
                PXSelectReadonly<
                    WmsPlan,
                    Where<
                        WmsPlan.shipmentNbr,
                        Equal<
                            Required<
                                WmsPlan.shipmentNbr>>,
                        And<
                            WmsPlan.packageLineNbr,
                            Equal<
                                Required<
                                    WmsPlan.packageLineNbr>>>>>
                .Select(
                    Base,
                    package.ShipmentNbr,
                    package.LineNbr)
                .RowCast<WmsPlan>()
                .ToList();

            /*
             * Load actual packed records across the entire shipment.
             *
             * This prevents a split that was packed into another carton
             * from continuing to appear as remaining in its originally
             * expected carton.
             */
            List<SOShipLineSplitPackage>
                shipmentActualRows =
                    PXSelectReadonly<
                        SOShipLineSplitPackage,
                        Where<
                            SOShipLineSplitPackage.shipmentNbr,
                            Equal<
                                Required<
                                    SOShipLineSplitPackage.shipmentNbr>>>>
                    .Select(
                        Base,
                        package.ShipmentNbr)
                    .RowCast<SOShipLineSplitPackage>()
                    .ToList();

            Dictionary<int?, decimal>
                shipmentActualQtyBySplit =
                    shipmentActualRows
                        .Where(row =>
                            row.ShipmentSplitLineNbr != null)
                        .GroupBy(row =>
                            row.ShipmentSplitLineNbr)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Sum(
                                row =>
                                    row.PackedQty ?? 0m));

            /*
             * Package-only quantities are retained for diagnostics.
             * Completion and RemainingQty are based on shipment-wide
             * packed quantities.
             */
            Dictionary<int?, decimal>
                packageActualQtyBySplit =
                    shipmentActualRows
                        .Where(row =>
                            row.PackageLineNbr ==
                                package.LineNbr &&
                            row.ShipmentSplitLineNbr != null)
                        .GroupBy(row =>
                            row.ShipmentSplitLineNbr)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Sum(
                                row =>
                                    row.PackedQty ?? 0m));

            List<RowCalc> result =
                new List<RowCalc>();

            foreach (WmsPlan row in plannedRows)
            {
                int? shipmentSplitLineNbr =
                    row.ShipmentSplitLineNbr;

                decimal expectedQty =
                    row.PackedQty ?? 0m;

                decimal packedInExpectedPackage =
                    0m;

                decimal packedAcrossShipment =
                    0m;

                packageActualQtyBySplit.TryGetValue(
                    shipmentSplitLineNbr,
                    out packedInExpectedPackage);

                shipmentActualQtyBySplit.TryGetValue(
                    shipmentSplitLineNbr,
                    out packedAcrossShipment);

                decimal remainingQty =
                    expectedQty -
                    packedAcrossShipment;

                if (remainingQty < 0m)
                {
                    remainingQty = 0m;
                }

                bool completed =
                    expectedQty > 0m &&
                    packedAcrossShipment >=
                        expectedQty;

                bool packedInDifferentPackage =
                    packedAcrossShipment >
                    packedInExpectedPackage;

                bool skipped =
                    SelectedPackageSkipState.IsSkipped(
                        Base,
                        row.ShipmentNbr,
                        row.PackageLineNbr,
                        shipmentSplitLineNbr);

                WmsDebugTrace.Info(
                    $"{TracePrefix} Row calculated. " +
                    $"RecordID={row.RecordID}, " +
                    $"InventoryID={row.InventoryID}, " +
                    $"ExpectedPackageLineNbr={row.PackageLineNbr}, " +
                    $"ShipmentSplitLineNbr={shipmentSplitLineNbr}, " +
                    $"Expected={expectedQty}, " +
                    $"PackedInExpectedPackage={packedInExpectedPackage}, " +
                    $"PackedAcrossShipment={packedAcrossShipment}, " +
                    $"PackedInDifferentPackage={packedInDifferentPackage}, " +
                    $"Remaining={remainingQty}, " +
                    $"Completed={completed}, " +
                    $"Skipped={skipped}, " +
                    $"SkipStatus={GetSkipStatusText(skipped ? 1 : 0)}");

                result.Add(
                    new RowCalc
                    {
                        Row =
                            row,

                        RealShipmentSplitLineNbr =
                            shipmentSplitLineNbr,

                        RemainingQty =
                            remainingQty,

                        CompletedSortOrder =
                            completed ? 1 : 0,

                        SkipSortOrder =
                            skipped ? 1 : 0
                    });
            }

            WmsDebugTrace.Info(
                $"{TracePrefix} Calculated rows complete. " +
                $"PlannedForSelectedPackage={plannedRows.Count}, " +
                $"ActualAcrossShipment={shipmentActualRows.Count}, " +
                $"Calculated={result.Count}");

            return result;
        }

        /*
         * Loads inventory codes once per view execution instead of
         * querying InventoryItem separately for each row while sorting.
         */
        private Dictionary<int?, string>
            GetInventoryCodes(
                IEnumerable<RowCalc> rows)
        {
            HashSet<int?> inventoryIDs =
                new HashSet<int?>(
                    rows
                        .Where(item =>
                            item?.Row?.InventoryID != null)
                        .Select(item =>
                            item.Row.InventoryID));

            if (inventoryIDs.Count == 0)
            {
                return new Dictionary<int?, string>();
            }

            List<InventoryItem> inventoryItems =
                PXSelectReadonly<InventoryItem>
                    .Select(Base)
                    .RowCast<InventoryItem>()
                    .Where(item =>
                        inventoryIDs.Contains(
                            item.InventoryID))
                    .ToList();

            return inventoryItems
                .GroupBy(item =>
                    item.InventoryID)
                .ToDictionary(
                    group => group.Key,
                    group =>
                        group
                            .Select(item =>
                                item.InventoryCD?.Trim())
                            .FirstOrDefault()
                        ?? string.Empty);
        }

        private string GetInventoryCD(
            Dictionary<int?, string> inventoryCodes,
            int? inventoryID)
        {
            if (inventoryID == null ||
                inventoryCodes == null)
            {
                return string.Empty;
            }

            string inventoryCD;

            return inventoryCodes.TryGetValue(
                inventoryID,
                out inventoryCD)
                    ? inventoryCD ?? string.Empty
                    : string.Empty;
        }

        /*
         * Wait until the database transaction has completed before
         * requesting the expected-content grid refresh.
         *
         * RowInserted, RowUpdated, and RowDeleted are intentionally not
         * used because they may run while the WMS/TrueCommerce Persist
         * transaction is still in progress.
         */
        protected virtual void _(
            Events.RowPersisted<SOShipLineSplitPackage> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (e.TranStatus != PXTranStatus.Completed)
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Packed-row persistence has not " +
                    $"completed successfully. " +
                    $"TranStatus={e.TranStatus}, " +
                    $"Operation={e.Operation}. " +
                    $"Refresh not requested.");

                return;
            }

            bool relevantOperation =
                e.Operation == PXDBOperation.Insert ||
                e.Operation == PXDBOperation.Update ||
                e.Operation == PXDBOperation.Delete;

            if (!relevantOperation)
            {
                return;
            }

            RequestEstimatedContentRefresh(
                $"Packed row persisted successfully. " +
                $"Operation={e.Operation}");
        }

        /*
         * Clears only the stored results for the custom view and marks
         * the grid for refresh.
         *
         * The WmsPlan cache is deliberately not cleared.
         */
        private void RequestEstimatedContentRefresh(
            string reason)
        {
            if (SelectedPackageContentsView?.View == null)
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Refresh could not be requested. " +
                    $"SelectedPackageContentsView was not available. " +
                    $"Reason={reason}");

                return;
            }

            SelectedPackageContentsView
                .View.Clear();

            SelectedPackageContentsView
                .View.RequestRefresh();

            WmsDebugTrace.Info(
                $"{TracePrefix} Expected-content refresh requested " +
                $"after successful transaction completion. " +
                $"Reason={reason}");
        }

        /*
         * Intentionally absent:
         *
         * - Persist override
         * - Second-level WMS graph-extension inheritance
         * - RowInserted<SOShipLineSplitPackage>
         * - RowUpdated<SOShipLineSplitPackage>
         * - RowDeleted<SOShipLineSplitPackage>
         * - SetValueExt against WmsPlan cache rows
         * - SelectedPackageContentsView.Cache.Clear()
         * - WMS extension cache clearing
         */

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