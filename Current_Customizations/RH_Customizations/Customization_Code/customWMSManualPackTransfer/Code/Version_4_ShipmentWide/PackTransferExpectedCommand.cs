using System;
using System.Collections.Generic;
using System.Linq;
using PX.BarcodeProcessing;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.SO.WMS;

using WmsShipmentExt = WMS.SOShipmentEntryExt;
using WmsPlan = WMS.SelectedPackageContents;

namespace CustomWMS2
{
    public class PackTransferExpectedCommand : PickPackShip.ScanExtension
    {
        private const string TracePrefix =
            "[PackTransferExpectedCommand]";

        private const string Version =
            "2026-07-13-V7-SHIPMENT-WIDE-PACK-01";

        public static bool IsActive()
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} IsActive TRUE. Version={Version}");

            return true;
        }

        public abstract class TransferExpectedBaseCommand
            : PickPackShip.ScanCommand
        {
            protected abstract bool TransferAllRemaining { get; }

            protected override bool IsEnabled =>
                WmsTransferAuthorization.IsAuthorized();

            protected override bool Process()
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Process ENTER. " +
                    $"Version={Version}, Command={Code}");

                if (!WmsTransferAuthorization.IsAuthorized())
                {
                    Basis.ReportError(
                        "You do not have permission to use manual package transfer.");

                    WmsDebugTrace.Warning(
                        $"{TracePrefix} Unauthorized transfer attempt. " +
                        $"Command={Code}");

                    return true;
                }

                PickPackShip.PackMode.Logic packLogic =
                    Basis.Get<PickPackShip.PackMode.Logic>();

                PickPackShip.PackMode.ConfirmState.Logic confirmLogic =
                    Basis.Get<PickPackShip.PackMode.ConfirmState.Logic>();

                SOPackageDetailEx package = packLogic?.SelectedPackage;

                if (package == null ||
                    package.ShipmentNbr == null ||
                    package.LineNbr == null)
                {
                    Basis.ReportError(
                        "No selected package was found.");

                    WmsDebugTrace.Warning(
                        $"{TracePrefix} No valid selected package. " +
                        $"Command={Code}");

                    return true;
                }

                List<WmsPlan> rowsToTransfer =
                    TransferAllRemaining
                        ? GetAllRemainingExpectedRows(package)
                        : GetSelectedExpectedRow(package).SingleToList();

                if (rowsToTransfer.Count == 0)
                {
                    Basis.ReportWarning(
                        TransferAllRemaining
                            ? "No remaining expected content was found for this package."
                            : "Please select an expected content row with remaining quantity first.");

                    WmsDebugTrace.Info(
                        $"{TracePrefix} No rows available to transfer. " +
                        $"Command={Code}");

                    return true;
                }

                int transferredRows = 0;
                decimal transferredTotalQty = 0m;

                foreach (WmsPlan expectedRow in rowsToTransfer)
                {
                    if (expectedRow?.ShipmentSplitLineNbr == null)
                    {
                        WmsDebugTrace.Warning(
                            $"{TracePrefix} Skipping row because " +
                            $"ShipmentSplitLineNbr is missing.");

                        continue;
                    }

                    SOShipLineSplit split = GetShipmentSplit(
                        expectedRow.ShipmentNbr,
                        expectedRow.ShipmentSplitLineNbr);

                    if (split == null)
                    {
                        WmsDebugTrace.Warning(
                            $"{TracePrefix} Skipping row because matching " +
                            $"SOShipLineSplit was not found. " +
                            $"ShipmentNbr={expectedRow.ShipmentNbr}, " +
                            $"SplitLineNbr={expectedRow.ShipmentSplitLineNbr}");

                        continue;
                    }

                    decimal transferQty =
                        GetRemainingQtyForPackage(
                            package,
                            expectedRow);

                    if (transferQty <= 0m)
                    {
                        WmsDebugTrace.Info(
                            $"{TracePrefix} Skipping row because remaining " +
                            $"qty is zero. " +
                            $"ShipmentNbr={expectedRow.ShipmentNbr}, " +
                            $"SplitLineNbr={expectedRow.ShipmentSplitLineNbr}");

                        continue;
                    }

                    decimal? targetQty =
                        confirmLogic.TargetQty(split);

                    decimal currentPackedOnSplit =
                        split.PackedQty ?? 0m;

                    if (targetQty != null)
                    {
                        decimal availableOnSplit =
                            targetQty.Value - currentPackedOnSplit;

                        if (availableOnSplit <= 0m)
                        {
                            WmsDebugTrace.Info(
                                $"{TracePrefix} Skipping row because split " +
                                $"is already fully packed. " +
                                $"ShipmentNbr={split.ShipmentNbr}, " +
                                $"SplitLineNbr={split.SplitLineNbr}");

                            continue;
                        }

                        transferQty =
                            Math.Min(
                                transferQty,
                                availableOnSplit);
                    }

                    if (transferQty <= 0m)
                        continue;

                    WmsDebugTrace.Info(
                        $"{TracePrefix} Calling PackSplit. " +
                        $"Command={Code}, " +
                        $"ShipmentNbr={split.ShipmentNbr}, " +
                        $"LineNbr={split.LineNbr}, " +
                        $"SplitLineNbr={split.SplitLineNbr}, " +
                        $"InventoryID={split.InventoryID}, " +
                        $"LotSerialNbr={split.LotSerialNbr}, " +
                        $"PackageLineNbr={package.LineNbr}, " +
                        $"TransferQty={transferQty}");

                    bool packed =
                        confirmLogic.PackSplit(
                            split,
                            package,
                            transferQty);

                    if (!packed)
                    {
                        WmsDebugTrace.Warning(
                            $"{TracePrefix} PackSplit returned false. " +
                            $"ShipmentNbr={split.ShipmentNbr}, " +
                            $"SplitLineNbr={split.SplitLineNbr}, " +
                            $"TransferQty={transferQty}");

                        continue;
                    }

                    transferredRows++;
                    transferredTotalQty += transferQty;
                }

                if (transferredRows == 0)
                {
                    Basis.ReportWarning(
                        "No rows were transferred.");

                    WmsDebugTrace.Warning(
                        $"{TracePrefix} No rows were successfully transferred. " +
                        $"Command={Code}");

                    return true;
                }

                confirmLogic.EnsureShipmentUserLinkForPack();

                packLogic.PackageLineNbrUI =
                    package.LineNbr;

                Basis.Graph.Packages.Current =
                    package;

                Basis.Save.Press();

                RequestRefresh(
                    TransferAllRemaining
                        ? "Manual transfer all remaining expected rows"
                        : "Manual selected expected-row transfer");

                Basis.ReportInfo(
                    TransferAllRemaining
                        ? "Transferred all remaining expected content. Rows={0}, Qty={1}."
                        : "Transferred selected expected row. Rows={0}, Qty={1}.",
                    transferredRows,
                    transferredTotalQty);

                WmsDebugTrace.Info(
                    $"{TracePrefix} Process EXIT success. " +
                    $"Command={Code}, " +
                    $"Rows={transferredRows}, " +
                    $"Qty={transferredTotalQty}");

                return true;
            }

            private WmsPlan GetSelectedExpectedRow(
                SOPackageDetailEx package)
            {
                WmsShipmentExt wmsExt =
                    Basis.Graph.GetExtension<WmsShipmentExt>();

                WmsPlan current =
                    wmsExt?.SelectedPackageContentsView?.Current
                    ?? Basis.Graph.Caches<WmsPlan>()?.Current as WmsPlan;

                if (current == null)
                    return null;

                if (!string.Equals(
                    current.ShipmentNbr,
                    package.ShipmentNbr,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                if (current.PackageLineNbr != package.LineNbr)
                    return null;

                if (GetRemainingQtyForPackage(
                    package,
                    current) <= 0m)
                {
                    return null;
                }

                return current;
            }

            private List<WmsPlan> GetAllRemainingExpectedRows(
                SOPackageDetailEx package)
            {
                List<WmsPlan> plannedRows =
                    PXSelectReadonly<
                        WmsPlan,
                        Where<
                            WmsPlan.shipmentNbr,
                            Equal<Required<WmsPlan.shipmentNbr>>,
                            And<
                                WmsPlan.packageLineNbr,
                                Equal<
                                    Required<
                                        WmsPlan.packageLineNbr>>>>>
                    .Select(
                        Basis,
                        package.ShipmentNbr,
                        package.LineNbr)
                    .RowCast<WmsPlan>()
                    .ToList();

                return plannedRows
                    .Where(row =>
                        GetRemainingQtyForPackage(
                            package,
                            row) > 0m)
                    .OrderBy(row => row.DefaultIssueFrom)
                    .ThenBy(row => row.OrderNbr)
                    .ThenBy(row => row.StoreNbr)
                    .ThenBy(row =>
                        GetInventoryCD(row.InventoryID))
                    .ThenBy(row => row.LotSerialNbr)
                    .ToList();
            }

            private decimal GetRemainingQtyForPackage(
                SOPackageDetailEx package,
                WmsPlan row)
            {
                decimal expectedQty =
                    row.PackedQty ?? 0m;

                decimal actualQty =
                    PXSelectReadonly<
                        SOShipLineSplitPackage,
                        Where<
                            SOShipLineSplitPackage.shipmentNbr,
                            Equal<
                                Required<
                                    SOShipLineSplitPackage.shipmentNbr>>,
                            And<
                                SOShipLineSplitPackage.packageLineNbr,
                                Equal<
                                    Required<
                                        SOShipLineSplitPackage.packageLineNbr>>,
                                And<
                                    SOShipLineSplitPackage.shipmentSplitLineNbr,
                                    Equal<
                                        Required<
                                            SOShipLineSplitPackage.shipmentSplitLineNbr>>>>>>
                    .Select(
                        Basis,
                        package.ShipmentNbr,
                        package.LineNbr,
                        row.ShipmentSplitLineNbr)
                    .RowCast<SOShipLineSplitPackage>()
                    .Sum(x => x.PackedQty ?? 0m);

                decimal remaining =
                    expectedQty - actualQty;

                return remaining > 0m
                    ? remaining
                    : 0m;
            }

            private SOShipLineSplit GetShipmentSplit(
                string shipmentNbr,
                int? splitLineNbr)
            {
                return PXSelect<
                    SOShipLineSplit,
                    Where<
                        SOShipLineSplit.shipmentNbr,
                        Equal<
                            Required<
                                SOShipLineSplit.shipmentNbr>>,
                        And<
                            SOShipLineSplit.splitLineNbr,
                            Equal<
                                Required<
                                    SOShipLineSplit.splitLineNbr>>>>>
                    .Select(
                        Basis,
                        shipmentNbr,
                        splitLineNbr)
                    .RowCast<SOShipLineSplit>()
                    .FirstOrDefault();
            }

            private string GetInventoryCD(
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
                        Basis,
                        inventoryID)
                    .RowCast<InventoryItem>()
                    .FirstOrDefault();

                return item?.InventoryCD?.Trim()
                    ?? string.Empty;
            }

            private void RequestRefresh(
                string reason)
            {
                WmsShipmentExt wmsExt =
                    Basis.Graph.GetExtension<WmsShipmentExt>();

                if (wmsExt?.SelectedPackageContentsView != null)
                {
                    wmsExt.SelectedPackageContentsView.Cache.Clear();
                    wmsExt.SelectedPackageContentsView.View.Clear();
                    wmsExt.SelectedPackageContentsView.View.RequestRefresh();
                }

                Basis.Graph.PackageDetailExt
                    .PackageDetailSplit.Cache.Clear();

                Basis.Graph.PackageDetailExt
                    .PackageDetailSplit.View.Clear();

                Basis.Graph.PackageDetailExt
                    .PackageDetailSplit.View.RequestRefresh();

                Basis.Graph.Packages.View.RequestRefresh();

                WmsDebugTrace.Info(
                    $"{TracePrefix} Refresh requested. " +
                    $"Reason={reason}");
            }
        }

        /// <summary>
        /// Packs every remaining expected row in every carton
        /// belonging to the current shipment.
        /// </summary>
        public sealed class PackEntireShipmentCommand
            : PickPackShip.ScanCommand
        {
            public override string Code =>
                "PACKENTIRESHIPMENT";

            public override string ButtonName =>
                "PackEntireShipment";

            public override string DisplayName =>
                "Pack Entire Shipment";

            protected override bool IsEnabled
            {
                get
                {
                    if (!WmsTransferAuthorization.IsAuthorized())
                        return false;

                    SOShipment shipment =
                        Basis?.Graph?.Document?.Current;

                    return shipment != null &&
                           !string.IsNullOrWhiteSpace(
                               shipment.ShipmentNbr);
                }
            }

            protected override bool Process()
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Shipment-wide Process ENTER. " +
                    $"Version={Version}, Command={Code}");

                if (!WmsTransferAuthorization.IsAuthorized())
                {
                    Basis.ReportError(
                        "You do not have permission to pack an entire shipment.");

                    WmsDebugTrace.Warning(
                        $"{TracePrefix} Unauthorized shipment-wide " +
                        $"pack attempt. Command={Code}");

                    return true;
                }

                PickPackShip.PackMode.Logic packLogic =
                    Basis.Get<PickPackShip.PackMode.Logic>();

                PickPackShip.PackMode.ConfirmState.Logic confirmLogic =
                    Basis.Get<PickPackShip.PackMode.ConfirmState.Logic>();

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
                        $"{TracePrefix} Shipment-wide packing blocked " +
                        $"because no shipment was selected.");

                    return true;
                }

                SOPackageDetailEx originalPackage =
                    packLogic?.SelectedPackage
                    ?? Basis.Graph.Packages.Current;

                List<SOPackageDetailEx> packages =
                    Basis.Graph.Packages
                        .SelectMain()
                        .Where(package =>
                            string.Equals(
                                package.ShipmentNbr,
                                shipmentNbr,
                                StringComparison.OrdinalIgnoreCase) &&
                            package.LineNbr != null)
                        .OrderBy(package => package.LineNbr)
                        .ToList();

                if (packages.Count == 0)
                {
                    Basis.ReportError(
                        "No cartons were found for the selected shipment.");

                    WmsDebugTrace.Warning(
                        $"{TracePrefix} No packages found. " +
                        $"ShipmentNbr={shipmentNbr}");

                    return true;
                }

                Dictionary<int?, SOPackageDetailEx> packageByLineNbr =
                    packages
                        .GroupBy(package => package.LineNbr)
                        .ToDictionary(
                            group => group.Key,
                            group => group.First());

                List<WmsPlan> expectedRows =
                    PXSelectReadonly<
                        WmsPlan,
                        Where<
                            WmsPlan.shipmentNbr,
                            Equal<
                                Required<
                                    WmsPlan.shipmentNbr>>>>
                    .Select(
                        Basis,
                        shipmentNbr)
                    .RowCast<WmsPlan>()
                    .Where(row =>
                        row.PackageLineNbr != null &&
                        row.ShipmentSplitLineNbr != null)
                    .OrderBy(row => row.PackageLineNbr)
                    .ThenBy(row => row.DefaultIssueFrom)
                    .ThenBy(row => row.OrderNbr)
                    .ThenBy(row => row.StoreNbr)
                    .ThenBy(row => row.InventoryID)
                    .ThenBy(row => row.LotSerialNbr)
                    .ToList();

                if (expectedRows.Count == 0)
                {
                    Basis.ReportWarning(
                        "No expected carton contents were found for the selected shipment.");

                    WmsDebugTrace.Info(
                        $"{TracePrefix} No expected rows found. " +
                        $"ShipmentNbr={shipmentNbr}");

                    return true;
                }

                List<SOShipLineSplitPackage> actualRows =
                    PXSelectReadonly<
                        SOShipLineSplitPackage,
                        Where<
                            SOShipLineSplitPackage.shipmentNbr,
                            Equal<
                                Required<
                                    SOShipLineSplitPackage.shipmentNbr>>>>
                    .Select(
                        Basis,
                        shipmentNbr)
                    .RowCast<SOShipLineSplitPackage>()
                    .ToList();

                Dictionary<PackageSplitKey, decimal>
                    actualQtyByPackageAndSplit =
                        actualRows
                            .GroupBy(row =>
                                new PackageSplitKey(
                                    row.PackageLineNbr,
                                    row.ShipmentSplitLineNbr))
                            .ToDictionary(
                                group => group.Key,
                                group => group.Sum(
                                    row => row.PackedQty ?? 0m));

                Dictionary<int?, decimal>
                    totalActualQtyBySplit =
                        actualRows
                            .GroupBy(row =>
                                row.ShipmentSplitLineNbr)
                            .ToDictionary(
                                group => group.Key,
                                group => group.Sum(
                                    row => row.PackedQty ?? 0m));

                Dictionary<int?, SOShipLineSplit> splitByLineNbr =
                    new Dictionary<int?, SOShipLineSplit>();

                List<ShipmentPackWorkItem> workItems =
                    new List<ShipmentPackWorkItem>();

                foreach (WmsPlan expectedRow in expectedRows)
                {
                    SOPackageDetailEx package;

                    if (!packageByLineNbr.TryGetValue(
                        expectedRow.PackageLineNbr,
                        out package))
                    {
                        Basis.ReportError(
                            "Expected content references carton {0}, but that carton was not found.",
                            expectedRow.PackageLineNbr);

                        WmsDebugTrace.Warning(
                            $"{TracePrefix} Missing package. " +
                            $"ShipmentNbr={shipmentNbr}, " +
                            $"PackageLineNbr={expectedRow.PackageLineNbr}");

                        return true;
                    }

                    PackageSplitKey key =
                        new PackageSplitKey(
                            expectedRow.PackageLineNbr,
                            expectedRow.ShipmentSplitLineNbr);

                    decimal actualQty = 0m;

                    actualQtyByPackageAndSplit.TryGetValue(
                        key,
                        out actualQty);

                    decimal expectedQty =
                        expectedRow.PackedQty ?? 0m;

                    decimal remainingQty =
                        expectedQty - actualQty;

                    if (remainingQty <= 0m)
                        continue;

                    SOShipLineSplit split;

                    if (!splitByLineNbr.TryGetValue(
                        expectedRow.ShipmentSplitLineNbr,
                        out split))
                    {
                        split = GetShipmentSplit(
                            shipmentNbr,
                            expectedRow.ShipmentSplitLineNbr);

                        if (split == null)
                        {
                            Basis.ReportError(
                                "The matching shipment split was not found for carton {0}.",
                                expectedRow.PackageLineNbr);

                            WmsDebugTrace.Warning(
                                $"{TracePrefix} Missing SOShipLineSplit. " +
                                $"ShipmentNbr={shipmentNbr}, " +
                                $"PackageLineNbr={expectedRow.PackageLineNbr}, " +
                                $"SplitLineNbr={expectedRow.ShipmentSplitLineNbr}");

                            return true;
                        }

                        splitByLineNbr[
                            expectedRow.ShipmentSplitLineNbr] =
                                split;
                    }

                    workItems.Add(
                        new ShipmentPackWorkItem
                        {
                            ExpectedRow = expectedRow,
                            Package = package,
                            Split = split,
                            RemainingQty = remainingQty
                        });
                }

                if (workItems.Count == 0)
                {
                    Basis.ReportWarning(
                        "All expected carton contents are already packed.");

                    WmsDebugTrace.Info(
                        $"{TracePrefix} Shipment already fully packed. " +
                        $"ShipmentNbr={shipmentNbr}");

                    return true;
                }

                int transferredRows = 0;
                decimal transferredTotalQty = 0m;

                HashSet<int?> affectedPackageLineNbrs =
                    new HashSet<int?>();

                foreach (ShipmentPackWorkItem workItem in workItems)
                {
                    SOShipLineSplit split =
                        workItem.Split;

                    SOPackageDetailEx package =
                        workItem.Package;

                    decimal transferQty =
                        workItem.RemainingQty;

                    decimal? targetQty =
                        confirmLogic.TargetQty(split);

                    decimal currentActualOnSplit = 0m;

                    totalActualQtyBySplit.TryGetValue(
                        split.SplitLineNbr,
                        out currentActualOnSplit);

                    if (targetQty != null)
                    {
                        decimal availableOnSplit =
                            targetQty.Value -
                            currentActualOnSplit;

                        if (availableOnSplit <= 0m)
                        {
                            WmsDebugTrace.Info(
                                $"{TracePrefix} Shipment-wide row skipped " +
                                $"because split is fully packed. " +
                                $"ShipmentNbr={shipmentNbr}, " +
                                $"PackageLineNbr={package.LineNbr}, " +
                                $"SplitLineNbr={split.SplitLineNbr}");

                            continue;
                        }

                        transferQty =
                            Math.Min(
                                transferQty,
                                availableOnSplit);
                    }

                    if (transferQty <= 0m)
                        continue;

                    /*
                     * Make the carton being processed the active package.
                     * This preserves the same package context used by the
                     * normal Acumatica Pack workflow.
                     */
                    Basis.Graph.Packages.Current =
                        package;

                    packLogic.PackageLineNbrUI =
                        package.LineNbr;

                    WmsDebugTrace.Info(
                        $"{TracePrefix} Shipment-wide PackSplit. " +
                        $"ShipmentNbr={shipmentNbr}, " +
                        $"PackageLineNbr={package.LineNbr}, " +
                        $"LineNbr={split.LineNbr}, " +
                        $"SplitLineNbr={split.SplitLineNbr}, " +
                        $"InventoryID={split.InventoryID}, " +
                        $"LotSerialNbr={split.LotSerialNbr}, " +
                        $"TransferQty={transferQty}");

                    bool packed =
                        confirmLogic.PackSplit(
                            split,
                            package,
                            transferQty);

                    if (!packed)
                    {
                        Basis.ReportError(
                            "Unable to pack inventory {0} into carton {1}.",
                            GetInventoryCD(split.InventoryID),
                            package.LineNbr);

                        WmsDebugTrace.Warning(
                            $"{TracePrefix} Shipment-wide PackSplit " +
                            $"returned false. " +
                            $"ShipmentNbr={shipmentNbr}, " +
                            $"PackageLineNbr={package.LineNbr}, " +
                            $"SplitLineNbr={split.SplitLineNbr}, " +
                            $"TransferQty={transferQty}");

                        return true;
                    }

                    totalActualQtyBySplit[
                        split.SplitLineNbr] =
                            currentActualOnSplit + transferQty;

                    transferredRows++;
                    transferredTotalQty += transferQty;

                    affectedPackageLineNbrs.Add(
                        package.LineNbr);
                }

                if (transferredRows == 0)
                {
                    Basis.ReportWarning(
                        "No shipment contents were packed.");

                    return true;
                }

                confirmLogic.EnsureShipmentUserLinkForPack();

                /*
                 * Restore the package that was selected before the
                 * shipment-wide operation.
                 */
                SOPackageDetailEx packageToRestore =
                    originalPackage
                    ?? packages.FirstOrDefault();

                if (packageToRestore != null)
                {
                    Basis.Graph.Packages.Current =
                        packageToRestore;

                    packLogic.PackageLineNbrUI =
                        packageToRestore.LineNbr;
                }

                Basis.Save.Press();

                RequestShipmentRefresh(
                    "Shipment-wide automatic packing");

                Basis.ReportInfo(
                    "Packed the entire shipment. Cartons={0}, Rows={1}, Qty={2}.",
                    affectedPackageLineNbrs.Count,
                    transferredRows,
                    transferredTotalQty);

                WmsDebugTrace.Info(
                    $"{TracePrefix} Shipment-wide Process EXIT success. " +
                    $"ShipmentNbr={shipmentNbr}, " +
                    $"Cartons={affectedPackageLineNbrs.Count}, " +
                    $"Rows={transferredRows}, " +
                    $"Qty={transferredTotalQty}");

                return true;
            }

            private SOShipLineSplit GetShipmentSplit(
                string shipmentNbr,
                int? splitLineNbr)
            {
                return PXSelect<
                    SOShipLineSplit,
                    Where<
                        SOShipLineSplit.shipmentNbr,
                        Equal<
                            Required<
                                SOShipLineSplit.shipmentNbr>>,
                        And<
                            SOShipLineSplit.splitLineNbr,
                            Equal<
                                Required<
                                    SOShipLineSplit.splitLineNbr>>>>>
                    .Select(
                        Basis,
                        shipmentNbr,
                        splitLineNbr)
                    .RowCast<SOShipLineSplit>()
                    .FirstOrDefault();
            }

            private string GetInventoryCD(
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
                        Basis,
                        inventoryID)
                    .RowCast<InventoryItem>()
                    .FirstOrDefault();

                return item?.InventoryCD?.Trim()
                    ?? string.Empty;
            }

            private void RequestShipmentRefresh(
                string reason)
            {
                WmsShipmentExt wmsExt =
                    Basis.Graph.GetExtension<WmsShipmentExt>();

                if (wmsExt?.SelectedPackageContentsView != null)
                {
                    wmsExt.SelectedPackageContentsView.Cache.Clear();
                    wmsExt.SelectedPackageContentsView.View.Clear();
                    wmsExt.SelectedPackageContentsView.View.RequestRefresh();
                }

                Basis.Graph.PackageDetailExt
                    .PackageDetailSplit.Cache.Clear();

                Basis.Graph.PackageDetailExt
                    .PackageDetailSplit.View.Clear();

                Basis.Graph.PackageDetailExt
                    .PackageDetailSplit.View.RequestRefresh();

                Basis.Graph.Packages.Cache.Clear();
                Basis.Graph.Packages.View.Clear();
                Basis.Graph.Packages.View.RequestRefresh();

                WmsDebugTrace.Info(
                    $"{TracePrefix} Shipment refresh requested. " +
                    $"Reason={reason}");
            }

            private sealed class ShipmentPackWorkItem
            {
                public WmsPlan ExpectedRow { get; set; }

                public SOPackageDetailEx Package { get; set; }

                public SOShipLineSplit Split { get; set; }

                public decimal RemainingQty { get; set; }
            }

            private struct PackageSplitKey
                : IEquatable<PackageSplitKey>
            {
                public PackageSplitKey(
                    int? packageLineNbr,
                    int? shipmentSplitLineNbr)
                {
                    PackageLineNbr = packageLineNbr;
                    ShipmentSplitLineNbr =
                        shipmentSplitLineNbr;
                }

                public int? PackageLineNbr { get; }

                public int? ShipmentSplitLineNbr { get; }

                public bool Equals(
                    PackageSplitKey other)
                {
                    return
                        PackageLineNbr ==
                            other.PackageLineNbr &&
                        ShipmentSplitLineNbr ==
                            other.ShipmentSplitLineNbr;
                }

                public override bool Equals(
                    object obj)
                {
                    return obj is PackageSplitKey &&
                           Equals((PackageSplitKey)obj);
                }

                public override int GetHashCode()
                {
                    unchecked
                    {
                        int hash = 17;

                        hash =
                            (hash * 23) +
                            PackageLineNbr.GetHashCode();

                        hash =
                            (hash * 23) +
                            ShipmentSplitLineNbr.GetHashCode();

                        return hash;
                    }
                }
            }
        }

        public sealed class TransferSelectedRowCommand
            : TransferExpectedBaseCommand
        {
            public override string Code =>
                "TRANSFERSELECTED";

            public override string ButtonName =>
                "TransferSelectedExpectedRow";

            public override string DisplayName =>
                "Transfer Selected Row";

            protected override bool TransferAllRemaining =>
                false;
        }

        public sealed class TransferAllRemainingCommand
            : TransferExpectedBaseCommand
        {
            public override string Code =>
                "TRANSFERALLREMAINING";

            public override string ButtonName =>
                "TransferAllRemainingExpectedRows";

            public override string DisplayName =>
                "Transfer All Remaining";

            protected override bool TransferAllRemaining =>
                true;
        }

        [PXOverride]
        public virtual ScanMode<PickPackShip> DecorateScanMode(
            ScanMode<PickPackShip> original,
            Func<
                ScanMode<PickPackShip>,
                ScanMode<PickPackShip>> base_DecorateScanMode)
        {
            ScanMode<PickPackShip> mode =
                base_DecorateScanMode(original);

            PickPackShip.PackMode packMode =
                mode as PickPackShip.PackMode;

            if (packMode != null)
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Appending transfer commands to PackMode. " +
                    $"Authorized={WmsTransferAuthorization.IsAuthorized()}");

                packMode.Intercept.CreateCommands.ByAppend(
                    basis => new PickPackShip.ScanCommand[]
                    {
                        new TransferSelectedRowCommand(),
                        new TransferAllRemainingCommand(),
                        new PackEntireShipmentCommand()
                    });
            }

            return mode;
        }
    }

    internal static class WmsTransferExtensions
    {
        public static List<T> SingleToList<T>(
            this T item)
        {
            return item == null
                ? new List<T>()
                : new List<T> { item };
        }
    }
}