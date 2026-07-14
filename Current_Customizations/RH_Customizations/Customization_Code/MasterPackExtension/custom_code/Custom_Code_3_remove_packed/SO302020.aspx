<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="SO302020.aspx.cs"
    Inherits="Page_SO302020" Title="Pick, Pack, and Ship" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:content id="cont1" contentplaceholderid="phDS" runat="Server">
<script language="javascript" type="text/javascript">
  function Barcode_Initialize(ctrl) {
    px.registerBusyStateKeySink(ctrl);
  }
</script>
<script language="javascript" type="text/javascript">
  function ActionCallback(callbackContext) {
    if ((callbackContext.info.name.toLowerCase().startsWith("scan") || callbackContext.info.name == "ElapsedTime") && callbackContext.control.longRunInProcess == null) {
      var edInfoMessageSoundFile = px_alls["edInfoMessageSoundFile"];
      if (edInfoMessageSoundFile) {
        px.playSound(edInfoMessageSoundFile.getValue());
      }
    }
  }

  window.addEventListener('load', function () { px_callback.addHandler(ActionCallback); });
</script>
    <style>
        .ProcessingStatusIcon .main-icon-img {
            font-size: 90px;
            margin: -10px;
        }
        .ProcessingStatusIcon .main-icon {
            height: 80px;
            width: 80px;
        }
        .ProcessingStatusIcon div.checkBox {
            height: 100px;
            width: 50px;
        }
    </style>
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.SO.WMS.PickPackShip+Host" PrimaryView="HeaderView">
        <CallbackCommands>
            <%-- View Linked Documents --%>
            <px:PXDSCallbackCommand DependOnGrid="gridPicked" Name="ViewOrder" Visible="False"/>

            <%-- Hide Allocation buttons --%>
            <px:PXDSCallbackCommand Name="SOShipmentLineSplittingExtension_GenerateNumbers" Visible="False" />
            <px:PXDSCallbackCommand Name="SOShipmentLineSplittingExtension_ShowSplit" Visible="False"/>
        </CallbackCommands>
    </px:PXDataSource>
</asp:content>
<asp:content id="cont2" contentplaceholderid="phF" runat="Server">
    <px:PXFormView ID="formHeader" runat="server" DataSourceID="ds" Height="120px" Width="100%" Visible="true" DataMember="HeaderView" DefaultControlID="edBarcode" FilesIndicator="True" >
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="L" />
            <px:PXTextEdit ID="edBarcode" runat="server" DataField="Barcode">
                <AutoCallBack Command="Scan" Target="ds">
                    <Behavior CommitChanges="True" />
                </AutoCallBack>
                <ClientEvents Initialize="Barcode_Initialize"/>
            </px:PXTextEdit>
            <px:PXSelector ID="edRefNbr" runat="server" DataField="RefNbr" AllowEdit="true" />
            <px:PXSelector ID="edWSNbr" runat="server" DataField="WorksheetNbr" AllowEdit="true" />
            <px:PXSelector ID="edSingleShipmentNbr" runat="server" DataField="SingleShipmentNbr" AllowEdit="true" />
            <px:PXSelector ID="edCurrentLocationID" runat="server" DataField="LastVisitedLocationID" />
            <px:PXSelector ID="edCartID" runat="server" DataField="CartID" />

            <px:PXLayoutRule runat="server" StartColumn="true" ControlSize="XS"/>
            <px:PXCheckBox ID="chkStatusIcon" runat="server" DataField="ProcessingSucceeded" RenderStyle="Button" CommitChanges="true" AlignLeft="True" CssClass="ProcessingStatusIcon">
                <CheckImages Normal="main@Success" />
                <UncheckImages Normal="main@Fail" />
            </px:PXCheckBox>

            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="L" ColumnWidth="M" />
            <px:PXTextEdit ID="edMessage" runat="server" DataField="Message" Width="800px" Style="font-size: 10pt; font-weight: bold;" SuppressLabel="true" TextMode="MultiLine" Height="55px" SkinID="Label" DisableSpellcheck="True" Enabled="False" />

            <px:PXLayoutRule runat="server" ColumnSpan="3" Merge="True" />
            <px:PXCheckBox ID="chkRemove" runat="server" DataField="Remove" AlignLeft="True" />
            <px:PXCheckBox ID="chkCartLoaded" runat="server" DataField="CartLoaded" AlignLeft="True" />

            <%-- Tab switchers --%>
            <px:PXCheckBox ID="chkShowPickWS" runat="server" DataField="ShowPickWS" Visible ="False" />
            <px:PXCheckBox ID="chkShowPick" runat="server" DataField="ShowPick" Visible ="False" />
            <px:PXCheckBox ID="chkShowPack" runat="server" DataField="ShowPack" Visible ="False" />
            <px:PXCheckBox ID="chkShowShip" runat="server" DataField="ShowShip" Visible ="False" />
            <px:PXCheckBox ID="chkShowReturn" runat="server" DataField="ShowReturn" Visible ="False" />
            <px:PXCheckBox ID="chkShowLog"  runat="server" DataField="ShowLog" Visible ="False" />
        </Template>
    </px:PXFormView>
    <px:PXFormView ID="formInfo" runat="server" DataSourceID="ds" DataMember="Info" Caption="Scan Info" CaptionVisible="false">
        <Template>
            <px:PXTextEdit ID="edInfoMode" runat="server" DataField="Mode"/>
            <px:PXTextEdit ID="edInfoModeCaption" runat="server" DataField="ModeCaption"/>
            <px:PXTextEdit ID="edInfoMessage" runat="server" DataField="Message"/>
            <px:PXTextEdit ID="edInfoMessageSoundFile" runat="server" DataField="MessageSoundFile"/>
            <px:PXTextEdit ID="edInfoInstructions" runat="server" DataField="Instructions"/>
            <px:PXTextEdit ID="edInfoQuestion" runat="server" DataField="Question"/>
            <px:PXTextEdit ID="edInfoPrompt" runat="server" DataField="Prompt"/>
        </Template>
    </px:PXFormView>
</asp:content>
<asp:content id="cont3" contentplaceholderid="phG" runat="Server">
    <px:PXTab ID="tab" runat="server" Height="540px" Style="z-index: 100;" Width="100%">
        <Items>
            <px:PXTabItem Visible="False" Text="Pick" VisibleExp="DataControls[&quot;chkShowPickWS&quot;].Value == true" BindingContext="formHeader">
                <Template>
                    <px:PXGrid ID="gridPickedWS" runat="server" DataSourceID="ds" SyncPosition="true" Width="100%" SkinID="Inquire" OnRowDataBound="PickWSGrid_RowDataBound">
                        <Levels>
                            <px:PXGridLevel DataMember="PickListOfPicker">
                                <Columns>
                                    <px:PXGridColumn DataField="FitsWS" Type="CheckBox" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="SiteID" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="LocationID" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="InventoryID" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="LotSerialNbr" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="ExpireDate" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="PickedQty" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="Qty" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="UOM" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="ShipmentNbr" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="ForceCompleted" Type="CheckBox"></px:PXGridColumn>
                                    <px:PXGridColumn DataField="ToteID" ></px:PXGridColumn>
                                </Columns>
                                <RowTemplate>
                                    <px:PXNumberEdit ID="edWSEntryNbr" runat="server" DataField="EntryNbr" Enabled="false"></px:PXNumberEdit>
                                    <px:PXSegmentMask ID="edWSInventoryID" runat="server" DataField="InventoryID" Enabled="False" AllowEdit="True" ></px:PXSegmentMask>
                                    <px:PXSegmentMask ID="edWSSiteID" runat="server" DataField="SiteID" Enabled="False" AllowEdit="True" ></px:PXSegmentMask>
                                    <px:PXSegmentMask ID="edWSLocationID" runat="server" DataField="LocationID" Enabled="False"></px:PXSegmentMask>
                                    <px:PXTextEdit ID="edWSLotSerialNbr" runat="server" DataField="LotSerialNbr" Enabled="False" ></px:PXTextEdit>
                                    <px:PXDateTimeEdit ID="edWSExpireDate" runat="server" DataField="ExpireDate" Enabled="False" ></px:PXDateTimeEdit>
                                    <px:PXNumberEdit ID="PXWSPickedQty" runat="server" DataField="PickedQty" Enabled="False"></px:PXNumberEdit>
                                    <px:PXNumberEdit ID="PXWSQty" runat="server" DataField="Qty" Enabled="False"></px:PXNumberEdit>
                                    <px:PXSelector ID="edWSUOM" runat="server" DataField="UOM" Enabled="False" ></px:PXSelector>
                                 </RowTemplate>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Container="Window" Enabled="True" MinHeight="400" ></AutoSize>
                        <ActionBar>
                            <CustomItems>
                                <px:PXToolBarButton CommandName="ReopenLineQty" CommandSourceID="ds" DependOnGrid="gridPickedWS" StateColumn="ForceCompleted"></px:PXToolBarButton>
                            </CustomItems>
                        </ActionBar>
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Visible="False" Text="Pick" VisibleExp="DataControls[&quot;chkShowPick&quot;].Value == true" BindingContext="formHeader">
                <Template>
                    <px:PXGrid ID="gridPicked" runat="server" DataSourceID="ds" SyncPosition="true" Width="100%" SkinID="Inquire" OnRowDataBound="PickGrid_RowDataBound">
                        <Levels>
                            <px:PXGridLevel DataMember="Picked">
                                <Columns>
                                    <px:PXGridColumn DataField="Fits" Type="CheckBox" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="LineNbr" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="SOShipLine__OrigOrderType" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="SOShipLine__OrigOrderNbr" LinkCommand="ViewOrder"></px:PXGridColumn>
                                    <px:PXGridColumn DataField="SiteID" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="LocationID" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="InventoryID" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="SOShipLine__TranDesc" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="LotSerialNbr" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="ExpireDate" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="CartQty" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="OverAllCartQty" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="PickedQty" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="PackedQty" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="Qty" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="UOM" ></px:PXGridColumn>
                                    <px:PXGridColumn DataField="SOShipLine__IsFree" Type="CheckBox" ></px:PXGridColumn>
                                </Columns>
                                <RowTemplate>
                                    <px:PXNumberEdit ID="edPickLineNbr" runat="server" DataField="LineNbr" Enabled="false"></px:PXNumberEdit>
                                    <px:PXTextEdit ID="edPickOrigOrderType" runat="server" DataField="SOShipLine__OrigOrderType" Enabled="False" ></px:PXTextEdit>
                                    <px:PXSelector ID="edPickOrigOrderNbr" runat="server" DataField="SOShipLine__OrigOrderNbr" Enabled="False"></px:PXSelector>
                                    <px:PXSegmentMask ID="edPickInventoryID" runat="server" DataField="InventoryID" Enabled="False" AllowEdit="True" ></px:PXSegmentMask>
                                    <px:PXSegmentMask ID="edPickSiteID" runat="server" DataField="SiteID" Enabled="False" AllowEdit="True" ></px:PXSegmentMask>
                                    <px:PXSegmentMask ID="edPickLocationID" runat="server" DataField="LocationID" Enabled="False"></px:PXSegmentMask>
                                    <px:PXSelector ID="edPickLotSerialNbr" runat="server" DataField="LotSerialNbr" Enabled="False" ></px:PXSelector>
                                    <px:PXDateTimeEdit ID="edPickExpireDate" runat="server" DataField="ExpireDate" Enabled="False" ></px:PXDateTimeEdit>
                                    <px:PXNumberEdit ID="PXPickPickedQty" runat="server" DataField="PickedQty" Enabled="False"></px:PXNumberEdit>
                                    <px:PXNumberEdit ID="PXPickPackedQty" runat="server" DataField="PackedQty" Enabled="False"></px:PXNumberEdit>
                                    <px:PXNumberEdit ID="PXPickQty" runat="server" DataField="Qty" Enabled="False"></px:PXNumberEdit>
                                    <px:PXSelector ID="edPickUOM" runat="server" DataField="UOM" Enabled="False" ></px:PXSelector>
                                    <px:PXCheckBox ID="chkPickIsFree" runat="server" DataField="SOShipLine__IsFree" Enabled="False" ></px:PXCheckBox>
                                 </RowTemplate>
                            </px:PXGridLevel>
                        </Levels>
                         <AutoSize Enabled="True" ></AutoSize>
                        <AutoSize Container="Window" Enabled="True" MinHeight="400" ></AutoSize>
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Return" VisibleExp="DataControls[&quot;chkShowReturn&quot;].Value == true" BindingContext="formHeader">
                <Template>
                    <px:PXGrid ID="gridReturned" runat="server" DataSourceID="ds" SyncPosition="true" Width="100%" SkinID="Inquire" OnRowDataBound="PickGrid_RowDataBound">
                        <Levels>
                            <px:PXGridLevel DataMember="Returned">
                                <Columns>
                                    <px:PXGridColumn DataField="Fits" Type="CheckBox" />
                                    <px:PXGridColumn DataField="LineNbr" />
                                    <px:PXGridColumn DataField="SOShipLine__OrigOrderType" />
                                    <px:PXGridColumn DataField="SOShipLine__OrigOrderNbr" LinkCommand="ViewOrder"/>
                                    <px:PXGridColumn DataField="SiteID" />
                                    <px:PXGridColumn DataField="LocationID" />
                                    <px:PXGridColumn DataField="InventoryID" />
                                    <px:PXGridColumn DataField="SOShipLine__TranDesc" />
                                    <px:PXGridColumn DataField="LotSerialNbr" />
                                    <px:PXGridColumn DataField="ExpireDate" />
                                    <px:PXGridColumn DataField="PickedQty" />
                                    <px:PXGridColumn DataField="Qty" />
                                    <px:PXGridColumn DataField="UOM" />
                                    <px:PXGridColumn DataField="SOShipLine__IsFree" Type="CheckBox" />
                                </Columns>
                                <RowTemplate>
                                    <px:PXNumberEdit ID="edReturnLineNbr" runat="server" DataField="LineNbr" Enabled="false"/>
                                    <px:PXTextEdit ID="edReturnOrigOrderType" runat="server" DataField="SOShipLine__OrigOrderType" Enabled="False" />
                                    <px:PXSelector ID="edReturnOrigOrderNbr" runat="server" DataField="SOShipLine__OrigOrderNbr" Enabled="False"/>
                                    <px:PXSegmentMask ID="edReturnInventoryID" runat="server" DataField="InventoryID" Enabled="False" AllowEdit="True" />
                                    <px:PXSegmentMask ID="edReturnSiteID" runat="server" DataField="SiteID" Enabled="False" AllowEdit="True" />
                                    <px:PXSegmentMask ID="edReturnLocationID" runat="server" DataField="LocationID" Enabled="False"/>
                                    <px:PXSelector ID="edReturnLotSerialNbr" runat="server" DataField="LotSerialNbr" Enabled="False" />
                                    <px:PXDateTimeEdit ID="edReturnExpireDate" runat="server" DataField="ExpireDate" Enabled="False" />
                                    <px:PXNumberEdit ID="edReturnPickedQty" runat="server" DataField="PickedQty" Enabled="False"/>
                                    <px:PXNumberEdit ID="edReturnQty" runat="server" DataField="Qty" Enabled="False"/>
                                    <px:PXSelector ID="edReturnUOM" runat="server" DataField="UOM" Enabled="False" />
                                    <px:PXCheckBox ID="edReturnIsFree" runat="server" DataField="SOShipLine__IsFree" Enabled="False" />
                                 </RowTemplate>
                            </px:PXGridLevel>
                        </Levels>
                         <AutoSize Enabled="True" />
                        <AutoSize Container="Window" Enabled="True" MinHeight="400" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Pack" VisibleExp="DataControls[&quot;chkShowPack&quot;].Value == true" BindingContext="formHeader">
                <Template>
                    <px:PXSplitContainer SplitterPosition="1" runat="server" ID="sp1" CollapseDirection="Panel1" Orientation="Horizontal" Panel2MinSize="700" Panel1MinSize="1" SkinID="Horizontal">
                        <AutoSize Enabled="true" Container="Window" ></AutoSize>
                        <Template1>
                            <px:PXGrid ID="gridPacked" runat="server" DataSourceID="ds" SyncPosition="true" Width="100%" SkinID="Inquire" Height="1px"  OnRowDataBound="PackGrid_RowDataBound">
                                <Levels>
                                    <px:PXGridLevel DataMember="PickedForPack">
                                        <Columns>
                                            <px:PXGridColumn DataField="Fits" Type="CheckBox" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="LineNbr" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="SOShipLine__OrigOrderType" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="SOShipLine__OrigOrderNbr" LinkCommand="ViewOrder"></px:PXGridColumn>
                                            <px:PXGridColumn DataField="SiteID" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="LocationID" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="InventoryID" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="SOShipLine__TranDesc" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="LotSerialNbr" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="ExpireDate" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="CartQty" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="OverAllCartQty" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="PickedQty" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="PackedQty" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="Qty" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="UOM" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="SOShipLine__IsFree" Type="CheckBox" ></px:PXGridColumn>
                                            <px:PXGridColumn DataField="RelatedPickListSplitForceCompleted" Type="CheckBox" ></px:PXGridColumn>
                                        </Columns>
                                        <RowTemplate>
                                            <px:PXNumberEdit ID="edPackLineNbr" runat="server" DataField="LineNbr" Enabled="false"></px:PXNumberEdit>
                                            <px:PXTextEdit ID="edPackOrigOrderType" runat="server" DataField="SOShipLine__OrigOrderType" Enabled="False" ></px:PXTextEdit>
                                            <px:PXSelector ID="edPackOrigOrderNbr" runat="server" DataField="SOShipLine__OrigOrderNbr" Enabled="False" ></px:PXSelector>
                                            <px:PXSegmentMask ID="edPackInventoryID" runat="server" DataField="InventoryID" Enabled="False" AllowEdit="True" ></px:PXSegmentMask>
                                            <px:PXSegmentMask ID="edPackSiteID" runat="server" DataField="SiteID" Enabled="False" AllowEdit="True" ></px:PXSegmentMask>
                                            <px:PXSegmentMask ID="edPackLocationID" runat="server" DataField="LocationID" Enabled="False"></px:PXSegmentMask>
                                            <px:PXTextEdit ID="edPackLotSerialNbr" runat="server" DataField="LotSerialNbr" Enabled="False" ></px:PXTextEdit>
                                            <px:PXDateTimeEdit ID="edPackExpireDate" runat="server" DataField="ExpireDate" Enabled="False" ></px:PXDateTimeEdit>
                                            <px:PXNumberEdit ID="PXPackPickedQty" runat="server" DataField="PickedQty" Enabled="False"></px:PXNumberEdit>
                                            <px:PXNumberEdit ID="PXPackPackedQty" runat="server" DataField="PackedQty" Enabled="False"></px:PXNumberEdit>
                                            <px:PXNumberEdit ID="PXPackQty" runat="server" DataField="Qty" Enabled="False"></px:PXNumberEdit>
                                            <px:PXSelector ID="edPackUOM" runat="server" DataField="UOM" Enabled="False" ></px:PXSelector>
                                            <px:PXCheckBox ID="chkPackIsFree" runat="server" DataField="SOShipLine__IsFree" Enabled="False" ></px:PXCheckBox>
                                         </RowTemplate>
                                    </px:PXGridLevel>
                                </Levels>
                                <AutoSize Container="Window" Enabled="True" MinHeight="1" ></AutoSize>
                                <ActionBar>
                                    <CustomItems>
                                        <px:PXToolBarButton CommandName="ReopenLineQty" CommandSourceID="ds" DependOnGrid="gridPacked" StateColumn="RelatedPickListSplitForceCompleted"></px:PXToolBarButton>
                                    </CustomItems>
                                </ActionBar>
                            </px:PXGrid>
                        </Template1>
                        <Template2>
  <px:PXSplitContainer runat="server" ID="sp2" Orientation="Vertical" AllowResize="True" SplitterPosition="1000" BorderColor="Transparent" BorderWidth="14px" BorderStyle="Solid" Height="100%" Width="100%">
    <Template1>
      <px:PXFormView runat="server" ID="formBoxPackage" RenderStyle="Simple" DataSourceID="ds" DataMember="HeaderView">
        <Template>
          <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="S" />
          <px:PXSelector runat="server" CommitChanges="True" DataField="PackageLineNbrUI" AutoRefresh="true" ID="edPackage" />
          <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="L" />
          <px:PXFormView runat="server" ID="formBoxInfo" RenderStyle="Simple" DataSourceID="ds" DataMember="ShownPackage">
            <Template>
              <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="XS" ControlSize="XS" />
              <px:PXCheckBox runat="server" ID="chPackageConfirmed" AlignLeft="True" Enabled="false" DataField="Confirmed" />
              <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="XS" ControlSize="S" />
              <px:PXNumberEdit runat="server" ID="edPackageWeight" Enabled="false" DataField="Weight" />
              <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="S" />
              <px:PXNumberEdit runat="server" ID="edBoxMaxWeight" DataField="MaxWeight" />
              <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="XXS" ControlSize="XS" />
              <px:PXTextEdit runat="server" DataField="WeightUOM" ID="edWeightUOM" />
              <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="S" />
              <px:PXNumberEdit runat="server" ID="edTotalEstimatedQty" DataField="UsrEstPackageQuantity" Enabled="False" />
              <px:PXNumberEdit runat="server" ID="edTotalPackedQty" DataField="UsrContentPackageQuantity" Enabled="False" />
              <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="L" />
              <px:PXTextEdit runat="server" DataField="PackageDimensionsCombined" ID="edDimensions" /></Template></px:PXFormView></Template></px:PXFormView>
      <px:PXGrid runat="server" ID="gridPackedItems" Height="200px" SkinID="Inquire" Width="100%" Caption="Package Content" CaptionVisible="true" DataSourceID="ds" AllowPaging="False">
        <AutoSize Container="Window" Enabled="True" MinHeight="200" />
        <Levels>
          <px:PXGridLevel DataMember="Packed">
            <RowTemplate>
              <px:PXSegmentMask runat="server" ID="edPackedItemInventoryID" DataField="InventoryID" Enabled="False" AllowEdit="True" /></RowTemplate>
            <Columns>
              <px:PXGridColumn DataField="LineNbr" />
              <px:PXGridColumn DataField="InventoryID" />
              <px:PXGridColumn DataField="SOShipLine__TranDesc" />
              <px:PXGridColumn DataField="LotSerialNbr" />
              <px:PXGridColumn DataField="PackedQtyPerBox" />
              <px:PXGridColumn DataField="TotalPackedQtyForPackage" />
              <px:PXGridColumn DataField="TotalEstimatedQtyForPackage" />
              <px:PXGridColumn DataField="Qty" />
              <px:PXGridColumn DataField="UOM" />
              <px:PXGridColumn DataField="RelatedPickListSplitForceCompleted" Type="CheckBox" /></Columns></px:PXGridLevel></Levels></px:PXGrid></Template1>
    <Template2>
      <px:PXGrid runat="server" ID="CstPXGrid3" SyncPosition="True" SkinID="DetailsInTab" Width="100%" Caption="Estimated Content of Packages to be Packed" Style='height:300px;'>
        <AutoSize Enabled="True" MinHeight="100" />
        <Levels>
          <px:PXGridLevel DataMember="SelectedPackageContentsView">
            <Columns>
              <px:PXGridColumn DataField="UsrCompletedSortOrder" Visible="True" Width="70" />
              <px:PXGridColumn DataField="InventoryID" Width="70" />
              <px:PXGridColumn DataField="UOM" Width="72" CommitChanges="False" />
              <px:PXGridColumn DataField="StoreNbr" Width="140" />
              <px:PXGridColumn DataField="ShipmentSplitLineNbr" Width="70" CommitChanges="True" />
              <px:PXGridColumn DataField="DefaultIssueFrom" Width="140" />
              <px:PXGridColumn DataField="PackedQty" Width="100" />
              <px:PXGridColumn DataField="UsrActualPackedQty" Width="100" />
              <px:PXGridColumn DataField="OrderNbr" Width="140" />
              <px:PXGridColumn DataField="OrderLineNbr" Width="140" />
              <px:PXGridColumn DataField="LotSerialNbr" Width="220" /></Columns></px:PXGridLevel></Levels></px:PXGrid></Template2></px:PXSplitContainer></Template2>
                    </px:PXSplitContainer>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Ship" VisibleExp="DataControls[&quot;chkShowShip&quot;].Value == true" BindingContext="formHeader">
                <Template>
                    <px:PXFormView ID="formShipAddress" runat="server" DataMember="Shipping_Address" DataSourceID="ds" AllowCollapse="true" RenderStyle="Simple">
                        <Template>
                            <px:PXLayoutRule runat="server" ControlSize="XM" LabelsWidth="SM" StartColumn="True" />
                            <px:PXTextEdit ID="edAddressLine1" runat="server" DataField="AddressLine1"  Enabled="False"/>
                            <px:PXTextEdit ID="edAddressLine2" runat="server" DataField="AddressLine2"  Enabled="False"/>
                            <px:PXTextEdit ID="edCity" runat="server" DataField="City"  Enabled="False"/>
                            <px:PXLayoutRule runat="server" StartColumn="True" />
                            <px:PXSelector ID="edCountryID" runat="server" DataField="CountryID" AutoRefresh="True" CommitChanges="true"  Enabled="False"/>
                            <px:PXSelector ID="edState" runat="server" DataField="State" AutoRefresh="True" Enabled="False"/>
                            <px:PXMaskEdit ID="edPostalCode" runat="server" DataField="PostalCode" CommitChanges="true"  Enabled="False"/>
                            <px:PXLayoutRule runat="server" StartColumn="True" />
                            <px:PXFormView ID="formShipInfo" runat="server" DataMember="CurrentDocument" DataSourceID="ds" CaptionVisible="False" RenderStyle="Simple">
                                <Template>
                                    <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="M" />
                                    <px:PXNumberEdit ID="edShipmentQty" runat="server" DataField="ShipmentQty" Enabled="False" />
                                    <px:PXNumberEdit ID="edShipmentWeight" runat="server" DataField="ShipmentWeight" Enabled="False" />
                                    <px:PXNumberEdit ID="edShipmentVolume" runat="server" DataField="ShipmentVolume" Enabled="False" />
                                    <px:PXLayoutRule runat="server" StartColumn="True" />
                                    <px:PXNumberEdit ID="edPackageCount" runat="server" DataField="PackageCount" Enabled="False" />
                                    <px:PXNumberEdit ID="edPackageWeight" runat="server" DataField="PackageWeight" Enabled="False" />
                                </Template>
                                <ContentStyle BackColor="Transparent" BorderStyle="None" />
                            </px:PXFormView>
                        </Template>
                        <ContentStyle BackColor="Transparent" BorderStyle="None" />
                    </px:PXFormView>
                    <px:PXGrid ID="gridRates" runat="server" Width="100%" DataSourceID="ds" Caption="Carrier Rates" SkinID="Details" Height="90px" CaptionVisible="True" AllowPaging="False" AllowFilter="True" AutoAdjustColumns="False" >
                        <Mode AllowAddNew="False" AllowDelete="False" AllowFormEdit="False" />
                        <ActionBar Position="Top" PagerVisible="False" CustomItemsGroup="1" ActionsVisible="True">
                            <CustomItems>
                                <px:PXToolBarButton CommandName="ScanRefreshRates" CommandSourceID="ds"/>
                                <px:PXToolBarButton CommandName="ScanGetLabels" CommandSourceID="ds"/>
                            </CustomItems>
                        </ActionBar>
                        <Levels>
                            <px:PXGridLevel DataMember="CarrierRates">
                                <Columns>
                                    <px:PXGridColumn DataField="Selected" Type="CheckBox" CommitChanges="true" TextAlign="Center" />
                                    <px:PXGridColumn DataField="Method" Label="Code" />
                                    <px:PXGridColumn DataField="Description" Label="Description" />
                                    <px:PXGridColumn AllowUpdate="False" DataField="Amount" />
                                    <px:PXGridColumn AllowUpdate="False" DataField="DaysInTransit" Label="Days in Transit" />
                                    <px:PXGridColumn AllowUpdate="False" DataField="DeliveryDate" Label="Delivery Date" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Container="Window" Enabled="True" MinHeight="50" />
                    </px:PXGrid>
                    <px:PXGrid ID="gridPackages" runat="server" Width="100%" DataSourceID="ds" Caption="Packages" SkinID="Details" Height="90px" CaptionVisible="True" AllowPaging="False">
                        <Levels>
                            <px:PXGridLevel DataMember="Packages">
                                <Columns>
                                    <px:PXGridColumn DataField="BoxID" CommitChanges="True" />
                                    <px:PXGridColumn DataField="Description" Label="Description" CommitChanges="True" />
                                    <px:PXGridColumn DataField="AllowOverrideDimension" TextAlign="Center" Type="CheckBox" />
                                    <px:PXGridColumn DataField="Length" CommitChanges="True" />
                                    <px:PXGridColumn DataField="Width" CommitChanges="True" />
                                    <px:PXGridColumn DataField="Height" CommitChanges="True" />
                                    <px:PXGridColumn DataField="LinearUOM" CommitChanges="True" />
                                    <px:PXGridColumn DataField="WeightUOM" CommitChanges="True" />
                                    <px:PXGridColumn DataField="Weight" CommitChanges="True" />
                                    <px:PXGridColumn DataField="BoxWeight" CommitChanges="True" />
                                    <px:PXGridColumn DataField="NetWeight" CommitChanges="True" />
                                    <px:PXGridColumn DataField="MaxWeight" CommitChanges="True" />
                                    <px:PXGridColumn DataField="DeclaredValue" CommitChanges="True" />
                                    <px:PXGridColumn DataField="COD" CommitChanges="True" />
                                    <px:PXGridColumn DataField="TrackNumber" CommitChanges="True" />
                                    <px:PXGridColumn DataField="StampsAddOns" Type="DropDownList" CommitChanges="True" />
                                </Columns>
                                <RowTemplate>
                                    <px:PXDropDown runat="server" ID="edStampsAddOns" DataField="StampsAddOns" AllowMultiSelect="True" />
                                </RowTemplate>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Container="Window" Enabled="True" MinHeight="50" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Scan Log" VisibleExp="DataControls[&quot;chkShowLog&quot;].Value == true" BindingContext="formHeader">
                <Template>
                    <px:PXGrid ID="grid4" runat="server" DataSourceID="ds" Style="height: 250px;" Width="100%" SkinID="Inquire" Height="372px" TabIndex="-7375" OnRowDataBound="LogGrid_RowDataBound">
                        <Levels>
                            <px:PXGridLevel DataMember="Logs">
                                <Columns>
                                    <px:PXGridColumn DataField="ScanTime" />
                                    <px:PXGridColumn DataField="Mode" />
                                    <px:PXGridColumn DataField="PromptCombined" />
                                    <px:PXGridColumn DataField="Scan" />
                                    <px:PXGridColumn DataField="Message" />
                                </Columns>
                            </px:PXGridLevel>
                        </Levels>
                        <AutoSize Container="Window" Enabled="True" MinHeight="400" />
                    </px:PXGrid>
                </Template>
            </px:PXTabItem>
        </Items>
        <AutoSize Enabled="True" Container="Window" />
    </px:PXTab>
    <%-- Settings --%>
    <px:PXSmartPanel ID="PanelSettings" runat="server" Caption="Settings" CaptionVisible="True" ShowAfterLoad="true"
        Key="UserSetupView" AutoCallBack-Command="Refresh" AutoCallBack-Enabled="True" AutoCallBack-Target="frmSettings" CloseButtonDialogResult="Abort">
        <px:PXFormView ID="frmSettings" runat="server" DataSourceID="ds" DataMember="UserSetupView" SkinID="Transparent">
            <Template>
                <px:PXLayoutRule ID="PXLayoutRule1" runat="server" LabelsWidth="M" ControlSize="M" StartGroup="True" SuppressLabel="True" GroupCaption="General"/>
                <px:PXCheckBox ID="edDefaultLocation" runat="server" DataField="DefaultLocationFromShipment" CommitChanges="true" />
                <px:PXCheckBox ID="edDefaultLotSerial" runat="server" DataField="DefaultLotSerialFromShipment" CommitChanges="true" />

                <px:PXLayoutRule ID="PXLayoutRule7" runat="server" LabelsWidth="M" ControlSize="M" StartGroup="True" SuppressLabel="True" GroupCaption="Printing"/>
                <px:PXCheckBox ID="edPrintShipmentConfirmation" runat="server" DataField="PrintShipmentConfirmation" CommitChanges="true" />
                <px:PXCheckBox ID="edPrintShipmentLabels" runat="server" DataField="PrintShipmentLabels" CommitChanges="true" />
                <px:PXCheckBox ID="edPrintCommercialInvoices" runat="server" DataField="PrintCommercialInvoices" CommitChanges="true" />

                <px:PXLayoutRule ID="PXLayoutRule4" runat="server" LabelsWidth="M" ControlSize="M" StartGroup="True" SuppressLabel="True" GroupCaption="Scale"/>
                <px:PXCheckBox ID="edUseScale" runat="server" DataField="UseScale" CommitChanges="true" />
                <px:PXLayoutRule ID="PXLayoutRule6" runat="server" LabelsWidth="M" ControlSize="M" SuppressLabel="False"/>
                <px:PXSelector ID="edScaleID" runat="server" DataField="ScaleDeviceID" CommitChanges="true" AutoComplete="false" />

                <px:PXLayoutRule ID="PXLayoutRule8" runat="server" LabelsWidth="M" ControlSize="M" StartGroup="True" SuppressLabel="True"/>
                <px:PXCheckBox ID="edEnterSizeForPackages" runat="server" DataField="EnterSizeForPackages" CommitChanges="true" />

            </Template>
        </px:PXFormView>
        <px:PXPanel ID="PXPanel2" runat="server" SkinID="Buttons">
            <px:PXButton ID="pbClose" runat="server" DialogResult="OK" Text="Save"/>
            <px:PXButton ID="pbCancel" runat="server" DialogResult="Abort" Text="Cancel"/>
        </px:PXPanel>
    </px:PXSmartPanel>
</asp:content>