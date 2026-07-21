using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;

using WmsShipmentExt = WMS.SOShipmentEntryExt;
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
     * This remains a standard first-level SOShipmentEntry extension.
     *
     * It does not inherit from:
     *
     *     PXGraphExtension<WMS.SOShipmentEntryExt, SOShipmentEntry>
     *
     * Instead, it retrieves the WMS extension as a sibling and replaces
     * the graph-level Base.Views registration for:
     *
     *     SelectedPackageContentsView
     *
     * The replacement PXView uses an explicit BQL command containing the
     * intended default OrderBy and this extension's custom delegate.
     */
    public class SOShipmentEntryExt_SelectedPackageSort
        : PXGraphExtension<SOShipmentEntry>
    {
        private const string TracePrefix =
            "[SelectedPackageSort]";

        private const string ViewName =
            "SelectedPackageContentsView";

        private const string Version =
            "2026-07-21-ORDERED-BASE-VIEW-SETVALUEEXT-01";

        public static bool IsActive()
        {
            return true;
        }

        /*
         * Obtain the WMS extension as a sibling.
         *
         * This does not make the customization a second-level extension.
         */
        private WmsShipmentExt GetWmsExtension()
        {
            return Base.GetExtension<WmsShipmentExt>();
        }

        /*
         * Replace the registered view with an explicitly ordered BQL
         * command and the custom delegate.
         *
         * The ASPX DataMember remains:
         *
         *     SelectedPackageContentsView
         */
        public override void Initialize()
        {
            base.Initialize();

            WmsShipmentExt wmsExt =
                GetWmsExtension();

            PXView wmsView =
                wmsExt?.SelectedPackageContentsView?.View;

            if (wmsView == null)
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Could not replace registered view. " +
                    $"WMS {ViewName} was unavailable. " +
                    $"Version={Version}");

                return;
            }

            /*
             * This command reproduces the intended default view:
             *
             * 1. Completed rows
             * 2. Skipped rows
             * 3. Default issue location
             * 4. Order number
             * 5. Store number
             * 6. Inventory ID
             * 7. Lot/serial number
             *
             * The delegate normally filters completed rows out, but the
             * completed sort field is retained to preserve the previous
             * view definition and support any future alternate workflow.
             */
            BqlCommand orderedCommand =
                BqlCommand.CreateInstance(
                    typeof(Select<
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
                                SelectedPackageContentsExt
                                    .usrCompletedSortOrder,
                                Asc<
                                    SelectedPackageContentsExt
                                        .usrSkipSortOrder,
                                    Asc<
                                        WmsPlan.defaultIssueFrom,
                                        Asc<
                                            WmsPlan.orderNbr,
                                            Asc<
                                                WmsPlan.storeNbr,
                                                Asc<
                                                    WmsPlan.inventoryID,
                                                    Asc<
                                                        WmsPlan
                                                            .lotSerialNbr
                                                    >
                                                >
                                            >
                                        >
                                    >
                                >
                            >
                        >
                    >));

            Base.Views[ViewName] =
                new PXView(
                    Base,
                    false,
                    orderedCommand,
                    new PXSelectDelegate(
                        selectedPackageContentsView));

            PXView registeredView =
                Base.Views[ViewName];

            WmsDebugTrace.Info(
                $"{TracePrefix} Replaced Base.Views registration. " +
                $"ViewName={ViewName}, " +
                $"Registered={registeredView != null}, " +
                $"BqlCommand=" +
                $"{registeredView?.BqlSelect?.GetType().FullName}, " +
                $"Version={Version}");
        }

        /*
         * Supplies rows for the replacement view.
         *
         * The rows are:
         *
         * - calculated;
         * - filtered;
         * - sorted;
         * - populated through SetValueExt;
         * - returned as a PXDelegateResult marked as sorted.
         */
        protected virtual IEnumerable
            selectedPackageContentsView()
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} Delegate ENTER. " +
                $"Version={Version}");

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

                return new PXDelegateResult
                {
                    IsResultFiltered = true,
                    IsResultSorted = true,
                    IsResultTruncated = false
                };
            }

            List<RowCalc> calculatedRows =
                GetCalculatedRows(package);

            Dictionary<int?, string> inventoryCodes =
                GetInventoryCodes(calculatedRows);

            List<RowCalc> sortedRows;

            if (ShouldBypassTopRowWorkflow())
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Top-row workflow bypassed. " +
                    $"Returning incomplete rows.");

                sortedRows =
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
                            item.Row.LotSerialNbr)
                        .ToList();
            }
            else
            {
                sortedRows =
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
                            item.Row.LotSerialNbr)
                        .ToList();
            }

            List<WmsPlan> rowsToReturn =
                new List<WmsPlan>();

            foreach (RowCalc item in sortedRows)
            {
                if (item?.Row == null)
                {
                    continue;
                }

                /*
                 * Populate the four unbound extension fields directly on
                 * the cached WmsPlan row.
                 *
                 * These fields are not database-bound, so this does not
                 * persist the values to the database.
                 */
                ApplyCalculatedValues(item);

                rowsToReturn.Add(item.Row);
            }

            PXDelegateResult delegateResult =
                new PXDelegateResult
                {
                    /*
                     * The delegate has already filtered and sorted the
                     * records. This prevents the PXView from unnecessarily
                     * reapplying its normal sorting to the delegate output.
                     */
                    IsResultFiltered = true,
                    IsResultSorted = true,
                    IsResultTruncated = false
                };

            delegateResult.AddRange(
                rowsToReturn);

            WmsDebugTrace.Info(
                $"{TracePrefix} Delegate EXIT. " +
                $"ShipmentNbr={package.ShipmentNbr}, " +
                $"PackageLineNbr={package.LineNbr}, " +
                $"Returned={rowsToReturn.Count}, " +
                $"MarkedFiltered=" +
                $"{delegateResult.IsResultFiltered}, " +
                $"MarkedSorted=" +
                $"{delegateResult.IsResultSorted}");

            return delegateResult;
        }

        /*
         * Apply calculated values to the unbound DAC extension fields.
         *
         * This uses the same cache that owns the WmsPlan row returned by
         * the replacement view.
         */
        private void ApplyCalculatedValues(
            RowCalc item)
        {
            if (item?.Row == null)
            {
                return;
            }

            PXCache cache =
                Base.Caches<WmsPlan>();

            cache.SetValueExt<
                SelectedPackageContentsExt.remainingQty>(
                    item.Row,
                    item.RemainingQty);

            cache.SetValueExt<
                SelectedPackageContentsExt.skippedStatus>(
                    item.Row,
                    item.SkipSortOrder == 1);

            cache.SetValueExt<
                SelectedPackageContentsExt
                    .usrCompletedSortOrder>(
                    item.Row,
                    item.CompletedSortOrder);

            cache.SetValueExt<
                SelectedPackageContentsExt
                    .usrSkipSortOrder>(
                    item.Row,
                    item.SkipSortOrder);

            WmsDebugTrace.Info(
                $"{TracePrefix} Applied unbound values. " +
                $"RecordID={item.Row.RecordID}, " +
                $"RemainingQty={item.RemainingQty}, " +
                $"SkippedStatus={item.SkipSortOrder == 1}, " +
                $"CompletedSortOrder=" +
                $"{item.CompletedSortOrder}, " +
                $"SkipSortOrder={item.SkipSortOrder}");
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
             * Load expected rows assigned to the selected package.
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
             * Load actual packed rows from the entire shipment.
             *
             * Remaining quantity and completion are calculated against
             * quantities packed in all cartons for the shipment.
             */
            List<SOShipLineSplitPackage>
                shipmentActualRows =
                    PXSelectReadonly<
                        SOShipLineSplitPackage,
                        Where<
                            SOShipLineSplitPackage.shipmentNbr,
                            Equal<
                                Required<
                                    SOShipLineSplitPackage
                                        .shipmentNbr>>>>
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
                    $"ExpectedPackageLineNbr=" +
                    $"{row.PackageLineNbr}, " +
                    $"ShipmentSplitLineNbr=" +
                    $"{shipmentSplitLineNbr}, " +
                    $"Expected={expectedQty}, " +
                    $"PackedInExpectedPackage=" +
                    $"{packedInExpectedPackage}, " +
                    $"PackedAcrossShipment=" +
                    $"{packedAcrossShipment}, " +
                    $"PackedInDifferentPackage=" +
                    $"{packedInDifferentPackage}, " +
                    $"Remaining={remainingQty}, " +
                    $"Completed={completed}, " +
                    $"Skipped={skipped}, " +
                    $"SkipStatus=" +
                    $"{GetSkipStatusText(skipped ? 1 : 0)}");

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
                $"PlannedForSelectedPackage=" +
                $"{plannedRows.Count}, " +
                $"ActualAcrossShipment=" +
                $"{shipmentActualRows.Count}, " +
                $"Calculated={result.Count}");

            return result;
        }

        /*
         * Load inventory codes once per view execution.
         *
         * The delegate sorts by InventoryCD while the BQL command retains
         * the older InventoryID default sort as a fallback/default view
         * definition.
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
         * Wait until the database transaction has completed successfully
         * before refreshing the expected-content view.
         *
         * No cache clearing is performed.
         */
        protected virtual void _(
            Events.RowPersisted<SOShipLineSplitPackage> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (e.TranStatus !=
                PXTranStatus.Completed)
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
                e.Operation ==
                    PXDBOperation.Insert ||
                e.Operation ==
                    PXDBOperation.Update ||
                e.Operation ==
                    PXDBOperation.Delete;

            if (!relevantOperation)
            {
                return;
            }

            RequestEstimatedContentRefresh(
                $"Packed row persisted successfully. " +
                $"Operation={e.Operation}");
        }

        /*
         * Refresh the active PXView registered under the ASPX DataMember.
         *
         * Intentionally does not call:
         *
         *     Base.Caches<WmsPlan>().Clear()
         *
         * or:
         *
         *     wmsExt.SelectedPackageContentsView.Cache.Clear()
         */
        private void RequestEstimatedContentRefresh(
            string reason)
        {
            PXView view =
                Base.Views[ViewName];

            if (view == null)
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Refresh could not be requested. " +
                    $"Base.Views did not contain {ViewName}. " +
                    $"Reason={reason}");

                return;
            }

            view.Clear();
            view.RequestRefresh();

            WmsDebugTrace.Info(
                $"{TracePrefix} Expected-content view refresh " +
                $"requested after successful transaction completion. " +
                $"ViewName={ViewName}, " +
                $"Reason={reason}");
        }

        /*
         * Intentionally absent:
         *
         * - Persist override
         * - Second-level WMS graph-extension inheritance
         * - Duplicate PXSelect member declaration
         * - ASPX DataMember change
         * - PXView.SetDelegate
         * - PXView.Delegate assignment
         * - RowInserted<SOShipLineSplitPackage>
         * - RowUpdated<SOShipLineSplitPackage>
         * - RowDeleted<SOShipLineSplitPackage>
         * - WmsPlan cache clearing
         * - SelectedPackageContentsView.Cache.Clear()
         * - Dictionary-backed FieldSelecting display values
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