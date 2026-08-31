using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.BarcodeProcessing;
using PX.Common;
using PX.Data;
using PX.Objects.SO.WMS;

namespace IStar.ScanPerformance
{
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
                return PickedForPackRequestCache.CreateResult(
                    cachedRows);
            }

            IEnumerable existingResult = baseMethod();

            if (existingResult == null)
            {
                return null;
            }

            List<object> rows =
                existingResult.Cast<object>().ToList();

            PickedForPackRequestCache.Store(
                Basis,
                mode,
                rows);

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
