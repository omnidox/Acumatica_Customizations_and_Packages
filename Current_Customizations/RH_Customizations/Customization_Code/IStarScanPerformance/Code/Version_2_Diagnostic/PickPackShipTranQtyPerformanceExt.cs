using AA.Objects.Labels.TranQty;
using PX.Data;
using PX.Objects.SO.WMS;

namespace IStar.ScanPerformance
{
    /// <summary>
    /// Prevents the Advanced Labels transaction-quantity formula from
    /// executing while Pick, Pack, and Ship loads SOShipLineSplit records.
    ///
    /// The formula is removed only from PickPackShip.Host. The normal
    /// SOShipmentEntry graph used by SO302000 retains the original formula.
    /// </summary>
    public class PickPackShipTranQtyPerformanceExt
        : PXGraphExtension<PickPackShip.Host>
    {
        public static bool IsActive()
        {
            return true;
        }

        /// <summary>
        /// Removes the global Advanced Labels PXFormula from UsrALTranQty
        /// for the PickPackShip host graph.
        /// </summary>
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXRemoveBaseAttribute(typeof(PXFormulaAttribute))]
        protected virtual void _(
            Events.CacheAttached<
                ALSOShipLineSplitExt.usrALTranQty> e)
        {
        }
    }
}