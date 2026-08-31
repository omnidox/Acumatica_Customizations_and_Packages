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

## Required evidence sources

| Source | Information provided |
|---|---|
| Acumatica Request Profiler | Scan boundary, SQL sequence, start times, tables, parameters, row counts, durations, cache status, and C# stack traces |
| SQL Server execution plans | Indexes used, seeks, scans, key lookups, joins, sorts, warnings, and actual row counts |
| JetBrains dotTrace | Application processing between SQL queries and the complete method flow |

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

### Important distinction

The report must distinguish between:

- SQL statements executed against SQL Server
- Results returned from Acumatica’s query cache

Cached queries may still require application processing even though SQL Server does not execute them again.

---

## Phase 3: Identify the indexes used

Acumatica Request Profiler identifies the tables accessed, but it does not identify the physical indexes selected by SQL Server. Actual execution plans are required.

### Execution-plan procedure

For each unique material SQL query:

1. Copy the SQL text from Request Profiler.
2. Copy the captured parameter values.
3. Open SQL Server Management Studio against the restored local database.
4. Open a new query window.
5. Enable **Include Actual Execution Plan** by pressing `Ctrl+M`.
6. Add:

    SET STATISTICS IO ON;
    SET STATISTICS TIME ON;

7. Declare or substitute the captured parameter values.
8. Execute the query.
9. Save the execution plan as a `.sqlplan` file.
10. Record every relevant execution-plan operator.

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

### Index-use worksheet

| Query hash | Table | Index | Access type | Seek predicate | Actual rows | Estimated rows | Logical reads | Warning |
|---|---|---|---|---|---:|---:|---:|---|
|  |  |  |  |  |  |  |  |  |

### Initial priority queries

1. `FF246783`
2. Current `INItemXRef` barcode query
3. `E93AD83C`
4. `11914AC2`
5. Queries used by the proposed targeted `IsItemMissing()` implementation

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

### 5. Query-to-index matrix

| Query hash | Tables | Indexes used | Access operators | Rows | SQL time |
|---|---|---|---|---:|---:|
|  |  |  |  |  |  |

### 6. Read-and-write flow

Document which tables are read before packing, modified during `PackSplit`, and reread during post-pack validation.

### 7. Three-scan comparison

Document repeatable activity, initialization activity, caching differences, and measurement variation.

### 8. Supporting artifacts

Include:

- Three Request Profiler ZIP files
- Exported SQL timeline
- SQL text and parameters
- Saved `.sqlplan` files
- `STATISTICS IO` output
- `STATISTICS TIME` output
- Relevant dotTrace screenshots or exports
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