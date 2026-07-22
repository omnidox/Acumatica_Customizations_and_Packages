using System;
using PX.Data;

namespace iStarCostCalculationExtensions
{
    /// <summary>
    /// Temporary filter DAC used by the CO_450
    /// Reverse Cost Calculation popup.
    ///
    /// This DAC is not persisted to the database.
    /// It simply holds the user's input while
    /// the popup is open.
    /// </summary>
    [Serializable]
    public class CostCalculationFilter : PXBqlTable, IBqlTable
    {
        #region UnitCost

        [PXDecimal]
        [PXUIField(DisplayName = "Unit Cost")]
        public virtual decimal? UnitCost { get; set; }

        public abstract class unitCost : PX.Data.BQL.BqlDecimal.Field<unitCost> { }

        #endregion

        #region MetalWeight

        [PXDecimal]
        [PXUIField(DisplayName = "Metal Weight", Enabled = false)]
        public virtual decimal? MetalWeight { get; set; }

        public abstract class metalWeight : PX.Data.BQL.BqlDecimal.Field<metalWeight> { }

        #endregion

        #region PreciousMetalCost

        [PXDecimal]
        [PXUIField(DisplayName = "Precious Metal Cost", Enabled = false)]
        public virtual decimal? PreciousMetalCost { get; set; }

        public abstract class preciousMetalCost : PX.Data.BQL.BqlDecimal.Field<preciousMetalCost> { }

        #endregion

        #region FabricationCost

        [PXDecimal]
        [PXUIField(DisplayName = "Fabrication / Value Add", Enabled = false)]
        public virtual decimal? FabricationCost { get; set; }

        public abstract class fabricationCost : PX.Data.BQL.BqlDecimal.Field<fabricationCost> { }

        #endregion
    }
}