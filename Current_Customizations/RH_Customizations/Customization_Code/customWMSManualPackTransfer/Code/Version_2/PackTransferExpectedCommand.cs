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
        private const string Version = "2026-07-09-V2-SELECTED-ROW-QTY-TRANSFER-01";

        public static bool IsActive()
        {
            WmsDebugTrace.Info($"{TracePrefix} IsActive TRUE. Version={Version}");
            return true;
        }

        public abstract class TransferExpectedBaseCommand : PickPackShip.ScanCommand
        {
            protected abstract bool UseEnteredQty { get; }

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

                WmsPlan expectedRow = GetSelectedExpectedRow(package);

                if (expectedRow == null)
                {
                    Basis.ReportError("Please select an expected content row first.");
                    return true;
                }

                if (expectedRow.ShipmentSplitLineNbr == null)
                {
                    Basis.ReportError("The selected expected row does not have a shipment split line number.");
                    return true;
                }

                SOShipLineSplit split = GetShipmentSplit(
                    expectedRow.ShipmentNbr,
                    expectedRow.ShipmentSplitLineNbr);

                if (split == null)
                {
                    Basis.ReportError("The matching shipment split was not found.");
                    return true;
                }

                decimal remainingQty = GetRemainingQtyForPackage(package, expectedRow);

                if (remainingQty <= 0m)
                {
                    Basis.ReportWarning("The selected expected row has no remaining quantity to transfer.");
                    return true;
                }

                decimal transferQty = remainingQty;

                if (UseEnteredQty)
                {
                    decimal enteredQty = Basis.Qty.GetValueOrDefault();

                    if (enteredQty <= 0m)
                    {
                        Basis.ReportError("Please enter a quantity before using Transfer Qty From Selected Row.");
                        return true;
                    }

                    transferQty = Math.Min(enteredQty, remainingQty);
                }

                decimal? targetQty = confirmLogic.TargetQty(split);
                decimal currentPackedOnSplit = split.PackedQty ?? 0m;

                if (targetQty != null)
                {
                    decimal availableOnSplit = targetQty.Value - currentPackedOnSplit;

                    if (availableOnSplit <= 0m)
                    {
                        Basis.ReportWarning("The matching shipment split is already fully packed.");
                        return true;
                    }

                    transferQty = Math.Min(transferQty, availableOnSplit);
                }

                if (transferQty <= 0m)
                {
                    Basis.ReportWarning("There is no quantity available to transfer.");
                    return true;
                }

                WmsDebugTrace.Info(
                    $"{TracePrefix} Calling PackSplit. Command={Code}, ShipmentNbr={split.ShipmentNbr}, LineNbr={split.LineNbr}, SplitLineNbr={split.SplitLineNbr}, InventoryID={split.InventoryID}, LotSerialNbr={split.LotSerialNbr}, PackageLineNbr={package.LineNbr}, RemainingQty={remainingQty}, TransferQty={transferQty}");

                bool packed = confirmLogic.PackSplit(split, package, transferQty);

                if (!packed)
                {
                    Basis.ReportError("The selected expected row could not be transferred.");
                    return true;
                }

                confirmLogic.EnsureShipmentUserLinkForPack();

                packLogic.PackageLineNbrUI = package.LineNbr;
                Basis.Graph.Packages.Current = package;

                Basis.Save.Press();

                RequestRefresh("Manual selected expected-row transfer");

                Basis.ReportInfo(
                    "Transferred {0} x {1} to the selected package.",
                    GetInventoryCD(split.InventoryID),
                    transferQty);

                WmsDebugTrace.Info($"{TracePrefix} Process EXIT success. Command={Code}");
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

            protected override bool UseEnteredQty => false;
        }

        public sealed class TransferQtyFromSelectedRowCommand : TransferExpectedBaseCommand
        {
            public override string Code => "TRANSFERSELECTEDQTY";
            public override string ButtonName => "TransferQtyFromSelectedExpectedRow";
            public override string DisplayName => "Transfer Qty From Selected Row";

            protected override bool UseEnteredQty => true;
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
                    new TransferQtyFromSelectedRowCommand()
                });
            }

            return mode;
        }
    }
}