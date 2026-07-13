<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="IN509900.aspx.cs" Inherits="Page_IN509900" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
  <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%"
        TypeName="MonthlyForecastReferenceTable.MonthlyForecastMaint"
        PrimaryView="ForecastRecords"
        >
    <CallbackCommands>

    </CallbackCommands>
  </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
  <px:PXGrid runat="server" Height="150px" SkinID="Primary" Width="100%" ID="grid" AllowAutoHide="false" DataSourceID="ds">
    <AutoSize Enabled="True" Container="Window" MinHeight="150" />
    <Levels>
      <px:PXGridLevel DataMember="ForecastRecords">
        <Columns>
          <px:PXGridColumn DataField="CustomerID" Width="150px" ></px:PXGridColumn>
          <px:PXGridColumn DataField="InventoryID" Width="150px" ></px:PXGridColumn>
          <px:PXGridColumn DataField="ForecastType" Width="120px" ></px:PXGridColumn>
          <px:PXGridColumn DataField="ForecastYear" Width="100px" ></px:PXGridColumn>
          <px:PXGridColumn DataField="ForecastMonth" Width="100px" ></px:PXGridColumn>
          <px:PXGridColumn TextAlign="Right" DataField="ForecastQty" Width="120px" ></px:PXGridColumn></Columns></px:PXGridLevel></Levels>
    <Mode AllowUpload="True" InitNewRow="True" AllowAddNew="True" AllowUpdate="True" AllowDelete="True" /></px:PXGrid></asp:Content>