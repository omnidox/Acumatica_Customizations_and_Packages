using System;
using System.Collections.Generic;
using System.Linq;
using PX.BarcodeProcessing;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.SO.WMS;

using WmsShipmentExt = WMS.SOShipmentEntryExt;

namespace CustomWMS2
{
    /// <summary>
    /// Adds an Unpack Entire Shipment command to Pick, Pack, and Ship.
    ///
    /// The command removes all package-content allocations belonging to
    /// the current shipment through the standard WMS PackSplit logic.
    ///
    /// PackSplit performs removal when it receives a negative quantity.
    ///
    /// Empty SOPackageDetailEx carton records are preserved.
    /// </summary>
    public class UnpackEntireShipmentCommandExtension
        : PickPackShip.ScanExtension
    {
        private const string TracePrefix =
            "[UnpackEntireShipmentCommand]";

        private const string Version =
            "2026-07-16-V3-NEGATIVE-PACKSPLIT-REMOVE-01";

        public static bool IsActive()
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} IsActive TRUE. Version={Version}");

            return true;
        }

        public sealed class UnpackEntireShipmentCommand
            : PickPackShip.ScanCommand
        {
            public override string Code =>
                "UNPACKENTIRESHIPMENT";

            public override string ButtonName =>
                "UnpackEntireShipment";

            public override string DisplayName =>
                "Unpack Entire Shipment";

            protected override bool IsEnabled
            {
                get
                {
                    if (!WmsTransferAuthorization.IsAuthorized())
                        return false;

                    SOShipment shipment =
                        Basis?.Graph?.Document?.Current;

                    if (shipment == null ||
                        string.IsNullOrWhiteSpace(
                            shipment.ShipmentNbr))
                    {
                        return false;
                    }

                    /*
                     * Do not allow shipment-wide unpacking after
                     * the shipment has been confirmed.
                     */
                    if (shipment.Confirmed == true)
                        return false;

                    return true;
                }
            }

            protected override bool Process()
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Process ENTER. " +
                    $"Version={Version}, Command={Code}");

                /*
                 * Server-side authorization check.
                 */
                if (!WmsTransferAuthorization.IsAuthorized())
                {
                    Basis.ReportError(
                        "You do not have permission to unpack an entire shipment.");

                    WmsDebugTrace.Warning(
                        $"{TracePrefix} Unauthorized attempt. " +
                        $"Command={Code}");

                    return true;
                }

                SOShipment shipment =
                    Basis.Graph.Document.Current;

                string shipmentNbr =
                    shipment?.ShipmentNbr
                    ?? Basis.RefNbr;

                if (string.IsNullOrWhiteSpace(shipmentNbr))
                {
                    Basis.ReportError(
                        "No shipment was selected.");

                    WmsDebugTrace.Warning(
                        $"{TracePrefix} No shipment selected.");

                    return true;
                }

                if (shipment?.Confirmed == true)
                {
                    Basis.ReportError(
                        "The shipment is confirmed and cannot be unpacked.");

                    WmsDebugTrace.Warning(
                        $"{TracePrefix} Confirmed shipment blocked. " +
                        $"ShipmentNbr={shipmentNbr}");

                    return true;
                }

                PickPackShip.PackMode.Logic packLogic =
                    Basis.Get<PickPackShip.PackMode.Logic>();

                SOPackageDetailEx originalPackage =
                    packLogic?.SelectedPackage
                    ?? Basis.Graph.Packages.Current;

                ShipmentUnpackLongRunData operationData =
                    new ShipmentUnpackLongRunData
                    {
                        ShipmentNbr = shipmentNbr,
                        OriginalPackageLineNbr =
                            originalPackage?.LineNbr
                    };

                /*
                 * Use the native barcode-driven WMS long-running
                 * operation mechanism.
                 *
                 * The WMS screen enters the WAIT state while the cloned
                 * graph processes the shipment.
                 */
                Basis
                    .WaitFor<ShipmentUnpackLongRunData>(
                        (longRunBasis, data) =>
                        {
                            ExecuteShipmentWideUnpack(
                                longRunBasis,
                                data);
                        })
                    .WithDescription(
                        "Unpacking all cartons for shipment {0}.",
                        shipmentNbr)
                    .OnSuccess(success =>
                        success
                            .Say(
                                "Shipment-wide unpacking operation completed.")
                            .ResetFull()
                            .Do(
                                (completedBasis, data) =>
                                {
                                    if (data.NothingWasPacked)
                                    {
                                        completedBasis.ReportWarning(
                                            "The shipment did not contain any packed carton contents.");
                                    }
                                    else
                                    {
                                        completedBasis.ReportInfo(
                                            "Unpacked the entire shipment. " +
                                            "Cartons={0}, Rows={1}, Qty={2}.",
                                            data.AffectedCartons,
                                            data.RemovedRows,
                                            data.RemovedQty);
                                    }

                                    RequestShipmentRefresh(
                                        completedBasis,
                                        "Shipment-wide unpack completed");

                                    WmsDebugTrace.Info(
                                        $"{TracePrefix} Long operation SUCCESS. " +
                                        $"ShipmentNbr={data.ShipmentNbr}, " +
                                        $"Cartons={data.AffectedCartons}, " +
                                        $"Rows={data.RemovedRows}, " +
                                        $"Qty={data.RemovedQty}");
                                }))
                    .OnFail(fail =>
                        fail
                            .Say(
                                "Shipment-wide unpacking failed. " +
                                "Review the error and try again.")
                            .Do(
                                (failedBasis, data) =>
                                {
                                    WmsDebugTrace.Error(
                                        $"{TracePrefix} Long operation FAILED. " +
                                        $"ShipmentNbr={data?.ShipmentNbr}");
                                }))
                    .BeginAwait(operationData);

                WmsDebugTrace.Info(
                    $"{TracePrefix} Long operation started. " +
                    $"ShipmentNbr={shipmentNbr}");

                return true;
            }

            private static void ExecuteShipmentWideUnpack(
                PickPackShip longRunBasis,
                ShipmentUnpackLongRunData data)
            {
                if (longRunBasis == null)
                {
                    throw new PXException(
                        "The WMS processing context could not be created.");
                }

                if (data == null ||
                    string.IsNullOrWhiteSpace(
                        data.ShipmentNbr))
                {
                    throw new PXException(
                        "The shipment number was not supplied to the unpack operation.");
                }

                /*
                 * Repeat authorization inside the long-running operation.
                 */
                if (!WmsTransferAuthorization.IsAuthorized())
                {
                    throw new PXException(
                        "You do not have permission to unpack an entire shipment.");
                }

                string shipmentNbr =
                    data.ShipmentNbr;

                WmsDebugTrace.Info(
                    $"{TracePrefix} Long operation ENTER. " +
                    $"Version={Version}, " +
                    $"ShipmentNbr={shipmentNbr}");

                SOShipment shipment =
                    PXSelect<
                        SOShipment,
                        Where<
                            SOShipment.shipmentNbr,
                            Equal<
                                Required<
                                    SOShipment.shipmentNbr>>>>
                    .Select(
                        longRunBasis,
                        shipmentNbr)
                    .RowCast<SOShipment>()
                    .FirstOrDefault();

                if (shipment == null)
                {
                    throw new PXException(
                        "Shipment {0} could not be found.",
                        shipmentNbr);
                }

                if (shipment.Confirmed == true)
                {
                    throw new PXException(
                        "Shipment {0} is confirmed and cannot be unpacked.",
                        shipmentNbr);
                }

                /*
                 * Set the cloned graph's current shipment.
                 */
                longRunBasis.Graph.Document.Current =
                    shipment;

                PickPackShip.PackMode.Logic packLogic =
                    longRunBasis.Get<
                        PickPackShip.PackMode.Logic>();

                PickPackShip.PackMode.ConfirmState.Logic confirmLogic =
                    longRunBasis.Get<
                        PickPackShip.PackMode.ConfirmState.Logic>();

                if (packLogic == null ||
                    confirmLogic == null)
                {
                    throw new PXException(
                        "The Acumatica Pack mode logic could not be initialized.");
                }

                /*
                 * Load all cartons belonging to this shipment.
                 *
                 * These SOPackageDetailEx records are preserved after
                 * their package-content rows have been removed.
                 */
                List<SOPackageDetailEx> packages =
                    longRunBasis.Graph.Packages
                        .SelectMain()
                        .Where(package =>
                            string.Equals(
                                package.ShipmentNbr,
                                shipmentNbr,
                                StringComparison.OrdinalIgnoreCase) &&
                            package.LineNbr != null)
                        .OrderBy(package =>
                            package.LineNbr)
                        .ToList();

                if (packages.Count == 0)
                {
                    throw new PXException(
                        "No cartons were found for shipment {0}.",
                        shipmentNbr);
                }

                Dictionary<int?, SOPackageDetailEx>
                    packageByLineNbr =
                        packages
                            .GroupBy(package =>
                                package.LineNbr)
                            .ToDictionary(
                                group => group.Key,
                                group => group.First());

                /*
                 * Take a snapshot of all package-content rows before
                 * modifying the PackageDetailSplit view.
                 *
                 * Do not enumerate a PXView while deleting rows from it.
                 */
                List<SOShipLineSplitPackage> packedRows =
                    PXSelectReadonly<
                        SOShipLineSplitPackage,
                        Where<
                            SOShipLineSplitPackage.shipmentNbr,
                            Equal<
                                Required<
                                    SOShipLineSplitPackage.shipmentNbr>>>>
                    .Select(
                        longRunBasis,
                        shipmentNbr)
                    .RowCast<SOShipLineSplitPackage>()
                    .Where(row =>
                        row.PackageLineNbr != null &&
                        row.ShipmentLineNbr != null &&
                        row.ShipmentSplitLineNbr != null &&
                        (row.PackedQty ?? 0m) > 0m)
                    .OrderBy(row =>
                        row.PackageLineNbr)
                    .ThenBy(row =>
                        row.ShipmentLineNbr)
                    .ThenBy(row =>
                        row.ShipmentSplitLineNbr)
                    .ThenBy(row =>
                        row.RecordID)
                    .ToList();

                if (packedRows.Count == 0)
                {
                    data.NothingWasPacked = true;

                    WmsDebugTrace.Info(
                        $"{TracePrefix} No package-content rows found. " +
                        $"ShipmentNbr={shipmentNbr}");

                    return;
                }

                /*
                 * Load shipment splits using the complete identifying key:
                 *
                 * ShipmentNbr + LineNbr + SplitLineNbr
                 *
                 * Because the query is already limited to one shipment,
                 * the dictionary key only requires LineNbr + SplitLineNbr.
                 */
                Dictionary<ShipmentSplitKey, SOShipLineSplit>
                    splitByKey =
                        LoadShipmentSplits(
                            longRunBasis,
                            shipmentNbr);

                /*
                 * Validate all references before changing any records.
                 */
                ValidateWorkItems(
                    packedRows,
                    packageByLineNbr,
                    splitByKey,
                    shipmentNbr);

                int removedRows = 0;
                decimal removedQty = 0m;

                HashSet<int?> affectedPackages =
                    new HashSet<int?>();

                /*
                 * The complete operation runs in one transaction.
                 *
                 * If PackSplit or Save throws an exception, Complete()
                 * is not called and the database changes roll back.
                 */
                using (PXTransactionScope transaction =
                    new PXTransactionScope())
                {
                    foreach (
                        SOShipLineSplitPackage packedRow
                        in packedRows)
                    {
                        SOPackageDetailEx package =
                            packageByLineNbr[
                                packedRow.PackageLineNbr];

                        ShipmentSplitKey splitKey =
                            new ShipmentSplitKey(
                                packedRow.ShipmentLineNbr,
                                packedRow.ShipmentSplitLineNbr);

                        SOShipLineSplit split =
                            splitByKey[splitKey];

                        /*
                         * Keep this value positive for validation,
                         * reporting, totals, and user-facing messages.
                         */
                        decimal qtyToRemove =
                            packedRow.PackedQty ?? 0m;

                        if (qtyToRemove <= 0m)
                            continue;

                        /*
                         * PackSplit performs unpacking when the supplied
                         * quantity is negative.
                         */
                        decimal signedRemovalQty =
                            -qtyToRemove;

                        /*
                         * Do not modify a confirmed carton.
                         */
                        if (package.Confirmed == true)
                        {
                            throw new PXException(
                                "Carton {0} is confirmed and cannot be unpacked.",
                                GetCartonIdentifier(
                                    package));
                        }

                        /*
                         * Establish the package context used by PackSplit.
                         */
                        longRunBasis.Graph.Packages.Current =
                            package;

                        packLogic.PackageLineNbrUI =
                            package.LineNbr;

                        WmsDebugTrace.Info(
                            $"{TracePrefix} Removing package content. " +
                            $"ShipmentNbr={shipmentNbr}, " +
                            $"PackageLineNbr={package.LineNbr}, " +
                            $"CartonNbr={GetCartonIdentifier(package)}, " +
                            $"RecordID={packedRow.RecordID}, " +
                            $"ShipmentLineNbr={split.LineNbr}, " +
                            $"ShipmentSplitLineNbr={split.SplitLineNbr}, " +
                            $"InventoryID={split.InventoryID}, " +
                            $"CurrentPackagePackedQty={packedRow.PackedQty}, " +
                            $"QtyToRemove={qtyToRemove}, " +
                            $"SignedPackSplitQty={signedRemovalQty}");

                        /*
                         * Critical behavior:
                         *
                         * PackSplit checks whether qty is less than zero.
                         * A negative quantity invokes its native removal
                         * branch.
                         *
                         * If the package-content quantity reaches zero,
                         * PackSplit deletes the SOShipLineSplitPackage row.
                         *
                         * The SOPackageDetailEx carton itself is not deleted.
                         */
                        bool removed =
                            confirmLogic.PackSplit(
                                split,
                                package,
                                signedRemovalQty);

                        if (!removed)
                        {
                            throw new PXException(
                                "Unable to remove inventory {0} from carton {1}.",
                                GetInventoryCD(
                                    longRunBasis,
                                    split.InventoryID),
                                GetCartonIdentifier(
                                    package));
                        }

                        removedRows++;

                        /*
                         * Accumulate the positive amount removed, not the
                         * negative PackSplit argument.
                         */
                        removedQty +=
                            qtyToRemove;

                        affectedPackages.Add(
                            package.LineNbr);

                        WmsDebugTrace.Info(
                            $"{TracePrefix} Package content removed. " +
                            $"ShipmentNbr={shipmentNbr}, " +
                            $"PackageLineNbr={package.LineNbr}, " +
                            $"RecordID={packedRow.RecordID}, " +
                            $"RemovedQty={qtyToRemove}");
                    }

                    if (removedRows == 0)
                    {
                        throw new PXException(
                            "No package contents could be removed.");
                    }

                    /*
                     * Restore the carton that was selected before the
                     * shipment-wide operation.
                     */
                    SOPackageDetailEx packageToRestore =
                        null;

                    if (data.OriginalPackageLineNbr != null)
                    {
                        packageByLineNbr.TryGetValue(
                            data.OriginalPackageLineNbr,
                            out packageToRestore);
                    }

                    packageToRestore =
                        packageToRestore
                        ?? packages.FirstOrDefault();

                    if (packageToRestore != null)
                    {
                        longRunBasis.Graph.Packages.Current =
                            packageToRestore;

                        packLogic.PackageLineNbrUI =
                            packageToRestore.LineNbr;
                    }

                    /*
                     * Save the package-content removals once after every
                     * row has been processed.
                     *
                     * No SOPackageDetailEx carton rows are deleted.
                     */
                    longRunBasis.Save.Press();

                    transaction.Complete();
                }

                data.AffectedCartons =
                    affectedPackages.Count;

                data.RemovedRows =
                    removedRows;

                data.RemovedQty =
                    removedQty;

                WmsDebugTrace.Info(
                    $"{TracePrefix} Long operation EXIT. " +
                    $"ShipmentNbr={shipmentNbr}, " +
                    $"Cartons={data.AffectedCartons}, " +
                    $"Rows={data.RemovedRows}, " +
                    $"Qty={data.RemovedQty}");
            }

            /// <summary>
            /// Loads shipment splits and indexes them using the
            /// composite LineNbr + SplitLineNbr key.
            /// </summary>
            private static Dictionary<
                ShipmentSplitKey,
                SOShipLineSplit>
                LoadShipmentSplits(
                    PickPackShip basis,
                    string shipmentNbr)
            {
                List<SOShipLineSplit> splits =
                    PXSelectReadonly<
                        SOShipLineSplit,
                        Where<
                            SOShipLineSplit.shipmentNbr,
                            Equal<
                                Required<
                                    SOShipLineSplit.shipmentNbr>>>>
                    .Select(
                        basis,
                        shipmentNbr)
                    .RowCast<SOShipLineSplit>()
                    .Where(split =>
                        split.LineNbr != null &&
                        split.SplitLineNbr != null)
                    .ToList();

                return splits
                    .GroupBy(split =>
                        new ShipmentSplitKey(
                            split.LineNbr,
                            split.SplitLineNbr))
                    .ToDictionary(
                        group => group.Key,
                        group => group.First());
            }

            /// <summary>
            /// Validates every package and shipment-split reference
            /// before the transaction changes package content.
            /// </summary>
            private static void ValidateWorkItems(
                IEnumerable<SOShipLineSplitPackage> packedRows,
                IDictionary<int?, SOPackageDetailEx> packages,
                IDictionary<
                    ShipmentSplitKey,
                    SOShipLineSplit> splits,
                string shipmentNbr)
            {
                foreach (
                    SOShipLineSplitPackage packedRow
                    in packedRows)
                {
                    if (!packages.ContainsKey(
                        packedRow.PackageLineNbr))
                    {
                        throw new PXException(
                            "Package line {0} could not be found for shipment {1}.",
                            packedRow.PackageLineNbr,
                            shipmentNbr);
                    }

                    ShipmentSplitKey splitKey =
                        new ShipmentSplitKey(
                            packedRow.ShipmentLineNbr,
                            packedRow.ShipmentSplitLineNbr);

                    if (!splits.ContainsKey(
                        splitKey))
                    {
                        throw new PXException(
                            "Shipment split could not be found. " +
                            "Shipment={0}, Line={1}, Split={2}.",
                            shipmentNbr,
                            packedRow.ShipmentLineNbr,
                            packedRow.ShipmentSplitLineNbr);
                    }
                }
            }

            private static string GetInventoryCD(
                PickPackShip basis,
                int? inventoryID)
            {
                if (inventoryID == null)
                    return string.Empty;

                InventoryItem item =
                    PXSelectReadonly<
                        InventoryItem,
                        Where<
                            InventoryItem.inventoryID,
                            Equal<
                                Required<
                                    InventoryItem.inventoryID>>>>
                    .Select(
                        basis,
                        inventoryID)
                    .RowCast<InventoryItem>()
                    .FirstOrDefault();

                return item?.InventoryCD?.Trim()
                    ?? string.Empty;
            }

            private static string GetCartonIdentifier(
                SOPackageDetailEx package)
            {
                if (package == null)
                    return string.Empty;

                /*
                 * LineNbr is available without introducing a dependency
                 * on a separate SOPackageDetail DAC extension.
                 */
                return package.LineNbr?.ToString()
                    ?? string.Empty;
            }

            private static void RequestShipmentRefresh(
                PickPackShip basis,
                string reason)
            {
                if (basis?.Graph == null)
                    return;

                WmsShipmentExt wmsExt =
                    basis.Graph.GetExtension<WmsShipmentExt>();

                if (wmsExt?.SelectedPackageContentsView != null)
                {
                    wmsExt.SelectedPackageContentsView
                        .Cache.Clear();

                    wmsExt.SelectedPackageContentsView
                        .View.Clear();

                    wmsExt.SelectedPackageContentsView
                        .View.RequestRefresh();
                }

                basis.Graph.PackageDetailExt
                    .PackageDetailSplit.Cache.Clear();

                basis.Graph.PackageDetailExt
                    .PackageDetailSplit.View.Clear();

                basis.Graph.PackageDetailExt
                    .PackageDetailSplit.View.RequestRefresh();

                /*
                 * Refresh the carton view without deleting any
                 * SOPackageDetailEx records.
                 */
                basis.Graph.Packages.Cache.Clear();
                basis.Graph.Packages.View.Clear();
                basis.Graph.Packages.View.RequestRefresh();

                WmsDebugTrace.Info(
                    $"{TracePrefix} Refresh requested. " +
                    $"Reason={reason}");
            }

            /// <summary>
            /// Composite key corresponding to the shipment line number
            /// and shipment split line number.
            /// </summary>
            private struct ShipmentSplitKey
                : IEquatable<ShipmentSplitKey>
            {
                public ShipmentSplitKey(
                    int? shipmentLineNbr,
                    int? shipmentSplitLineNbr)
                {
                    ShipmentLineNbr =
                        shipmentLineNbr;

                    ShipmentSplitLineNbr =
                        shipmentSplitLineNbr;
                }

                public int? ShipmentLineNbr { get; }

                public int? ShipmentSplitLineNbr { get; }

                public bool Equals(
                    ShipmentSplitKey other)
                {
                    return
                        ShipmentLineNbr ==
                            other.ShipmentLineNbr &&
                        ShipmentSplitLineNbr ==
                            other.ShipmentSplitLineNbr;
                }

                public override bool Equals(
                    object obj)
                {
                    return
                        obj is ShipmentSplitKey &&
                        Equals(
                            (ShipmentSplitKey)obj);
                }

                public override int GetHashCode()
                {
                    unchecked
                    {
                        int hash = 17;

                        hash =
                            (hash * 23) +
                            ShipmentLineNbr.GetHashCode();

                        hash =
                            (hash * 23) +
                            ShipmentSplitLineNbr.GetHashCode();

                        return hash;
                    }
                }

                public override string ToString()
                {
                    return
                        $"Line={ShipmentLineNbr}, " +
                        $"Split={ShipmentSplitLineNbr}";
                }
            }

            [Serializable]
            private sealed class ShipmentUnpackLongRunData
            {
                public string ShipmentNbr { get; set; }

                public int? OriginalPackageLineNbr { get; set; }

                public int AffectedCartons { get; set; }

                public int RemovedRows { get; set; }

                public decimal RemovedQty { get; set; }

                public bool NothingWasPacked { get; set; }
            }
        }

        [PXOverride]
        public virtual ScanMode<PickPackShip> DecorateScanMode(
            ScanMode<PickPackShip> original,
            Func<
                ScanMode<PickPackShip>,
                ScanMode<PickPackShip>>
                base_DecorateScanMode)
        {
            ScanMode<PickPackShip> mode =
                base_DecorateScanMode(
                    original);

            PickPackShip.PackMode packMode =
                mode as PickPackShip.PackMode;

            if (packMode != null)
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Appending shipment-wide unpack command. " +
                    $"Version={Version}, " +
                    $"Authorized={WmsTransferAuthorization.IsAuthorized()}");

                /*
                 * Always register the command to keep the WMS command
                 * schema consistent.
                 *
                 * IsEnabled and Process both enforce authorization.
                 */
                packMode.Intercept.CreateCommands.ByAppend(
                    basis => new PickPackShip.ScanCommand[]
                    {
                        new UnpackEntireShipmentCommand()
                    });
            }

            return mode;
        }
    }
}