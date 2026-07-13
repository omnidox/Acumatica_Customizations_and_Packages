using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AR;
using PX.Objects.IN;

namespace MonthlyForecastReferenceTable
{
    [Serializable]
    [PXCacheName("Monthly Item Forecast")]
    public class UsrMonthlyForecast : PXBqlTable, IBqlTable
    {
        public class PK : PrimaryKeyOf<UsrMonthlyForecast>
            .By<customerID, inventoryID, forecastType, forecastYear, forecastMonth>
        {
            public static UsrMonthlyForecast Find(
                PXGraph graph,
                int? customerID,
                int? inventoryID,
                string forecastType,
                string forecastYear,
                string forecastMonth)
                => FindBy(graph, customerID, inventoryID, forecastType, forecastYear, forecastMonth);
        }

        public static class FK
        {
            public class Customer :
                PX.Objects.AR.Customer.PK.ForeignKeyOf<UsrMonthlyForecast>
                    .By<customerID>
            { }

            public class InventoryItem :
                PX.Objects.IN.InventoryItem.PK.ForeignKeyOf<UsrMonthlyForecast>
                    .By<inventoryID>
            { }
        }

        #region CustomerID
        [Customer(IsKey = true)]
        [PXDefault]
        [PXUIField(DisplayName = "Customer")]
        public virtual int? CustomerID { get; set; }
        public abstract class customerID : BqlInt.Field<customerID> { }
        #endregion

        #region InventoryID
        [Inventory(IsKey = true)]
        [PXDefault]
        [PXUIField(DisplayName = "Inventory ID")]
        public virtual int? InventoryID { get; set; }
        public abstract class inventoryID : BqlInt.Field<inventoryID> { }
        #endregion

        #region ForecastType
        [PXDBString(2, IsKey = true, IsFixed = true, InputMask = ">??")]
        [PXDefault]
        [PXUIField(DisplayName = "Forecast Type")]
        public virtual string ForecastType { get; set; }
        public abstract class forecastType : BqlString.Field<forecastType> { }
        #endregion

        #region ForecastDate
        [PXDate]
        [PXUIField(DisplayName = "Forecast Date")]
        public virtual DateTime? ForecastDate { get; set; }
        public abstract class forecastDate : BqlDateTime.Field<forecastDate> { }
        #endregion

        #region ForecastYear
        [PXDBString(4, IsKey = true, IsFixed = true)]
        [PXDefault]
        [PXUIField(DisplayName = "Forecast Year")]
        public virtual string ForecastYear { get; set; }
        public abstract class forecastYear : BqlString.Field<forecastYear> { }
        #endregion

        #region ForecastMonth
        [PXDBString(2, IsKey = true, IsFixed = true)]
        [PXDefault]
        [PXUIField(DisplayName = "Forecast Month")]
        public virtual string ForecastMonth { get; set; }
        public abstract class forecastMonth : BqlString.Field<forecastMonth> { }
        #endregion

        #region ForecastQty
        [PXDBInt]
        [PXDefault(0)]
        [PXUIField(DisplayName = "Forecast Qty")]
        public virtual int? ForecastQty { get; set; }
        public abstract class forecastQty : BqlInt.Field<forecastQty> { }
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