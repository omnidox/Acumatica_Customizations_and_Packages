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
        private const string TracePrefix = "[PackTransferExpectedCommand]";
        private const string Version = "2026-07-09-V5-SELECTED-AND-ALL-REMAINING-01";

        public static bool IsActive()
        {
            WmsDebugTrace.Info($"{TracePrefix} IsActive TRUE. Version={Version}");
            return true;
        }

        public abstract class TransferExpectedBaseCommand : PickPackShip.ScanCommand
        {
            protected abstract bool TransferAllRemaining { get; }

            protected override bool IsEnabled => true;

            protected override bool Process()
            {
                WmsDebugTrace.Info($"{TracePrefix} Process ENTER. Version={Version}, Command={Code}");

                PickPackShip.PackMode.Logic packLogic =
                    Basis.Get<PickPackShip.PackMode.Logic>();

                PickPackShip.PackMode.ConfirmState.Logic confirmLogic =
                    Basis.Get<PickPackShip.PackMode.ConfirmState.Logic>();

                SOPackageDetailEx package = packLogic?.SelectedPackage;

                if (package == null || package.ShipmentNbr == null || package.LineNbr == null)
                {
                    Basis.ReportError("No selected package was found.");
                    return true;
                }

                List<WmsPlan> rowsToTransfer = TransferAllRemaining
                    ? GetAllRemainingExpectedRows(package)
                    : GetSelectedExpectedRow(package).SingleToList();

                if (rowsToTransfer.Count == 0)
                {
                    Basis.ReportWarning(
                        TransferAllRemaining
                            ? "No remaining expected content was found for this package."
                            : "Please select an expected content row with remaining quantity first.");
                    return true;
                }

                int transferredRows = 0;
                decimal transferredTotalQty = 0m;

                foreach (WmsPlan expectedRow in rowsToTransfer)
                {
                    if (expectedRow?.ShipmentSplitLineNbr == null)
                    {
                        WmsDebugTrace.Warning($"{TracePrefix} Skipping row because ShipmentSplitLineNbr is missing.");
                        continue;
                    }

                    SOShipLineSplit split = GetShipmentSplit(
                        expectedRow.ShipmentNbr,
                        expectedRow.ShipmentSplitLineNbr);

                    if (split == null)
                    {
                        WmsDebugTrace.Warning(
                            $"{TracePrefix} Skipping row because matching SOShipLineSplit was not found. ShipmentNbr={expectedRow.ShipmentNbr}, SplitLineNbr={expectedRow.ShipmentSplitLineNbr}");
                        continue;
                    }

                    decimal transferQty = GetRemainingQtyForPackage(package, expectedRow);

                    if (transferQty <= 0m)
                    {
                        WmsDebugTrace.Info($"{TracePrefix} Skipping row because remaining qty is zero.");
                        continue;
                    }

                    decimal? targetQty = confirmLogic.TargetQty(split);
                    decimal currentPackedOnSplit = split.PackedQty ?? 0m;

                    if (targetQty != null)
                    {
                        decimal availableOnSplit = targetQty.Value - currentPackedOnSplit;

                        if (availableOnSplit <= 0m)
                        {
                            WmsDebugTrace.Info($"{TracePrefix} Skipping row because split is already fully packed.");
                            continue;
                        }

                        transferQty = Math.Min(transferQty, availableOnSplit);
                    }

                    if (transferQty <= 0m)
                        continue;

                    WmsDebugTrace.Info(
                        $"{TracePrefix} Calling PackSplit. Command={Code}, ShipmentNbr={split.ShipmentNbr}, LineNbr={split.LineNbr}, SplitLineNbr={split.SplitLineNbr}, InventoryID={split.InventoryID}, LotSerialNbr={split.LotSerialNbr}, PackageLineNbr={package.LineNbr}, TransferQty={transferQty}");

                    bool packed = confirmLogic.PackSplit(split, package, transferQty);

                    if (!packed)
                    {
                        WmsDebugTrace.Warning(
                            $"{TracePrefix} PackSplit returned false. ShipmentNbr={split.ShipmentNbr}, SplitLineNbr={split.SplitLineNbr}, TransferQty={transferQty}");
                        continue;
                    }

                    transferredRows++;
                    transferredTotalQty += transferQty;
                }

                if (transferredRows == 0)
                {
                    Basis.ReportWarning("No rows were transferred.");
                    return true;
                }

                confirmLogic.EnsureShipmentUserLinkForPack();

                packLogic.PackageLineNbrUI = package.LineNbr;
                Basis.Graph.Packages.Current = package;

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
                    $"{TracePrefix} Process EXIT success. Command={Code}, Rows={transferredRows}, Qty={transferredTotalQty}");

                return true;
            }

            private WmsPlan GetSelectedExpectedRow(SOPackageDetailEx package)
            {
                WmsShipmentExt wmsExt = Basis.Graph.GetExtension<WmsShipmentExt>();

                WmsPlan current =
                    wmsExt?.SelectedPackageContentsView?.Current
                    ?? Basis.Graph.Caches<WmsPlan>()?.Current as WmsPlan;

                if (current == null)
                    return null;

                if (!string.Equals(current.ShipmentNbr, package.ShipmentNbr, StringComparison.OrdinalIgnoreCase))
                    return null;

                if (current.PackageLineNbr != package.LineNbr)
                    return null;

                if (GetRemainingQtyForPackage(package, current) <= 0m)
                    return null;

                return current;
            }

            private List<WmsPlan> GetAllRemainingExpectedRows(SOPackageDetailEx package)
            {
                List<WmsPlan> plannedRows =
                    PXSelectReadonly<
                        WmsPlan,
                        Where<
                            WmsPlan.shipmentNbr, Equal<Required<WmsPlan.shipmentNbr>>,
                            And<WmsPlan.packageLineNbr, Equal<Required<WmsPlan.packageLineNbr>>>>>
                    .Select(Basis, package.ShipmentNbr, package.LineNbr)
                    .RowCast<WmsPlan>()
                    .ToList();

                return plannedRows
                    .Where(row => GetRemainingQtyForPackage(package, row) > 0m)
                    .OrderBy(row => row.DefaultIssueFrom)
                    .ThenBy(row => row.OrderNbr)
                    .ThenBy(row => row.StoreNbr)
                    .ThenBy(row => GetInventoryCD(row.InventoryID))
                    .ThenBy(row => row.LotSerialNbr)
                    .ToList();
            }

            private decimal GetRemainingQtyForPackage(SOPackageDetailEx package, WmsPlan row)
            {
                decimal expectedQty = row.PackedQty ?? 0m;

                decimal actualQty =
                    PXSelectReadonly<
                        SOShipLineSplitPackage,
                        Where<
                            SOShipLineSplitPackage.shipmentNbr, Equal<Required<SOShipLineSplitPackage.shipmentNbr>>,
                            And<SOShipLineSplitPackage.packageLineNbr, Equal<Required<SOShipLineSplitPackage.packageLineNbr>>,
                            And<SOShipLineSplitPackage.shipmentSplitLineNbr, Equal<Required<SOShipLineSplitPackage.shipmentSplitLineNbr>>>>>>
                    .Select(Basis, package.ShipmentNbr, package.LineNbr, row.ShipmentSplitLineNbr)
                    .RowCast<SOShipLineSplitPackage>()
                    .Sum(x => x.PackedQty ?? 0m);

                decimal remaining = expectedQty - actualQty;

                return remaining > 0m ? remaining : 0m;
            }

            private SOShipLineSplit GetShipmentSplit(string shipmentNbr, int? splitLineNbr)
            {
                return PXSelect<
                    SOShipLineSplit,
                    Where<
                        SOShipLineSplit.shipmentNbr, Equal<Required<SOShipLineSplit.shipmentNbr>>,
                        And<SOShipLineSplit.splitLineNbr, Equal<Required<SOShipLineSplit.splitLineNbr>>>>>
                .Select(Basis, shipmentNbr, splitLineNbr)
                .RowCast<SOShipLineSplit>()
                .FirstOrDefault();
            }

            private string GetInventoryCD(int? inventoryID)
            {
                if (inventoryID == null)
                    return string.Empty;

                InventoryItem item =
                    PXSelectReadonly<
                        InventoryItem,
                        Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>
                    .Select(Basis, inventoryID)
                    .RowCast<InventoryItem>()
                    .FirstOrDefault();

                return item?.InventoryCD?.Trim() ?? string.Empty;
            }

            private void RequestRefresh(string reason)
            {
                WmsShipmentExt wmsExt = Basis.Graph.GetExtension<WmsShipmentExt>();

                if (wmsExt?.SelectedPackageContentsView != null)
                {
                    wmsExt.SelectedPackageContentsView.Cache.Clear();
                    wmsExt.SelectedPackageContentsView.View.Clear();
                    wmsExt.SelectedPackageContentsView.View.RequestRefresh();
                }

                Basis.Graph.PackageDetailExt.PackageDetailSplit.Cache.Clear();
                Basis.Graph.PackageDetailExt.PackageDetailSplit.View.Clear();
                Basis.Graph.PackageDetailExt.PackageDetailSplit.View.RequestRefresh();

                WmsDebugTrace.Info($"{TracePrefix} Refresh requested. Reason={reason}");
            }
        }

        public sealed class TransferSelectedRowCommand : TransferExpectedBaseCommand
        {
            public override string Code => "TRANSFERSELECTED";
            public override string ButtonName => "TransferSelectedExpectedRow";
            public override string DisplayName => "Transfer Selected Row";

            protected override bool TransferAllRemaining => false;
        }

        public sealed class TransferAllRemainingCommand : TransferExpectedBaseCommand
        {
            public override string Code => "TRANSFERALLREMAINING";
            public override string ButtonName => "TransferAllRemainingExpectedRows";
            public override string DisplayName => "Transfer All Remaining";

            protected override bool TransferAllRemaining => true;
        }

        [PXOverride]
        public virtual ScanMode<PickPackShip> DecorateScanMode(
            ScanMode<PickPackShip> original,
            Func<ScanMode<PickPackShip>, ScanMode<PickPackShip>> base_DecorateScanMode)
        {
            ScanMode<PickPackShip> mode = base_DecorateScanMode(original);

            PickPackShip.PackMode packMode = mode as PickPackShip.PackMode;

            if (packMode != null)
            {
                WmsDebugTrace.Info($"{TracePrefix} Appending transfer commands to PackMode.");

                packMode.Intercept.CreateCommands.ByAppend(basis => new PickPackShip.ScanCommand[]
                {
                    new TransferSelectedRowCommand(),
                    new TransferAllRemainingCommand()
                });
            }

            return mode;
        }
    }

    internal static class WmsTransferExtensions
    {
        public static List<T> SingleToList<T>(this T item)
        {
            return item == null ? new List<T>() : new List<T> { item };
        }
    }
}