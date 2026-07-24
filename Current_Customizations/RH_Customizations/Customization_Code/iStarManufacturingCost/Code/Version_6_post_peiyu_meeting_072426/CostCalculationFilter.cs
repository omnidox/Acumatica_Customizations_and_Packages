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
    /// 3. Displays enough selector columns to distinguish rows
    ///    belonging to the same vendor, including:
    ///
    ///        - Vendor ID
    ///        - Vendor name
    ///        - Vendor location
    ///        - Subitem
    ///        - Vendor inventory ID
    ///        - Purchase unit
    ///        - Default status
    ///        - Record ID
    ///
    /// 4. Stores POVendorInventory.RecordID as the authoritative
    ///    selector value.
    /// 5. Populates VendorID and VendorName from the selected row.
    /// 6. Loads the selected vendor row's Precious Metal Cost.
    /// 7. Calculates:
    ///
    ///        Fabrication / Piece =
    ///            Vendor Quote Cost - Precious Metal Cost
    ///
    /// VendorRecordID and VendorID serve different purposes:
    ///
    /// - VendorRecordID is the editable selector and uniquely
    ///   identifies the exact POVendorInventory row.
    /// - VendorID is read-only informational data derived from
    ///   the selected POVendorInventory row.
    /// </summary>
    [Serializable]
    public class CostCalculationFilter
        : PXBqlTable,
          IBqlTable
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
        /// Unique identifier of the exact POVendorInventory row
        /// selected in the popup.
        ///
        /// This is the authoritative selector value.
        ///
        /// Unlike VendorID, RecordID remains unique when one
        /// vendor has multiple rows for the same stock item,
        /// distinguished by values such as:
        ///
        /// - Vendor location
        /// - Subitem
        /// - Vendor inventory ID
        /// - Purchase unit
        ///
        /// The selector is restricted to POVendorInventory rows
        /// belonging to the current stock item.
        ///
        /// RecordID is intentionally used as the selector's
        /// displayed key. Using Vendor.AcctCD as SubstituteKey
        /// would reintroduce ambiguity because multiple rows can
        /// have the same Vendor ID.
        /// </summary>
        [PXInt]
        [PXUIField(
            DisplayName = "Vendor Record",
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
            typeof(POVendorInventory.recordID),
            typeof(Vendor.acctCD),
            typeof(Vendor.acctName),
            typeof(POVendorInventory.vendorLocationID),
            typeof(POVendorInventory.subItemID),
            typeof(POVendorInventory.vendorInventoryID),
            typeof(POVendorInventory.purchaseUnit),
            typeof(POVendorInventory.isDefault),
            DescriptionField = typeof(Vendor.acctName))]
        public virtual int? VendorRecordID { get; set; }

        public abstract class vendorRecordID
            : BqlInt.Field<vendorRecordID>
        {
        }

        #endregion

        #region VendorID

        /// <summary>
        /// Internal BAccountID of the vendor associated with the
        /// selected POVendorInventory row.
        ///
        /// This is no longer the popup selector.
        ///
        /// The graph extension populates it after the user selects
        /// VendorRecordID. It is retained for:
        ///
        /// - displaying the selected vendor;
        /// - logging;
        /// - validation before applying the calculation.
        /// </summary>
        [PXInt]
        [PXUIField(
            DisplayName = "Vendor ID",
            Enabled = false)]
        [PXSelector(
            typeof(
                Search<
                    Vendor.bAccountID>),
            typeof(Vendor.acctCD),
            typeof(Vendor.acctName),
            SubstituteKey = typeof(Vendor.acctCD),
            DescriptionField = typeof(Vendor.acctName))]
        public virtual int? VendorID { get; set; }

        public abstract class vendorID
            : BqlInt.Field<vendorID>
        {
        }

        #endregion

        #region VendorName

        /// <summary>
        /// Name of the vendor associated with the exact selected
        /// POVendorInventory row.
        ///
        /// This field is populated programmatically whenever:
        ///
        /// - the popup is initialized; or
        /// - the user changes the Vendor Record selector.
        /// </summary>
        [PXString(
            255,
            IsUnicode = true)]
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
        /// for the exact selected POVendorInventory row.
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