using System;
using System.Collections.Generic;
using System.Linq;
using PX.BarcodeProcessing;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.SO.WMS;

using WmsPlan = WMS.SelectedPackageContents;

namespace CustomWMS
{
    public class PackTopRowOnlyScanGuard
        : BarcodeDrivenStateMachine<
            PickPackShip,
            PickPackShip.Host>
            .ScanExtension<
                PickPackShip.PackMode
                    .ConfirmState.Logic>
    {
        private const string TracePrefix =
            "[PackTopRowOnlyScanGuard]";

        private const string Version =
            "2026-07-21-SHIPMENT-WIDE-PK-CACHE-01";

        /*
         * Cache stable InventoryID-to-InventoryCD lookup data for this
         * scan-extension instance.
         *
         * Top-row results and packed quantities are intentionally not
         * cached because scans can change them.
         */
        private readonly Dictionary<int?, string>
            _inventoryCodeCache =
                new Dictionary<int?, string>();

        public static bool IsActive()
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} IsActive TRUE. " +
                $"Version={Version}");

            return true;
        }

        [PXOverride]
        public virtual FlowStatus Confirm(
            Func<FlowStatus> base_Confirm)
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} Confirm ENTER. " +
                $"Version={Version}, " +
                $"Remove={Basis.Remove}, " +
                $"InventoryID={Basis.InventoryID}, " +
                $"LotSerialNbr={Basis.LotSerialNbr}, " +
                $"Qty={Basis.Qty}, " +
                $"BaseQty={Basis.BaseQty}, " +
                $"UOM={Basis.UOM}");

            PickPackShip.PackMode.Logic packLogic =
                Basis.Get<PickPackShip.PackMode.Logic>();

            SOPackageDetailEx package =
                packLogic?.SelectedPackage;

            if (ShouldBypassTopRowWorkflow(
                package))
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Confirm bypassed " +
                    $"top-row scan guard by customer.");

                return base_Confirm();
            }

            if (!Basis.Remove.GetValueOrDefault())
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Confirm package. " +
                    $"HasPackLogic={packLogic != null}, " +
                    $"HasPackage={package != null}, " +
                    $"PackageShipmentNbr=" +
                    $"{package?.ShipmentNbr}, " +
                    $"PackageLineNbr={package?.LineNbr}, " +
                    $"PackageLineNbrUI=" +
                    $"{packLogic?.PackageLineNbrUI}");

                if (package != null &&
                    Basis.InventoryID != null)
                {
                    WmsPlan expectedTopRow =
                        GetExpectedTopRow(
                            package);

                    TraceExpectedVsScanned(
                        "Confirm",
                        expectedTopRow);

                    if (expectedTopRow != null &&
                        expectedTopRow.InventoryID
                            .GetValueOrDefault() !=
                        Basis.InventoryID
                            .GetValueOrDefault())
                    {
                        WmsDebugTrace.Warning(
                            $"{TracePrefix} BLOCKED in Confirm: " +
                            $"scanned inventory does not match " +
                            $"expected top row.");

                        return FlowStatus
                            .Fail(
                                "Scan rejected. Please scan the " +
                                "first item shown in Estimated " +
                                "Content of Packages to be Packed.")
                            .WithModeReset
                            .WithPostAction(
                                () =>
                                {
                                    packLogic.PackageLineNbrUI =
                                        package.LineNbr;

                                    Basis.Graph.Packages.Current =
                                        package;
                                });
                    }

                    if (expectedTopRow != null &&
                        !string.IsNullOrEmpty(
                            expectedTopRow.LotSerialNbr) &&
                        !string.IsNullOrEmpty(
                            Basis.LotSerialNbr) &&
                        !string.Equals(
                            expectedTopRow.LotSerialNbr,
                            Basis.LotSerialNbr,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        WmsDebugTrace.Warning(
                            $"{TracePrefix} BLOCKED in Confirm: " +
                            $"scanned lot/serial does not match " +
                            $"expected top row.");

                        return FlowStatus
                            .Fail(
                                "Scan rejected. Please scan the " +
                                "first lot/serial number shown in " +
                                "Estimated Content of Packages " +
                                "to be Packed.")
                            .WithModeReset
                            .WithPostAction(
                                () =>
                                {
                                    packLogic.PackageLineNbrUI =
                                        package.LineNbr;

                                    Basis.Graph.Packages.Current =
                                        package;
                                });
                    }
                }
                else
                {
                    WmsDebugTrace.Info(
                        $"{TracePrefix} Confirm skipped guard " +
                        $"because package or scanned " +
                        $"InventoryID is null.");
                }
            }

            FlowStatus result =
                base_Confirm();

            WmsDebugTrace.Info(
                $"{TracePrefix} Confirm EXIT base result. " +
                $"IsError={result.IsError}, " +
                $"Message={result.Message}");

            return result;
        }

        [PXOverride]
        public virtual IEnumerable<SOShipLineSplit>
            GetSplitsToPack(
                Func<IEnumerable<SOShipLineSplit>>
                    base_GetSplitsToPack)
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} GetSplitsToPack ENTER. " +
                $"Remove={Basis.Remove}, " +
                $"InventoryID={Basis.InventoryID}, " +
                $"LotSerialNbr={Basis.LotSerialNbr}, " +
                $"Qty={Basis.Qty}, " +
                $"BaseQty={Basis.BaseQty}");

            List<SOShipLineSplit> baseSplits =
                base_GetSplitsToPack()
                    .ToList();

            WmsDebugTrace.Info(
                $"{TracePrefix} Base splits count=" +
                $"{baseSplits.Count}. " +
                $"Splits=" +
                $"{string.Join(
                    " | ",
                    baseSplits.Select(
                        DescribeSplit))}");

            if (Basis.Remove.GetValueOrDefault())
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Remove mode. " +
                    $"Returning base splits.");

                return baseSplits;
            }

            PickPackShip.PackMode.Logic packLogic =
                Basis.Get<PickPackShip.PackMode.Logic>();

            SOPackageDetailEx package =
                packLogic?.SelectedPackage;

            if (ShouldBypassTopRowWorkflow(
                package))
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} GetSplitsToPack bypassed " +
                    $"top-row split filtering by customer.");

                return baseSplits;
            }

            WmsDebugTrace.Info(
                $"{TracePrefix} GetSplitsToPack package. " +
                $"HasPackLogic={packLogic != null}, " +
                $"HasPackage={package != null}, " +
                $"PackageShipmentNbr=" +
                $"{package?.ShipmentNbr}, " +
                $"PackageLineNbr={package?.LineNbr}, " +
                $"PackageLineNbrUI=" +
                $"{packLogic?.PackageLineNbrUI}");

            if (package == null ||
                string.IsNullOrEmpty(package.ShipmentNbr) ||
                package.LineNbr == null)
            {
                WmsDebugTrace.Warning(
                    $"{TracePrefix} No valid selected package. " +
                    $"Returning base splits.");

                return baseSplits;
            }

            WmsPlan expectedTopRow =
                GetExpectedTopRow(
                    package);

            TraceExpectedVsScanned(
                "GetSplitsToPack",
                expectedTopRow);

            if (expectedTopRow == null ||
                expectedTopRow.ShipmentSplitLineNbr == null)
            {
                WmsDebugTrace.Warning(
                    $"{TracePrefix} No expected top row found. " +
                    $"Returning base splits.");

                return baseSplits;
            }

            List<SOShipLineSplit> filteredSplits =
                baseSplits
                    .Where(
                        split =>
                            split.SplitLineNbr ==
                            expectedTopRow
                                .ShipmentSplitLineNbr)
                    .ToList();

            WmsDebugTrace.Info(
                $"{TracePrefix} Filtered splits count=" +
                $"{filteredSplits.Count}. " +
                $"ExpectedSplitLineNbr=" +
                $"{expectedTopRow.ShipmentSplitLineNbr}. " +
                $"Filtered=" +
                $"{string.Join(
                    " | ",
                    filteredSplits.Select(
                        DescribeSplit))}");

            return filteredSplits;
        }

        private bool ShouldBypassTopRowWorkflow(
            SOPackageDetailEx package)
        {
            SOShipment shipment =
                Basis.Graph.Document.Current;

            if (shipment == null &&
                !string.IsNullOrEmpty(
                    package?.ShipmentNbr))
            {
                shipment =
                    PXSelectReadonly<
                        SOShipment,
                        Where<
                            SOShipment.shipmentNbr,
                            Equal<
                                Required<
                                    SOShipment.shipmentNbr>>>>
                    .Select(
                        Basis,
                        package.ShipmentNbr)
                    .RowCast<SOShipment>()
                    .FirstOrDefault();
            }

            bool bypass =
                CustomerWorkflowBypass
                    .ShouldBypassTopRowWorkflow(
                        Basis.Graph,
                        shipment);

            WmsDebugTrace.Info(
                $"{TracePrefix} Customer bypass check. " +
                $"ShipmentNbr={shipment?.ShipmentNbr}, " +
                $"CustomerID={shipment?.CustomerID}, " +
                $"Bypass={bypass}");

            return bypass;
        }

        private WmsPlan GetExpectedTopRow(
            SOPackageDetailEx package)
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} GetExpectedTopRow ENTER. " +
                $"ShipmentNbr={package?.ShipmentNbr}, " +
                $"PackageLineNbr={package?.LineNbr}");

            /*
             * Load expected rows for the selected package.
             */
            List<WmsPlan> plannedRows =
                PXSelectReadonly<
                    WmsPlan,
                    Where<
                        WmsPlan.shipmentNbr,
                        Equal<
                            Required<
                                WmsPlan.shipmentNbr>>,
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

            /*
             * Load actual packed rows across the complete shipment.
             *
             * This calculation now matches the Estimated Content grid.
             * Quantities packed in other cartons count toward completion
             * for the same shipment split.
             */
            List<SOShipLineSplitPackage> actualRows =
                PXSelectReadonly<
                    SOShipLineSplitPackage,
                    Where<
                        SOShipLineSplitPackage.shipmentNbr,
                        Equal<
                            Required<
                                SOShipLineSplitPackage
                                    .shipmentNbr>>>>
                .Select(
                    Basis,
                    package.ShipmentNbr)
                .RowCast<SOShipLineSplitPackage>()
                .ToList();

            Dictionary<int?, decimal>
                actualQtyBySplit =
                    actualRows
                        .Where(row =>
                            row.ShipmentSplitLineNbr != null)
                        .GroupBy(row =>
                            row.ShipmentSplitLineNbr)
                        .ToDictionary(
                            group =>
                                group.Key,
                            group =>
                                group.Sum(
                                    row =>
                                        row.PackedQty ?? 0m));

            List<TopRowCandidate> candidates =
                new List<TopRowCandidate>();

            foreach (WmsPlan row in plannedRows)
            {
                if (row == null)
                {
                    continue;
                }

                decimal expectedQty =
                    row.PackedQty ?? 0m;

                decimal actualQty =
                    0m;

                if (row.ShipmentSplitLineNbr != null)
                {
                    actualQtyBySplit.TryGetValue(
                        row.ShipmentSplitLineNbr,
                        out actualQty);
                }

                bool incomplete =
                    expectedQty <= 0m ||
                    actualQty < expectedQty;

                bool skipped =
                    SelectedPackageSkipState.IsSkipped(
                        Basis.Graph,
                        row.ShipmentNbr,
                        row.PackageLineNbr,
                        row.ShipmentSplitLineNbr);

                string inventoryCD =
                    GetInventoryCD(
                        row.InventoryID);

                WmsDebugTrace.Info(
                    $"{TracePrefix} Candidate row. " +
                    $"RecordID={row.RecordID}, " +
                    $"InventoryID={row.InventoryID}, " +
                    $"InventoryCD={inventoryCD}, " +
                    $"LotSerialNbr={row.LotSerialNbr}, " +
                    $"SplitLineNbr=" +
                    $"{row.ShipmentSplitLineNbr}, " +
                    $"ExpectedQty={expectedQty}, " +
                    $"ActualAcrossShipment={actualQty}, " +
                    $"Incomplete={incomplete}, " +
                    $"Skipped={skipped}, " +
                    $"DefaultIssueFrom=" +
                    $"{row.DefaultIssueFrom}, " +
                    $"OrderNbr={row.OrderNbr}, " +
                    $"StoreNbr={row.StoreNbr}");

                if (!incomplete)
                {
                    continue;
                }

                candidates.Add(
                    new TopRowCandidate
                    {
                        Row =
                            row,

                        SkipSortOrder =
                            skipped ? 1 : 0,

                        InventoryCD =
                            inventoryCD
                    });
            }

            /*
             * This ordering matches the Estimated Content replacement
             * view:
             *
             * 1. Active rows before skipped rows
             * 2. Default issue location
             * 3. Order number
             * 4. Store number
             * 5. Inventory code
             * 6. Lot/serial number
             */
            WmsPlan topRow =
                candidates
                    .OrderBy(candidate =>
                        candidate.SkipSortOrder)
                    .ThenBy(candidate =>
                        candidate.Row.DefaultIssueFrom)
                    .ThenBy(candidate =>
                        candidate.Row.OrderNbr)
                    .ThenBy(candidate =>
                        candidate.Row.StoreNbr)
                    .ThenBy(candidate =>
                        candidate.InventoryCD)
                    .ThenBy(candidate =>
                        candidate.Row.LotSerialNbr)
                    .Select(candidate =>
                        candidate.Row)
                    .FirstOrDefault();

            WmsDebugTrace.Info(
                $"{TracePrefix} Expected top row selected. " +
                $"HasTopRow={topRow != null}, " +
                $"{DescribePlan(topRow)}");

            return topRow;
        }

        /*
         * Resolve InventoryCD by InventoryItem primary key and cache the
         * result for this scan-extension instance.
         */
        private string GetInventoryCD(
            int? inventoryID)
        {
            if (inventoryID == null)
            {
                return string.Empty;
            }

            string inventoryCD;

            if (_inventoryCodeCache.TryGetValue(
                inventoryID,
                out inventoryCD))
            {
                return inventoryCD ??
                    string.Empty;
            }

            InventoryItem item =
                InventoryItem.PK.Find(
                    Basis.Graph,
                    inventoryID);

            inventoryCD =
                item?.InventoryCD?.Trim()
                ?? string.Empty;

            _inventoryCodeCache[inventoryID] =
                inventoryCD;

            return inventoryCD;
        }

        private void TraceExpectedVsScanned(
            string source,
            WmsPlan expectedTopRow)
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} {source} compare. " +
                $"ScannedInventoryID={Basis.InventoryID}, " +
                $"ScannedInventoryCD=" +
                $"{GetInventoryCD(Basis.InventoryID)}, " +
                $"ScannedLotSerialNbr=" +
                $"{Basis.LotSerialNbr}, " +
                $"ScannedQty={Basis.Qty}, " +
                $"ExpectedInventoryID=" +
                $"{expectedTopRow?.InventoryID}, " +
                $"ExpectedInventoryCD=" +
                $"{GetInventoryCD(
                    expectedTopRow?.InventoryID)}, " +
                $"ExpectedSplitLineNbr=" +
                $"{expectedTopRow?.ShipmentSplitLineNbr}, " +
                $"ExpectedLotSerialNbr=" +
                $"{expectedTopRow?.LotSerialNbr}");
        }

        private string DescribePlan(
            WmsPlan row)
        {
            if (row == null)
            {
                return "ExpectedTopRow=NULL";
            }

            return
                $"RecordID={row.RecordID}, " +
                $"InventoryID={row.InventoryID}, " +
                $"InventoryCD=" +
                $"{GetInventoryCD(row.InventoryID)}, " +
                $"LotSerialNbr={row.LotSerialNbr}, " +
                $"ShipmentSplitLineNbr=" +
                $"{row.ShipmentSplitLineNbr}, " +
                $"PackedQty={row.PackedQty}, " +
                $"UOM={row.UOM}, " +
                $"OrderNbr={row.OrderNbr}, " +
                $"StoreNbr={row.StoreNbr}, " +
                $"DefaultIssueFrom={row.DefaultIssueFrom}";
        }

        private string DescribeSplit(
            SOShipLineSplit split)
        {
            if (split == null)
            {
                return "Split=NULL";
            }

            return
                $"SplitLineNbr={split.SplitLineNbr}, " +
                $"InventoryID={split.InventoryID}, " +
                $"LotSerialNbr={split.LotSerialNbr}, " +
                $"LocationID={split.LocationID}, " +
                $"Qty={split.Qty}, " +
                $"PackedQty={split.PackedQty}, " +
                $"UOM={split.UOM}";
        }

        /*
         * Stores values used to sort each candidate so that skip state and
         * InventoryCD do not need to be recalculated during ordering.
         */
        private sealed class TopRowCandidate
        {
            public WmsPlan Row { get; set; }

            public int SkipSortOrder { get; set; }

            public string InventoryCD { get; set; }
        }
    }
}