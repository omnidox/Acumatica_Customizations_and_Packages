Use SSMS against the local restored Acumatica database. Start with the expensive `SELECT` queries; do not execute captured `UPDATE`, `INSERT`, or `DELETE` statements casually because an actual plan executes the statement.

## Recommended first targets

Based on Scan 12:

| Priority | Query hash | Tables | Reason |
|---:|---|---|---|
| 1 | `FF246783` | `SOShipLineSplit`, `SOShipLine`, `INLocation` | Runs twice and returns 1,808 rows each time |
| 2 | `8659BCA6` | `SOShipLine` | Returns 1,808 rows |
| 3 | `B730E2F8` | `SOLine`, `SOShipLine` | Returns 66 rows |
| 4 | `D4571A51` | `INItemXRef`, `InventoryItem` | Barcode-resolution query |

## Step-by-step procedure

### 1. Copy one query from the CSV

Open:

[Scan_12_Chronological_SQL_Timeline.csv](C:\Users\Procomm\Documents\GitHub\Acumatica_Customizations_and_Packages\Istar_Development\Projects\Acumatica_Speed_up\Asgard_Off\ProfilerLog_020\Scan_12_Chronological_SQL_Timeline.csv)

Filter `SQLHash` to `FF246783`, then copy:

- `SQLText`
- `SQLParams`

Use one execution with the representative shipment parameters.

see an extracted query below
```

```

### 2. Open SSMS

1. Connect to the local SQL Server.
2. Select the restored Acumatica database.
3. Open **New Query**.
4. Confirm the database dropdown shows the correct database.

Do not perform this initially against production.

### 3. Define the captured parameters

The SQL will contain placeholders such as:

```sql
@P0
```

Add declarations before the query using the captured values:

```sql
DECLARE @P0 nvarchar(15) = N'0000787';
```

The exact data type should match the column receiving the parameter. If several parameters appear:

```sql
DECLARE @P0 nvarchar(15) = N'0000787';
DECLARE @P1 int = 1237;
```

Do not guess silently. Check the referenced column’s SQL type in SSMS under:

```text
Database
→ Tables
→ dbo.TableName
→ Columns
```

### 4. Enable measurements

Place this before the query:

```sql
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
```

Place this afterward:

```sql
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
```

`STATISTICS IO` reports logical reads—the number of database pages read from SQL Server’s data cache. [Microsoft: SET STATISTICS IO](https://learn.microsoft.com/en-us/sql/t-sql/statements/set-statistics-io-transact-sql)

The complete script will resemble:

```sql
USE [AcumaticaDB];
GO

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
GO

DECLARE @P0 nvarchar(15) = N'0000787';

-- Paste the exact captured SELECT statement here.

GO
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
GO
```

### 5. Enable the actual execution plan

In SSMS:

1. Select **Query → Include Actual Execution Plan**.
2. Alternatively, press `Ctrl+M`.
3. Press `F5` or select **Execute**.

The query executes normally, and an **Execution Plan** tab appears. An actual plan includes the runtime plan SQL Server truly used, rather than only the optimizer’s estimate. The login needs permission to execute the query and `SHOWPLAN` permission. [Microsoft: Display an Actual Execution Plan](https://learn.microsoft.com/en-us/sql/relational-databases/performance/display-an-actual-execution-plan)

### 6. Record each table-access operator

In the graphical plan, locate operators such as:

- Index Seek
- Clustered Index Seek
- Index Scan
- Clustered Index Scan
- Key Lookup
- Table Scan

Select an operator and press `F4` to open **Properties**. Record:

| Field | What to capture |
|---|---|
| Physical Operation | Seek, scan, lookup, update, etc. |
| Object | Database, schema, table, and index name |
| Actual Number of Rows | Rows produced by the operator |
| Estimated Number of Rows | Optimizer’s estimate |
| Actual Number of Rows for All Executions | Total rows processed |
| Number of Executions | How often it ran |
| Seek Predicates | Columns used to navigate the index |
| Predicate | Residual filtering performed after access |
| Output List | Columns requested from the operator |

A seek is not automatically good and a scan is not automatically bad. The row count, logical reads, frequency, and shipment size must be considered together.

### 7. Capture logical reads

Open the SSMS **Messages** tab. You should see output resembling:

```text
Table 'SOShipLineSplit'. Scan count 1, logical reads 123, physical reads 0
Table 'SOShipLine'. Scan count 1, logical reads 45, physical reads 0
```

Record the logical reads for every table.

### 8. Save the plan

In the **Execution Plan** tab:

1. Right-click the plan background.
2. Select **Save Execution Plan As**.
3. Save using a clear name, such as:

```text
FF246783_Shipment0000787_Actual.sqlplan
```

Also save the SQL script and Messages output alongside it.

## Suggested results table

| Query hash | Table | Index used | Operator | Seek columns | Residual predicate | Actual rows | Estimated rows | Logical reads |
|---|---|---|---|---|---|---:|---:|---:|
| `FF246783` | `SOShipLineSplit` | TBD | TBD | TBD | TBD | TBD | TBD | TBD |
| `FF246783` | `SOShipLine` | TBD | TBD | TBD | TBD | TBD | TBD | TBD |
| `FF246783` | `INLocation` | TBD | TBD | TBD | TBD | TBD | TBD | TBD |

## Important warning for write statements

An actual execution plan executes the query. Therefore:

- `SELECT`: generally safe on the local restored database.
- `UPDATE`, `INSERT`, `DELETE`: do not replay merely to obtain a plan.
- Use an estimated plan for write statements first (`Ctrl+L`).
- If an actual write plan is genuinely necessary, use a disposable restored database and carefully controlled transaction, because triggers and application-related side effects may still occur.

Begin with `FF246783`. Once that plan is captured, it will demonstrate the complete process and reveal whether its existing primary-key access is already efficient or whether a different index could plausibly reduce reads.