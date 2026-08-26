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

        #region LegacyItemNbr

        public abstract class legacyItemNbr
            : PX.Data.BQL.BqlString.Field<legacyItemNbr>
        {
        }

        [PXString(30, IsUnicode = true)]
        [PXUIField(
            DisplayName = "Legacy Item Number",
            Enabled = false)]
        public string LegacyItemNbr { get; set; }

        #endregion

        #region UPC

        public abstract class uPC
            : PX.Data.BQL.BqlString.Field<uPC>
        {
        }

        [PXString(50, IsUnicode = true)]
        [PXUIField(
            DisplayName = "UPC",
            Enabled = false)]
        public string UPC { get; set; }

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
            "2026-08-18-LEGACY-UPC-01";

        /*
         * Graph-instance lookup cache.
         *
         * This prevents repeated InventoryItem lookups when Acumatica
         * executes the view delegate multiple times during one callback.
         *
         * Only stable lookup data is cached here. Calculated package rows
         * and remaining quantities are intentionally not cached.
         */
        private readonly Dictionary<int?, string>
            _inventoryCodeCache =
                new Dictionary<int?, string>();

        private readonly Dictionary<int?, string>
            _locationCodeCache =
                new Dictionary<int?, string>();

        private readonly Dictionary<int?, string>
            _legacyItemNbrCache =
                new Dictionary<int?, string>();

        private readonly Dictionary<int?, string>
            _upcCache =
                new Dictionary<int?, string>();

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
                            GetLocationCD(
                                item.Row.DefaultIssueFrom))
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
                            GetLocationCD(
                                item.Row.DefaultIssueFrom))
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
                 * Populate the unbound extension fields directly on
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

            cache.SetValueExt<
                SelectedPackageContentsExt.legacyItemNbr>(
                    item.Row,
                    GetCachedValue(
                        _legacyItemNbrCache,
                        item.Row.InventoryID));

            cache.SetValueExt<
                SelectedPackageContentsExt.uPC>(
                    item.Row,
                    GetCachedValue(
                        _upcCache,
                        item.Row.InventoryID));

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
         * Resolve InventoryCD only for the inventory IDs required by the
         * current package.
         *
         * Previous implementation:
         *
         *     PXSelectReadonly<InventoryItem>.Select(Base)
         *
         * selected the complete accessible InventoryItem table and then
         * filtered the results in memory. Request Profiler showed that
         * query returning approximately 42,450 rows per execution.
         *
         * The PK finder instead performs an indexed lookup by InventoryID.
         * It also uses Acumatica's graph/cache lookup behavior.
         *
         * The graph-instance dictionary prevents repeated lookups if the
         * delegate executes multiple times during the same callback.
         */
        private Dictionary<int?, string>
            GetInventoryCodes(
                IEnumerable<RowCalc> rows)
        {
            int?[] inventoryIDs =
                rows
                    .Where(item =>
                        item?.Row?.InventoryID != null)
                    .Select(item =>
                        item.Row.InventoryID)
                    .Distinct()
                    .ToArray();

            if (inventoryIDs.Length == 0)
            {
                return _inventoryCodeCache;
            }

            int lookupCount =
                0;

            int cacheHitCount =
                0;

            foreach (int? inventoryID in inventoryIDs)
            {
                if (inventoryID == null)
                {
                    continue;
                }

                string existingInventoryCD;

                if (!_inventoryCodeCache.TryGetValue(
                    inventoryID,
                    out existingInventoryCD))
                {
                    InventoryItem inventoryItem =
                        InventoryItem.PK.Find(
                            Base,
                            inventoryID);

                    _inventoryCodeCache[inventoryID] =
                        inventoryItem?.InventoryCD?.Trim()
                        ?? string.Empty;

                    if (!_legacyItemNbrCache.ContainsKey(
                        inventoryID))
                    {
                        object legacyValue =
                            inventoryItem == null
                                ? null
                                : Base.Caches<InventoryItem>()
                                    .GetValue(
                                        inventoryItem,
                                        "UsrLegacyID");

                        _legacyItemNbrCache[inventoryID] =
                            legacyValue?.ToString()?.Trim()
                            ?? string.Empty;
                    }

                    lookupCount++;
                }
                else
                {
                    cacheHitCount++;
                }

                if (!_upcCache.ContainsKey(inventoryID))
                {
                    INItemXRef upcReference =
                        PXSelectReadonly<
                            INItemXRef,
                            Where<
                                INItemXRef.inventoryID,
                                Equal<
                                    Required<
                                        INItemXRef.inventoryID>>,
                                And<
                                    INItemXRef.alternateType,
                                    Equal<
                                        Required<
                                            INItemXRef
                                                .alternateType>>>>>
                        .SelectWindowed(
                            Base,
                            0,
                            1,
                            inventoryID,
                            "GIN");

                    _upcCache[inventoryID] =
                        upcReference?.AlternateID?.Trim()
                        ?? string.Empty;
                }
            }

            WmsDebugTrace.Info(
                $"{TracePrefix} Inventory codes resolved. " +
                $"RequestedIDs={inventoryIDs.Length}, " +
                $"PKLookups={lookupCount}, " +
                $"ExtensionCacheHits={cacheHitCount}, " +
                $"CachedCodes={_inventoryCodeCache.Count}");

            return _inventoryCodeCache;
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

        private string GetLocationCD(
            int? locationID)
        {
            if (locationID == null)
            {
                return string.Empty;
            }

            string locationCD;

            if (_locationCodeCache.TryGetValue(
                locationID,
                out locationCD))
            {
                return locationCD ?? string.Empty;
            }

            INLocation location =
                INLocation.PK.Find(
                    Base,
                    locationID);

            locationCD =
                location?.LocationCD?.Trim()
                ?? string.Empty;

            _locationCodeCache[locationID] =
                locationCD;

            return locationCD;
        }

        private string GetCachedValue(
            Dictionary<int?, string> values,
            int? inventoryID)
        {
            if (inventoryID == null || values == null)
            {
                return string.Empty;
            }

            string value;

            return values.TryGetValue(
                inventoryID,
                out value)
                    ? value ?? string.Empty
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
         * - Unrestricted InventoryItem table selection
         * - Caching of calculated package quantities
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
