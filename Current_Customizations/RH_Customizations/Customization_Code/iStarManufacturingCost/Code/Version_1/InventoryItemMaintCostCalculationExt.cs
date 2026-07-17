using System.Collections;
using PX.Data;
using PX.Objects.IN;

namespace iStarCostCalculationExtensions
{
    /// <summary>
    /// Adds the CO_450 unit-cost calculation action and popup
    /// to the Stock Items screen (IN202500).
    ///
    /// This first version only opens the popup.
    /// It does not yet calculate or update any item costs.
    /// </summary>
    public class InventoryItemMaintCostCalculationExt
        : PXGraphExtension<InventoryItemMaint>
    {
        #region Views

        /// <summary>
        /// Unbound filter that supplies the values displayed
        /// in the cost-calculation Smart Panel.
        /// </summary>
        public PXFilter<CostCalculationFilter> CostCalculation;

        #endregion

        #region Actions

        public PXAction<InventoryItem> CalculateUnitCost;

        [PXButton(CommitChanges = true)]
        [PXUIField(
            DisplayName = "Calculate Unit Cost",
            MapEnableRights = PXCacheRights.Update,
            MapViewRights = PXCacheRights.Select)]
        protected virtual IEnumerable calculateUnitCost(
            PXAdapter adapter)
        {
            InventoryItem item = Base.Item.Current;

            if (item == null)
            {
                throw new PXException(
                    "Select a stock item before calculating the unit cost.");
            }

            InitializeCostCalculationFilter();

            WebDialogResult result =
                CostCalculation.AskExt();

            if (result == WebDialogResult.OK)
            {
                // The calculation and application logic will be added
                // after the popup itself has been tested successfully.
                PXTrace.WriteInformation(
                    "CO_450 Calculate Unit Cost popup returned OK.");
            }

            return adapter.Get();
        }

        #endregion

        #region Event Handlers

        protected virtual void _(
            Events.RowSelected<InventoryItem> e)
        {
            bool hasCurrentItem =
                e.Row?.InventoryID != null;

            CalculateUnitCost.SetEnabled(
                hasCurrentItem &&
                Base.Item.Cache.AllowUpdate);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Resets the popup values before it is displayed.
        ///
        /// In a later version, this method will populate the
        /// metal weight and calculate the precious-metal cost.
        /// </summary>
        private void InitializeCostCalculationFilter()
        {
            CostCalculationFilter filter =
                CostCalculation.Current;

            if (filter == null)
            {
                filter =
                    CostCalculation.Insert(
                        new CostCalculationFilter());
            }

            filter.UnitCost = null;
            filter.MetalWeight = null;
            filter.PreciousMetalCost = null;
            filter.FabricationCost = null;

            CostCalculation.Update(filter);
        }

        #endregion

        public static bool IsActive()
        {
            return true;
        }
    }
}