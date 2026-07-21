using System;
using System.Collections.Generic;
using System.Linq;
using PX.BarcodeProcessing;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.SO.WMS;
using PX.Objects.IN;

using WmsPlan = WMS.SelectedPackageContents;

namespace CustomWMS
{
    public class PackTopRowOnlyScanGuard
        : BarcodeDrivenStateMachine<PickPackShip, PickPackShip.Host>
            .ScanExtension<PickPackShip.PackMode.ConfirmState.Logic>
    {
        private const string TracePrefix = "[PackTopRowOnlyScanGuard]";
        private const string Version = "2026-06-17-CUSTOMER-BYPASS-01";

        public static bool IsActive()
        {
            WmsDebugTrace.Info($"{TracePrefix} IsActive TRUE. Version={Version}");
            return true;
        }

        [PXOverride]
        public virtual FlowStatus Confirm(Func<FlowStatus> base_Confirm)
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} Confirm ENTER. Version={Version}, Remove={Basis.Remove}, InventoryID={Basis.InventoryID}, LotSerialNbr={Basis.LotSerialNbr}, Qty={Basis.Qty}, BaseQty={Basis.BaseQty}, UOM={Basis.UOM}");

            PickPackShip.PackMode.Logic packLogic =
                Basis.Get<PickPackShip.PackMode.Logic>();

            SOPackageDetailEx package = packLogic?.SelectedPackage;

            if (ShouldBypassTopRowWorkflow(package))
            {
                WmsDebugTrace.Info($"{TracePrefix} Confirm bypassed top-row scan guard by customer.");
                return base_Confirm();
            }

            if (!Basis.Remove.GetValueOrDefault())
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Confirm package. HasPackLogic={packLogic != null}, HasPackage={package != null}, PackageShipmentNbr={package?.ShipmentNbr}, PackageLineNbr={package?.LineNbr}, PackageLineNbrUI={packLogic?.PackageLineNbrUI}");

                if (package != null && Basis.InventoryID != null)
                {
                    WmsPlan expectedTopRow = GetExpectedTopRow(package);

                    TraceExpectedVsScanned("Confirm", expectedTopRow);

                    if (expectedTopRow != null &&
                        expectedTopRow.InventoryID.GetValueOrDefault() != Basis.InventoryID.GetValueOrDefault())
                    {
                        WmsDebugTrace.Warning(
                            $"{TracePrefix} BLOCKED in Confirm: scanned inventory does not match expected top row.");

                        return FlowStatus
                            .Fail("Scan rejected. Please scan the first item shown in Estimated Content of Packages to be Packed.")
                            .WithModeReset
                            .WithPostAction(() =>
                            {
                                packLogic.PackageLineNbrUI = package.LineNbr;
                                Basis.Graph.Packages.Current = package;
                            });
                    }

                    if (expectedTopRow != null &&
                        !string.IsNullOrEmpty(expectedTopRow.LotSerialNbr) &&
                        !string.IsNullOrEmpty(Basis.LotSerialNbr) &&
                        !string.Equals(expectedTopRow.LotSerialNbr, Basis.LotSerialNbr, StringComparison.OrdinalIgnoreCase))
                    {
                        WmsDebugTrace.Warning(
                            $"{TracePrefix} BLOCKED in Confirm: scanned lot/serial does not match expected top row.");

                        return FlowStatus
                            .Fail("Scan rejected. Please scan the first lot/serial number shown in Estimated Content of Packages to be Packed.")
                            .WithModeReset
                            .WithPostAction(() =>
                            {
                                packLogic.PackageLineNbrUI = package.LineNbr;
                                Basis.Graph.Packages.Current = package;
                            });
                    }
                }
                else
                {
                    WmsDebugTrace.Info(
                        $"{TracePrefix} Confirm skipped guard because package or scanned InventoryID is null.");
                }
            }

            FlowStatus result = base_Confirm();

            WmsDebugTrace.Info(
                $"{TracePrefix} Confirm EXIT base result. IsError={result.IsError}, Message={result.Message}");

            return result;
        }

        [PXOverride]
        public virtual IEnumerable<SOShipLineSplit> GetSplitsToPack(
            Func<IEnumerable<SOShipLineSplit>> base_GetSplitsToPack)
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} GetSplitsToPack ENTER. Remove={Basis.Remove}, InventoryID={Basis.InventoryID}, LotSerialNbr={Basis.LotSerialNbr}, Qty={Basis.Qty}, BaseQty={Basis.BaseQty}");

            List<SOShipLineSplit> baseSplits = base_GetSplitsToPack().ToList();

            WmsDebugTrace.Info(
                $"{TracePrefix} Base splits count={baseSplits.Count}. Splits={string.Join(" | ", baseSplits.Select(DescribeSplit))}");

            if (Basis.Remove.GetValueOrDefault())
            {
                WmsDebugTrace.Info($"{TracePrefix} Remove mode. Returning base splits.");
                return baseSplits;
            }

            PickPackShip.PackMode.Logic packLogic =
                Basis.Get<PickPackShip.PackMode.Logic>();

            SOPackageDetailEx package = packLogic?.SelectedPackage;

            if (ShouldBypassTopRowWorkflow(package))
            {
                WmsDebugTrace.Info($"{TracePrefix} GetSplitsToPack bypassed top-row split filtering by customer.");
                return baseSplits;
            }

            WmsDebugTrace.Info(
                $"{TracePrefix} GetSplitsToPack package. HasPackLogic={packLogic != null}, HasPackage={package != null}, PackageShipmentNbr={package?.ShipmentNbr}, PackageLineNbr={package?.LineNbr}, PackageLineNbrUI={packLogic?.PackageLineNbrUI}");

            if (package == null || package.ShipmentNbr == null || package.LineNbr == null)
            {
                WmsDebugTrace.Warning($"{TracePrefix} No valid selected package. Returning base splits.");
                return baseSplits;
            }

            WmsPlan expectedTopRow = GetExpectedTopRow(package);

            TraceExpectedVsScanned("GetSplitsToPack", expectedTopRow);

            if (expectedTopRow == null || expectedTopRow.ShipmentSplitLineNbr == null)
            {
                WmsDebugTrace.Warning($"{TracePrefix} No expected top row found. Returning base splits.");
                return baseSplits;
            }

            List<SOShipLineSplit> filteredSplits = baseSplits
                .Where(split => split.SplitLineNbr == expectedTopRow.ShipmentSplitLineNbr)
                .ToList();

            WmsDebugTrace.Info(
                $"{TracePrefix} Filtered splits count={filteredSplits.Count}. ExpectedSplitLineNbr={expectedTopRow.ShipmentSplitLineNbr}. Filtered={string.Join(" | ", filteredSplits.Select(DescribeSplit))}");

            return filteredSplits;
        }

        private bool ShouldBypassTopRowWorkflow(SOPackageDetailEx package)
        {
            SOShipment shipment = Basis.Graph.Document.Current;

            if (shipment == null && package?.ShipmentNbr != null)
            {
                shipment =
                    PXSelectReadonly<
                        SOShipment,
                        Where<SOShipment.shipmentNbr, Equal<Required<SOShipment.shipmentNbr>>>>
                    .Select(Basis, package.ShipmentNbr)
                    .RowCast<SOShipment>()
                    .FirstOrDefault();
            }

            bool bypass = CustomerWorkflowBypass.ShouldBypassTopRowWorkflow(
                Basis.Graph,
                shipment);

            WmsDebugTrace.Info(
                $"{TracePrefix} Customer bypass check. ShipmentNbr={shipment?.ShipmentNbr}, CustomerID={shipment?.CustomerID}, Bypass={bypass}");

            return bypass;
        }

        private WmsPlan GetExpectedTopRow(SOPackageDetailEx package)
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} GetExpectedTopRow ENTER. ShipmentNbr={package?.ShipmentNbr}, PackageLineNbr={package?.LineNbr}");

            List<WmsPlan> plannedRows =
                PXSelectReadonly<
                    WmsPlan,
                    Where<
                        WmsPlan.shipmentNbr, Equal<Required<WmsPlan.shipmentNbr>>,
                        And<WmsPlan.packageLineNbr, Equal<Required<WmsPlan.packageLineNbr>>>>>
                .Select(Basis, package.ShipmentNbr, package.LineNbr)
                .RowCast<WmsPlan>()
                .ToList();

            List<SOShipLineSplitPackage> actualRows =
                PXSelectReadonly<
                    SOShipLineSplitPackage,
                    Where<
                        SOShipLineSplitPackage.shipmentNbr, Equal<Required<SOShipLineSplitPackage.shipmentNbr>>,
                        And<SOShipLineSplitPackage.packageLineNbr, Equal<Required<SOShipLineSplitPackage.packageLineNbr>>>>>
                .Select(Basis, package.ShipmentNbr, package.LineNbr)
                .RowCast<SOShipLineSplitPackage>()
                .ToList();

            Dictionary<int?, decimal> actualQtyBySplit =
                actualRows
                    .GroupBy(x => x.ShipmentSplitLineNbr)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(x => x.PackedQty ?? 0m));

            List<WmsPlan> incompleteRows = plannedRows
                .Where(row =>
                {
                    decimal expectedQty = row.PackedQty ?? 0m;
                    decimal actualQty = 0m;

                    actualQtyBySplit.TryGetValue(row.ShipmentSplitLineNbr, out actualQty);

                    bool incomplete = expectedQty <= 0m || actualQty < expectedQty;

                    bool skipped = SelectedPackageSkipState.IsSkipped(
                        Basis.Graph,
                        row.ShipmentNbr,
                        row.PackageLineNbr,
                        row.ShipmentSplitLineNbr);

                    WmsDebugTrace.Info(
                        $"{TracePrefix} Candidate row. RecordID={row.RecordID}, InventoryID={row.InventoryID}, InventoryCD={GetInventoryCD(row.InventoryID)}, LotSerialNbr={row.LotSerialNbr}, SplitLineNbr={row.ShipmentSplitLineNbr}, ExpectedQty={expectedQty}, ActualQty={actualQty}, Incomplete={incomplete}, Skipped={skipped}, DefaultIssueFrom={row.DefaultIssueFrom}, OrderNbr={row.OrderNbr}, StoreNbr={row.StoreNbr}");

                    return incomplete;
                })
                .OrderBy(row =>
                    SelectedPackageSkipState.IsSkipped(
                        Basis.Graph,
                        row.ShipmentNbr,
                        row.PackageLineNbr,
                        row.ShipmentSplitLineNbr) ? 1 : 0)
                .ThenBy(row => row.DefaultIssueFrom)
                .ThenBy(row => row.OrderNbr)
                .ThenBy(row => row.StoreNbr)
                .ThenBy(row => GetInventoryCD(row.InventoryID))
                .ThenBy(row => row.LotSerialNbr)
                .ToList();

            WmsPlan topRow = incompleteRows.FirstOrDefault();

            WmsDebugTrace.Info(
                $"{TracePrefix} Expected top row selected. HasTopRow={topRow != null}, {DescribePlan(topRow)}");

            return topRow;
        }

        private string GetInventoryCD(int? inventoryID)
        {
            if (inventoryID == null)
                return null;

            InventoryItem item =
                PXSelectReadonly<
                    InventoryItem,
                    Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>
                .Select(Basis, inventoryID)
                .RowCast<InventoryItem>()
                .FirstOrDefault();

            return item?.InventoryCD?.Trim();
        }

        private void TraceExpectedVsScanned(string source, WmsPlan expectedTopRow)
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} {source} compare. " +
                $"ScannedInventoryID={Basis.InventoryID}, " +
                $"ScannedInventoryCD={GetInventoryCD(Basis.InventoryID)}, " +
                $"ScannedLotSerialNbr={Basis.LotSerialNbr}, " +
                $"ScannedQty={Basis.Qty}, " +
                $"ExpectedInventoryID={expectedTopRow?.InventoryID}, " +
                $"ExpectedInventoryCD={GetInventoryCD(expectedTopRow?.InventoryID)}, " +
                $"ExpectedSplitLineNbr={expectedTopRow?.ShipmentSplitLineNbr}, " +
                $"ExpectedLotSerialNbr={expectedTopRow?.LotSerialNbr}");
        }

        private string DescribePlan(WmsPlan row)
        {
            if (row == null)
                return "ExpectedTopRow=NULL";

            return
                $"RecordID={row.RecordID}, InventoryID={row.InventoryID}, InventoryCD={GetInventoryCD(row.InventoryID)}, LotSerialNbr={row.LotSerialNbr}, ShipmentSplitLineNbr={row.ShipmentSplitLineNbr}, PackedQty={row.PackedQty}, UOM={row.UOM}, OrderNbr={row.OrderNbr}, StoreNbr={row.StoreNbr}, DefaultIssueFrom={row.DefaultIssueFrom}";
        }

        private string DescribeSplit(SOShipLineSplit split)
        {
            if (split == null)
                return "Split=NULL";

            return
                $"SplitLineNbr={split.SplitLineNbr}, InventoryID={split.InventoryID}, LotSerialNbr={split.LotSerialNbr}, LocationID={split.LocationID}, Qty={split.Qty}, PackedQty={split.PackedQty}, UOM={split.UOM}";
        }
    }
}