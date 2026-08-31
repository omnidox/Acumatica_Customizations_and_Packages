-- Query hash: 8659BCA6
-- Representative Scan 12 execution
-- Shipment: 0000787
-- Shipment type: I
-- Captured start offset: 609.4077 ms
-- Captured SQL time: 42.6629 ms
-- Captured rows: 1808
-- IMPORTANT: Verify the selected database before executing.
-- This is a read-only SELECT captured from Acumatica Request Profiler.

SET STATISTICS IO ON;
SET STATISTICS TIME ON;

DECLARE @P0 nvarchar(15) = N'0000787';
DECLARE @P1 char(1) = 'I';

SELECT /* SO.30.20.20, 8659BCA6 */[SOShipLine].[ShipmentNbr], [SOShipLine].[ShipmentType], [SOShipLine].[LineNbr], [SOShipLine].[SortOrder], [SOShipLine].[CustomerID], [SOShipLine].[ShipDate], [SOShipLine].[Confirmed], [SOShipLine].[Released], [SOShipLine].[LineType], [SOShipLine].[OrigOrderType], [SOShipLine].[OrigOrderNbr], [SOShipLine].[OrigLineNbr], [SOShipLine].[OrigSplitLineNbr], [SOShipLine].[Operation], [SOShipLine].[SOLineSign], [SOShipLine].[OrigPlanType], [SOShipLine].[InvtMult], [SOShipLine].[IsStockItem], [SOShipLine].[InventoryID], [SOShipLine].[IsIntercompany], [SOShipLine].[PlanType], [SOShipLine].[SubItemID], [SOShipLine].[SiteID], [SOShipLine].[LocationID], [SOShipLine].[LotSerialNbr], [SOShipLine].[ExpireDate], [SOShipLine].[OrderUOM], [SOShipLine].[UOM], [SOShipLine].[ShippedQty], [SOShipLine].[BaseShippedQty], [SOShipLine].[BaseOriginalShippedQty], [SOShipLine].[UnassignedQty], [SOShipLine].[CompleteQtyMin], [SOShipLine].[BaseOrigOrderQty], [SOShipLine].[OrigOrderQty], [SOShipLine].[FullOrderQty], [SOShipLine].[BaseFullOrderQty], [SOShipLine].[FullOpenQty], [SOShipLine].[BaseFullOpenQty], [SOShipLine].[UnitCost], [SOShipLine].[ExtCost], [SOShipLine].[UnitPrice], [SOShipLine].[DiscPct], [SOShipLine].[AlternateID], [SOShipLine].[TranDesc], [SOShipLine].[UnitWeigth], [SOShipLine].[UnitVolume], [SOShipLine].[ExtWeight], [SOShipLine].[ExtVolume], [SOShipLine].[ProjectID], [SOShipLine].[TaskID], [SOShipLine].[CostCodeID], [SOShipLine].[ReasonCode], [SOShipLine].[IsFree], [SOShipLine].[ManualPrice], [SOShipLine].[ManualDisc], [SOShipLine].[IsUnassigned], [SOShipLine].[DiscountsAppliedToLine], [SOShipLine].[DiscountID], [SOShipLine].[DiscountSequenceID], [SOShipLine].[ShipComplete], [SOShipLine].[RequireINUpdate], [SOShipLine].[PickedQty], [SOShipLine].[BasePickedQty], [SOShipLine].[PackedQty], [SOShipLine].[BasePackedQty], [SOShipLine].[NoteID], NULL, NULL, NULL, [SOShipLine].[CreatedByID], [SOShipLine].[CreatedByScreenID], [SOShipLine].[CreatedDateTime], [SOShipLine].[LastModifiedByID], [SOShipLine].[LastModifiedByScreenID], [SOShipLine].[LastModifiedDateTime], [SOShipLine].[tstamp], [SOShipLine].[BlanketType], [SOShipLine].[BlanketNbr], [SOShipLine].[BlanketLineNbr], [SOShipLine].[BlanketSplitLineNbr], [SOShipLine].[IsSpecialOrder], [SOShipLine].[CostCenterID], [SOShipLine].[InvoiceGroupNbr], [SOShipLine].[UsrLineNbr], [SOShipLine].[UsrTCCustomField1], [SOShipLine].[UsrTCCustomField2], [SOShipLine].[UsrTCCustomField3], [SOShipLine].[UsrTCCustomField4], [SOShipLine].[UsrTCCustomField5], [SOShipLine].[UsrTCCustomField6], [SOShipLine].[UsrTCCustomField7], [SOShipLine].[UsrTCCustomField8], [SOShipLine].[UsrTCCustomField9], [SOShipLine].[UsrTCCustomField10], [SOShipLine].[UsrTCPOLineNumber], [SOShipLine].[UsrTCPOLineSeq], [SOShipLine].[DatabaseRecordStatus]
FROM [SOShipLine] [SOShipLine]
WHERE ( [SOShipLine].[CompanyID] = 3) AND ( [SOShipLine].[DatabaseRecordStatus] = 0) AND ( @P0 = [SOShipLine].[ShipmentNbr] AND @P1 = [SOShipLine].[ShipmentType])
ORDER BY [SOShipLine].[ShipmentNbr], [SOShipLine].[ShipmentType], [SOShipLine].[LineNbr] OPTION(OPTIMIZE FOR UNKNOWN);

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;

