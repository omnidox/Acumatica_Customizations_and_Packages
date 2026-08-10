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
            public List<decimal?> InitialPackedQuantities { get; set; }
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
            List<decimal?> initialPackedQuantities = rows
                .Select(row =>
                {
                    SOShipLineSplit split =
                        PXResult.Unwrap<SOShipLineSplit>(row);

                    return split?.PackedQty;
                })
                .ToList();

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
                    Rows = rows,
                    InitialPackedQuantities = initialPackedQuantities
                });
        }

        /// <summary>
        /// Retains the request-scoped result only when Confirm changed one or
        /// more assigned splits in place and every retained split is still
        /// the canonical PXCache object with the current PackedQty value.
        /// Any unsupported or uncertain condition falls back to invalidation.
        /// </summary>
        public static void ValidateAfterConfirm(PickPackShip basis)
        {
            State state = PXContext.GetSlot<State>(SlotKey);

            try
            {
                if (state == null || !state.HasResult || state.Rows == null)
                {
                    Invalidate();
                    return;
                }

                PXCache splitCache =
                    basis?.Graph?.Caches<SOShipLineSplit>();

                if (splitCache == null)
                {
                    Invalidate();
                    return;
                }

                int total = 0;
                int assigned = 0;
                int unassigned = 0;
                int unwrapFailed = 0;
                int canonicalFound = 0;
                int sameReference = 0;
                int differentReference = 0;
                int canonicalMissing = 0;
                int retainedQtyChanged = 0;
                int packedQtyMismatch = 0;

                for (int index = 0; index < state.Rows.Count; index++)
                {
                    total++;

                    SOShipLineSplit retained =
                        PXResult.Unwrap<SOShipLineSplit>(state.Rows[index]);

                    if (retained == null)
                    {
                        unwrapFailed++;
                        continue;
                    }

                    if (retained.IsUnassigned.GetValueOrDefault())
                    {
                        unassigned++;
                    }
                    else
                    {
                        assigned++;
                    }

                    decimal? initialPackedQty =
                        state.InitialPackedQuantities != null &&
                        index < state.InitialPackedQuantities.Count
                            ? state.InitialPackedQuantities[index]
                            : null;

                    if (!Nullable.Equals(
                        initialPackedQty,
                        retained.PackedQty))
                    {
                        retainedQtyChanged++;
                    }

                    SOShipLineSplit canonical =
                        splitCache.Locate(retained) as SOShipLineSplit;

                    if (canonical == null)
                    {
                        canonicalMissing++;
                        continue;
                    }

                    canonicalFound++;

                    if (ReferenceEquals(retained, canonical))
                    {
                        sameReference++;
                    }
                    else
                    {
                        differentReference++;
                    }

                    if (!Nullable.Equals(
                        retained.PackedQty,
                        canonical.PackedQty))
                    {
                        packedQtyMismatch++;
                    }
                }

                int inserted = splitCache.Inserted
                    .Cast<object>()
                    .Select(row =>
                        PXResult.Unwrap<SOShipLineSplit>(row))
                    .Count(split => IsForShipment(split, state.ShipmentNbr));

                int deleted = splitCache.Deleted
                    .Cast<object>()
                    .Select(row =>
                        PXResult.Unwrap<SOShipLineSplit>(row))
                    .Count(split => IsForShipment(split, state.ShipmentNbr));

                bool sameBasis = ReferenceEquals(state.Basis, basis);
                bool sameShipment = string.Equals(
                    state.ShipmentNbr,
                    basis?.RefNbr,
                    StringComparison.OrdinalIgnoreCase);

                PickPackShip.PackMode.Logic currentMode =
                    basis?.Get<PickPackShip.PackMode.Logic>();

                bool sameContext =
                    string.Equals(
                        state.Mode,
                        basis?.Header?.Mode,
                        StringComparison.Ordinal) &&
                    state.PackageLineNbr == currentMode?.PackageLineNbr &&
                    state.PackageLineNbrUI == currentMode?.PackageLineNbrUI &&
                    state.Remove ==
                        (basis?.Remove.GetValueOrDefault() ?? false);

                // Only the ordinary Pack mode has been proven safe. Special
                // worksheet/paperless modes use their own CanPack semantics.
                bool supportedMode =
                    string.Equals(state.Mode, "PACK", StringComparison.Ordinal);

                bool safeCandidate =
                    sameBasis &&
                    sameShipment &&
                    sameContext &&
                    supportedMode &&
                    state.PackageLineNbr != null &&
                    total > 0 &&
                    unwrapFailed == 0 &&
                    unassigned == 0 &&
                    canonicalFound == assigned &&
                    sameReference == assigned &&
                    differentReference == 0 &&
                    canonicalMissing == 0 &&
                    retainedQtyChanged > 0 &&
                    packedQtyMismatch == 0 &&
                    inserted == 0 &&
                    deleted == 0;

                if (safeCandidate)
                {
                    PXTrace.WriteInformation(
                        "[PFP-CACHE-REUSE] Retained=True; Shipment={0}; Mode={1}; Remove={2}; Rows={3}; Changed={4}.",
                        state.ShipmentNbr,
                        state.Mode,
                        state.Remove,
                        total,
                        retainedQtyChanged);

                    return;
                }

                Invalidate();

                PXTrace.WriteInformation(
                    "[PFP-CACHE-REUSE] Retained=False; Shipment={0}; Mode={1}; Remove={2}; Rows={3}; Changed={4}; Unassigned={5}; UnwrapFailed={6}; DifferentReference={7}; CanonicalMissing={8}; PackedQtyMismatch={9}; SplitInserted={10}; SplitDeleted={11}; SameBasis={12}; SameShipment={13}; SameContext={14}; SupportedMode={15}.",
                    state.ShipmentNbr,
                    state.Mode,
                    state.Remove,
                    total,
                    retainedQtyChanged,
                    unassigned,
                    unwrapFailed,
                    differentReference,
                    canonicalMissing,
                    packedQtyMismatch,
                    inserted,
                    deleted,
                    sameBasis,
                    sameShipment,
                    sameContext,
                    supportedMode);
            }
            catch (Exception exception)
            {
                // Validation must never replace or hide the scan result.
                Invalidate();

                PXTrace.WriteInformation(
                    "[PFP-CACHE-REUSE] Retained=False; ValidationError={0}; Message={1}.",
                    exception.GetType().FullName,
                    exception.Message);
            }
        }

        private static bool IsForShipment(
            SOShipLineSplit split,
            string shipmentNbr)
        {
            return split != null &&
                string.Equals(
                    split.ShipmentNbr,
                    shipmentNbr,
                    StringComparison.OrdinalIgnoreCase);
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
    /// which changes SOShipLineSplitPackage.PackedQty and synchronously updates
    /// canonical SOShipLineSplit rows. Reuse is allowed only after the strict
    /// post-confirmation validation succeeds; all other paths invalidate.
    /// </summary>
    public class ConfirmStatePickedForPackConditionalReuseExt
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
            FlowStatus result;

            try
            {
                result = baseMethod();
            }
            catch
            {
                PickedForPackRequestCache.Invalidate();
                throw;
            }

            PickedForPackRequestCache.ValidateAfterConfirm(Basis);

            return result;
        }
    }
}
