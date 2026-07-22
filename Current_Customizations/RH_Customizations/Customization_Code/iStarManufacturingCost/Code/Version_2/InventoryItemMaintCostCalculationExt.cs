using System;
using System.Collections;
using ASCJSMCustom.PO.CacheExt;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.PO;

namespace iStarCostCalculationExtensions
{
    /// <summary>
    /// Adds the CO_450 vendor quote reverse-cost calculation
    /// popup to the Stock Items screen, IN202500.
    ///
    /// Confirmed Silver workflow:
    ///
    /// 1. User selects a stock item.
    /// 2. User selects a vendor row.
    /// 3. Existing licensed logic calculates Precious Metal Cost.
    /// 4. User opens this popup.
    /// 5. User enters Vendor Quote Cost.
    /// 6. The customization calculates:
    ///
    ///        Fabrication / Piece =
    ///            Vendor Quote Cost - Precious Metal Cost
    ///
    /// 7. The customization updates only UsrFabricationPiece
    ///    by using SetValueExt.
    /// 8. The licensed FieldUpdated handler calls:
    ///
    ///        RecalculatePOVendorFabricationValue()
    ///
    /// 9. Licensed logic recalculates UsrFabricationCost and
    ///    the existing PXFormula recalculates Unit Cost.
    ///
    /// The action intentionally does not save automatically.
    /// The user may review the resulting costs before saving.
    /// </summary>
    public class InventoryItemMaintCostCalculationExt
        : PXGraphExtension<InventoryItemMaint>
    {
        private const string TracePrefix =
            "[CO_450 Vendor Quote Calculation]";

        #region Views

        /// <summary>
        /// Unbound filter supplying the values displayed
        /// in the reverse-cost Smart Panel.
        /// </summary>
        public PXFilter<CostCalculationFilter> CostCalculation;

        #endregion

        #region Actions

        public PXAction<InventoryItem> CalculateUnitCost;

        /// <summary>
        /// Opens the vendor quote popup for the currently
        /// selected vendor row.
        /// </summary>
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
                    "Select a stock item before calculating the vendor quote.");
            }

            POVendorInventory vendorRow =
                GetSelectedVendorRow();

            if (vendorRow?.VendorID == null)
            {
                throw new PXException(
                    "Select a vendor row before calculating the vendor quote.");
            }

            ASCJSMPOVendorInventoryExt vendorExt =
                vendorRow.GetExtension<
                    ASCJSMPOVendorInventoryExt>();

            if (vendorExt == null)
            {
                throw new PXException(
                    "The jewelry costing fields could not be loaded " +
                    "for the selected vendor.");
            }

            InitializeCostCalculationFilter(
                item,
                vendorRow,
                vendorExt);

            WebDialogResult result =
                CostCalculation.AskExt();

            if (result == WebDialogResult.OK)
            {
                ApplyCostCalculation(
                    item,
                    vendorRow,
                    vendorExt);
            }

            return adapter.Get();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Enables the action only when:
        ///
        /// - an existing stock item is selected,
        /// - a vendor row is selected, and
        /// - the stock item is editable.
        /// </summary>
        protected virtual void _(
            Events.RowSelected<InventoryItem> e)
        {
            bool hasCurrentItem =
                e.Row?.InventoryID != null;

            POVendorInventory vendorRow =
                GetSelectedVendorRow();

            bool hasSelectedVendor =
                vendorRow?.VendorID != null;

            CalculateUnitCost.SetEnabled(
                hasCurrentItem &&
                hasSelectedVendor &&
                Base.Item.Cache.AllowUpdate);
        }

        /// <summary>
        /// Recalculates Fabrication / Piece whenever the user
        /// changes Vendor Quote Cost in the popup.
        ///
        /// The corresponding ASPX control should have:
        ///
        /// CommitChanges="True"
        /// </summary>
        protected virtual void _(
            Events.FieldUpdated<
                CostCalculationFilter,
                CostCalculationFilter.vendorQuoteCost> e)
        {
            if (e.Row == null)
            {
                return;
            }

            RecalculateFabricationPiece(e.Row);
        }

        /// <summary>
        /// Recalculates Fabrication / Piece if Precious Metal Cost
        /// is changed programmatically while the popup is open.
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

            RecalculateFabricationPiece(e.Row);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Returns the vendor row currently selected in the
        /// Vendor Details grid.
        ///
        /// InventoryItemMaint normally exposes vendor records
        /// through the VendorItems data view.
        /// </summary>
        private POVendorInventory GetSelectedVendorRow()
        {
            return Base.VendorItems.Current;
        }

        /// <summary>
        /// Initializes the popup using the Precious Metal Cost
        /// already stored on the selected vendor record.
        ///
        /// Vendor Quote Cost is intentionally cleared every time
        /// the popup opens so that a previous entry is not reused.
        /// </summary>
        private void InitializeCostCalculationFilter(
            InventoryItem item,
            POVendorInventory vendorRow,
            ASCJSMPOVendorInventoryExt vendorExt)
        {
            CostCalculationFilter filter =
                CostCalculation.Current;

            if (filter == null)
            {
                filter =
                    CostCalculation.Insert(
                        new CostCalculationFilter());
            }

            decimal preciousMetalCost =
                vendorExt.UsrPreciousMetalCost ?? 0m;

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.vendorQuoteCost>(
                    filter,
                    null);

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.preciousMetalCost>(
                    filter,
                    preciousMetalCost);

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.fabricationPiece>(
                    filter,
                    null);

            CostCalculation.Update(filter);

            PXTrace.WriteInformation(
                $"{TracePrefix} Popup initialized. " +
                $"InventoryID={item.InventoryID}, " +
                $"VendorID={vendorRow.VendorID}, " +
                $"VendorLocationID={vendorRow.VendorLocationID}, " +
                $"IsDefault={vendorRow.IsDefault}, " +
                $"PreciousMetalCost={preciousMetalCost}.");
        }

        /// <summary>
        /// Performs the confirmed CO_450 reverse calculation:
        ///
        /// Fabrication / Piece =
        ///     Vendor Quote Cost - Precious Metal Cost
        ///
        /// This only updates the unbound popup field.
        /// It does not update the vendor record until the user
        /// presses OK.
        /// </summary>
        private void RecalculateFabricationPiece(
            CostCalculationFilter filter)
        {
            if (filter == null)
            {
                return;
            }

            if (filter.VendorQuoteCost == null)
            {
                CostCalculation.Cache.SetValue<
                    CostCalculationFilter.fabricationPiece>(
                        filter,
                        null);

                PXTrace.WriteInformation(
                    $"{TracePrefix} Vendor Quote Cost is empty. " +
                    "Fabrication / Piece was cleared.");

                return;
            }

            decimal vendorQuoteCost =
                filter.VendorQuoteCost.Value;

            decimal preciousMetalCost =
                filter.PreciousMetalCost ?? 0m;

            decimal fabricationPiece =
                vendorQuoteCost - preciousMetalCost;

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.fabricationPiece>(
                    filter,
                    fabricationPiece);

            PXTrace.WriteInformation(
                $"{TracePrefix} Popup calculation. " +
                $"VendorQuoteCost={vendorQuoteCost}, " +
                $"PreciousMetalCost={preciousMetalCost}, " +
                $"FabricationPiece={fabricationPiece}.");
        }

        /// <summary>
        /// Validates and applies the popup result to the selected
        /// POVendorInventory row.
        ///
        /// Only UsrFabricationPiece is updated.
        ///
        /// SetValueExt is required because the licensed
        /// customization has this existing handler:
        ///
        /// FieldUpdated&lt;
        ///     POVendorInventory,
        ///     usrFabricationPiece&gt;
        ///
        /// That handler calls:
        ///
        /// RecalculatePOVendorFabricationValue(row)
        ///
        /// which recalculates UsrFabricationCost.
        /// </summary>
        private void ApplyCostCalculation(
            InventoryItem item,
            POVendorInventory vendorRow,
            ASCJSMPOVendorInventoryExt vendorExt)
        {
            CostCalculationFilter filter =
                CostCalculation.Current;

            if (filter == null)
            {
                throw new PXException(
                    "The vendor quote calculation values are unavailable.");
            }

            if (filter.VendorQuoteCost == null)
            {
                throw new PXException(
                    "Enter the Vendor Quote Cost.");
            }

            decimal preciousMetalCost =
                filter.PreciousMetalCost ?? 0m;

            decimal vendorQuoteCost =
                filter.VendorQuoteCost.Value;

            decimal fabricationPiece =
                vendorQuoteCost - preciousMetalCost;

            if (fabricationPiece < 0m)
            {
                throw new PXException(
                    "Vendor Quote Cost cannot be less than " +
                    "Precious Metal Cost.");
            }

            PXCache vendorCache =
                Base.VendorItems.Cache;

            vendorCache.SetValueExt<
                ASCJSMPOVendorInventoryExt.usrFabricationPiece>(
                    vendorRow,
                    fabricationPiece);

            Base.VendorItems.Update(vendorRow);

            ASCJSMPOVendorInventoryExt updatedVendorExt =
                vendorRow.GetExtension<
                    ASCJSMPOVendorInventoryExt>();

            PXTrace.WriteInformation(
                $"{TracePrefix} Calculation applied. " +
                $"InventoryID={item.InventoryID}, " +
                $"VendorID={vendorRow.VendorID}, " +
                $"VendorLocationID={vendorRow.VendorLocationID}, " +
                $"IsDefault={vendorRow.IsDefault}, " +
                $"VendorQuoteCost={vendorQuoteCost}, " +
                $"PreciousMetalCost={preciousMetalCost}, " +
                $"FabricationPiece={fabricationPiece}, " +
                $"ResultingFabricationCost=" +
                $"{updatedVendorExt?.UsrFabricationCost}, " +
                $"ResultingUnitCost=" +
                $"{updatedVendorExt?.UsrUnitCost}.");

            /*
             * Do not press Save here.
             *
             * The record remains dirty so that the user can review:
             *
             * - Fabrication / Piece
             * - Fabrication / Value Add
             * - Unit Cost
             *
             * before saving the Stock Item.
             */
        }

        #endregion

        public static bool IsActive()
        {
            return true;
        }
    }
}