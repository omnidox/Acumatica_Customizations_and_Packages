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

namespace CustomWMS
{
    public class PackSkipTopRowCommand : PickPackShip.ScanExtension
    {
        private const string TracePrefix = "[PackSkipTopRowCommand]";
        private const string Version = "2026-06-17-SKIP-TOP-ROW-COMMAND-CUSTOMER-BYPASS-01";

        public static bool IsActive()
        {
            PXTrace.WriteInformation($"{TracePrefix} IsActive TRUE. Version={Version}");
            return true;
        }

        public sealed class SkipTopRowCommand : PickPackShip.ScanCommand
        {
            public override string Code => "SKIPCONTENT";
            public override string ButtonName => "SkipTopRow";
            public override string DisplayName => "Skip Top Row";

            protected override bool IsEnabled
            {
                get
                {
                    SOShipment shipment = Basis?.Graph?.Document?.Current;

                    if (CustomerWorkflowBypass.ShouldBypassTopRowWorkflow(Basis.Graph, shipment))
                        return false;

                    return true;
                }
            }

            protected override bool Process()
            {
                PXTrace.WriteInformation($"{TracePrefix} SkipTopRow Process ENTER.");

                SOShipment shipment = Basis.Graph.Document.Current;

                if (CustomerWorkflowBypass.ShouldBypassTopRowWorkflow(Basis.Graph, shipment))
                {
                    PXTrace.WriteInformation($"{TracePrefix} SkipTopRow blocked because customer bypasses top-row workflow.");
                    Basis.ReportWarning("Skip Top Row is not required for this customer.");
                    return true;
                }

                PickPackShip.PackMode.Logic packLogic =
                    Basis.Get<PickPackShip.PackMode.Logic>();

                SOPackageDetailEx package = packLogic?.SelectedPackage;

                if (package == null || package.ShipmentNbr == null || package.LineNbr == null)
                {
                    PXTrace.WriteWarning($"{TracePrefix} No valid selected package found.");
                    Basis.ReportError("No selected package was found.");
                    return true;
                }

                WmsPlan topRow = GetTopIncompleteUnskippedRow(package);

                if (topRow == null)
                {
                    PXTrace.WriteWarning($"{TracePrefix} No unskipped incomplete row found.");
                    Basis.ReportWarning("No unskipped incomplete row was found.");
                    return true;
                }

                SelectedPackageSkipState.Skip(
                    Basis.Graph,
                    topRow.ShipmentNbr,
                    topRow.PackageLineNbr,
                    topRow.ShipmentSplitLineNbr);

                PXTrace.WriteInformation(
                    $"{TracePrefix} Row skipped. ShipmentNbr={topRow.ShipmentNbr}, PackageLineNbr={topRow.PackageLineNbr}, SplitLineNbr={topRow.ShipmentSplitLineNbr}, InventoryID={topRow.InventoryID}, LotSerialNbr={topRow.LotSerialNbr}");

                RequestEstimatedContentRefresh("SkipTopRowCommand");

                packLogic.PackageLineNbrUI = package.LineNbr;
                Basis.Graph.Packages.Current = package;

                Basis.ReportInfo("Top row skipped.");

                PXTrace.WriteInformation($"{TracePrefix} SkipTopRow Process EXIT.");
                return true;
            }

            private WmsPlan GetTopIncompleteUnskippedRow(SOPackageDetailEx package)
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

                return plannedRows
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

                        PXTrace.WriteInformation(
                            $"{TracePrefix} Candidate row. RecordID={row.RecordID}, InventoryID={row.InventoryID}, InventoryCD={GetInventoryCD(row.InventoryID)}, LotSerialNbr={row.LotSerialNbr}, SplitLineNbr={row.ShipmentSplitLineNbr}, Expected={expectedQty}, Actual={actualQty}, Incomplete={incomplete}, Skipped={skipped}");

                        return incomplete && !skipped;
                    })
                    .OrderBy(row => row.DefaultIssueFrom)
                    .ThenBy(row => row.OrderNbr)
                    .ThenBy(row => row.StoreNbr)
                    .ThenBy(row => GetInventoryCD(row.InventoryID))
                    .ThenBy(row => row.LotSerialNbr)
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

                if (wmsExt?.SelectedPackageContentsView == null)
                {
                    PXTrace.WriteInformation($"{TracePrefix} Could not refresh: WMS extension/view not found. Reason={reason}");
                    return;
                }

                wmsExt.SelectedPackageContentsView.Cache.Clear();
                wmsExt.SelectedPackageContentsView.View.Clear();
                wmsExt.SelectedPackageContentsView.View.RequestRefresh();

                PXTrace.WriteInformation($"{TracePrefix} Refresh requested. Reason={reason}");
            }
        }

        public sealed class ClearSkippedRowsCommand : PickPackShip.ScanCommand
        {
            public override string Code => "CLEARSKIPS";
            public override string ButtonName => "ClearSkippedRows";
            public override string DisplayName => "Clear Skipped Rows";

            protected override bool IsEnabled
            {
                get
                {
                    SOShipment shipment = Basis?.Graph?.Document?.Current;

                    if (CustomerWorkflowBypass.ShouldBypassTopRowWorkflow(Basis.Graph, shipment))
                        return false;

                    return true;
                }
            }

            protected override bool Process()
            {
                PXTrace.WriteInformation($"{TracePrefix} ClearSkippedRows Process ENTER.");

                SOShipment shipment = Basis.Graph.Document.Current;

                if (CustomerWorkflowBypass.ShouldBypassTopRowWorkflow(Basis.Graph, shipment))
                {
                    PXTrace.WriteInformation($"{TracePrefix} ClearSkippedRows blocked because customer bypasses top-row workflow.");
                    Basis.ReportWarning("Clear Skipped Rows is not required for this customer.");
                    return true;
                }

                PickPackShip.PackMode.Logic packLogic =
                    Basis.Get<PickPackShip.PackMode.Logic>();

                SOPackageDetailEx package = packLogic?.SelectedPackage;

                if (package == null || package.ShipmentNbr == null || package.LineNbr == null)
                {
                    PXTrace.WriteWarning($"{TracePrefix} Clear skipped rows clicked but no valid selected package was found.");
                    Basis.ReportError("No selected package was found.");
                    return true;
                }

                SelectedPackageSkipState.ClearPackage(
                    Basis.Graph,
                    package.ShipmentNbr,
                    package.LineNbr);

                RequestEstimatedContentRefresh("ClearSkippedRowsCommand");

                packLogic.PackageLineNbrUI = package.LineNbr;
                Basis.Graph.Packages.Current = package;

                Basis.ReportInfo("Skipped rows cleared.");

                PXTrace.WriteInformation($"{TracePrefix} ClearSkippedRows Process EXIT.");
                return true;
            }

            private void RequestEstimatedContentRefresh(string reason)
            {
                WmsShipmentExt wmsExt = Basis.Graph.GetExtension<WmsShipmentExt>();

                if (wmsExt?.SelectedPackageContentsView == null)
                {
                    PXTrace.WriteInformation($"{TracePrefix} Could not refresh: WMS extension/view not found. Reason={reason}");
                    return;
                }

                wmsExt.SelectedPackageContentsView.Cache.Clear();
                wmsExt.SelectedPackageContentsView.View.Clear();
                wmsExt.SelectedPackageContentsView.View.RequestRefresh();

                PXTrace.WriteInformation($"{TracePrefix} Refresh requested. Reason={reason}");
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
                PXTrace.WriteInformation($"{TracePrefix} Appending SkipTopRowCommand and ClearSkippedRowsCommand to PackMode.");

                packMode.Intercept.CreateCommands.ByAppend(basis => new PickPackShip.ScanCommand[]
                {
                    new SkipTopRowCommand(),
                    new ClearSkippedRowsCommand()
                });
            }

            return mode;
        }
    }
}