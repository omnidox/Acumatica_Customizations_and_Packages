using System;
using PX.Data;
using PX.Data.BQL;

namespace iStarCostCalculationExtensions
{
    /// <summary>
    /// Unbound filter DAC used by the CO_450
    /// vendor quote reverse-cost calculation popup.
    ///
    /// This DAC is not persisted to the database.
    /// It only holds values while the Smart Panel is open.
    /// </summary>
    [Serializable]
    public class CostCalculationFilter : PXBqlTable, IBqlTable
    {
        #region VendorQuoteCost

        /// <summary>
        /// Final cost supplied by the vendor.
        ///
        /// Peiyu confirmed the calculation:
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
        /// for the selected vendor row.
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