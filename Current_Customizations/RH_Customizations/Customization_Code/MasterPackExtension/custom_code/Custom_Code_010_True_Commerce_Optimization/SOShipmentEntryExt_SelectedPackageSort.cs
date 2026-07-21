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
     * This remains a standard SOShipmentEntry extension.
     *
     * It does not inherit from:
     *
     *     PXGraphExtension<WMS.SOShipmentEntryExt, SOShipmentEntry>
     *
     * Instead, it retrieves the existing WMS extension as a sibling
     * graph extension and installs a delegate on the existing
     * SelectedPackageContentsView.
     *
     * This preserves the existing ASPX DataMember while avoiding the
     * second-level extension dependency that changed Persist ordering.
     */
    public class SOShipmentEntryExt_SelectedPackageSort
        : PXGraphExtension<SOShipmentEntry>
    {
        private const string TracePrefix =
            "[SelectedPackageSort]";

        private const string Version =
            "2026-07-21-WMS-VIEW-DELEGATE-POST-COMMIT-REFRESH-01";

        /*
         * Stores calculated unbound display values for the rows returned
         * by the current execution of the WMS-owned view.
         *
         * FieldSelecting supplies these values when the grid renders the
         * unbound fields.
         *
         * The original WmsPlan records are not modified.
         */
        private readonly Dictionary<string, RowDisplayValues>
            _displayValuesByRow =
                new Dictionary<string, RowDisplayValues>();

        public static bool IsActive()
        {
            return true;
        }

        /*
         * Obtain the existing WMS extension as a sibling extension.
         *
         * This does not make this customization a second-level graph
         * extension and does not change the class inheritance hierarchy.
         */
        private WmsShipmentExt GetWmsExtension()
        {
            return Base.GetExtension<WmsShipmentExt>();
        }

        /*
         * Install the custom delegate on the view that the existing ASPX
         * grid already uses:
         *
         *     DataMember="SelectedPackageContentsView"
         *
         * No duplicate PXSelect view is declared in this extension.
         */
        public override void Initialize()
        {
            base.Initialize();

            WmsShipmentExt wmsExt =
                GetWmsExtension();

            if (wmsExt?.SelectedPackageContentsView?.View == null)
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Could not install custom delegate. " +
                    $"WMS SelectedPackageContentsView was unavailable. " +
                    $"Version={Version}");

                return;
            }

            wmsExt.SelectedPackageContentsView
                .View.SetDelegate(
                    selectedPackageContentsView);

            WmsDebugTrace.Info(
                $"{TracePrefix} Installed custom delegate on existing " +
                $"WMS SelectedPackageContentsView. " +
                $"Version={Version}");
        }

        /*
         * This delegate now supplies the data for the WMS-owned
         * SelectedPackageContentsView.
         */
        protected virtual IEnumerable
            selectedPackageContentsView()
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} Delegate ENTER. " +
                $"Version={Version}");

            /*
             * Remove values from a previous package or previous execution
             * before calculating the current rows.
             */
            _displayValuesByRow.Clear();

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
                $"{TracePrefix} Returning calculated rows. " +
                $"ShipmentNbr={package.ShipmentNbr}, " +
                $"PackageLineNbr={package.LineNbr}, " +
                $"Count={sortedRows.Count}");

            foreach (RowCalc item in sortedRows)
            {
                if (item?.Row == null)
                {
                    continue;
                }

                StoreDisplayValues(item);

                /*
                 * Return the original row.
                 *
                 * FieldSelecting supplies the unbound calculated values
                 * without using SetValueExt.
                 */
                yield return item.Row;
            }

            WmsDebugTrace.Info(
                $"{TracePrefix} Delegate EXIT. " +
                $"Returned={sortedRows.Count}");
        }

        #region Unbound field display

        private void StoreDisplayValues(
            RowCalc item)
        {
            if (item?.Row == null)
            {
                return;
            }

            string key =
                BuildDisplayKey(item.Row);

            _displayValuesByRow[key] =
                new RowDisplayValues
                {
                    RemainingQty =
                        item.RemainingQty,

                    SkippedStatus =
                        item.SkipSortOrder == 1,

                    CompletedSortOrder =
                        item.CompletedSortOrder,

                    SkipSortOrder =
                        item.SkipSortOrder
                };

            WmsDebugTrace.Info(
                $"{TracePrefix} Stored display values. " +
                $"Key={key}, " +
                $"RemainingQty={item.RemainingQty}, " +
                $"SkippedStatus={item.SkipSortOrder == 1}, " +
                $"CompletedSortOrder={item.CompletedSortOrder}, " +
                $"SkipSortOrder={item.SkipSortOrder}");
        }

        private bool TryGetDisplayValues(
            WmsPlan row,
            out RowDisplayValues values)
        {
            values = null;

            if (row == null)
            {
                return false;
            }

            string key =
                BuildDisplayKey(row);

            bool found =
                _displayValuesByRow.TryGetValue(
                    key,
                    out values);

            if (!found)
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} No display values found. " +
                    $"Key={key}");
            }

            return found;
        }

        private string BuildDisplayKey(
            WmsPlan row)
        {
            return
                $"Shipment={row?.ShipmentNbr ?? string.Empty}" +
                $"|Package={row?.PackageLineNbr}" +
                $"|Split={row?.ShipmentSplitLineNbr}" +
                $"|Record={row?.RecordID}";
        }

        protected virtual void _(
            Events.FieldSelecting<
                WmsPlan,
                SelectedPackageContentsExt.remainingQty> e)
        {
            RowDisplayValues values;

            if (TryGetDisplayValues(
                e.Row,
                out values))
            {
                e.ReturnValue =
                    values.RemainingQty;
            }
        }

        protected virtual void _(
            Events.FieldSelecting<
                WmsPlan,
                SelectedPackageContentsExt.skippedStatus> e)
        {
            RowDisplayValues values;

            if (TryGetDisplayValues(
                e.Row,
                out values))
            {
                e.ReturnValue =
                    values.SkippedStatus;
            }
        }

        protected virtual void _(
            Events.FieldSelecting<
                WmsPlan,
                SelectedPackageContentsExt.usrCompletedSortOrder> e)
        {
            RowDisplayValues values;

            if (TryGetDisplayValues(
                e.Row,
                out values))
            {
                e.ReturnValue =
                    values.CompletedSortOrder;
            }
        }

        protected virtual void _(
            Events.FieldSelecting<
                WmsPlan,
                SelectedPackageContentsExt.usrSkipSortOrder> e)
        {
            RowDisplayValues values;

            if (TryGetDisplayValues(
                e.Row,
                out values))
            {
                e.ReturnValue =
                    values.SkipSortOrder;
            }
        }

        #endregion

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
             * Load actual packed records across the entire shipment.
             *
             * Remaining quantity and completion are based on quantities
             * packed in any carton belonging to the shipment.
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
         * Load the inventory records once per view execution rather than
         * issuing one InventoryItem query for every row during sorting.
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
         * Wait for successful transaction completion before requesting
         * that the WMS-owned expected-content view execute again.
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
         * Refresh the actual WMS-owned view used by the grid.
         *
         * No cache clearing is performed.
         */
        private void RequestEstimatedContentRefresh(
            string reason)
        {
            _displayValuesByRow.Clear();

            WmsShipmentExt wmsExt =
                GetWmsExtension();

            if (wmsExt?.SelectedPackageContentsView?.View == null)
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Refresh could not be requested. " +
                    $"WMS SelectedPackageContentsView was unavailable. " +
                    $"Reason={reason}");

                return;
            }

            wmsExt.SelectedPackageContentsView
                .View.Clear();

            wmsExt.SelectedPackageContentsView
                .View.RequestRefresh();

            WmsDebugTrace.Info(
                $"{TracePrefix} WMS expected-content view refresh " +
                $"requested after successful transaction completion. " +
                $"Reason={reason}");
        }

        /*
         * Intentionally absent:
         *
         * - Persist override
         * - Second-level WMS graph-extension inheritance
         * - Duplicate SelectedPackageContentsView declaration
         * - ASPX DataMember change
         * - RowInserted<SOShipLineSplitPackage>
         * - RowUpdated<SOShipLineSplitPackage>
         * - RowDeleted<SOShipLineSplitPackage>
         * - SetValueExt against WmsPlan records
         * - SelectedPackageContentsView.Cache.Clear()
         * - WMS cache clearing
         * - Detached display copies
         */

        private sealed class RowCalc
        {
            public WmsPlan Row { get; set; }

            public int? RealShipmentSplitLineNbr { get; set; }

            public decimal RemainingQty { get; set; }

            public int CompletedSortOrder { get; set; }

            public int SkipSortOrder { get; set; }
        }

        private sealed class RowDisplayValues
        {
            public decimal RemainingQty { get; set; }

            public bool SkippedStatus { get; set; }

            public int CompletedSortOrder { get; set; }

            public int SkipSortOrder { get; set; }
        }
    }
}