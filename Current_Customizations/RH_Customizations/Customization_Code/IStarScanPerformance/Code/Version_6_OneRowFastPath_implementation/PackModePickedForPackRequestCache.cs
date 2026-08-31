using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.BarcodeProcessing;
using PX.Common;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.SO.WMS;

namespace IStar.ScanPerformance
{
    /// <summary>
    /// Diagnostic-only capture of the PXView request that invokes
    /// pickedForPack(). One consolidated trace line is written per
    /// invocation so the output remains usable in both the Trace screen and
    /// PXFileTraceProvider output. Diagnostic failures never interrupt scans.
    /// </summary>
    internal static class PickedForPackViewDiagnostic
    {
        // Detailed PXView tracing was needed by Versions 4 and 5. Keep it
        // disabled in the implementation build so performance measurements
        // are not distorted by repeated diagnostic file writes.
        public const bool Enabled = false;

        public static void Write(
            PickPackShip basis,
            PickPackShip.PackMode.Logic mode,
            string resultSource,
            int? resultRows)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                SOShipLineSplit currentSplit = null;

                if (basis?.Graph != null)
                {
                    PXCache splitCache =
                        basis.Graph.Caches<SOShipLineSplit>();

                    currentSplit =
                        PXResult.Unwrap<SOShipLineSplit>(
                            splitCache.Current);
                }

                PXTrace.WriteInformation(
                    "[PFP-VIEW-DIAG] Utc={0}; Shipment={1}; HeaderMode={2}; PackageLineNbr={3}; PackageLineNbrUI={4}; Remove={5}; StartRow={6}; MaximumRows={7}; Searches={8}; SortColumns={9}; Descendings={10}; Filters={11}; CurrentSplitShipment={12}; LineNbr={13}; SplitLineNbr={14}; IsUnassigned={15}; InventoryID={16}; SubItemID={17}; LocationID={18}; ResultSource={19}; ResultRows={20}.",
                    DateTime.UtcNow.ToString("O"),
                    SafeValue(basis?.RefNbr),
                    SafeValue(basis?.Header?.Mode),
                    SafeValue(mode?.PackageLineNbr),
                    SafeValue(mode?.PackageLineNbrUI),
                    basis?.Remove.GetValueOrDefault() ?? false,
                    PXView.StartRow,
                    PXView.MaximumRows,
                    FormatValues(PXView.Searches),
                    FormatValues(PXView.SortColumns),
                    FormatValues(PXView.Descendings),
                    FormatFilters(PXView.Filters),
                    SafeValue(currentSplit?.ShipmentNbr),
                    SafeValue(currentSplit?.LineNbr),
                    SafeValue(currentSplit?.SplitLineNbr),
                    SafeValue(currentSplit?.IsUnassigned),
                    SafeValue(currentSplit?.InventoryID),
                    SafeValue(currentSplit?.SubItemID),
                    SafeValue(currentSplit?.LocationID),
                    SafeValue(resultSource),
                    SafeValue(resultRows));
            }
            catch (Exception exception)
            {
                // Diagnostics must never replace or hide the scan result.
                PXTrace.WriteInformation(
                    "[PFP-VIEW-DIAG] DiagnosticError={0}; Message={1}.",
                    exception.GetType().FullName,
                    SafeValue(exception.Message));
            }
        }

        private static string FormatValues(
            IEnumerable values)
        {
            if (values == null)
            {
                return "<null>";
            }

            return "[" + string.Join(
                "|",
                values.Cast<object>().Select(SafeValue)) + "]";
        }

        private static string FormatFilters(
            PXFilterRow[] filters)
        {
            if (filters == null)
            {
                return "<null>";
            }

            return "[" + string.Join(
                "|",
                filters.Select(
                    filter => filter == null
                        ? "<null>"
                        : string.Format(
                            "{0}:{1}:{2}:{3}:Open={4}:Close={5}:Or={6}",
                            SafeValue(filter.DataField),
                            SafeValue(filter.Condition),
                            SafeValue(filter.Value),
                            SafeValue(filter.Value2),
                            filter.OpenBrackets,
                            filter.CloseBrackets,
                            filter.OrOperator))) + "]";
        }

        private static string SafeValue(
            object value)
        {
            if (value == null)
            {
                return "<null>";
            }

            string text = Convert.ToString(value) ?? "<null>";

            text = text
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("|", "/")
                .Replace(";", ",");

            return text.Length <= 120
                ? text
                : text.Substring(0, 120) + "...";
        }
    }

    /// <summary>
    /// One-row implementation for qualified grid SyncPosition requests.
    /// Qualified requests return the exact assigned split directly without
    /// executing or materializing the standard full PickedForPack result.
    /// </summary>
    internal static class PickedForPackOneRowFastPath
    {
        public const bool EnableFastPath = true;

        internal sealed class Request
        {
            public string ShipmentNbr { get; set; }
            public int LineNbr { get; set; }
            public int SplitLineNbr { get; set; }
        }

        public static bool TryCreateRequest(
            PickPackShip basis,
            out Request request,
            out string fallbackReason)
        {
            request = null;

            if (PXView.StartRow != 0)
            {
                fallbackReason = "StartRowNotZero";
                return false;
            }

            if (PXView.MaximumRows != 1)
            {
                fallbackReason = "MaximumRowsNotOne";
                return false;
            }

            if (PXView.Filters != null && PXView.Filters.Length != 0)
            {
                fallbackReason = "FiltersPresent";
                return false;
            }

            object[] searches = PXView.Searches;
            string[] sortColumns = PXView.SortColumns;

            if (searches == null ||
                sortColumns == null ||
                searches.Length != sortColumns.Length)
            {
                fallbackReason = "SearchSortShapeMismatch";
                return false;
            }

            object shipmentValue = null;
            object lineValue = null;
            object splitValue = null;
            object isUnassignedValue = null;

            for (int index = 0; index < sortColumns.Length; index++)
            {
                string fieldName = NormalizeFieldName(
                    sortColumns[index]);

                if (fieldName == "shipmentnbr")
                {
                    shipmentValue = searches[index];
                }
                else if (fieldName == "linenbr")
                {
                    lineValue = searches[index];
                }
                else if (fieldName == "splitlinenbr")
                {
                    splitValue = searches[index];
                }
                else if (fieldName == "isunassigned")
                {
                    isUnassignedValue = searches[index];
                }
            }

            string shipmentNbr = Convert.ToString(shipmentValue);
            int lineNbr;
            int splitLineNbr;

            if (string.IsNullOrWhiteSpace(shipmentNbr) ||
                !TryConvertInt(lineValue, out lineNbr) ||
                !TryConvertInt(splitValue, out splitLineNbr))
            {
                fallbackReason = "IncompleteSearchKey";
                return false;
            }

            if (!string.Equals(
                shipmentNbr,
                basis?.RefNbr,
                StringComparison.OrdinalIgnoreCase))
            {
                fallbackReason = "ShipmentMismatch";
                return false;
            }

            bool isUnassigned;

            if (isUnassignedValue != null &&
                (!bool.TryParse(
                    Convert.ToString(isUnassignedValue),
                    out isUnassigned) ||
                 isUnassigned))
            {
                fallbackReason = "UnassignedUnsupported";
                return false;
            }

            request = new Request
            {
                ShipmentNbr = shipmentNbr,
                LineNbr = lineNbr,
                SplitLineNbr = splitLineNbr
            };

            fallbackReason = null;
            return true;
        }

        public static List<object> SelectTargetedRows(
            PickPackShip basis,
            Request request)
        {
            var result = new List<object>();

            PXResultset<SOShipLineSplit> rows =
                PXSelectJoin<
                    SOShipLineSplit,
                    InnerJoin<
                        SOShipLine,
                        On<
                            SOShipLine.shipmentNbr,
                            Equal<SOShipLineSplit.shipmentNbr>,
                            And<
                                SOShipLine.lineNbr,
                                Equal<SOShipLineSplit.lineNbr>>>,
                    InnerJoin<
                        INLocation,
                        On<
                            INLocation.locationID,
                            Equal<SOShipLineSplit.locationID>>>>,
                    Where<
                        SOShipLineSplit.shipmentNbr,
                        Equal<Required<SOShipLineSplit.shipmentNbr>>,
                        And<
                            SOShipLineSplit.lineNbr,
                            Equal<Required<SOShipLineSplit.lineNbr>>,
                        And<
                            SOShipLineSplit.splitLineNbr,
                            Equal<Required<SOShipLineSplit.splitLineNbr>>,
                        And<
                            SOShipLineSplit.isUnassigned,
                            Equal<False>>>>>>
                .SelectWindowed(
                    basis,
                    0,
                    2,
                    request.ShipmentNbr,
                    request.LineNbr,
                    request.SplitLineNbr);

            foreach (PXResult<SOShipLineSplit> rawRow in rows)
            {
                SOShipLineSplit split =
                    rawRow.GetItem<SOShipLineSplit>();
                SOShipLine line =
                    rawRow.GetItem<SOShipLine>();
                INLocation location =
                    rawRow.GetItem<INLocation>();

                if (split != null && line != null && location != null)
                {
                    result.Add(
                        new PXResult<
                            SOShipLineSplit,
                            SOShipLine,
                            INLocation>(
                                split,
                                line,
                                location));
                }
            }

            return result;
        }

        public static void WriteDecision(
            Request request,
            string decision,
            string reason,
            int? targetedRows)
        {
            PXTrace.WriteInformation(
                "[PFP-FASTPATH] Mode={0}; Decision={1}; Reason={2}; Shipment={3}; LineNbr={4}; SplitLineNbr={5}; TargetRows={6}; FastPathEnabled={7}.",
                "Enabled",
                decision,
                reason,
                request?.ShipmentNbr,
                request?.LineNbr,
                request?.SplitLineNbr,
                targetedRows,
                EnableFastPath);
        }

        private static string NormalizeFieldName(
            string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return string.Empty;
            }

            int separator = fieldName.LastIndexOf('.');
            string normalized = separator >= 0
                ? fieldName.Substring(separator + 1)
                : fieldName;

            return normalized
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Trim()
                .ToLowerInvariant();
        }

        private static bool TryConvertInt(
            object value,
            out int result)
        {
            if (value is int)
            {
                result = (int)value;
                return true;
            }

            return int.TryParse(
                Convert.ToString(value),
                out result);
        }
    }

    /// <summary>
    /// Stores one materialized PickedForPack result for the current HTTP
    /// request. PXContext slots do not persist into the next callback.
    /// </summary>
    internal static class PickedForPackRequestCache
    {
        private const string SlotKey =
            "IStar.ScanPerformance.PickedForPackRequestCache";

        internal sealed class State
        {
            public PickPackShip Basis { get; set; }
            public string ShipmentNbr { get; set; }
            public string Mode { get; set; }
            public int? PackageLineNbr { get; set; }
            public int? PackageLineNbrUI { get; set; }
            public bool Remove { get; set; }
            public bool HasResult { get; set; }
            public List<object> Rows { get; set; }
        }

        public static bool TryGet(
            PickPackShip basis,
            PickPackShip.PackMode.Logic mode,
            out List<object> rows)
        {
            State state = PXContext.GetSlot<State>(SlotKey);

            if (state != null &&
                state.HasResult &&
                ReferenceEquals(state.Basis, basis) &&
                string.Equals(
                    state.ShipmentNbr,
                    basis?.RefNbr,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    state.Mode,
                    basis?.Header?.Mode,
                    StringComparison.Ordinal) &&
                state.PackageLineNbr == mode?.PackageLineNbr &&
                state.PackageLineNbrUI == mode?.PackageLineNbrUI &&
                state.Remove ==
                    (basis?.Remove.GetValueOrDefault() ?? false))
            {
                rows = state.Rows;
                return true;
            }

            rows = null;
            return false;
        }

        public static void Store(
            PickPackShip basis,
            PickPackShip.PackMode.Logic mode,
            List<object> rows)
        {
            PXContext.SetSlot<State>(
                SlotKey,
                new State
                {
                    Basis = basis,
                    ShipmentNbr = basis?.RefNbr,
                    Mode = basis?.Header?.Mode,
                    PackageLineNbr = mode?.PackageLineNbr,
                    PackageLineNbrUI = mode?.PackageLineNbrUI,
                    Remove = basis?.Remove.GetValueOrDefault() ?? false,
                    HasResult = true,
                    Rows = rows
                });
        }

        public static void Invalidate()
        {
            PXContext.ClearSlot(SlotKey);
        }

        public static PXDelegateResult CreateResult(
            IEnumerable<object> rows)
        {
            var result = new PXDelegateResult
            {
                IsResultSorted = true
            };

            if (rows != null)
            {
                result.AddRange(rows);
            }

            return result;
        }
    }

    /// <summary>
    /// Extends the third-party Master Pack pickedForPack override and reuses
    /// its materialized result for repeated reads in the same scan request.
    /// </summary>
    public class PackModePickedForPackRequestCacheExt
        : BarcodeDrivenStateMachine<
            PickPackShip,
            PickPackShip.Host>
            .ScanExtension<
                WMS.PackModeLogicExt>
    {
        public static bool IsActive()
        {
            return true;
        }

        [PXOverride]
        public virtual IEnumerable pickedForPack(
            WMS.PackModeLogicExt.PickedForPackDelegate baseMethod)
        {
            PickPackShip.PackMode.Logic mode =
                Basis.Get<PickPackShip.PackMode.Logic>();

            PickedForPackOneRowFastPath.Request fastPathRequest;
            string fallbackReason;
            List<object> targetedRows = null;

            bool fastPathCandidate =
                PickedForPackOneRowFastPath.TryCreateRequest(
                    Basis,
                    out fastPathRequest,
                    out fallbackReason);

            if (fastPathCandidate)
            {
                targetedRows =
                    PickedForPackOneRowFastPath.SelectTargetedRows(
                        Basis,
                        fastPathRequest);

                if (targetedRows.Count != 1)
                {
                    PickedForPackOneRowFastPath.WriteDecision(
                        fastPathRequest,
                        "Fallback",
                        targetedRows.Count == 0
                            ? "RowNotFound"
                            : "AmbiguousResult",
                        targetedRows.Count);

                    fastPathCandidate = false;
                }
                else
                {
                    // Never put this one-row result in the full-result cache.
                    // A later MaximumRows=0 CanPack consumer requires all
                    // shipment splits from the standard implementation.
                    PickedForPackOneRowFastPath.WriteDecision(
                        fastPathRequest,
                        "Applied",
                        "QualifiedAssignedSplit",
                        targetedRows.Count);

                    return PickedForPackRequestCache.CreateResult(
                        targetedRows);
                }
            }

            List<object> cachedRows;

            if (PickedForPackRequestCache.TryGet(
                Basis,
                mode,
                out cachedRows))
            {
                PickedForPackViewDiagnostic.Write(
                    Basis,
                    mode,
                    "RequestCache",
                    cachedRows?.Count);

                return PickedForPackRequestCache.CreateResult(
                    cachedRows);
            }

            IEnumerable existingResult = baseMethod();

            if (existingResult == null)
            {
                PickedForPackViewDiagnostic.Write(
                    Basis,
                    mode,
                    "BaseNull",
                    null);

                return null;
            }

            List<object> rows =
                existingResult.Cast<object>().ToList();

            PickedForPackRequestCache.Store(
                Basis,
                mode,
                rows);

            PickedForPackViewDiagnostic.Write(
                Basis,
                mode,
                "BaseMaterialized",
                rows.Count);

            if (PXView.MaximumRows == 1)
            {
                PickedForPackOneRowFastPath.WriteDecision(
                    fastPathRequest,
                    "Fallback",
                    fallbackReason ?? "TargetedLookupRejected",
                    targetedRows?.Count);
            }

            return PickedForPackRequestCache.CreateResult(rows);
        }
    }

    /// <summary>
    /// Extends the third-party confirmation logic. Confirm can call PackSplit,
    /// which changes SOShipLineSplitPackage.PackedQty. Always invalidate so a
    /// later CanPack evaluation cannot reuse pre-confirmation quantities.
    /// </summary>
    public class ConfirmStatePickedForPackCacheInvalidationExt
        : BarcodeDrivenStateMachine<
            PickPackShip,
            PickPackShip.Host>
            .ScanExtension<
                WMS.ConfirmStateLogicExt>
    {
        public static bool IsActive()
        {
            return true;
        }

        [PXOverride]
        public virtual FlowStatus Confirm(
            WMS.ConfirmStateLogicExt.ConfirmDelegate baseMethod)
        {
            try
            {
                return baseMethod();
            }
            finally
            {
                PickedForPackRequestCache.Invalidate();
            }
        }
    }
}
