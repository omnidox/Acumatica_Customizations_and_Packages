using System;
using System.Collections.Generic;
using System.Linq;
using PX.BarcodeProcessing;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.SO.WMS;

using WmsPackModeLogicExt = WMS.PackModeLogicExt;
using WmsPackageExt = WMS.SOPackageDetailExt;

namespace CustomWMS2
{
    /// <summary>
    /// Adds a Confirm All Packages command to Pick, Pack, and Ship.
    ///
    /// The command confirms every unconfirmed carton that has contents,
    /// using the same weight Acumatica calculates when a package is
    /// confirmed without weighing: the carton's existing weight, or the
    /// box weight plus item weight times packed quantity.
    ///
    /// Cartons are skipped and reported when:
    ///   - packed totals do not match the WMS carton plan, or
    ///   - the weight fails Acumatica's weight validation
    ///     (for example, it exceeds the box's maximum weight).
    ///
    /// Master-pack cartons are left alone; they keep their own
    /// confirmation flow in the WMS package.
    /// </summary>
    public class ConfirmAllPackagesCommandExtension
        : PickPackShip.ScanExtension
    {
        private const string TracePrefix =
            "[ConfirmAllPackagesCommand]";

        private const string Version =
            "2026-09-24-V1-CONFIRM-ALL-01";

        public static bool IsActive()
        {
            return true;
        }

        public sealed class ConfirmAllPackagesCommand
            : PickPackShip.ScanCommand
        {
            public override string Code =>
                "CONFIRMALLPACKAGES";

            public override string ButtonName =>
                "ConfirmAllPackages";

            public override string DisplayName =>
                "Confirm All Packages";

            protected override bool IsEnabled
            {
                get
                {
                    if (!WmsTransferAuthorization.IsAuthorized())
                        return false;

                    /*
                     * DocumentIsEditable is false when no shipment is
                     * loaded or the shipment is already confirmed.
                     */
                    if (Basis?.DocumentIsEditable != true)
                        return false;

                    PickPackShip.PackMode.Logic packLogic =
                        Basis.Get<PickPackShip.PackMode.Logic>();

                    return packLogic?.HasConfirmableBoxes == true;
                }
            }

            protected override bool Process()
            {
                WmsDebugTrace.Info(
                    $"{TracePrefix} Process ENTER. " +
                    $"Version={Version}, Command={Code}");

                if (!WmsTransferAuthorization.IsAuthorized())
                {
                    Basis.ReportError(
                        "You do not have permission to confirm all packages.");

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

                    return true;
                }

                if (shipment?.Confirmed == true)
                {
                    Basis.ReportError(
                        "The shipment is confirmed and its packages cannot be changed.");

                    return true;
                }

                ConfirmAllLongRunData operationData =
                    new ConfirmAllLongRunData
                    {
                        ShipmentNbr = shipmentNbr
                    };

                Basis
                    .WaitFor<ConfirmAllLongRunData>(
                        (longRunBasis, data) =>
                        {
                            ExecuteConfirmAll(
                                longRunBasis,
                                data);
                        })
                    .WithDescription(
                        "Confirming all cartons for shipment {0}.",
                        shipmentNbr)
                    .OnSuccess(success =>
                        success
                            .Say(
                                "Confirm all packages operation completed.")
                            .ResetFull()
                            .Do(
                                (completedBasis, data) =>
                                {
                                    ReportResult(
                                        completedBasis,
                                        data);

                                    WmsDebugTrace.Info(
                                        $"{TracePrefix} Long operation SUCCESS. " +
                                        $"ShipmentNbr={data.ShipmentNbr}, " +
                                        $"Confirmed={data.ConfirmedCartons}, " +
                                        $"Mismatch={data.MismatchCartons.Count}, " +
                                        $"Weight={data.WeightCartons.Count}, " +
                                        $"MasterPack={data.MasterPackCartons}");
                                }))
                    .OnFail(fail =>
                        fail
                            .Say(
                                "Confirming all packages failed. " +
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

            private static void ExecuteConfirmAll(
                PickPackShip longRunBasis,
                ConfirmAllLongRunData data)
            {
                if (longRunBasis == null)
                {
                    throw new PXException(
                        "The WMS processing context could not be created.");
                }

                if (data == null ||
                    string.IsNullOrWhiteSpace(data.ShipmentNbr))
                {
                    throw new PXException(
                        "The shipment number was not supplied to the confirm operation.");
                }

                /*
                 * Repeat authorization inside the long-running operation.
                 */
                if (!WmsTransferAuthorization.IsAuthorized())
                {
                    throw new PXException(
                        "You do not have permission to confirm all packages.");
                }

                /*
                 * The WMS plan comparison returns "match" for every
                 * carton when the scan header is not in Pack mode,
                 * so refuse to run rather than confirm unchecked cartons.
                 */
                if (longRunBasis.Header?.Mode !=
                    PickPackShip.PackMode.Value)
                {
                    throw new PXException(
                        "Confirm All Packages can only run in Pack mode.");
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
                        "Shipment {0} is confirmed and its packages cannot be changed.",
                        shipmentNbr);
                }

                longRunBasis.Graph.Document.Current =
                    shipment;

                PickPackShip.PackMode.Logic packLogic =
                    longRunBasis.Get<
                        PickPackShip.PackMode.Logic>();

                WmsPackModeLogicExt wmsPackLogic =
                    longRunBasis.Get<
                        WmsPackModeLogicExt>();

                if (packLogic == null ||
                    wmsPackLogic == null)
                {
                    throw new PXException(
                        "The Acumatica Pack mode logic could not be initialized.");
                }

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

                foreach (SOPackageDetailEx package in packages)
                {
                    string cartonNbr =
                        GetCartonIdentifier(
                            package);

                    if (package.Confirmed == true)
                        continue;

                    /*
                     * Master-pack cartons are confirmed through the
                     * WMS master-pack scan flow, not here.
                     */
                    if (package
                            .GetExtension<WmsPackageExt>()?
                            .UsrIsParentBox == true)
                    {
                        data.MasterPackCartons++;
                        continue;
                    }

                    if (packLogic.IsPackageEmpty(package))
                        continue;

                    /*
                     * Same check the WMS package runs when Confirm
                     * Package is clicked: packed totals per style
                     * must equal the carton plan.
                     */
                    if (!wmsPackLogic
                            .DoesSelectedPackageMatchEstimates(
                                package.LineNbr))
                    {
                        data.MismatchCartons.Add(
                            cartonNbr);

                        WmsDebugTrace.Info(
                            $"{TracePrefix} Skipped, plan mismatch. " +
                            $"ShipmentNbr={shipmentNbr}, " +
                            $"PackageLineNbr={package.LineNbr}, " +
                            $"Carton={cartonNbr}");

                        continue;
                    }

                    /*
                     * Acumatica keeps a non-zero carton weight as is
                     * and calculates one only when the weight is zero.
                     */
                    decimal weight =
                        (package.Weight ?? 0m) != 0m
                            ? package.Weight.Value
                            : CalculateWeightFromContents(
                                longRunBasis,
                                package);

                    weight =
                        Math.Round(
                            weight,
                            4);

                    /*
                     * Runs every weight rule Acumatica applies to a
                     * package, including the box maximum weight.
                     */
                    string weightError;

                    if (!longRunBasis.IsValid<
                            SOPackageDetail.weight,
                            SOPackageDetailEx>(
                                package,
                                weight,
                                out weightError))
                    {
                        data.WeightCartons.Add(
                            package.MaxWeight != null
                                ? $"{cartonNbr} ({weight:0.####} > max {package.MaxWeight:0.####})"
                                : $"{cartonNbr} ({weightError})");

                        WmsDebugTrace.Info(
                            $"{TracePrefix} Skipped, weight invalid. " +
                            $"ShipmentNbr={shipmentNbr}, " +
                            $"PackageLineNbr={package.LineNbr}, " +
                            $"Carton={cartonNbr}, " +
                            $"Weight={weight}, " +
                            $"MaxWeight={package.MaxWeight}, " +
                            $"Error={weightError}");

                        continue;
                    }

                    package.Weight =
                        weight;

                    package.Confirmed =
                        true;

                    longRunBasis.Graph.Packages.Update(
                        package);

                    data.ConfirmedCartons++;

                    WmsDebugTrace.Info(
                        $"{TracePrefix} Carton confirmed. " +
                        $"ShipmentNbr={shipmentNbr}, " +
                        $"PackageLineNbr={package.LineNbr}, " +
                        $"Carton={cartonNbr}, " +
                        $"Weight={weight}");
                }

                /*
                 * Save once. If Save throws, the framework clears the
                 * graph and no carton is confirmed.
                 */
                if (data.ConfirmedCartons > 0)
                {
                    longRunBasis.Save.Press();
                }

                WmsDebugTrace.Info(
                    $"{TracePrefix} Long operation EXIT. " +
                    $"ShipmentNbr={shipmentNbr}, " +
                    $"Confirmed={data.ConfirmedCartons}, " +
                    $"Mismatch={data.MismatchCartons.Count}, " +
                    $"Weight={data.WeightCartons.Count}, " +
                    $"MasterPack={data.MasterPackCartons}");
            }

            /// <summary>
            /// Mirrors Acumatica's AutoCalculateBoxWeightBasedOnItems:
            /// empty box weight plus item base weight times base
            /// packed quantity.
            /// </summary>
            private static decimal CalculateWeightFromContents(
                PickPackShip basis,
                SOPackageDetailEx package)
            {
                decimal weight =
                    (CSBox.PK.Find(
                        basis,
                        package.BoxID)?
                        .BoxWeight)
                    .GetValueOrDefault();

                SOShipLineSplitPackage[] contents =
                    basis.Graph.PackageDetailExt
                        .PackageDetailSplit
                        .SelectMain(
                            package.ShipmentNbr,
                            package.LineNbr);

                foreach (SOShipLineSplitPackage content in contents)
                {
                    InventoryItem item =
                        InventoryItem.PK.Find(
                            basis,
                            content.InventoryID);

                    weight +=
                        (item?.BaseWeight).GetValueOrDefault() *
                        content.BasePackedQty.GetValueOrDefault();
                }

                return weight;
            }

            private static void ReportResult(
                PickPackShip basis,
                ConfirmAllLongRunData data)
            {
                bool anySkipped =
                    data.MismatchCartons.Count > 0 ||
                    data.WeightCartons.Count > 0;

                if (data.ConfirmedCartons == 0 &&
                    !anySkipped)
                {
                    basis.ReportWarning(
                        "There were no unconfirmed cartons with contents to confirm.");

                    return;
                }

                string message =
                    $"Confirmed {data.ConfirmedCartons} carton(s).";

                if (data.MismatchCartons.Count > 0)
                {
                    message +=
                        " Skipped, packed qty does not match plan: " +
                        string.Join(", ", data.MismatchCartons) +
                        ".";
                }

                if (data.WeightCartons.Count > 0)
                {
                    message +=
                        " Skipped, weight exceeds box max: " +
                        string.Join(", ", data.WeightCartons) +
                        ".";
                }

                if (anySkipped)
                    basis.ReportWarning("{0}", message);
                else
                    basis.ReportInfo("{0}", message);
            }

            private static string GetCartonIdentifier(
                SOPackageDetailEx package)
            {
                if (package == null)
                    return string.Empty;

                string cartonNbr =
                    package
                        .GetExtension<WmsPackageExt>()?
                        .UsrCartonNbr?
                        .Trim();

                return !string.IsNullOrEmpty(cartonNbr)
                    ? "#" + cartonNbr
                    : $"{package.BoxID?.Trim()} line {package.LineNbr}";
            }

            [Serializable]
            private sealed class ConfirmAllLongRunData
            {
                public string ShipmentNbr { get; set; }

                public int ConfirmedCartons { get; set; }

                public int MasterPackCartons { get; set; }

                public List<string> MismatchCartons { get; set; } =
                    new List<string>();

                public List<string> WeightCartons { get; set; } =
                    new List<string>();
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
                packMode.Intercept.CreateCommands.ByAppend(
                    basis => new PickPackShip.ScanCommand[]
                    {
                        new ConfirmAllPackagesCommand()
                    });
            }

            return mode;
        }
    }
}
