using System;
using System.Collections;
using ASCJSMCustom.IN.CacheExt;
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
    /// This calculation is available only for Silver items.
    ///
    /// Workflow:
    ///
    /// 1. User selects a Silver stock item.
    /// 2. User selects a row on the Vendors tab.
    /// 3. User opens the Calculate Vendor Quote popup.
    /// 4. The popup defaults to the vendor selected on the
    ///    Vendors tab.
    /// 5. The user may select another vendor through the popup's
    ///    Vendor ID selector.
    /// 6. The popup resolves the applicable POVendorInventory row
    ///    and loads:
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

        /*
         * The licensed jewelry-costing customization uses:
         *
         *     UsrCommodityType == "S"
         *
         * to identify Silver items.
         */
        private const string SilverCommodityType =
            "S";

        private const string SilverOnlyMessage =
            "Vendor quote calculation is available only for Silver items.";

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
            InventoryItem item =
                Base.Item.Current;

            if (item?.InventoryID == null)
            {
                throw new PXException(
                    "Select a stock item before calculating " +
                    "the vendor quote.");
            }

            /*
             * Do not rely only on the browser-side button visibility.
             *
             * A callback could still be invoked directly, so the Silver-only
             * rule must also be enforced on the server.
             */
            if (!IsSilverItem(item))
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} CalculateVendorQuote was rejected because " +
                    $"the current item is not Silver. " +
                    $"InventoryID={item.InventoryID}, " +
                    $"CommodityType={GetCommodityType(item) ?? "<null>"}.");

                throw new PXException(
                    SilverOnlyMessage);
            }

            POVendorInventory initiallySelectedVendorRow =
                GetSelectedVendorRow();

            if (initiallySelectedVendorRow?.RecordID == null ||
                initiallySelectedVendorRow.VendorID == null)
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
                ApplyCostCalculation(
                    item);
            }

            return adapter.Get();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Enables and displays the action only when:
        ///
        /// - an existing stock item is selected,
        /// - the item is a Silver item,
        /// - a vendor row is selected, and
        /// - the Stock Item record is editable.
        /// </summary>
        protected virtual void _(
            Events.RowSelected<InventoryItem> e)
        {
            bool hasCurrentItem =
                e.Row?.InventoryID != null;

            bool isSilverItem =
                IsSilverItem(
                    e.Row);

            POVendorInventory vendorRow =
                GetSelectedVendorRow();

            bool hasSelectedVendor =
                vendorRow?.RecordID != null &&
                vendorRow.VendorID != null;

            bool actionAvailable =
                hasCurrentItem
                && isSilverItem
                && hasSelectedVendor
                && Base.Item.Cache.AllowUpdate;

            CalculateVendorQuote.SetEnabled(
                actionAvailable);

            /*
             * This also hides any standard Acumatica presentation of the
             * PXAction for non-Silver items.
             *
             * The runtime button extension separately follows the rendered
             * Silver field visibility.
             */
            CalculateVendorQuote.SetVisible(
                isSilverItem);


            PXTrace.WriteInformation(
                $"{TracePrefix} Action state evaluated. " +
                $"InventoryID={e.Row?.InventoryID}, " +
                $"CommodityType={GetCommodityType(e.Row) ?? "<null>"}, " +
                $"IsSilver={isSilverItem}, " +
                $"HasSelectedVendor={hasSelectedVendor}, " +
                $"AllowUpdate={Base.Item.Cache.AllowUpdate}, " +
                $"Enabled={actionAvailable}.");
        }

        /// <summary>
        /// Reloads vendor-specific popup values whenever the user
        /// selects a different Vendor ID in the popup.
        ///
        /// The corresponding ASPX PXSelector must use:
        ///
        /// DataField="VendorID"
        /// CommitChanges="True"
        /// </summary>
        protected virtual void _(
            Events.FieldUpdated<
                CostCalculationFilter,
                CostCalculationFilter.vendorID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            /*
             * The previously resolved RecordID may belong to the
             * vendor that was selected before this change.
             *
             * Clear it before resolving the new vendor.
             */
            CostCalculation.Cache.SetValue<
                CostCalculationFilter.vendorRecordID>(
                    e.Row,
                    null);

            LoadSelectedVendorIntoFilter(
                e.Row);
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

            RecalculateFabricationPiece(
                e.Row);
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

            RecalculateFabricationPiece(
                e.Row);
        }

        #endregion

        #region Silver Applicability

        /// <summary>
        /// Returns the commodity type stored by the licensed
        /// jewelry-costing extension for the supplied stock item.
        /// </summary>
        private static string GetCommodityType(
            InventoryItem item)
        {
            if (item == null)
            {
                return null;
            }

            ASCJSMINInventoryItemExt itemExt =
                item.GetExtension<
                    ASCJSMINInventoryItemExt>();

            return itemExt?.UsrCommodityType;
        }

        /// <summary>
        /// Determines whether the supplied stock item is Silver.
        ///
        /// The licensed customization identifies Silver using:
        ///
        ///     UsrCommodityType == "S"
        /// </summary>
        private static bool IsSilverItem(
            InventoryItem item)
        {
            return string.Equals(
                GetCommodityType(item),
                SilverCommodityType,
                StringComparison.OrdinalIgnoreCase);
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
        /// Finds the exact POVendorInventory row identified by
        /// its RecordID.
        ///
        /// RecordID is used when applying the calculation so that
        /// the exact row resolved by the popup is updated.
        /// </summary>
        private POVendorInventory FindVendorRowByRecordID(
            int? vendorRecordID)
        {
            if (vendorRecordID == null)
            {
                return null;
            }

            POVendorInventory vendorRow =
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
        /// Resolves a POVendorInventory row for the current item
        /// and the Vendor ID selected in the popup.
        ///
        /// If more than one row exists for the same item/vendor
        /// combination, the default row is preferred. RecordID is
        /// used as the secondary ordering value to keep selection
        /// deterministic.
        /// </summary>
        private POVendorInventory FindVendorRowBySelection(
            int? inventoryID,
            int? vendorID)
        {
            if (inventoryID == null ||
                vendorID == null)
            {
                return null;
            }

            POVendorInventory vendorRow =
                PXSelect<
                    POVendorInventory,
                    Where<
                        POVendorInventory.inventoryID,
                        Equal<
                            Required<
                                POVendorInventory.inventoryID>>,
                        And<
                            POVendorInventory.vendorID,
                            Equal<
                                Required<
                                    POVendorInventory.vendorID>>>>,
                    OrderBy<
                        Desc<
                            POVendorInventory.isDefault,
                            Asc<POVendorInventory.recordID>>>>
                .SelectWindowed(
                    Base,
                    0,
                    1,
                    inventoryID,
                    vendorID);

            return vendorRow;
        }

        /// <summary>
        /// Retrieves the Vendor account associated with the
        /// supplied internal Vendor ID.
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
        /// Initializes the popup from the row currently selected
        /// on the Vendors tab.
        ///
        /// The initializer is passed to AskExt so it executes when
        /// the dialog is first opened, rather than clearing values
        /// again when the user presses OK.
        /// </summary>
        private void InitializeCostCalculationFilter(
            InventoryItem item,
            POVendorInventory vendorRow)
        {
            if (item?.InventoryID == null)
            {
                throw new PXException(
                    "The current stock item is unavailable.");
            }

            /*
             * Revalidate before initializing the popup.
             */
            if (!IsSilverItem(item))
            {
                throw new PXException(
                    SilverOnlyMessage);
            }

            if (vendorRow?.RecordID == null ||
                vendorRow.VendorID == null)
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

            /*
             * InventoryID restricts the popup selector to vendors
             * configured for the current stock item.
             */
            CostCalculation.Cache.SetValue<
                CostCalculationFilter.inventoryID>(
                    filter,
                    item.InventoryID);

            /*
             * VendorID is the visible popup selector value.
             *
             * It stores Vendor.BAccountID internally while the
             * PXSelector displays Vendor.AcctCD.
             */
            CostCalculation.Cache.SetValue<
                CostCalculationFilter.vendorID>(
                    filter,
                    vendorRow.VendorID);

            /*
             * Preserve the exact vendor row selected on the
             * Vendors tab for the initial popup state.
             */
            CostCalculation.Cache.SetValue<
                CostCalculationFilter.vendorRecordID>(
                    filter,
                    vendorRow.RecordID);

            /*
             * Populate Vendor Name and Precious Metal Cost from
             * the exact Vendors-tab row.
             */
            LoadVendorRowIntoFilter(
                filter,
                vendorRow);

            /*
             * Do not reuse the quote entered during a previous
             * invocation of the popup.
             */
            CostCalculation.Cache.SetValue<
                CostCalculationFilter.vendorQuoteCost>(
                    filter,
                    null);

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.fabricationPiece>(
                    filter,
                    null);

            CostCalculation.Update(
                filter);

            PXTrace.WriteInformation(
                $"{TracePrefix} Popup initialized. " +
                $"InventoryID={item.InventoryID}, " +
                $"CommodityType={GetCommodityType(item)}, " +
                $"VendorID={vendorRow.VendorID}, " +
                $"VendorRecordID={vendorRow.RecordID}, " +
                $"VendorLocationID={vendorRow.VendorLocationID}, " +
                $"SubItemID={vendorRow.SubItemID}, " +
                $"IsDefault={vendorRow.IsDefault}, " +
                $"VendorName={filter.VendorName}, " +
                $"PreciousMetalCost={filter.PreciousMetalCost}.");
        }

        /// <summary>
        /// Resolves and loads vendor-specific information after
        /// the popup Vendor ID selector changes.
        ///
        /// This method deliberately does not use:
        ///
        /// Base.VendorItems.Current
        ///
        /// because that value represents the row selected on the
        /// Vendors tab and does not automatically change when the
        /// popup selector changes.
        /// </summary>
        private void LoadSelectedVendorIntoFilter(
            CostCalculationFilter filter)
        {
            if (filter == null)
            {
                return;
            }

            if (filter.InventoryID == null ||
                filter.VendorID == null)
            {
                ClearVendorInformation(
                    filter);

                return;
            }

            POVendorInventory vendorRow =
                FindVendorRowBySelection(
                    filter.InventoryID,
                    filter.VendorID);

            if (vendorRow == null)
            {
                ClearVendorInformation(
                    filter);

                throw new PXException(
                    "A vendor record could not be found for " +
                    "the selected stock item and vendor.");
            }

            LoadVendorRowIntoFilter(
                filter,
                vendorRow);

            PXTrace.WriteInformation(
                $"{TracePrefix} Popup vendor selection changed. " +
                $"InventoryID={filter.InventoryID}, " +
                $"VendorID={filter.VendorID}, " +
                $"VendorRecordID={vendorRow.RecordID}, " +
                $"VendorLocationID={vendorRow.VendorLocationID}, " +
                $"SubItemID={vendorRow.SubItemID}, " +
                $"IsDefault={vendorRow.IsDefault}, " +
                $"VendorName={filter.VendorName}, " +
                $"PreciousMetalCost={filter.PreciousMetalCost}.");
        }

        /// <summary>
        /// Loads one resolved POVendorInventory row into the
        /// unbound popup filter.
        /// </summary>
        private void LoadVendorRowIntoFilter(
            CostCalculationFilter filter,
            POVendorInventory vendorRow)
        {
            if (filter == null)
            {
                return;
            }

            if (vendorRow?.RecordID == null ||
                vendorRow.VendorID == null)
            {
                ClearVendorInformation(
                    filter);

                throw new PXException(
                    "The selected vendor record is unavailable.");
            }

            if (vendorRow.InventoryID !=
                filter.InventoryID)
            {
                ClearVendorInformation(
                    filter);

                throw new PXException(
                    "The selected vendor does not belong to " +
                    "the current stock item.");
            }

            ASCJSMPOVendorInventoryExt vendorExt =
                vendorRow.GetExtension<
                    ASCJSMPOVendorInventoryExt>();

            if (vendorExt == null)
            {
                ClearVendorInformation(
                    filter);

                throw new PXException(
                    "The jewelry costing fields could not be loaded " +
                    "for the selected vendor.");
            }

            Vendor vendor =
                FindVendor(
                    vendorRow.VendorID);

            decimal preciousMetalCost =
                vendorExt.UsrPreciousMetalCost ?? 0m;

            /*
             * Keep the visible selector synchronized with the
             * resolved POVendorInventory row.
             */
            CostCalculation.Cache.SetValue<
                CostCalculationFilter.vendorID>(
                    filter,
                    vendorRow.VendorID);

            /*
             * Store the exact row that will be updated if the user
             * presses OK.
             */
            CostCalculation.Cache.SetValue<
                CostCalculationFilter.vendorRecordID>(
                    filter,
                    vendorRow.RecordID);

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.vendorName>(
                    filter,
                    vendor?.AcctName);

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.preciousMetalCost>(
                    filter,
                    preciousMetalCost);

            RecalculateFabricationPiece(
                filter);

            CostCalculation.Update(
                filter);
        }

        /// <summary>
        /// Clears all vendor-dependent popup fields.
        /// </summary>
        private void ClearVendorInformation(
            CostCalculationFilter filter)
        {
            if (filter == null)
            {
                return;
            }

            CostCalculation.Cache.SetValue<
                CostCalculationFilter.vendorRecordID>(
                    filter,
                    null);

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
        /// This only updates the unbound popup field. It does not
        /// update POVendorInventory until the user presses OK.
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
                $"InventoryID={filter.InventoryID}, " +
                $"VendorID={filter.VendorID}, " +
                $"VendorRecordID={filter.VendorRecordID}, " +
                $"VendorQuoteCost={vendorQuoteCost}, " +
                $"PreciousMetalCost={preciousMetalCost}, " +
                $"FabricationPiece={fabricationPiece}.");
        }

        #endregion

        #region Apply Calculation

        /// <summary>
        /// Validates and applies the popup result to the exact
        /// POVendorInventory row resolved by the popup.
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
            /*
             * Revalidate immediately before changing the vendor record.
             *
             * This protects against stale popup state, direct callbacks, or
             * the item changing while the popup is open.
             */
            if (!IsSilverItem(item))
            {
                PXTrace.WriteWarning(
                    $"{TracePrefix} ApplyCostCalculation was rejected because " +
                    $"the current item is not Silver. " +
                    $"InventoryID={item?.InventoryID}, " +
                    $"CommodityType={GetCommodityType(item) ?? "<null>"}.");

                throw new PXException(
                    SilverOnlyMessage);
            }

            CostCalculationFilter filter =
                CostCalculation.Current;

            if (filter == null)
            {
                throw new PXException(
                    "The vendor quote calculation values " +
                    "are unavailable.");
            }

            if (filter.VendorID == null)
            {
                throw new PXException(
                    "Select a vendor.");
            }

            if (filter.VendorRecordID == null)
            {
                throw new PXException(
                    "The selected vendor record could not be resolved.");
            }

            if (filter.VendorQuoteCost == null)
            {
                throw new PXException(
                    "Enter the Vendor Quote Cost.");
            }

            POVendorInventory vendorRow =
                FindVendorRowByRecordID(
                    filter.VendorRecordID);

            if (vendorRow == null)
            {
                throw new PXException(
                    "The selected vendor record could not be found.");
            }

            if (vendorRow.InventoryID !=
                item.InventoryID)
            {
                throw new PXException(
                    "The selected vendor does not belong to " +
                    "the current stock item.");
            }

            if (vendorRow.VendorID !=
                filter.VendorID)
            {
                throw new PXException(
                    "The selected vendor record no longer matches " +
                    "the Vendor ID shown in the popup.");
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
             * Read the current Precious Metal Cost from the exact
             * vendor record rather than relying solely on the
             * unbound popup copy.
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
             * Align the Vendors view with the row being updated.
             */
            Base.VendorItems.Current =
                vendorRow;

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
                Base.VendorItems.Update(
                    vendorRow);

            ASCJSMPOVendorInventoryExt updatedVendorExt =
                vendorRow.GetExtension<
                    ASCJSMPOVendorInventoryExt>();

            /*
             * Refresh the Vendors grid so that the calculated
             * values become visible after the popup closes.
             */
            Base.VendorItems.View.RequestRefresh();

            PXTrace.WriteInformation(
                $"{TracePrefix} Calculation applied. " +
                $"InventoryID={item.InventoryID}, " +
                $"CommodityType={GetCommodityType(item)}, " +
                $"VendorID={vendorRow.VendorID}, " +
                $"VendorRecordID={vendorRow.RecordID}, " +
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