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
        private const string Version = "2026-07-08-POC-TRANSFER-TOP-EXPECTED-01";

        public static bool IsActive()
        {
            WmsDebugTrace.Info($"{TracePrefix} IsActive TRUE. Version={Version}");
            return true;
        }

        public sealed class TransferTopExpectedRowCommand : PickPackShip.ScanCommand
        {
            public override string Code => "TRANSFEREXPECTED";
            public override string ButtonName => "TransferTopExpectedRow";
            public override string DisplayName => "Transfer Top Expected Row";

            protected override bool IsEnabled => true;

            protected override bool Process()
            {
                WmsDebugTrace.Info($"{TracePrefix} Process ENTER. Version={Version}");

                PickPackShip.PackMode.Logic packLogic =
                    Basis.Get<PickPackShip.PackMode.Logic>();

                PickPackShip.PackMode.ConfirmState.Logic confirmLogic =
                    Basis.Get<PickPackShip.PackMode.ConfirmState.Logic>();

                SOPackageDetailEx package = packLogic?.SelectedPackage;

                if (package == null || package.ShipmentNbr == null || package.LineNbr == null)
                {
                    Basis.ReportError("No selected package was found.");
                    WmsDebugTrace.Warning($"{TracePrefix} No valid selected package.");
                    return true;
                }

                WmsPlan expectedRow = GetFirstIncompleteExpectedRow(package);

                if (expectedRow == null)
                {
                    Basis.ReportWarning("No remaining expected content was found for this package.");
                    WmsDebugTrace.Info($"{TracePrefix} No incomplete expected row found.");
                    return true;
                }

                if (expectedRow.ShipmentSplitLineNbr == null)
                {
                    Basis.ReportError("The expected row does not have a shipment split line number.");
                    WmsDebugTrace.Warning($"{TracePrefix} Expected row missing ShipmentSplitLineNbr.");
                    return true;
                }

                SOShipLineSplit split = GetShipmentSplit(
                    expectedRow.ShipmentNbr,
                    expectedRow.ShipmentSplitLineNbr);

                if (split == null)
                {
                    Basis.ReportError("The matching shipment split was not found.");
                    WmsDebugTrace.Warning(
                        $"{TracePrefix} Matching SOShipLineSplit not found. ShipmentNbr={expectedRow.ShipmentNbr}, SplitLineNbr={expectedRow.ShipmentSplitLineNbr}");
                    return true;
                }

                decimal transferQty = GetRemainingQtyForPackage(package, expectedRow);

                if (transferQty <= 0m)
                {
                    Basis.ReportWarning("The selected expected row has no remaining quantity to transfer.");
                    WmsDebugTrace.Info($"{TracePrefix} Remaining qty is zero.");
                    return true;
                }

                decimal? targetQty = confirmLogic.TargetQty(split);
                decimal currentPackedOnSplit = split.PackedQty ?? 0m;

                if (targetQty != null)
                {
                    decimal availableOnSplit = targetQty.Value - currentPackedOnSplit;

                    if (availableOnSplit <= 0m)
                    {
                        Basis.ReportWarning("The matching shipment split is already fully packed.");
                        WmsDebugTrace.Info($"{TracePrefix} Split already fully packed.");
                        return true;
                    }

                    if (transferQty > availableOnSplit)
                        transferQty = availableOnSplit;
                }

                WmsDebugTrace.Info(
                    $"{TracePrefix} Calling PackSplit. ShipmentNbr={split.ShipmentNbr}, LineNbr={split.LineNbr}, SplitLineNbr={split.SplitLineNbr}, InventoryID={split.InventoryID}, LotSerialNbr={split.LotSerialNbr}, PackageLineNbr={package.LineNbr}, TransferQty={transferQty}");

                bool packed = confirmLogic.PackSplit(split, package, transferQty);

                if (!packed)
                {
                    Basis.ReportError("The selected expected row could not be transferred.");
                    WmsDebugTrace.Warning($"{TracePrefix} PackSplit returned false.");
                    return true;
                }

                confirmLogic.EnsureShipmentUserLinkForPack();

                packLogic.PackageLineNbrUI = package.LineNbr;
                Basis.Graph.Packages.Current = package;

                Basis.Save.Press();

                RequestEstimatedContentRefresh("Manual expected-row transfer");

                Basis.ReportInfo(
                    "Transferred {0} x {1} to the selected package.",
                    GetInventoryCD(split.InventoryID),
                    transferQty);

                WmsDebugTrace.Info($"{TracePrefix} Process EXIT success.");
                return true;
            }

            private WmsPlan GetFirstIncompleteExpectedRow(SOPackageDetailEx package)
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
                    .FirstOrDefault();
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

            private void RequestEstimatedContentRefresh(string reason)
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

        [PXOverride]
        public virtual ScanMode<PickPackShip> DecorateScanMode(
            ScanMode<PickPackShip> original,
            Func<ScanMode<PickPackShip>, ScanMode<PickPackShip>> base_DecorateScanMode)
        {
            ScanMode<PickPackShip> mode = base_DecorateScanMode(original);

            PickPackShip.PackMode packMode = mode as PickPackShip.PackMode;

            if (packMode != null)
            {
                WmsDebugTrace.Info($"{TracePrefix} Appending TransferTopExpectedRowCommand to PackMode.");

                packMode.Intercept.CreateCommands.ByAppend(basis => new PickPackShip.ScanCommand[]
                {
                    new TransferTopExpectedRowCommand()
                });
            }

            return mode;
        }
    }
}