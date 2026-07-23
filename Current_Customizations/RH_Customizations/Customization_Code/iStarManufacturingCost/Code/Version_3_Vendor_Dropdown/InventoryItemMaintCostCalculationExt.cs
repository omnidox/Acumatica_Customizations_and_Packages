using System;
using System.Collections;
using ASCJSMCustom.PO.CacheExt;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.IN;
using PX.Objects.PO;

namespace iStarCostCalculationExtensions
{
    /// <summary>
    /// Adds the CO_450 vendor quote reverse-cost calculation
    /// popup to the Stock Items screen, IN202500.
    ///
    /// Workflow:
    ///
    /// 1. User selects a stock item.
    /// 2. User selects a row on the Vendors tab.
    /// 3. User opens the Calculate Vendor Quote popup.
    /// 4. The popup defaults to the selected vendor row.
    /// 5. The user may optionally choose a different vendor row
    ///    through the popup selector.
    /// 6. The popup loads the selected vendor's:
    ///
    ///        - Vendor name
    ///        - Precious Metal Cost
    ///
    /// 7. The user enters Vendor Quote Cost.
    /// 8. The customization calculates:
    ///
    ///        Fabrication / Piece =
    ///            Vendor Quote Cost - Precious Metal Cost
    ///
    /// 9. On OK, the customization updates only:
    ///
    ///        UsrFabricationPiece
    ///
    ///    using SetValueExt.
    ///
    /// 10. Licensed jewelry-costing logic recalculates:
    ///
    ///        - UsrFabricationCost
    ///        - UsrUnitCost
    ///
    /// The action intentionally does not save automatically.
    /// </summary>
    public class InventoryItemMaintCostCalculationExt
        : PXGraphExtension<InventoryItemMaint>
    {
        private const string TracePrefix =
            "[CO_450 Vendor Quote Calculation]";

        #region Views

        /// <summary>
        /// Unbound filter supplying the values displayed
        /// in the vendor quote Smart Panel.
        /// </summary>
        public PXFilter<CostCalculationFilter> CostCalculation;

        #endregion

        #region Actions

        public PXAction<InventoryItem> CalculateVendorQuote;

        [PXButton(CommitChanges = true)]
        [PXUIField(
            DisplayName = "Calculate Vendor Quote",
            MapEnableRights = PXCacheRights.Update,
            MapViewRights = PXCacheRights.Select)]
        protected virtual IEnumerable calculateVendorQuote(
            PXAdapter adapter)
        {
            InventoryItem item = Base.Item.Current;

            if (item?.InventoryID == null)
            {
                throw new PXException(
                    "Select a stock item before calculating " +
                    "the vendor quote.");
            }

            POVendorInventory initiallySelectedVendorRow =
                GetSelectedVendorRow();

            if (initiallySelectedVendorRow?.RecordID == null)
            {
                throw new PXException(
                    "Select a vendor row before calculating " +
                    "the vendor quote.");
            }

            WebDialogResult result =
                CostCalculation.AskExt(
                    (graph, viewName) =>
                    {
                        InitializeCostCalculationFilter(
                            item,
                            initiallySelectedVendorRow);
                    });

            if (result == WebDialogResult.OK)
            {
                ApplyCostCalculation(item);
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
        /// - the Stock Item record is editable.
        /// </summary>
        protected virtual void _(
            Events.RowSelected<InventoryItem> e)
        {
            bool hasCurrentItem =
                e.Row?.InventoryID != null;

            POVendorInventory vendorRow =
                GetSelectedVendorRow();

            bool hasSelectedVendor =
                vendorRow?.RecordID != null;

            CalculateVendorQuote.SetEnabled(
                hasCurrentItem
                && hasSelectedVendor
                && Base.Item.Cache.AllowUpdate);
        }

        /// <summary>
        /// Reloads vendor-specific popup values whenever the user
        /// selects a different vendor record in the popup.
        ///
        /// The corresponding ASPX PXSelector must use:
        ///
        /// CommitChanges="True"
        /// </summary>
        protected virtual void _(
            Events.FieldUpdated<
                CostCalculationFilter,
                CostCalculationFilter.vendorRecordID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            LoadSelectedVendorIntoFilter(e.Row);
        }

        /// <summary>
        /// Recalculates Fabrication / Piece whenever the user
        /// changes Vendor Quote Cost.
        ///
        /// The corresponding ASPX control must use:
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

        #region Vendor Selection

        /// <summary>
        /// Returns the vendor row currently selected on the
        /// Vendors tab.
        /// </summary>
        private POVendorInventory GetSelectedVendorRow()
        {
            return Base.VendorItems.Current;
        }

        /// <summary>
        /// Finds a vendor inventory row through the VendorItems
        /// view by its unique RecordID.
        ///
        /// Searching through the graph view helps ensure that the
        /// returned row participates in the graph's cache and
        /// event pipeline.
        /// </summary>
        private POVendorInventory FindVendorRow(
            int? vendorRecordID)
        {
            if (vendorRecordID == null)
            {
                return null;
            }

            POVendorInventory vendorRow =
                Base.VendorItems.Search<
                    POVendorInventory.recordID>(
                        vendorRecordID);

            if (vendorRow != null)
            {
                return vendorRow;
            }

            /*
             * Fallback query in case Search does not locate the row
             * because of the current state of the VendorItems view.
             */
            vendorRow =
                PXSelect<
                    POVendorInventory,
                    Where<
                        POVendorInventory.recordID,
                        Equal<
                            Required<
                                POVendorInventory.recordID>>>>
                .Select(
                    Base,
                    vendorRecordID);

            return vendorRow;
        }

        /// <summary>
        /// Retrieves the vendor account associated with a
        /// POVendorInventory row.
        /// </summary>
        private Vendor FindVendor(
            int? vendorID)
        {
            if (vendorID == null)
            {
                return null;
            }

            Vendor vendor =
                PXSelect<
                    Vendor,
                    Where<
                        Vendor.bAccountID,
                        Equal<
                            Required<
                                Vendor.bAccountID>>>>
                .Select(
                    Base,
                    vendorID);

            return vendor;
        }

        #endregion

        #region Popup Initialization

        /// <summary>
        /// Initializes the popup using the vendor row currently
        /// selected on the Vendors tab.
        ///
        /// This method is called through the AskExt initializer,
        /// so the values are initialized only when the popup is
        /// first opened. They are not cleared again when the user
        /// presses OK.
        /// </summary>
        private void InitializeCostCalculationFilter(
            InventoryItem item,
            POVendorInventory vendorRow)
        {
            if (vendorRow?.RecordID == null)
            {
                throw new PXException(
                    "The selected vendor record is unavailable.");
            }

            CostCalculationFilter filter =
                CostCalculation.Current;

            if (filter == null)
            {
                filter =
                    CostCalculation.Insert(
                        new CostCalculationFilter());
            }

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.vendorRecordID>(
                    filter,
                    vendorRow.RecordID);

            /*
             * LoadSelectedVendorIntoFilter populates:
             *
             * - Vendor Name
             * - Precious Metal Cost
             *
             * It also recalculates Fabrication / Piece.
             */
            LoadSelectedVendorIntoFilter(filter);

            /*
             * The vendor quote is intentionally cleared whenever
             * the popup is newly opened so that a previous quote
             * is not accidentally reused.
             */
            CostCalculation.Cache.SetValue<
                CostCalculationFilter.vendorQuoteCost>(
                    filter,
                    null);

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.fabricationPiece>(
                    filter,
                    null);

            CostCalculation.Update(filter);

            PXTrace.WriteInformation(
                $"{TracePrefix} Popup initialized. " +
                $"InventoryID={item.InventoryID}, " +
                $"VendorRecordID={vendorRow.RecordID}, " +
                $"VendorID={vendorRow.VendorID}, " +
                $"VendorLocationID={vendorRow.VendorLocationID}, " +
                $"IsDefault={vendorRow.IsDefault}, " +
                $"VendorName={filter.VendorName}, " +
                $"PreciousMetalCost={filter.PreciousMetalCost}.");
        }

        /// <summary>
        /// Loads vendor-specific information into the popup after
        /// the popup Vendor selector changes.
        /// </summary>
        private void LoadSelectedVendorIntoFilter(
            CostCalculationFilter filter)
        {
            if (filter == null)
            {
                return;
            }

            if (filter.VendorRecordID == null)
            {
                ClearVendorInformation(filter);
                return;
            }

            POVendorInventory vendorRow =
                FindVendorRow(filter.VendorRecordID);

            if (vendorRow == null)
            {
                ClearVendorInformation(filter);

                throw new PXException(
                    "The selected vendor record could not be found.");
            }

            /*
             * Confirm that the selected vendor row belongs to the
             * Stock Item currently loaded in the graph.
             */
            if (vendorRow.InventoryID !=
                Base.Item.Current?.InventoryID)
            {
                ClearVendorInformation(filter);

                throw new PXException(
                    "The selected vendor does not belong to " +
                    "the current stock item.");
            }

            ASCJSMPOVendorInventoryExt vendorExt =
                vendorRow.GetExtension<
                    ASCJSMPOVendorInventoryExt>();

            if (vendorExt == null)
            {
                ClearVendorInformation(filter);

                throw new PXException(
                    "The jewelry costing fields could not be loaded " +
                    "for the selected vendor.");
            }

            Vendor vendor =
                FindVendor(vendorRow.VendorID);

            decimal preciousMetalCost =
                vendorExt.UsrPreciousMetalCost ?? 0m;

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.vendorName>(
                    filter,
                    vendor?.AcctName);

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.preciousMetalCost>(
                    filter,
                    preciousMetalCost);

            RecalculateFabricationPiece(filter);

            CostCalculation.Update(filter);

            PXTrace.WriteInformation(
                $"{TracePrefix} Popup vendor loaded. " +
                $"VendorRecordID={vendorRow.RecordID}, " +
                $"VendorID={vendorRow.VendorID}, " +
                $"VendorLocationID={vendorRow.VendorLocationID}, " +
                $"SubItemID={vendorRow.SubItemID}, " +
                $"VendorName={vendor?.AcctName}, " +
                $"PreciousMetalCost={preciousMetalCost}.");
        }

        /// <summary>
        /// Clears vendor-dependent popup fields.
        /// </summary>
        private void ClearVendorInformation(
            CostCalculationFilter filter)
        {
            CostCalculation.Cache.SetValue<
                CostCalculationFilter.vendorName>(
                    filter,
                    null);

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.preciousMetalCost>(
                    filter,
                    null);

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.fabricationPiece>(
                    filter,
                    null);
        }

        #endregion

        #region Popup Calculation

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
                $"VendorRecordID={filter.VendorRecordID}, " +
                $"VendorQuoteCost={vendorQuoteCost}, " +
                $"PreciousMetalCost={preciousMetalCost}, " +
                $"FabricationPiece={fabricationPiece}.");
        }

        #endregion

        #region Apply Calculation

        /// <summary>
        /// Validates and applies the popup result to the vendor
        /// row selected in the popup.
        ///
        /// The popup vendor may be different from the row that
        /// was originally selected on the Vendors tab.
        ///
        /// Only UsrFabricationPiece is explicitly updated.
        ///
        /// SetValueExt is required because the licensed
        /// customization has an existing handler for:
        ///
        /// FieldUpdated&lt;
        ///     POVendorInventory,
        ///     usrFabricationPiece&gt;
        ///
        /// That handler recalculates UsrFabricationCost.
        /// Existing formula logic subsequently recalculates
        /// UsrUnitCost.
        /// </summary>
        private void ApplyCostCalculation(
            InventoryItem item)
        {
            CostCalculationFilter filter =
                CostCalculation.Current;

            if (filter == null)
            {
                throw new PXException(
                    "The vendor quote calculation values " +
                    "are unavailable.");
            }

            if (filter.VendorRecordID == null)
            {
                throw new PXException(
                    "Select a vendor.");
            }

            if (filter.VendorQuoteCost == null)
            {
                throw new PXException(
                    "Enter the Vendor Quote Cost.");
            }

            POVendorInventory vendorRow =
                FindVendorRow(filter.VendorRecordID);

            if (vendorRow == null)
            {
                throw new PXException(
                    "The selected vendor record could not be found.");
            }

            if (vendorRow.InventoryID != item.InventoryID)
            {
                throw new PXException(
                    "The selected vendor does not belong to " +
                    "the current stock item.");
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

            /*
             * Read the current value from the selected vendor row
             * again instead of relying only on the popup copy.
             */
            decimal preciousMetalCost =
                vendorExt.UsrPreciousMetalCost ?? 0m;

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

            /*
             * Make the popup-selected row the current row in the
             * Vendors view. This aligns the graph state with the
             * row being updated.
             */
            Base.VendorItems.Current = vendorRow;

            /*
             * Update only Fabrication / Piece.
             *
             * SetValueExt raises the licensed FieldUpdated event,
             * which recalculates the remaining cost fields.
             */
            vendorCache.SetValueExt<
                ASCJSMPOVendorInventoryExt.usrFabricationPiece>(
                    vendorRow,
                    fabricationPiece);

            vendorRow =
                Base.VendorItems.Update(vendorRow);

            ASCJSMPOVendorInventoryExt updatedVendorExt =
                vendorRow.GetExtension<
                    ASCJSMPOVendorInventoryExt>();

            /*
             * Refresh the Vendors grid so the newly calculated
             * values are displayed.
             */
            Base.VendorItems.View.RequestRefresh();

            PXTrace.WriteInformation(
                $"{TracePrefix} Calculation applied. " +
                $"InventoryID={item.InventoryID}, " +
                $"VendorRecordID={vendorRow.RecordID}, " +
                $"VendorID={vendorRow.VendorID}, " +
                $"VendorLocationID={vendorRow.VendorLocationID}, " +
                $"SubItemID={vendorRow.SubItemID}, " +
                $"IsDefault={vendorRow.IsDefault}, " +
                $"VendorName={filter.VendorName}, " +
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
             * The record remains dirty so the user can review:
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