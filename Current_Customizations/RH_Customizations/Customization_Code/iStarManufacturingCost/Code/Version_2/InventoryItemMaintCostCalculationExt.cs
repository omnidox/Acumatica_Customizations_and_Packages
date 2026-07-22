using System;
using System.Collections;
using PX.Data;
using PX.Objects.IN;

namespace iStarCostCalculationExtensions
{
    /// <summary>
    /// Adds the CO_450 reverse unit-cost calculation popup
    /// to the Stock Items screen (IN202500).
    ///
    /// Current implementation:
    ///
    /// 1. Opens the popup.
    /// 2. Allows the user to enter a vendor Unit Cost.
    /// 3. Calculates:
    ///
    ///    Fabrication Cost =
    ///        Unit Cost - Precious Metal Cost
    ///
    /// Precious-metal calculation through ASCJSMCostBuilder
    /// will be connected in the next implementation stage.
    /// </summary>
    public class InventoryItemMaintCostCalculationExt
        : PXGraphExtension<InventoryItemMaint>
    {
        #region Views

        /// <summary>
        /// Unbound filter supplying the values shown
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

            if (item?.InventoryID == null)
            {
                throw new PXException(
                    "Select a stock item before calculating the unit cost.");
            }

            InitializeCostCalculationFilter();

            WebDialogResult result =
                CostCalculation.AskExt();

            if (result == WebDialogResult.OK)
            {
                CostCalculationFilter filter =
                    CostCalculation.Current;

                PXTrace.WriteInformation(
                    "CO_450 popup returned OK. " +
                    $"InventoryID={item.InventoryID}, " +
                    $"UnitCost={filter?.UnitCost}, " +
                    $"MetalWeight={filter?.MetalWeight}, " +
                    $"PreciousMetalCost={filter?.PreciousMetalCost}, " +
                    $"FabricationCost={filter?.FabricationCost}.");

                // We are deliberately not updating the InventoryItem
                // or licensed customization fields yet.
                //
                // That will be added only after the popup calculation
                // has been tested successfully.
            }

            return adapter.Get();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Enables the action only when an existing stock item
        /// is selected and the record is editable.
        /// </summary>
        protected virtual void _(
            Events.RowSelected<InventoryItem> e)
        {
            bool hasCurrentItem =
                e.Row?.InventoryID != null;

            CalculateUnitCost.SetEnabled(
                hasCurrentItem &&
                Base.Item.Cache.AllowUpdate);
        }

        /// <summary>
        /// Recalculates Fabrication / Value Add whenever
        /// the user changes Unit Cost in the popup.
        ///
        /// The ASPX field has CommitChanges="True",
        /// so changing Unit Cost sends a callback and invokes
        /// this event handler.
        /// </summary>
        protected virtual void _(
            Events.FieldUpdated<
                CostCalculationFilter,
                CostCalculationFilter.unitCost> e)
        {
            if (e.Row == null)
            {
                return;
            }

            RecalculateFabricationCost(e.Row);
        }

        /// <summary>
        /// Also recalculates fabrication whenever precious-metal
        /// cost is changed programmatically.
        ///
        /// This will become useful when ASCJSMCostBuilder is
        /// connected in the next stage.
        /// </summary>
        protected virtual void _(
            Events.FieldUpdated<
                CostCalculationFilter,
                CostCalculationFilter.preciousMetalCost> e)
        {
            if (e.Row == null)
            {
                return;
            }

            RecalculateFabricationCost(e.Row);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Resets the popup before it is displayed.
        ///
        /// Metal Weight and Precious Metal Cost remain zero
        /// until the licensed ASCJSM costing engine is connected.
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

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.unitCost>(
                    filter,
                    null);

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.metalWeight>(
                    filter,
                    0m);

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.preciousMetalCost>(
                    filter,
                    0m);

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.fabricationCost>(
                    filter,
                    0m);

            CostCalculation.Update(filter);
        }

        /// <summary>
        /// Performs the reverse fabrication calculation.
        ///
        /// Fabrication Cost =
        ///     Vendor Unit Cost - Precious Metal Cost
        /// </summary>
        private void RecalculateFabricationCost(
            CostCalculationFilter filter)
        {
            if (filter == null)
            {
                return;
            }

            decimal unitCost =
                filter.UnitCost ?? 0m;

            decimal preciousMetalCost =
                filter.PreciousMetalCost ?? 0m;

            decimal fabricationCost =
                unitCost - preciousMetalCost;

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.fabricationCost>(
                    filter,
                    fabricationCost);

            CostCalculation.Update(filter);

            PXTrace.WriteInformation(
                "CO_450 fabrication calculation. " +
                $"UnitCost={unitCost}, " +
                $"PreciousMetalCost={preciousMetalCost}, " +
                $"FabricationCost={fabricationCost}.");
        }

        #endregion

        public static bool IsActive()
        {
            return true;
        }
    }
}