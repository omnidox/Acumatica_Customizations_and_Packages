/*
 * Process Orders speed-up: read-only DB checks.
 * Created 2026-10-02. See Findings_Log.md (F5, F6 and section 6).
 *
 * Read-only. Set @CompanyID and @Shipment before running.
 * Local run: sqlcmd -S "PCWC-Legion\SQLEXPRESS" -d AcumaticaDB -E -i db_checks.sql
 */
SET NOCOUNT ON;

DECLARE @CompanyID int = 3;
DECLARE @Shipment nvarchar(15) = '0000787';

PRINT '=== Feature flags (FlexMan drives F3, AutoPackaging drives F5)';
SELECT CompanyID, UsrFlexMan, AutoPackaging
FROM FeaturesSet
WHERE CompanyID = @CompanyID;

PRINT '=== Table sizes';
SELECT 'SelectedPackageContents' AS tbl, COUNT(*) AS n FROM SelectedPackageContents WHERE CompanyID = @CompanyID
UNION ALL SELECT 'SOShipLine',             COUNT(*) FROM SOShipLine             WHERE CompanyID = @CompanyID
UNION ALL SELECT 'SOShipLineSplit',        COUNT(*) FROM SOShipLineSplit        WHERE CompanyID = @CompanyID
UNION ALL SELECT 'SOLine',                 COUNT(*) FROM SOLine                 WHERE CompanyID = @CompanyID
UNION ALL SELECT 'SOOrder',                COUNT(*) FROM SOOrder                WHERE CompanyID = @CompanyID
UNION ALL SELECT 'SOPackageDetail',        COUNT(*) FROM SOPackageDetail        WHERE CompanyID = @CompanyID;

PRINT '=== Largest shipments (LineCntr is a counter, about 2x the real line count)';
SELECT TOP 10
    s.ShipmentNbr, s.ShipDate, s.Status, s.OrderCntr, s.LineCntr, s.PackageCount,
    (SELECT COUNT(*) FROM SOShipLine x
      WHERE x.CompanyID = s.CompanyID AND x.ShipmentNbr = s.ShipmentNbr) AS real_lines,
    (SELECT COUNT(*) FROM SelectedPackageContents x
      WHERE x.CompanyID = s.CompanyID AND x.ShipmentNbr = s.ShipmentNbr) AS contents
FROM SOShipment s
WHERE s.CompanyID = @CompanyID
ORDER BY real_lines DESC;

PRINT '=== Indexes on tables used by the Create Shipment path (F6)';
SELECT t.name AS tbl, i.name AS idx, i.type_desc,
    STUFF((SELECT ',' + c.name
           FROM sys.index_columns ic
           JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
           WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
           ORDER BY ic.key_ordinal
           FOR XML PATH('')), 1, 1, '') AS key_cols
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
WHERE t.name IN ('SelectedPackageContents', 'SOShipLine', 'SOLine', 'SOOrder', 'SOPackageDetail')
  AND i.type > 0
ORDER BY t.name, i.index_id;

PRINT '=== Page reads for the query patterns in F6 (see the Messages output)';
DECLARE @Order nvarchar(15), @Inv int;
SELECT TOP 1 @Order = OrigOrderNbr, @Inv = InventoryID
FROM SOShipLine WHERE CompanyID = @CompanyID AND ShipmentNbr = @Shipment;

SET STATISTICS IO ON;
SELECT TOP 1 ShipmentNbr FROM SOShipLine
 WHERE CompanyID = @CompanyID AND OrigOrderNbr = @Order AND InventoryID = @Inv AND DatabaseRecordStatus = 0;
SELECT TOP 1 OrderType FROM SOOrder
 WHERE CompanyID = @CompanyID AND OrderNbr = @Order AND DatabaseRecordStatus = 0;
SELECT COUNT(*) FROM SOLine
 WHERE CompanyID = @CompanyID AND OrderNbr = @Order AND DatabaseRecordStatus = 0;
SELECT COUNT(*) FROM SOLine
 WHERE CompanyID = @CompanyID AND OrderType = 'SZ' AND OrderNbr = @Order AND DatabaseRecordStatus = 0;
SELECT TOP 1 RecordID FROM SelectedPackageContents
 WHERE CompanyID = @CompanyID AND ShipmentNbr = @Shipment AND PackageLineNbr = 5;
SET STATISTICS IO OFF;
