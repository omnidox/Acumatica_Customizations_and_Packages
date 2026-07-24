using System;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.AP;
using PX.Objects.PO;

namespace iStarCostCalculationExtensions
{
    /// <summary>
    /// Unbound filter DAC used by the CO_450
    /// vendor quote reverse-cost calculation popup.
    ///
    /// This DAC is not persisted to the database.
    /// It only holds values while the Smart Panel is open.
    ///
    /// The popup:
    ///
    /// 1. Defaults to the exact vendor row currently selected
    ///    on the Vendors tab.
    /// 2. Allows the user to select another exact
    ///    POVendorInventory row.
    /// 3. Displays the selected row's user-facing Vendor ID.
    /// 4. Displays the selected vendor's name.
    /// 5. Internally stores the exact POVendorInventory.RecordID.
    /// 6. Loads the selected vendor row's Precious Metal Cost.
    /// 7. Calculates:
    ///
    ///        Fabrication / Piece =
    ///            Vendor Quote Cost - Precious Metal Cost
    ///
    /// VendorRecordID is the selector's stored value.
    ///
    /// Although the stored value is POVendorInventory.RecordID,
    /// the selector displays Vendor.AcctCD as the user-facing
    /// Vendor ID.
    ///
    /// This allows the user to distinguish and select multiple
    /// POVendorInventory rows belonging to the same vendor by
    /// using the selector columns:
    ///
    /// - Vendor ID
    /// - Vendor Name
    /// - Vendor Location
    /// - Subitem
    /// - Vendor Inventory ID
    /// - Purchase Unit
    ///
    /// VendorID is retained as a hidden informational field and
    /// should be populated by the graph extension after the exact
    /// POVendorInventory row is loaded.
    /// </summary>
    [Serializable]
    public class CostCalculationFilter : PXBqlTable, IBqlTable
    {
        #region InventoryID

        /// <summary>
        /// Current stock item's internal InventoryID.
        ///
        /// This field is populated programmatically when the
        /// popup opens. It restricts the VendorRecordID selector
        /// to POVendorInventory rows configured for the current
        /// stock item.
        ///
        /// It is not displayed in the popup.
        /// </summary>
        [PXInt]
        [PXUIField(
            DisplayName = "Inventory ID",
            Visible = false)]
        public virtual int? InventoryID { get; set; }

        public abstract class inventoryID
            : BqlInt.Field<inventoryID>
        {
        }

        #endregion

        #region VendorRecordID

        /// <summary>
        /// Exact POVendorInventory row selected in the popup.
        ///
        /// The stored value is POVendorInventory.RecordID.
        ///
        /// The selector displays Vendor.AcctCD as the user-facing
        /// Vendor ID rather than displaying the numeric RecordID.
        ///
        /// Because the selector stores RecordID, multiple rows for
        /// the same vendor and stock item can be selected
        /// independently when they differ by:
        ///
        /// - Vendor location
        /// - Subitem
        /// - Vendor inventory ID
        /// - Purchase unit
        ///
        /// The graph extension should use this value to load and
        /// update the exact POVendorInventory row selected by the
        /// user.
        /// </summary>
        [PXInt]
        [PXUIField(
            DisplayName = "Vendor ID",
            Required = true)]
        [PXSelector(
            typeof(
                Search2<
                    POVendorInventory.recordID,
                    InnerJoin<
                        Vendor,
                        On<
                            Vendor.bAccountID,
                            Equal<
                                POVendorInventory.vendorID>>>,
                    Where<
                        POVendorInventory.inventoryID,
                        Equal<
                            Current<
                                CostCalculationFilter.inventoryID>>>>),
            typeof(Vendor.acctCD),
            typeof(Vendor.acctName),
            typeof(POVendorInventory.vendorLocationID),
            typeof(POVendorInventory.subItemID),
            typeof(POVendorInventory.vendorInventoryID),
            typeof(POVendorInventory.purchaseUnit),
            SubstituteKey = typeof(Vendor.acctCD),
            DescriptionField = typeof(Vendor.acctName))]
        public virtual int? VendorRecordID { get; set; }

        public abstract class vendorRecordID
            : BqlInt.Field<vendorRecordID>
        {
        }

        #endregion

        #region VendorID

        /// <summary>
        /// Hidden internal BAccountID of the vendor associated
        /// with the selected POVendorInventory row.
        ///
        /// This is not the selector's stored value.
        ///
        /// The graph extension should populate this field after
        /// loading the exact POVendorInventory row identified by
        /// VendorRecordID.
        ///
        /// This field may be used by calculation or validation
        /// logic that needs the vendor account independently from
        /// the exact vendor-inventory record.
        /// </summary>
        [PXInt]
        [PXUIField(
            DisplayName = "Vendor ID",
            Visible = false)]
        public virtual int? VendorID { get; set; }

        public abstract class vendorID
            : BqlInt.Field<vendorID>
        {
        }

        #endregion

        #region VendorName

        /// <summary>
        /// Name of the vendor associated with the selected
        /// POVendorInventory row.
        ///
        /// This field is populated programmatically whenever:
        ///
        /// - the popup is initialized, or
        /// - the user changes the Vendor Record selector.
        /// </summary>
        [PXString(255, IsUnicode = true)]
        [PXUIField(
            DisplayName = "Vendor Name",
            Enabled = false)]
        public virtual string VendorName { get; set; }

        public abstract class vendorName
            : BqlString.Field<vendorName>
        {
        }

        #endregion

        #region VendorQuoteCost

        /// <summary>
        /// Final cost supplied by the vendor.
        ///
        /// Confirmed calculation:
        ///
        /// Fabrication / Piece =
        ///     Vendor Quote Cost - Precious Metal Cost
        /// </summary>
        [PXDecimal(4)]
        [PXUIField(
            DisplayName = "Vendor Quote Cost",
            Required = true)]
        public virtual decimal? VendorQuoteCost { get; set; }

        public abstract class vendorQuoteCost
            : BqlDecimal.Field<vendorQuoteCost>
        {
        }

        #endregion

        #region PreciousMetalCost

        /// <summary>
        /// Precious-metal cost already calculated by the
        /// existing licensed jewelry-costing customization
        /// for the exact POVendorInventory row selected through
        /// VendorRecordID.
        ///
        /// This field is read-only because it is supplied by
        /// the existing costing customization.
        /// </summary>
        [PXDecimal(6)]
        [PXUIField(
            DisplayName = "Precious Metal Cost",
            Enabled = false)]
        public virtual decimal? PreciousMetalCost { get; set; }

        public abstract class preciousMetalCost
            : BqlDecimal.Field<preciousMetalCost>
        {
        }

        #endregion

        #region FabricationPiece

        /// <summary>
        /// Result of the reverse calculation.
        ///
        /// Fabrication / Piece =
        ///     Vendor Quote Cost - Precious Metal Cost
        ///
        /// This field is calculated in the graph extension
        /// and is read-only in the popup.
        /// </summary>
        [PXDecimal(4)]
        [PXUIField(
            DisplayName = "Fabrication / Piece",
            Enabled = false)]
        public virtual decimal? FabricationPiece { get; set; }

        public abstract class fabricationPiece
            : BqlDecimal.Field<fabricationPiece>
        {
        }

        #endregion
    }
}
