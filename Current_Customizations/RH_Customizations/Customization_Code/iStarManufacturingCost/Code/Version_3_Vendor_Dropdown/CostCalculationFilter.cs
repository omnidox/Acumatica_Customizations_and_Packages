using System;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.AP;
using PX.Objects.IN;
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
    /// 2. Allows the user to select another vendor row.
    /// 3. Displays the selected vendor's name.
    /// 4. Loads the selected vendor row's Precious Metal Cost.
    /// 5. Calculates:
    ///
    ///        Fabrication / Piece =
    ///            Vendor Quote Cost - Precious Metal Cost
    /// </summary>
    [Serializable]
    public class CostCalculationFilter : PXBqlTable, IBqlTable
    {
        #region InventoryID

        /// <summary>
        /// Current stock item's internal InventoryID.
        ///
        /// This field is populated programmatically when the
        /// popup opens. It is used to limit the vendor selector
        /// to POVendorInventory records for the current item.
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
        /// Identifies the exact POVendorInventory row selected
        /// in the popup.
        ///
        /// RecordID is used as the actual selector value because
        /// one stock item can potentially contain multiple rows
        /// for the same VendorID, differentiated by values such as:
        ///
        /// - Vendor location
        /// - Subitem
        /// - Vendor inventory ID
        /// - Purchase unit
        ///
        /// VendorID is used as the visible substitute key.
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
                            Equal<POVendorInventory.vendorID>>>,
                    Where<
                        POVendorInventory.inventoryID,
                        Equal<
                            Current<
                                CostCalculationFilter.inventoryID>>>>),
            typeof(POVendorInventory.vendorID),
            typeof(Vendor.acctName),
            typeof(POVendorInventory.vendorLocationID),
            typeof(POVendorInventory.subItemID),
            typeof(POVendorInventory.vendorInventoryID),
            typeof(POVendorInventory.purchaseUnit),
            SubstituteKey = typeof(POVendorInventory.vendorID),
            DescriptionField = typeof(Vendor.acctName))]
        public virtual int? VendorRecordID { get; set; }

        public abstract class vendorRecordID
            : BqlInt.Field<vendorRecordID>
        {
        }

        #endregion

        #region VendorName

        /// <summary>
        /// Name of the vendor associated with VendorRecordID.
        ///
        /// This field is populated programmatically whenever:
        ///
        /// - the popup is initialized, or
        /// - the user changes the vendor selector.
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
        /// existing licensed jewelry costing customization
        /// for the vendor row selected in the popup.
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