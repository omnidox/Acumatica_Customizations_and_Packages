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
    public class PackSkipTopRowCommand
        : PickPackShip.ScanExtension
    {
        private const string TracePrefix =
            "[PackSkipTopRowCommand]";

        private const string ViewName =
            "SelectedPackageContentsView";

        private const string Version =
            "2026-07-21-SHIPMENT-WIDE-PK-CACHE-01";

        public static bool IsActive()
        {
            WmsDebugTrace.Info(
                $"{TracePrefix} IsActive TRUE. " +
                $"Version={Version}");

            return true;
        }

        public sealed class SkipTopRowCommand
            : PickPackShip.ScanCommand
        {
            /*
             * Cache stable InventoryID-to-InventoryCD lookup data for the
             * lifetime of this command instance.
             *
             * Calculated packed quantities and top-row results are not
             * cached because they can change after every scan.
             */
            private readonly Dictionary<int?, string>
                _inventoryCodeCache =
                    new Dictionary<int?, string>();

            private readonly Dictionary<int?, string>
                _locationCodeCache =
                    new Dictionary<int?, string>();

            public override string Code =>
                "SKIPCONTENT";

            public override string ButtonName =>
                "SkipTopRow";

            public override string DisplayName =>
                "Skip Top Row";

            protected override bool IsEnabled
            {
                get
                {
                    SOShipment shipment =
                        Basis?.Graph?.Document?.Current;

                    if (Basis?.Graph == null)
                    {
                        return false;
                    }

                    if (CustomerWorkflowBypass
                        .ShouldBypassTopRowWorkflow(
                            Basis.Graph,
                            shipment))
                    {
                        return false;
                    }

                    return true;
                }
            }

            protected override bool Process()
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} SkipTopRow Process ENTER. " +
                    $"Version={Version}");

                SOShipment shipment =
                    Basis.Graph.Document.Current;

                if (CustomerWorkflowBypass
                    .ShouldBypassTopRowWorkflow(
                        Basis.Graph,
                        shipment))
                {
                    WmsDebugTrace.Info(
                        $"{TracePrefix} SkipTopRow blocked because " +
                        $"customer bypasses top-row workflow.");

                    Basis.ReportWarning(
                        "Skip Top Row is not required for this customer.");

                    return true;
                }

                PickPackShip.PackMode.Logic packLogic =
                    Basis.Get<PickPackShip.PackMode.Logic>();

                SOPackageDetailEx package =
                    packLogic?.SelectedPackage;

                if (package == null ||
                    string.IsNullOrEmpty(package.ShipmentNbr) ||
                    package.LineNbr == null)
                {
                    WmsDebugTrace.Warning(
                        $"{TracePrefix} No valid selected package found.");

                    Basis.ReportError(
                        "No selected package was found.");

                    return true;
                }

                WmsPlan topRow =
                    GetTopIncompleteUnskippedRow(
                        package);

                if (topRow == null)
                {
                    WmsDebugTrace.Warning(
                        $"{TracePrefix} No unskipped incomplete " +
                        $"row found.");

                    Basis.ReportWarning(
                        "No unskipped incomplete row was found.");

                    return true;
                }

                SelectedPackageSkipState.Skip(
                    Basis.Graph,
                    topRow.ShipmentNbr,
                    topRow.PackageLineNbr,
                    topRow.ShipmentSplitLineNbr);

                WmsDebugTrace.Info(
                    $"{TracePrefix} Row skipped. " +
                    $"ShipmentNbr={topRow.ShipmentNbr}, " +
                    $"PackageLineNbr={topRow.PackageLineNbr}, " +
                    $"SplitLineNbr=" +
                    $"{topRow.ShipmentSplitLineNbr}, " +
                    $"InventoryID={topRow.InventoryID}, " +
                    $"LotSerialNbr={topRow.LotSerialNbr}");

                RequestEstimatedContentRefresh(
                    "SkipTopRowCommand");

                /*
                 * Preserve the selected package after the command.
                 */
                packLogic.PackageLineNbrUI =
                    package.LineNbr;

                Basis.Graph.Packages.Current =
                    package;

                Basis.ReportInfo(
                    "Top row skipped.");

                WmsDebugTrace.Info(
                    $"{TracePrefix} SkipTopRow Process EXIT.");

                return true;
            }

            private WmsPlan GetTopIncompleteUnskippedRow(
                SOPackageDetailEx package)
            {
                /*
                 * Load the expected rows assigned to the selected package.
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
                 * This intentionally matches the Remaining Qty calculation
                 * used by the replacement Estimated Content view.
                 *
                 * If a shipment split was already packed in a different
                 * carton, that quantity must still count toward completion.
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

                List<WmsPlan> candidateRows =
                    new List<WmsPlan>();

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

                    WmsDebugTrace.Info(
                        $"{TracePrefix} Candidate row. " +
                        $"RecordID={row.RecordID}, " +
                        $"InventoryID={row.InventoryID}, " +
                        $"InventoryCD=" +
                        $"{GetInventoryCD(row.InventoryID)}, " +
                        $"LotSerialNbr={row.LotSerialNbr}, " +
                        $"SplitLineNbr=" +
                        $"{row.ShipmentSplitLineNbr}, " +
                        $"Expected={expectedQty}, " +
                        $"ActualAcrossShipment={actualQty}, " +
                        $"Incomplete={incomplete}, " +
                        $"Skipped={skipped}");

                    if (incomplete && !skipped)
                    {
                        candidateRows.Add(row);
                    }
                }

                WmsPlan topRow =
                    candidateRows
                        .OrderBy(row =>
                            GetLocationCD(
                                row.DefaultIssueFrom))
                        .ThenBy(row =>
                            row.OrderNbr)
                        .ThenBy(row =>
                            row.StoreNbr)
                        .ThenBy(row =>
                            GetInventoryCD(
                                row.InventoryID))
                        .ThenBy(row =>
                            row.LotSerialNbr)
                        .FirstOrDefault();

                WmsDebugTrace.Info(
                    $"{TracePrefix} Top incomplete unskipped " +
                    $"row selected. " +
                    $"HasTopRow={topRow != null}, " +
                    $"RecordID={topRow?.RecordID}, " +
                    $"InventoryID={topRow?.InventoryID}, " +
                    $"InventoryCD=" +
                    $"{GetInventoryCD(topRow?.InventoryID)}, " +
                    $"ShipmentSplitLineNbr=" +
                    $"{topRow?.ShipmentSplitLineNbr}");

                return topRow;
            }

            /*
             * Resolve InventoryCD through the InventoryItem primary key.
             *
             * This avoids repeated PXSelect calls while sorting and
             * diagnostic logging.
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

            private string GetLocationCD(
                int? locationID)
            {
                if (locationID == null)
                {
                    return string.Empty;
                }

                string locationCD;

                if (_locationCodeCache.TryGetValue(
                    locationID,
                    out locationCD))
                {
                    return locationCD ?? string.Empty;
                }

                INLocation location =
                    INLocation.PK.Find(
                        Basis.Graph,
                        locationID);

                locationCD =
                    location?.LocationCD?.Trim()
                    ?? string.Empty;

                _locationCodeCache[locationID] =
                    locationCD;

                return locationCD;
            }

            /*
             * Refresh the active PXView registered under the ASPX
             * DataMember.
             *
             * The replacement Estimated Content extension registers its
             * custom view in:
             *
             *     Basis.Graph.Views["SelectedPackageContentsView"]
             *
             * Do not clear the WmsPlan cache here.
             */
            private void RequestEstimatedContentRefresh(
                string reason)
            {
                PXView view =
                    Basis.Graph.Views[ViewName];

                if (view == null)
                {
                    WmsDebugTrace.Info(
                        $"{TracePrefix} Could not refresh. " +
                        $"Basis.Graph.Views did not contain " +
                        $"{ViewName}. Reason={reason}");

                    return;
                }

                view.Clear();
                view.RequestRefresh();

                WmsDebugTrace.Info(
                    $"{TracePrefix} Refresh requested. " +
                    $"ViewName={ViewName}, " +
                    $"Reason={reason}");
            }
        }

        public sealed class ClearSkippedRowsCommand
            : PickPackShip.ScanCommand
        {
            public override string Code =>
                "CLEARSKIPS";

            public override string ButtonName =>
                "ClearSkippedRows";

            public override string DisplayName =>
                "Clear Skipped Rows";

            protected override bool IsEnabled
            {
                get
                {
                    SOShipment shipment =
                        Basis?.Graph?.Document?.Current;

                    if (Basis?.Graph == null)
                    {
                        return false;
                    }

                    if (CustomerWorkflowBypass
                        .ShouldBypassTopRowWorkflow(
                            Basis.Graph,
                            shipment))
                    {
                        return false;
                    }

                    return true;
                }
            }

            protected override bool Process()
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} ClearSkippedRows " +
                    $"Process ENTER. Version={Version}");

                SOShipment shipment =
                    Basis.Graph.Document.Current;

                if (CustomerWorkflowBypass
                    .ShouldBypassTopRowWorkflow(
                        Basis.Graph,
                        shipment))
                {
                    WmsDebugTrace.Info(
                        $"{TracePrefix} ClearSkippedRows blocked " +
                        $"because customer bypasses top-row workflow.");

                    Basis.ReportWarning(
                        "Clear Skipped Rows is not required " +
                        "for this customer.");

                    return true;
                }

                PickPackShip.PackMode.Logic packLogic =
                    Basis.Get<PickPackShip.PackMode.Logic>();

                SOPackageDetailEx package =
                    packLogic?.SelectedPackage;

                if (package == null ||
                    string.IsNullOrEmpty(package.ShipmentNbr) ||
                    package.LineNbr == null)
                {
                    WmsDebugTrace.Warning(
                        $"{TracePrefix} Clear skipped rows clicked " +
                        $"but no valid selected package was found.");

                    Basis.ReportError(
                        "No selected package was found.");

                    return true;
                }

                SelectedPackageSkipState.ClearPackage(
                    Basis.Graph,
                    package.ShipmentNbr,
                    package.LineNbr);

                RequestEstimatedContentRefresh(
                    "ClearSkippedRowsCommand");

                /*
                 * Preserve the selected package after the command.
                 */
                packLogic.PackageLineNbrUI =
                    package.LineNbr;

                Basis.Graph.Packages.Current =
                    package;

                Basis.ReportInfo(
                    "Skipped rows cleared.");

                WmsDebugTrace.Info(
                    $"{TracePrefix} ClearSkippedRows Process EXIT.");

                return true;
            }

            private void RequestEstimatedContentRefresh(
                string reason)
            {
                PXView view =
                    Basis.Graph.Views[ViewName];

                if (view == null)
                {
                    WmsDebugTrace.Info(
                        $"{TracePrefix} Could not refresh. " +
                        $"Basis.Graph.Views did not contain " +
                        $"{ViewName}. Reason={reason}");

                    return;
                }

                view.Clear();
                view.RequestRefresh();

                WmsDebugTrace.Info(
                    $"{TracePrefix} Refresh requested. " +
                    $"ViewName={ViewName}, " +
                    $"Reason={reason}");
            }
        }

        [PXOverride]
        public virtual ScanMode<PickPackShip>
            DecorateScanMode(
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
                    $"{TracePrefix} Appending " +
                    $"SkipTopRowCommand and " +
                    $"ClearSkippedRowsCommand to PackMode. " +
                    $"Version={Version}");

                packMode.Intercept.CreateCommands
                    .ByAppend(
                        basis =>
                            new PickPackShip.ScanCommand[]
                            {
                                new SkipTopRowCommand(),
                                new ClearSkippedRowsCommand()
                            });
            }

            return mode;
        }
    }
}
