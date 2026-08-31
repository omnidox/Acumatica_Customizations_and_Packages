using System;
using System.Collections;
using System.Collections.Generic;
using PX.BarcodeProcessing;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.SO.WMS;

namespace IStar.ScanPerformance
{
    /// <summary>
    /// Replaces the standard Pack-mode Packed view delegate. The standard
    /// delegate cross-joins package links with the complete PickedForPack
    /// result in memory and invokes a PXResult LINQ conversion fallback.
    /// This implementation retrieves only the splits in the selected package.
    /// </summary>
    public class PackModePackedViewOptimization
        : BarcodeDrivenStateMachine<
            PickPackShip,
            PickPackShip.Host>
            .ScanExtension<
                WMS.PackModeLogicExt>
    {
        public static bool IsActive()
        {
            return true;
        }

        public delegate IEnumerable PackedDelegate();

        [PXOverride]
        public virtual IEnumerable packed(
            PackedDelegate baseMethod)
        {
            PickPackShip.PackMode.Logic mode =
                Basis.Get<PickPackShip.PackMode.Logic>();

            if (Basis.Header == null ||
                string.IsNullOrEmpty(Basis.RefNbr) ||
                mode == null ||
                mode.PackageLineNbrUI == null)
            {
                return Array.Empty<
                    PXResult<SOShipLineSplit, SOShipLine>>();
            }

            var result = new List<
                PXResult<SOShipLineSplit, SOShipLine>>();

            PXResultset<SOShipLineSplit> rows =
                PXSelectJoin<
                    SOShipLineSplit,
                    InnerJoin<
                        SOShipLine,
                        On<
                            SOShipLine.shipmentNbr,
                            Equal<SOShipLineSplit.shipmentNbr>,
                            And<
                                SOShipLine.lineNbr,
                                Equal<SOShipLineSplit.lineNbr>>>,
                    InnerJoin<
                        SOShipLineSplitPackage,
                        On<
                            SOShipLineSplitPackage.shipmentNbr,
                            Equal<SOShipLineSplit.shipmentNbr>,
                            And<
                                SOShipLineSplitPackage.shipmentLineNbr,
                                Equal<SOShipLineSplit.lineNbr>,
                            And<
                                SOShipLineSplitPackage.shipmentSplitLineNbr,
                                Equal<SOShipLineSplit.splitLineNbr>>>>>>,
                    Where<
                        SOShipLineSplitPackage.shipmentNbr,
                        Equal<Required<
                            SOShipLineSplitPackage.shipmentNbr>>,
                        And<
                            SOShipLineSplitPackage.packageLineNbr,
                            Equal<Required<
                                SOShipLineSplitPackage.packageLineNbr>>>>,
                    OrderBy<
                        Asc<
                            SOShipLineSplit.lineNbr,
                        Asc<SOShipLineSplit.splitLineNbr>>>>
                .Select(
                    Basis,
                    Basis.RefNbr,
                    mode.PackageLineNbrUI);

            // The query is fully materialized before this loop. Construct the
            // two-table result explicitly instead of calling LINQ Cast(),
            // which invokes SQLQueryable PXResult.Convert().
            foreach (PXResult<SOShipLineSplit> rawRow in rows)
            {
                SOShipLineSplit split =
                    rawRow.GetItem<SOShipLineSplit>();
                SOShipLine line =
                    rawRow.GetItem<SOShipLine>();

                if (split != null && line != null)
                {
                    result.Add(
                        new PXResult<
                            SOShipLineSplit,
                            SOShipLine>(split, line));
                }
            }

            return result;
        }
    }
}
