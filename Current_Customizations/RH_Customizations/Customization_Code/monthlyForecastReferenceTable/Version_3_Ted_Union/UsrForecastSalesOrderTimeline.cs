using System;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR;
using PX.Objects.IN;

namespace MonthlyForecastReferenceTable
{
    [Serializable]
    [PXCacheName("Forecast and Sales Order Timeline")]
    public class UsrForecastSalesOrderTimeline :
        PXBqlTable,
        IBqlTable
    {
        #region CompanyID

        [PXDBInt(IsKey = true)]
        [PXUIField(Visible = false)]
        public virtual int? CompanyID { get; set; }

        public abstract class companyID :
            BqlInt.Field<companyID>
        {
        }

        #endregion

        #region SourceRecordKey

        [PXDBString(
            150,
            IsUnicode = true,
            IsKey = true)]
        [PXUIField(Visible = false)]
        public virtual string SourceRecordKey { get; set; }

        public abstract class sourceRecordKey :
            BqlString.Field<sourceRecordKey>
        {
        }

        #endregion

        #region SourceType

        [PXDBString(1, IsFixed = true)]
        [PXStringList(
            new[] { "F", "S" },
            new[] { "Forecast", "Sales Order" })]
        [PXUIField(
            DisplayName = "Source Type",
            Enabled = false)]
        public virtual string SourceType { get; set; }

        public abstract class sourceType :
            BqlString.Field<sourceType>
        {
        }

        #endregion

        #region SourceDescription

        [PXDBString(20, IsUnicode = true)]
        [PXUIField(
            DisplayName = "Source",
            Enabled = false)]
        public virtual string SourceDescription { get; set; }

        public abstract class sourceDescription :
            BqlString.Field<sourceDescription>
        {
        }

        #endregion

        #region DemandDate

        [PXDBDate]
        [PXUIField(
            DisplayName = "Date",
            Enabled = false)]
        public virtual DateTime? DemandDate { get; set; }

        public abstract class demandDate :
            BqlDateTime.Field<demandDate>
        {
        }

        #endregion

        #region CustomerID

        [Customer(
            DisplayName = "Customer",
            Enabled = false)]
        public virtual int? CustomerID { get; set; }

        public abstract class customerID :
            BqlInt.Field<customerID>
        {
        }

        #endregion

        #region InventoryID

        [Inventory(
            DisplayName = "Inventory ID",
            Enabled = false)]
        public virtual int? InventoryID { get; set; }

        public abstract class inventoryID :
            BqlInt.Field<inventoryID>
        {
        }

        #endregion

        #region DemandQty

        [PXDBDecimal(6)]
        [PXUIField(
            DisplayName = "Quantity",
            Enabled = false)]
        public virtual decimal? DemandQty { get; set; }

        public abstract class demandQty :
            BqlDecimal.Field<demandQty>
        {
        }

        #endregion

        #region BaseOpenQty

        [PXDBDecimal(6)]
        [PXUIField(
            DisplayName = "Base Open Qty.",
            Enabled = false)]
        public virtual decimal? BaseOpenQty { get; set; }

        public abstract class baseOpenQty :
            BqlDecimal.Field<baseOpenQty>
        {
        }

        #endregion

        #region ForecastType

        [PXDBString(2, IsFixed = true)]
        [PXUIField(
            DisplayName = "Forecast Type",
            Enabled = false)]
        public virtual string ForecastType { get; set; }

        public abstract class forecastType :
            BqlString.Field<forecastType>
        {
        }

        #endregion

        #region ForecastYear

        [PXDBString(4, IsFixed = true)]
        [PXUIField(
            DisplayName = "Forecast Year",
            Enabled = false)]
        public virtual string ForecastYear { get; set; }

        public abstract class forecastYear :
            BqlString.Field<forecastYear>
        {
        }

        #endregion

        #region ForecastMonth

        [PXDBString(2, IsFixed = true)]
        [PXUIField(
            DisplayName = "Forecast Month",
            Enabled = false)]
        public virtual string ForecastMonth { get; set; }

        public abstract class forecastMonth :
            BqlString.Field<forecastMonth>
        {
        }

        #endregion

        #region OrderType

        [PXDBString(2, IsFixed = true)]
        [PXUIField(
            DisplayName = "Order Type",
            Enabled = false)]
        public virtual string OrderType { get; set; }

        public abstract class orderType :
            BqlString.Field<orderType>
        {
        }

        #endregion

        #region OrderNbr

        [PXDBString(15, IsUnicode = true)]
        [PXUIField(
            DisplayName = "Order Nbr.",
            Enabled = false)]
        public virtual string OrderNbr { get; set; }

        public abstract class orderNbr :
            BqlString.Field<orderNbr>
        {
        }

        #endregion

        #region LineNbr

        [PXDBInt]
        [PXUIField(
            DisplayName = "Line Nbr.",
            Enabled = false)]
        public virtual int? LineNbr { get; set; }

        public abstract class lineNbr :
            BqlInt.Field<lineNbr>
        {
        }

        #endregion

        #region Completed

        [PXDBBool]
        [PXUIField(
            DisplayName = "Completed",
            Enabled = false)]
        public virtual bool? Completed { get; set; }

        public abstract class completed :
            BqlBool.Field<completed>
        {
        }

        #endregion

        #region Cancelled

        [PXDBBool]
        [PXUIField(
            DisplayName = "Cancelled",
            Enabled = false)]
        public virtual bool? Cancelled { get; set; }

        public abstract class cancelled :
            BqlBool.Field<cancelled>
        {
        }

        #endregion

        #region OpenLine

        [PXDBBool]
        [PXUIField(
            DisplayName = "Open Line",
            Enabled = false)]
        public virtual bool? OpenLine { get; set; }

        public abstract class openLine :
            BqlBool.Field<openLine>
        {
        }

        #endregion
    }
}