# Per-Item Scan Data Access Investigation Plan

## Objective

Document everything accessed during one Pick, Pack, and Ship item scan, from the beginning of the scan request through completion.

The investigation will identify:

- Acumatica processing stage
- SQL query sequence
- Tables accessed
- Indexes used
- Read and write operations
- Parameters
- Rows returned or affected
- SQL execution time
- Cached versus database-executed queries
- Application methods responsible for each query
- Indexes defined on each accessed table
- Physical indexes and access operators actually selected by SQL Server
- Database rows returned versus HTTP response data returned to the scanner/browser

## Required evidence sources

| Source | Information provided |
|---|---|
| Acumatica Request Profiler | Scan boundary, SQL sequence, start times, tables, parameters, row counts, durations, cache status, and C# stack traces |
| SQL Server index metadata | Index names, key columns, included columns, uniqueness, and clustered/nonclustered definitions |
| SQL Server execution plans | Indexes used, seeks, scans, key lookups, joins, sorts, warnings, and actual row counts |
| JetBrains dotTrace | Application processing between SQL queries and the complete method flow |
| Browser developer tools | HTTP scan callback, response size, transferred bytes, and client-observed duration |

---

## Phase 1: Capture isolated item scans

### Preparation

1. Use the local Acumatica environment and restored representative database.
2. Confirm all intended production-safe scan customizations are enabled.
3. Open Pick, Pack, and Ship (`SO302020`).
4. Load the test shipment and select the carton before beginning the capture.
5. Perform one warm-up scan that will not be included in the measurements.
6. Ensure no other users or processes are creating unnecessary activity in the local instance.

### Request Profiler configuration

1. Open Request Profiler (`SM205070`).
2. Clear the existing profiler log.
3. Enable:
   - Log Requests
   - Log SQL
   - Log SQL stack traces
   - Include cached SQL results, if available
4. Filter the capture by:
   - Screen: `SO302020`
   - Test username
5. Start logging.
6. Scan exactly one valid item.
7. Wait until the scan response finishes completely.
8. Stop logging immediately.
9. Locate the request with:
   - Screen: `SO302020`
   - Command Name: `scan`
10. Export the Request Profiler ZIP.

### Repeated captures

Capture three item scans separately:

- `ItemScan_01.zip`
- `ItemScan_02.zip`
- `ItemScan_03.zip`

Each exported snapshot should contain one item scan only. Separate captures make the beginning and end of each scan unambiguous and allow cache-related differences to be identified.

---

## Phase 2: Create the chronological SQL timeline

Extract every SQL entry belonging to the scan request and place it in execution order.

### Current completed baseline

The Scan 12 baseline has been extracted to:

`ProfilerLog_020\Scan_12_Chronological_SQL_Timeline.csv`

Verified contents:

- 182 SQL executions
- 77 distinct query hashes
- 5,774 total database rows returned
- 280.9517 ms of summed logged SQL duration
- Complete SQL text and C# stack traces for all 182 entries
- No results served from Acumatica's query cache
- Chronological coverage from 2.0846 ms through 1,360.8388 ms of the parent request

This baseline is sufficient for the initial query-flow and table-access analysis. Additional captures remain useful for measuring repeatability and cache variation.

| Sequence | Start time | Query hash | Tables | Operation | Parameters | Rows | SQL time | From cache | Calling method |
|---:|---:|---|---|---|---|---:|---:|---|---|
| 1 |  |  |  |  |  |  |  |  |  |
| 2 |  |  |  |  |  |  |  |  |  |
| 3 |  |  |  |  |  |  |  |  |  |

### Fields to capture

- Sequence number
- Start Time
- Query Hash
- SQL text
- Tables
- Parameters
- Row Count
- SQL Time
- From Cache
- Stack trace or calling method
- Read, insert, update, or delete operation

### Ordering rule

Sort numerically by `RequestStartTime`, then assign a new unique sequence number beginning with 1. Do not use `QueryOrderId` as the timeline sequence because it can repeat for nested or related query activity.

### Important distinction

The report must distinguish between:

- SQL statements executed against SQL Server
- Results returned from Acumatica’s query cache

Cached queries may still require application processing even though SQL Server does not execute them again.

### Database rows versus API response data

`NRows` measures rows returned or affected by an individual SQL statement. It does not measure the size of the HTTP response returned to the scanner or browser.

For Peiyu's request, report both measurements separately:

1. **Database response:** SQL executions and `NRows` from Request Profiler.
2. **Browser response:** transferred bytes, resource size, status, and duration of the `SO302020.aspx` scan `POST`, captured from the browser Network panel or an exported HAR file.

The SO302020 item scan is normally one browser callback containing many internal database operations. The SQL entries must not be described as separate browser API calls.

---

## Phase 2A: Inventory index definitions

For every table referenced by a material scan query, collect SQL Server index metadata:

- Schema and table
- Index name
- Clustered or nonclustered type
- Primary-key and uniqueness status
- Key columns in ordinal order
- Ascending or descending direction
- Included columns
- Filter definition, if present

This inventory answers which columns are indexed. It does not prove that a scan query used a particular index; Phase 3 provides that evidence.

| Table | Index | Type | Key columns | Included columns | Unique | Filter |
|---|---|---|---|---|---|---|
|  |  |  |  |  |  |  |

---

## Phase 3: Identify the indexes used

Acumatica Request Profiler identifies the tables accessed, but it does not identify the physical indexes selected by SQL Server. Actual execution plans are required.

### Execution-plan procedure

For each high-impact material `SELECT` query:

1. Copy the SQL text from Request Profiler.
2. Copy the captured parameter values.
3. Open SQL Server Management Studio against the restored local database.
4. Open a new query window.
5. Enable **Include Actual Execution Plan** by pressing `Ctrl+M`.
6. Add:

    ```sql
    SET STATISTICS IO ON;
    SET STATISTICS TIME ON;
    ```

7. Declare the captured parameter values using SQL data types that match the referenced columns.
8. Execute the query.
9. Save the execution plan as a `.sqlplan` file.
10. Save the Messages output containing `STATISTICS IO` and `STATISTICS TIME`.
11. Record every relevant execution-plan operator.

### Execution safety

An actual execution plan executes the SQL statement. Do not replay captured `UPDATE`, `INSERT`, or `DELETE` statements merely to obtain an actual plan.

- Use actual execution plans initially for `SELECT` statements only.
- Use an estimated plan (`Ctrl+L`) first for write statements.
- If an actual write plan is essential, use a disposable restored database and document the rollback and side-effect controls.
- Never perform experimental plan capture against the SaaS production database without ASC's authorization and supervision.

### Reproduction controls

For each plan, record:

- Database and Acumatica snapshot/version
- Query hash and exact SQL text
- Parameter values and SQL data types
- Capture timestamp
- Relevant customization version
- Whether the database and application caches were warm or cold
- Whether the query was replayed manually or captured during a live scan

Different parameter types, statistics, indexes, data volumes, or database compatibility settings can produce a different plan from the original Acumatica execution.

### Operators to document

- Index Seek
- Clustered Index Seek
- Index Scan
- Clustered Index Scan
- Table Scan
- Key Lookup
- Sort
- Hash Match
- Nested Loops
- Merge Join
- Spill or execution warning
- Number of executions
- Seek predicates
- Residual predicates
- Output columns

### Index-use worksheet

| Query hash | Table | Index used | Access type | Seek columns | Residual predicate | Executions | Actual rows | Estimated rows | Logical reads | Warning |
|---|---|---|---|---|---|---:|---:|---:|---:|---|
|  |  |  |  |  |  |  |  |  |  |  |

### Initial priority queries

1. `FF246783` — two full 1,808-row `SOShipLineSplit`/`SOShipLine`/`INLocation` loads
2. `8659BCA6` — full 1,808-row `SOShipLine` load
3. `B730E2F8` — `SOLine`/`SOShipLine` lookup returning 66 rows
4. `D4571A51` — current `INItemXRef`/`InventoryItem` barcode-resolution query
5. `E93AD83C` — package-split lookup, if repetition remains material in the current capture
6. Queries supporting the proposed targeted `IsItemMissing()` implementation
7. `11914AC2`, if its current executions, rows, or SQL time justify plan analysis

The priority list must be recalculated for later captures instead of assuming that an older high-cost query remains important.

---

## Phase 4: Map queries to scan-processing stages

Use Request Profiler stack traces and dotTrace results to group the SQL activity into business stages.

Expected high-level stages include:

1. HTTP callback and grid synchronization
2. Barcode and alternate-ID resolution
3. Inventory-item retrieval
4. Shipment-item validation
5. Packing confirmation
6. `PackSplit` quantity and package updates
7. Database persistence
8. Post-pack `CanPack` evaluation
9. Command-state refresh
10. Callback response rendering

The final stage assignments must be based on the captured stack traces rather than assumptions.

### Process-flow worksheet

| Stage | Acumatica method | Query hash | Tables | Indexes | Operation | Rows | SQL time |
|---|---|---|---|---|---|---:|---:|
| Grid synchronization |  |  |  |  |  |  |  |
| Barcode resolution |  |  |  |  |  |  |  |
| Item validation |  |  |  |  |  |  |  |
| Packing update |  |  |  |  |  |  |  |
| Persistence |  |  |  |  |  |  |  |
| Command refresh |  |  |  |  |  |  |  |

---

## Phase 5: Compare the three scans

Compare the captures to distinguish repeatable behavior from one-time initialization or caching.

| Metric | Scan 1 | Scan 2 | Scan 3 | Average |
|---|---:|---:|---:|---:|
| Server time |  |  |  |  |
| Server CPU |  |  |  |  |
| SQL time |  |  |  |  |
| SQL count |  |  |  |  |
| SQL rows |  |  |  |  |
| Cached query count |  |  |  |  |
| Exceptions |  |  |  |  |
| HTTP response bytes |  |  |  |  |
| HTTP transferred bytes |  |  |  |  |

Also compare:

- Queries present in all three scans
- Queries occurring only during the first scan
- Differences in cache use
- Differences in rows returned
- Differences in execution plans
- Differences caused by changing packed quantities

---

## Extended Events fallback

Use SQL Server Extended Events only if an important application query cannot be reproduced accurately in SSMS.

If required:

1. Run Extended Events only in the local environment.
2. Filter the session to the Acumatica database.
3. Keep the capture period as short as possible.
4. Perform exactly one scan.
5. Stop the session immediately.
6. Export the captured events and plans.

The `query_post_execution_showplan` event can add noticeable overhead to workloads containing many short queries. It must not remain enabled during ordinary performance measurements.

---

## Final deliverable structure

### 1. Executive overview

Describe the captured workflow, test shipment, item, carton, environment, customization version, and capture timestamp.

### 2. Scan flow diagram

    Barcode submitted
    → Grid synchronization
    → Barcode resolution
    → Shipment-item validation
    → PackSplit
    → Save changes
    → CanPack evaluation
    → Command refresh
    → Response returned

### 3. Chronological query timeline

List every SQL or cached query in the order it occurred.

### 4. Table-access inventory

| Table | Processing stage | Read count | Write count | Rows returned | Total SQL time |
|---|---|---:|---:|---:|---:|
|  |  |  |  |  |  |

### 5. Defined-index inventory

List the indexes available on every material table, including ordered key columns and included columns. Keep this separate from the indexes actually selected by SQL Server.

### 6. Query-to-index matrix

| Query hash | Tables | Indexes used | Access operators | Rows | SQL time |
|---|---|---|---|---:|---:|
|  |  |  |  |  |  |

### 7. API and database response measurements

For every representative item scan, report:

- One complete browser scan callback
- HTTP status
- HTTP response resource size and transferred bytes
- Client-observed duration
- SQL execution count
- Database rows returned or affected

Do not equate database row counts with HTTP payload size.

### 8. Read-and-write flow

Document which tables are read before packing, modified during `PackSplit`, and reread during post-pack validation.

### 9. Three-scan comparison

Document repeatable activity, initialization activity, caching differences, and measurement variation.

### 10. Supporting artifacts

Include:

- Three Request Profiler ZIP files
- Exported SQL timeline
- SQL text and parameters
- Saved `.sqlplan` files
- `STATISTICS IO` output
- `STATISTICS TIME` output
- Relevant dotTrace screenshots or exports
- Browser Network export or HAR file for each representative scan
- SQL Server index-definition export
- Test conditions and timestamps

---

## Completion criteria

The investigation is complete when the report can answer, for one item scan:

1. Which Acumatica methods executed?
2. Which SQL statements executed and in what order?
3. Which tables were accessed?
4. Which indexes were used?
5. Were the accesses seeks, scans, or lookups?
6. How many rows were returned or modified?
7. How long did each query take?
8. Which results came from cache?
9. Which tables were updated?
10. What processing occurred before and after each database call?
11. Which columns are defined as index keys or included columns on each material table?
12. Which physical index did SQL Server actually select for each high-impact query?
13. How many database rows were returned, and how large was the separate HTTP response?
