using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.BarcodeProcessing;
using PX.Common;
using PX.Data;
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
        public static void Write(
            PickPackShip basis,
            PickPackShip.PackMode.Logic mode,
            string resultSource,
            int? resultRows)
        {
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
