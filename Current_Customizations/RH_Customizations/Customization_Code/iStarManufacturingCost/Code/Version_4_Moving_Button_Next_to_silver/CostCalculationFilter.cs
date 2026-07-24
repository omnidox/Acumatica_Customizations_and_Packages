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
    /// 1. Defaults to the vendor row currently selected
    ///    on the Vendors tab.
    /// 2. Allows the user to select another vendor.
    /// 3. Displays the selected vendor's name.
    /// 4. Internally tracks the exact POVendorInventory row.
    /// 5. Loads the selected vendor row's Precious Metal Cost.
    /// 6. Calculates:
    ///
    ///        Fabrication / Piece =
    ///            Vendor Quote Cost - Precious Metal Cost
    ///
    /// VendorID and VendorRecordID serve different purposes:
    ///
    /// - VendorID is the visible selector value.
    /// - VendorRecordID is the hidden identifier of the exact
    ///   POVendorInventory row that will be updated.
    /// </summary>
    [Serializable]
    public class CostCalculationFilter : PXBqlTable, IBqlTable
    {
        #region InventoryID

        /// <summary>
        /// Current stock item's internal InventoryID.
        ///
        /// This field is populated programmatically when the
        /// popup opens. It restricts the VendorID selector to
        /// vendors configured for the current stock item.
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

        #region VendorID

        /// <summary>
        /// Vendor selected in the popup.
        ///
        /// The stored value is the vendor's internal BAccountID,
        /// which corresponds to POVendorInventory.VendorID.
        ///
        /// The selector displays Vendor.AcctCD, which is the
        /// user-facing Vendor ID shown in Acumatica.
        ///
        /// The selector is limited to vendors that have a
        /// POVendorInventory row for the current stock item.
        /// </summary>
        [PXInt]
        [PXUIField(
            DisplayName = "Vendor ID",
            Required = true)]
        [PXSelector(
            typeof(
                Search2<
                    Vendor.bAccountID,
                    InnerJoin<
                        POVendorInventory,
                        On<
                            POVendorInventory.vendorID,
                            Equal<Vendor.bAccountID>>>,
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
        public virtual int? VendorID { get; set; }

        public abstract class vendorID
            : BqlInt.Field<vendorID>
        {
        }

        #endregion

        #region VendorRecordID

        /// <summary>
        /// Hidden identifier of the exact POVendorInventory row
        /// resolved from the selected VendorID.
        ///
        /// This is kept separate from VendorID because one vendor
        /// may potentially have multiple POVendorInventory records
        /// for the same stock item, distinguished by:
        ///
        /// - Vendor location
        /// - Subitem
        /// - Vendor inventory ID
        /// - Purchase unit
        ///
        /// The graph extension populates this field after resolving
        /// the POVendorInventory row for the selected vendor.
        ///
        /// It is not displayed in the popup.
        /// </summary>
        [PXInt]
        [PXUIField(
            DisplayName = "Vendor Record ID",
            Visible = false)]
        public virtual int? VendorRecordID { get; set; }

        public abstract class vendorRecordID
            : BqlInt.Field<vendorRecordID>
        {
        }

        #endregion

        #region VendorName

        /// <summary>
        /// Name of the vendor associated with VendorID.
        ///
        /// This field is populated programmatically whenever:
        ///
        /// - the popup is initialized, or
        /// - the user changes the Vendor ID selector.
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
        /// for the resolved POVendorInventory row.
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