# Process Orders Speed-Up: Findings Log

**Basecamp task:** "SO Process Speed / Issue" (Peiyu Wu, opened 2026-09-30)
**Owner:** Rafael Hidalgo (took over 2026-10-02)
**Source material:** `../Problem Discussion/` (Basecamp thread export and screenshots)

Each entry is timestamped (Eastern time). Every finding is labeled with how it was established:

- **[Verified-Code]**: read in decompiled or runtime source.
- **[Verified-DB]**: confirmed by querying the local database.
- **[Inferred]**: reasoned from code or data, not yet measured.
- **[Open]**: still needs confirmation.

---

## 2026-10-02 11:55 EDT: Static code review and local DB checks

### 1. Problem as reported

- **Run:** Process Orders (SO501000), action Create Shipment, 1,131 Kohl's orders (type `SZ`), shipment dates 9/30–10/31/2026.
- **Result:** 2:45:07 elapsed, 1,040 processed, **91 failed**.
  - Every failure was: *"The number of the lines in this document has exceeded the limit set for the current license."*
  - Average time was about 8.8 seconds per order. The early rate was about 6.7 seconds per order, so the run slowed as it went.
- **Server load:** CPU was 21% and managed memory 1.3 GB during the run. DB space usage was 1%. The hardware upgrade made no difference.
- **License** (from the License Monitoring Console): **2,000 lines per transaction**, 7,500 serial numbers per document.
- **Vadym (Acupower), 2026-10-01:**
  - Orders with the same customer, location, warehouse, ship via and date merge into one shipment, and those shipments hit the line limit.
  - Estimated logic optimization at **40–50 hours**.
  - SQL optimization was estimated separately, with the BA and Yuriy.

### 2. Who owns the code on the Create Shipment path

The source for these packages isn't in the repo. They were decompiled with `ilspycmd` 11.1 from `C:\Program Files\Acumatica ERP\AcumaticaERP\Bin`.

| Assembly | Owner | How sure |
|---|---|---|
| `AA.Objects.*`, `Asgard.*` | Asgard Alliance (labels) | Confirmed by publisher info |
| `TCAddon` | TrueCommerce (EDI) | Confirmed by publisher info |
| `SNPMFG.PRD` | Advanced Solutions & Consulting (ASC), FlexMFG | Confirmed by publisher info |
| `ASCiStarKohls`, `ASCiStarWMS`, `ASCJSM`, `ASCJewelryLibrary` | ASC-branded custom work for iStar | Likely (naming) |
| `WMS` | Custom work, probably ASC/Acupower | Unconfirmed: no publisher info; the Acumatica copyright is template boilerplate |
| `App_RuntimeCode\*.cs` | Code-only customization projects, including ours | — |

Acupower is a development subcontractor for Acumatica partners (resellers), run by Yuriy Zaletskyy. That matches Vadym working alongside Jim Carroll (ASC). The authorship of the `ASC*` and `WMS` assemblies should be confirmed with Jim or Vadym.

### 3. How the base Create Shipment flow works [Verified-Code]

`PX.Objects.SO.GraphExtensions.SOShipmentEntryExt.CreateShipmentExtension.CreateShipment` runs once per order:

1. `Base.Clear()`, then `FindOrCreateShipment(args)`. This finds an open shipment matching date, warehouse, type, customer, ship-to address/contact, ship via, terms, zone, FOB, freight source and `IsManualPackage`. The criteria come from `CreateShipmentSOExtension.GetShipmentFieldLookups`.
2. Reload that shipment, add the order's lines and splits, then `Save.Press()`.

Every order for the same DC and date therefore goes into **one shipment that keeps growing**. The per-order cost of any logic that touches the whole shipment grows as the shipment does, so the total grows roughly with the square of the shipment size. The same merging is what pushes shipments past the 2,000-line license limit.

### 4. Findings, ranked by likely impact

#### F1. `WMS.SOShipmentEntryExt.CreateShipment` rebuilds packages for the whole shipment on every order [Verified-Code], growth with shipment size [Inferred]

After the base method runs, on every order it:

- Deletes the current package's `SelectedPackageContents` and all base auto (`"A"`) packages.
- Rebuilds packages for the order. For Kohl's this goes through `CreateOrderPackages` and `CreatePackagesByVolumeAndWeightSeparately`.
- Calls `DeleteEmptyPackages(all packages)`, which runs **one query per package** (`HasSelectedPackageContents`).
- Calls `SetBoxNbrStr(all packages)`, which renumbers "1 of N" and calls `MarkUpdated`.
- Loops over **every package in the shipment**, setting `Confirmed = false` and `Weight = 0` and calling `Packages.Update`. Every package becomes dirty and is written to the database again on every order. This also wipes the weights of packages that belong to earlier orders, which looks like a functional bug.
- Calls `Actions.PressSave()`, an additional save.

Event handlers in the same extension make this worse:

- `RowInserted<SOPackageDetailEx>` and `RowDeleted<SOPackageDetailEx>` re-select all packages and run `SetBoxNbrStr` on every package insert or delete.
- `RowInserted`, `RowUpdated` and `RowDeleted` on `SelectedPackageContents` and on `SOShipLineSplitPackage` call `RecalculatePackageByLineNbr`. That scans all packages, runs two content queries, and calls `Packages.Update`, which in turn fires `RowUpdated<SOPackageDetailEx>`, and that can re-scan all packages again.

Minor defect noticed: `SOPackageDetailExt.usrSepareteOrderNbr` is declared as `Field<usrSelectedParentBox>`, with the wrong type argument.

#### F2. Up to 6 saves per order instead of 1 [Verified-Code]

| Source | Saves |
|---|---|
| Base `CreateShipment` | 1 |
| `WMS.SOShipmentEntryExt.CreateShipment` | 1 |
| `ASCiStarKohls.SOShipmentEntryExt.CreateShipment` (`UpdateShipmentLines`) | 1 |
| FlexMFG `CreateShipmentExtensionFLXExt.CreateShipment` (`Save.Press` + `Persist` + `Persist`) | 3 |

Each save also runs the `Persist` overrides in TrueCommerce (which opens a transaction scope) and in `WMS` (which retries on lock violations). If Auto Packaging recalculation is triggered, that runs too (see F5).

#### F3. FlexMFG `CreateShipmentExtensionFLXExt` loops over every shipment line twice per order [Verified-Code], feature on [Verified-DB]

- It only runs when the FlexMan feature is on. **`FeaturesSet.UsrFlexMan = 1`.**
- `AssignSerialNumbersFromWOKitAssembly` and `DeleteLinesForNotCompletedWO` each loop over **all lines in the shipment**, not just the new order's lines, and run `SOLine.PK.Find` for each line. Each then calls `Persist`.
- For a shipment of about 1,800 lines, that is about 3,600 line lookups per order merged in.
- This is ASC's FlexMFG product. Changes would go through ASC.

#### F4. `ASCiStarKohls.SOShipmentEntryExt.CreateShipment` saves an extra time [Verified-Code]

After the base method, it reloads all shipment lines (`Transactions.Select()`), matches each order line by a linear search, sets `UsrLineNbr`, then calls `Save.PressButton()`. This runs for **every customer**, not only Kohl's.

#### F5. Base auto-packaging recalculation [Verified-Code], feature on [Verified-DB]

- **`FeaturesSet.AutoPackaging = 1`**, and Kohl's orders have `IsManualPackage = 0`.
- Base `SOShipmentEntry` recalculates auto packages for the whole shipment when packages are flagged invalid, which adding lines does.
- `WMS` then deletes every `"A"` package, so the base work is thrown away.

[Open] How often the recalculation actually fires during Process Orders still needs to be measured.

#### F6. Missing or partial indexes [Verified-DB]

Indexes and page reads were measured locally for shipment 0000787.

| Query pattern (where it's issued) | Matching index | Local page reads |
|---|---|---|
| `SelectedPackageContents WHERE ShipmentNbr AND PackageLineNbr` (WMS: `GetPackageContents`, `HasSelectedPackageContents`, `GetTotalPackageWeight/Volume`, the CreatePackages methods) | **None**, only the `RecordID` primary key | **499** (full scan, 56,893 rows) |
| `SOLine WHERE OrderNbr` without `OrderType` (WMS `CreateOrderPackages`; TrueCommerce `CreateShipmentFromSchedules` per line) | No (`OrderType` is the first PK column) | **1,068**, against 3 with `OrderType` (16 ms against 0 ms) |
| `SOShipLine WHERE OrigOrderNbr AND InventoryID` (WMS CreatePackages methods, per split; not limited to the shipment) | Partial (`SOShipLine_SOOrderLine` starts with `OrigOrderType`) | 74 |
| `SOOrder WHERE OrderNbr` without `OrderType` (WMS CreatePackages methods, per split) | Partial | 15 |

At local data sizes each call costs milliseconds, so **indexes alone don't explain 8.8 seconds per order** [Inferred]. The cost will be higher in production, where the tables are larger.

#### F7. TrueCommerce `CreateShipmentFromSchedules` [Verified-Code]

This runs once per shipment line. It does one `SOLine` lookup by `OrderNbr` + `LineNbr` without `OrderType` (see F6) and attaches and detaches 12 field-defaulting handlers. It's light otherwise, and it's a commercial ISV product.

### 5. Ruled out on the Create Shipment path [Verified-Code]

- **Our `App_RuntimeCode` extensions:**
  - The `RowSelecting` in `SOPackageDetail_MasterPackUnitsExt` only queries master cartons, and Create Shipment doesn't create any.
  - The `RowPersisted` in `SOShipmentEntryExt_SelectedPackageSort` only requests a view refresh.
  - The rest are `RowSelected` handlers, which only matter on screen, or are view delegates.
- **`ASCiStarWMS.ASCiStarWMSSOShipmentEntryExt`** only overrides `CreateInvoice`.
- **`ASCJewelryLibrary[v1.2.2].SOShipmentEntry.cs`** is an empty extension.
- **Asgard** `SOShipmentEntry` extensions: a `RowPersisting<SOShipment>` (own-shipment mode) and a `FieldUpdated<SOPackageDetail.confirmed>` (box-print mode), both light.

### 6. Local data observations [Verified-DB]

Local database: `PCWC-Legion\SQLEXPRESS` / `AcumaticaDB`. It has the same schema as production but older data. Tenant `CompanyID = 3`.

| Shipment | Orders | Lines | Packages | Content rows |
|---|---|---|---|---|
| 0000787 | 76 | 1,808 | 76 | 1,808 |
| 0000011 | 17 | 1,007 | 18 | 1,007 |
| 0000786 | 73 | 798 | 73 | 798 |
| 0000780 | 88 | 714 | 88 | 714 |

- One DC shipment already comes close to the 2,000-line license limit.
- `SOShipment.LineCntr` is about **twice** the real line count. Peiyu's "39k split lines" figure was most likely a counter, not a real line count.
- Order type `SZ` uses template `SO`, so the WMS logic runs.
- Kohl's orders: `UsrPackOrderSeparately = 1`, `IsManualPackage = 0`.
- Items use `PackageOption = V` (volume and weight).
- Packages are type `M`, one per order.

### 7. Recommendations so far

| # | Action | Owner | Effort | Effect |
|---|---|---|---|---|
| R1 | Override `FindOrCreateShipment` to start a new shipment before the line limit (for example around 1,800 lines), or to split by store | Us | Small | Stops the license errors without a license upgrade; shrinks every cost in F1–F4 |
| R2 | Add an index on `SelectedPackageContents (CompanyID, ShipmentNbr, PackageLineNbr)` | Us or Acupower (DB schema change) | Small | Turns full scans into index seeks |
| R3 | Add `OrderType` (and `ShipmentNbr` where relevant) to the queries in F6 and F7 | Acupower (`WMS`); TrueCommerce for F7 | Small to medium | Index seeks instead of scans |
| R4 | Limit the `WMS` package rebuild to the order being added: no update of every package, no re-scan in each handler, one save | Acupower/ASC | Medium to large (part of Vadym's 40–50 hours) | Removes the main cause of the growing cost |
| R5 | Check whether FlexMFG's per-order loops are needed for Kohl's orders; limit them to the new order's lines | ASC | Medium | Removes 3 saves and about 2× (lines in shipment) lookups per order |
| R6 | Fold `ASCiStarKohls`'s `UsrLineNbr` stamping into line creation, with no second save | ASC/Acupower | Small | One fewer save per order |

### 8. Open items

- [Open] Measure where the time actually goes. Rafael will run Process Orders for about 20 Kohl's orders on the local site with Request Profiler (SM205070) recording. The trace will be broken down by save, query and package rebuild.
- [Open] Confirm authorship of `WMS` and `ASC*` with Jim and Vadym.
- [Open] Run `db_checks.sql` against production (read-only) to compare table sizes and shipment shapes.
- [Open] Check how often the base auto-package recalculation fires (F5).

### 9. Reproducing this review

- **Decompile:** `ilspycmd -p -o <outdir> "<Bin>\<Assembly>.dll"` for the custom DLLs. For base types, use `ilspycmd -t PX.Objects.SO.SOShipmentEntry` (and `CreateShipmentExtension`, `CreateShipmentSOExtension`, `SOCreateShipment`) against `PX.Objects.dll`. Decompiled output goes in the scratchpad, never the repo or `Bin`.
- **Runtime code:** `C:\Program Files\Acumatica ERP\AcumaticaERP\App_RuntimeCode\`.
- **DB checks:** `db_checks.sql` in this folder, run with `sqlcmd -S "PCWC-Legion\SQLEXPRESS" -d AcumaticaDB -E -i db_checks.sql`.

---

## 2026-10-02 12:18 EDT: Profiling test set up and restore point created

### Restore point
- **Backup:** `C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\AcumaticaDB_pre_ProcessOrders_20261002.bak`
- **Taken:** 2026-10-02 12:17:45, 8.06 GB, `WITH CHECKSUM`. `RESTORE VERIFYONLY` passed.
- **State:** taken before either test run; the user was logged out of the local site.
- **Method:** SQL Server backup rather than an Acumatica snapshot. The community recommends it for repeat testing and it is much faster. Don't publish customization packages between backup and restore; if you do, re-publish after restoring.
- **Restore** (from an Administrator prompt):
  ```
  iisreset /stop
  sqlcmd -S "PCWC-Legion\SQLEXPRESS" -E -Q "ALTER DATABASE [AcumaticaDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [AcumaticaDB] FROM DISK = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\AcumaticaDB_pre_ProcessOrders_20261002.bak' WITH REPLACE, STATS = 10; ALTER DATABASE [AcumaticaDB] SET MULTI_USER;"
  iisreset /start
  ```
- **Size limit:** SQL Express caps a database at 10 GB. `AcumaticaDB` uses 8.2 GB of 10.2 GB allocated.

### Test batch [Verified-DB]
Kohl's (DKOHLS) DC **00840**, ship date **2026-04-27**, warehouse ISTAR. There was no existing shipment for this DC and date before the test. All 14 items for the 40 orders (178 lines) are fully available.

- **Run 1** (starts a new shipment): SO00006128–SO00006133, SO00006185–SO00006197, SO00006266
- **Run 2** (should merge into run 1's shipment): SO00006267–SO00006275, SO00006333–SO00006337, SO00006386, SO00006395–SO00006399

**Process Orders settings** (confirmed from the user's screenshot): Action = Create Shipment, Select By = Ship Date, Start = End = Shipment Date = 4/27/2026, Customer = DKOHLS, Warehouse = ISTAR, Location column filtered to 00840, sorted by Order Nbr ascending, **Process** (not Process All).

**Expectation if F1–F4 hold:** the per-order time in run 2 is noticeably higher than in run 1.

---

## 2026-10-02 12:36 EDT: Request Profiler results (two 20-order runs)

**Source:** `profiler results/ProfilerLog_first_20.zip` and `ProfilerLog_second_20.zip`, both exported from SM205070. The breakdown below was produced with `profiler results/analyze_profiler.py`. To re-run it, unzip each export into `first/` and `second/` folders and run `python analyze_profiler.py first 206` or `python analyze_profiler.py second 220`. The second argument is the long-running request's RecordId.

### Correction to the test design [Verified-Code]
**Run 2 did not merge into run 1's shipment.** Run 1 created shipment **0000792** (20 orders, 109 lines, 25 packages) and run 2 created **0000793** (20 orders, 69 lines, 21 packages).

`SOOrderEntry.CreateShipment` creates a new `DocumentList<SOShipment>` for each processing run, and `FindOrCreateShipment` only searches that list. **Orders only merge into shipments created earlier in the same Process run**, never into shipments that already exist in the database. The expectation recorded in the 12:18 entry (run 2 slower per order) was therefore wrong. Growth with shipment size has to be measured inside a single run.

### Overall numbers [Verified-Profiler]

| | Run 1 | Run 2 |
|---|---|---|
| Elapsed (long-running request) | 37.3 s | 18.4 s |
| CPU | 24.4 s | 13.8 s |
| SQL statements / SQL time | 6,730 / 7.7 s | 4,847 / 5.6 s |
| Startup before the first order | 6.7 s (cold start), plus a 7.9 s first order | 0.9 s (warm) |
| Per order (orders 2–19, warm) | about 0.65–1.2 s | about 0.35–0.95 s |
| Post-run loop (F8 below) | **3.8 s, 20 shipment saves** | **3.5 s, 20 shipment saves** |

- **SQL is only 15–30% of elapsed time**; most of the time is application CPU. This supports the view that indexes (F6) are a secondary issue locally and that logic (F1–F4, F8) dominates [Inferred].
- Run 1 was slower mainly because of cold start: the first request after login loads, compiles and caches graphs. It also had more lines per order (5.45 against 3.45 in run 2).

### F1 confirmed: package rewrites grow with every order [Verified-Profiler]
The number of `UPDATE SOPackageDetail` statements per order rises steadily: 0, 2, 3, 4 … 23 in run 1 and 0, 1, 2 … 19 in run 2. **Order *k* re-saves roughly every package created before it.** For a shipment of *N* orders that is about *N*²/2 package writes, and each write also runs the WMS row handlers.

For a production-sized DC shipment of about 76 orders, that is about 2,900 package writes, against about 200 for this 20-order test [Inferred by extrapolation].

### F2 confirmed: two shipment saves per order [Verified-Profiler]
Each order shows **2 `UPDATE SOShipment` statements**. Save SQL by the code that started the save (run 1):

| Triggered by | Queries | SQL ms |
|---|---|---|
| Base `CreateShipmentExtension.CreateShipment` | 1,430 | 816 |
| `WMS.SOShipmentEntryExt.CreateShipment` | 630 | 209 |
| `ASCiStarWMSSOOrderEntryExt.UpdateShipmentCustomerOrderNbr` (F8) | 160 | 59 |
| `ASCiStarKohls.UpdateShipmentLines` | 21 | 21 |

The FlexMFG and Kohl's saves issue little or no SQL when nothing has changed, but they still run the save pipeline.

### Attribution caveat
Queries were assigned to the innermost customization method on the stack. **TrueCommerce `Persist` (2,220 queries) and `ASCiStarKohls.CreateShipment` (411 queries) wrap the base methods, so those totals are mostly base save and base create work passing through them.** They are not overhead of their own.

Real customization overhead identified (run 1):

| Code | Queries | SQL ms | Note |
|---|---|---|---|
| FlexMFG `AssignSerialNumbersFromWOKitAssembly` (F3) | 1,292 | 573 | Its own loop over every shipment line |
| TrueCommerce `CreateShipmentFromSchedules` | 109 of 903 | 432 | Its own `SOLine` lookup without `OrderType`, about **4 ms each** (F6/F7) |
| WMS `HasSelectedPackageContents` | 280 | 509 | One query per package per order (F1 and F6: no index) |
| WMS `CreateOrderPackages` | 50 | 389 | `SOLine` by `OrderNbr` without `OrderType` (F6) |
| WMS `CreatePackagesByVolumeAndWeightSeparately` | 239 | 229 | Per-split `SOShipLine`, `SOOrder` and `InventoryItem` lookups |
| FlexMFG `SOOrderEntryExtLM.SOOrder_RowSelected` | 122 | 332 | `RowSelected` running queries during processing (new) |
| Asgard `PrintRowSelectedOptimization.SOShipment_RowSelected` | 102 | 159 | `RowSelected` running queries during processing (new, minor) |

### F8 (new): `ASCiStarWMS.ASCiStarWMSSOOrderEntryExt.CreateShipment` loads and saves the shipment once per order after the run [Verified-Code], [Verified-Profiler]

- This is an override on `SOOrderEntry.CreateShipment`, the action that Process Orders calls.
- After the base method finishes (in a `finally` block), it loops over **every selected order**. For each of that order's shipments it calls `UpdateShipmentCustomerOrderNbr`. That method creates a **new `SOShipmentEntry` graph**, loads the shipment, sets `CustomerOrderNbr` and calls `Save.Press()`.
- It neither de-duplicates shipments nor skips shipments that already have the value. In the test, all 20 orders shared one shipment and one PO, so the same shipment was loaded and saved **20 times**, which took 3.5–3.8 s and was **10–19% of each run**.
- In production, that becomes **1,131 graph creations and shipment saves** at the end of the run, on shipments of up to about 2,000 lines. None of this shows in the per-order progress counter [Inferred].
- **Fix (small, ASC/Acupower):** collect the distinct shipment numbers, skip any where the value is already set, and save each shipment once.

### Updated recommendations
- **R7 (new): fix F8.** De-duplicate shipments and save each once. Small effort, ASC/Acupower.
- **R1–R6 unchanged.** F1 is now measured rather than inferred.

### Open items (updated)
- [Open] **Measure growth inside a single run.** Restore the 12:17 backup, then process all **172 orders for DC 00840 / 4/27/2026 in one run**, with the profiler recording. That is about 803 lines, under the 2,000-line limit. The run should show how per-order time grows as the shipment grows, which the 20-order runs were too small to show.
- [Open] Production-scale comparison: `db_checks.sql` against production (read-only).
- [Open] Confirm authorship of `WMS` and `ASC*` with Jim and Vadym.
