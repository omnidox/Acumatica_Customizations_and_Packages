using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.IN;

namespace MonthlyForecastReferenceTable
{
    [Serializable]
    [PXCacheName("Monthly Item Forecast")]
    public class UsrMonthlyForecast : PXBqlTable, IBqlTable
    {
        public class PK : PrimaryKeyOf<UsrMonthlyForecast>.By<inventoryID, finPeriodID>
        {
            public static UsrMonthlyForecast Find(PXGraph graph, int? inventoryID, string finPeriodID)
                => FindBy(graph, inventoryID, finPeriodID);
        }

        public static class FK
        {
            public class InventoryItem : PX.Objects.IN.InventoryItem.PK.ForeignKeyOf<UsrMonthlyForecast>.By<inventoryID> { }
        }

        #region InventoryID
        [Inventory(IsKey = true)]
        [PXDefault]
        [PXUIField(DisplayName = "Inventory ID")]
        public virtual int? InventoryID { get; set; }
        public abstract class inventoryID : BqlInt.Field<inventoryID> { }
        #endregion

        #region FinPeriodID
        [PXDBString(6, IsKey = true, IsFixed = true, InputMask = "")]
        [PXDefault]
        [PXUIField(DisplayName = "Financial Period")]
        public virtual string FinPeriodID { get; set; }
        public abstract class finPeriodID : BqlString.Field<finPeriodID> { }
        #endregion

        #region ForecastQty
        [PXDBDecimal(6)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "Forecast Qty")]
        public virtual decimal? ForecastQty { get; set; }
        public abstract class forecastQty : BqlDecimal.Field<forecastQty> { }
        #endregion

        #region NoteID
        [PXNote]
        public virtual Guid? NoteID { get; set; }
        public abstract class noteID : BqlGuid.Field<noteID> { }
        #endregion

        #region Audit Fields
        [PXDBCreatedByID]
        public virtual Guid? CreatedByID { get; set; }
        public abstract class createdByID : BqlGuid.Field<createdByID> { }

        [PXDBCreatedByScreenID]
        public virtual string CreatedByScreenID { get; set; }
        public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }

        [PXDBCreatedDateTime]
        public virtual DateTime? CreatedDateTime { get; set; }
        public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }

        [PXDBLastModifiedByID]
        public virtual Guid? LastModifiedByID { get; set; }
        public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }

        [PXDBLastModifiedByScreenID]
        public virtual string LastModifiedByScreenID { get; set; }
        public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }

        [PXDBLastModifiedDateTime]
        public virtual DateTime? LastModifiedDateTime { get; set; }
        public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }

        [PXDBTimestamp]
        public virtual byte[] Tstamp { get; set; }
        public abstract class tstamp : BqlByteArray.Field<tstamp> { }
        #endregion
    }
}