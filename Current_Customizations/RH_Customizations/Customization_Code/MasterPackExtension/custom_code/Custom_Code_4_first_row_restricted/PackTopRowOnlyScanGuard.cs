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
        private const string Version = "2026-06-08-UAT-DEBUG-01";

        public static bool IsActive()
        {
            PXTrace.WriteInformation($"{TracePrefix} IsActive TRUE. Version={Version}");
            return true;
        }

        [PXOverride]
        public virtual FlowStatus Confirm(Func<FlowStatus> base_Confirm)
        {
            PXTrace.WriteInformation(
                $"{TracePrefix} Confirm ENTER. Version={Version}, Remove={Basis.Remove}, InventoryID={Basis.InventoryID}, LotSerialNbr={Basis.LotSerialNbr}, Qty={Basis.Qty}, BaseQty={Basis.BaseQty}, UOM={Basis.UOM}");

            if (!Basis.Remove.GetValueOrDefault())
            {
                PickPackShip.PackMode.Logic packLogic =
                    Basis.Get<PickPackShip.PackMode.Logic>();

                SOPackageDetailEx package = packLogic?.SelectedPackage;

                PXTrace.WriteInformation(
                    $"{TracePrefix} Confirm package. HasPackLogic={packLogic != null}, HasPackage={package != null}, PackageShipmentNbr={package?.ShipmentNbr}, PackageLineNbr={package?.LineNbr}, PackageLineNbrUI={packLogic?.PackageLineNbrUI}");

                if (package != null && Basis.InventoryID != null)
                {
                    WmsPlan expectedTopRow = GetExpectedTopRow(package);

                    TraceExpectedVsScanned("Confirm", expectedTopRow);

                    if (expectedTopRow != null &&
                        expectedTopRow.InventoryID.GetValueOrDefault() != Basis.InventoryID.GetValueOrDefault())
                    {
                        PXTrace.WriteWarning(
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
                        PXTrace.WriteWarning(
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
                    PXTrace.WriteInformation(
                        $"{TracePrefix} Confirm skipped guard because package or scanned InventoryID is null.");
                }
            }

            FlowStatus result = base_Confirm();

            PXTrace.WriteInformation(
                $"{TracePrefix} Confirm EXIT base result. IsError={result.IsError}, Message={result.Message}");

            return result;
        }

        [PXOverride]
        public virtual IEnumerable<SOShipLineSplit> GetSplitsToPack(
            Func<IEnumerable<SOShipLineSplit>> base_GetSplitsToPack)
        {
            PXTrace.WriteInformation(
                $"{TracePrefix} GetSplitsToPack ENTER. Remove={Basis.Remove}, InventoryID={Basis.InventoryID}, LotSerialNbr={Basis.LotSerialNbr}, Qty={Basis.Qty}, BaseQty={Basis.BaseQty}");

            List<SOShipLineSplit> baseSplits = base_GetSplitsToPack().ToList();

            PXTrace.WriteInformation(
                $"{TracePrefix} Base splits count={baseSplits.Count}. Splits={string.Join(" | ", baseSplits.Select(DescribeSplit))}");

            if (Basis.Remove.GetValueOrDefault())
            {
                PXTrace.WriteInformation($"{TracePrefix} Remove mode. Returning base splits.");
                return baseSplits;
            }

            PickPackShip.PackMode.Logic packLogic =
                Basis.Get<PickPackShip.PackMode.Logic>();

            SOPackageDetailEx package = packLogic?.SelectedPackage;

            PXTrace.WriteInformation(
                $"{TracePrefix} GetSplitsToPack package. HasPackLogic={packLogic != null}, HasPackage={package != null}, PackageShipmentNbr={package?.ShipmentNbr}, PackageLineNbr={package?.LineNbr}, PackageLineNbrUI={packLogic?.PackageLineNbrUI}");

            if (package == null || package.ShipmentNbr == null || package.LineNbr == null)
            {
                PXTrace.WriteWarning($"{TracePrefix} No valid selected package. Returning base splits.");
                return baseSplits;
            }

            WmsPlan expectedTopRow = GetExpectedTopRow(package);

            TraceExpectedVsScanned("GetSplitsToPack", expectedTopRow);

            if (expectedTopRow == null || expectedTopRow.ShipmentSplitLineNbr == null)
            {
                PXTrace.WriteWarning($"{TracePrefix} No expected top row found. Returning base splits.");
                return baseSplits;
            }

            List<SOShipLineSplit> filteredSplits = baseSplits
                .Where(split => split.SplitLineNbr == expectedTopRow.ShipmentSplitLineNbr)
                .ToList();

            PXTrace.WriteInformation(
                $"{TracePrefix} Filtered splits count={filteredSplits.Count}. ExpectedSplitLineNbr={expectedTopRow.ShipmentSplitLineNbr}. Filtered={string.Join(" | ", filteredSplits.Select(DescribeSplit))}");

            return filteredSplits;
        }

        private WmsPlan GetExpectedTopRow(SOPackageDetailEx package)
        {
            PXTrace.WriteInformation(
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

            PXTrace.WriteInformation(
                $"{TracePrefix} Planned rows count={plannedRows.Count}. Actual packed rows count={actualRows.Count}");

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

                   
                    PXTrace.WriteInformation(
                        $"{TracePrefix} Candidate row. RecordID={row.RecordID}, InventoryID={row.InventoryID}, InventoryCD={GetInventoryCD(row.InventoryID)}, LotSerialNbr={row.LotSerialNbr}, SplitLineNbr={row.ShipmentSplitLineNbr}, ExpectedQty={expectedQty}, ActualQty={actualQty}, Incomplete={incomplete}, DefaultIssueFrom={row.DefaultIssueFrom}, OrderNbr={row.OrderNbr}, StoreNbr={row.StoreNbr}");
                    return incomplete;
                })
                              
                .OrderBy(row => row.DefaultIssueFrom)
                .ThenBy(row => row.OrderNbr)
                .ThenBy(row => row.StoreNbr)
                .ThenBy(row => GetInventoryCD(row.InventoryID))
                .ThenBy(row => row.LotSerialNbr)
                .ToList();

            WmsPlan topRow = incompleteRows.FirstOrDefault();

            PXTrace.WriteInformation(
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
            PXTrace.WriteInformation(
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