# Acumatica Scan Performance Investigation Report

## Initial problem

Warehouse scans on shipment `0000787`, containing approximately 1,808 shipment splits, were taking more than five seconds on average. Profiler analysis showed unusually high SQL, CPU, and cache activity during every scan.

### Initial profiler evidence

Across four scan requests:

| Metric | Observed |
|---|---:|
| Average scan time | 5.19 seconds |
| Range | 4.20-5.82 seconds |
| SQL calls per scan | 1,101-2,110 |
| Cache/select operations | 13,558-15,716 |
| SQL time per scan | 1.30-1.91 seconds |
| CPU time per scan | 3.14-4.45 seconds |
| Shipment splits returned | Approximately 1,808 |

## First problem: Advanced Labels transaction quantity formula

The largest bottleneck was located in the third-party Advanced Labels customization:

```text
AA.Objects.Labels.TranQty.ALTranQty.ForSOShipLineSplit
```

The `UsrALTranQty` field on `SOShipLineSplit` used a `PXFormula` that called:

```csharp
SOShipLine.PK.Find(...)
```

The formula was evaluated repeatedly while Acumatica processed the shipment splits. On this shipment, it could execute approximately 1,814 individual `SOShipLine` lookups during one scan.

### Solution applied

A separate customization was added for the Pick/Pack/Ship screen. It replaces the field attributes in that graph context so the Advanced Labels formula is not evaluated during warehouse scanning.

The third-party Advanced Labels code was not modified directly.

### Verified improvement

| Metric | Before | After | Improvement |
|---|---:|---:|---:|
| Average scan time | 5.19 sec | 2.72 sec | 48% faster |
| SQL calls per scan | ~1,850 | ~273 | 85% fewer |
| SQL time per scan | 1.55 sec | 0.56 sec | 64% lower |
| CPU time per scan | 3.99 sec | 2.16 sec | 46% lower |
| Per-split `SOShipLine.PK.Find()` | Up to 1,814 | 0 | Eliminated |

This was the most significant optimization in the investigation.

## Second problem: Master Pack barcode resolution

The next bottleneck was located in the third-party Master Pack extension:

```text
WMS.PackModeLogicExt.DecorateScanState
```

Its barcode resolver loaded every shipment split and then queried `InventoryItem` and `INItemXRef` repeatedly until it found the item matching the scanned barcode.

The profiler recorded approximately 64 repeated `INItemXRef` queries per scan.

### Solution applied

A separate customization was created:

```text
PackModeBarcodeLookupOptimization.cs
```

It uses Acumatica's supported extension-of-extension structure to run after `WMS.PackModeLogicExt`. It replaces only the third-party barcode handler and leaves the remainder of the Master Pack functionality unchanged.

The optimized process is:

```text
Look up the scanned barcode
-> identify the inventory item
-> confirm the item exists on the shipment
```

The third-party code was not edited directly.

### Verified improvement

| Metric | Before barcode fix | After barcode fix | Improvement |
|---|---:|---:|---:|
| Average scan time | 2.63 sec | 2.43 sec | 7.7% faster |
| SQL calls per scan | ~273 | ~208 | 24% fewer |
| Barcode/XRef queries | 64 | 1 | Repeated calls eliminated |
| Cache/select operations | ~12,072 | ~10,228 | 15% fewer |

The profiler stack confirmed that the optimized handler is active:

```text
IStar.ScanPerformance.PackModeBarcodeLookupOptimization
    .FindShipmentItemByBarcode
```

## Repeated split consumers identified

The profiler still shows three executions of the full 1,808-row shipment-split query during a scan.

The three confirmed consumers are:

1. Item-presence validation:

```text
IsItemMissing
-> pickedForPack
-> GetSplits
```

2. Scanner state selection:

```text
ShipmentState.SetNextState
-> CanPack
-> pickedForPack
-> GetSplits
```

3. Command enablement:

```text
PackAllIntoBoxCommand.IsEnabled
-> CanPack
-> pickedForPack
-> GetSplits
```

The three evaluations are initiated by Acumatica's standard scan workflow to validate the item, select the next scan state, and determine command availability. The third-party `PackModeLogicExt` processes and sorts the results but does not create the consumers, and no additional customization was found creating extra calls. This behavior is generally insignificant for small shipments but becomes costly for shipment `0000787`. One evaluation also enables `PackAllIntoBoxCommand`, even though worksheet picking is not currently used.

## Third problem: Repeated shipment-split processing

The profiler showed three executions of the same 1,808-row `pickedForPack` split query during each scan. A request-scoped cache was added for:

```text
WMS.PackModeLogicExt.pickedForPack()
```

The customization stores one materialized result for repeated reads during the current HTTP request. The cache is keyed by the scan instance, shipment, mode, selected package, and removal state. It cannot persist into the next scan callback or be shared between requests.

Research confirmed that packing quantities are modified through:

```text
Confirm()
-> PackSplit()
-> SOShipLineSplitPackage.PackedQty
```

Therefore, the cache must be invalidated after confirmation or any packing mutation. Reusing a pre-update result after `PackSplit()` could cause stale quantities or incorrect command states.

### Solution applied

A separate customization was created:

```text
PackModePickedForPackRequestCache.cs
```

It extends the third-party `WMS.PackModeLogicExt` and reuses its materialized `pickedForPack()` result during the current request. A coordinated extension of `WMS.ConfirmStateLogicExt` clears the cache after every confirmation attempt so subsequent `CanPack` evaluations reload current quantities.

### Verified improvement

| Metric | Before cache | After cache | Improvement |
|---|---:|---:|---:|
| Average scan time | 2.43 sec | 2.08 sec | 14% faster |
| Average CPU time | 1.92 sec | 1.64 sec | 15% lower |
| Cache/select operations | 10,228 | 8,371 | 18% fewer |
| Select processing time | 1.56 sec | 1.26 sec | 19% lower |
| SQL time | 0.60 sec | 0.51 sec | 15% lower |
| Full `pickedForPack` split loads | 3 | 2 | One eliminated |

Two of the three comparable scans completed in approximately 1.86-1.90 seconds. The third took approximately 2.48 seconds because of higher SQL time, producing an overall average of 2.08 seconds. The profiler confirmed that `PackModePickedForPackRequestCacheExt.pickedForPack()` was active and reduced the rows returned by this repeated query from 5,424 to 3,616 per scan.

Two loads remain intentionally on quantity-changing requests:

```text
Initial state and command evaluation
-> load and cache current splits

Confirm and PackSplit
-> change package quantities
-> invalidate the cache

Post-confirmation state and command evaluation
-> reload current splits
```

This preserves correct packed quantities and command states. Forcing a quantity-changing request to use only one load could reuse pre-confirmation data after `PackSplit()`.

## LINQ fallback investigation

At this stage of the investigation, the profiler continued to report the following application-side LINQ fallback during scan and related grid callbacks:

```text
SQLQueryable<PXResult<SOShipLineSplit>>
-> Convert()
-> SelectMany
-> OfType
-> ToList
```

The source filename is not retained in the dynamically compiled `_CustomMethod` stack. Controlled customization isolation was therefore used to narrow the possible source.

### `GetSplits()` investigation

**Updated:** August 6, 2026 at 8:46 AM EDT

dnSpy identified a matching conversion path in standard Acumatica's `PickPackShip.GetSplits()` method:

```text
AsEnumerable<PXResult<SOShipLineSplit>>()
-> Cast<PXResult<SOShipLineSplit, SOShipLine, INLocation>>()
-> PXResult.Convert<TResult>()
-> LINQ fallback
```

The related call path is:

```text
WMS.PackModeLogicExt.pickedForPack()
-> PickPackShip.PackMode.Logic.pickedForPack()
-> PickPackShip.GetSplits()
-> PXResult.Convert<TResult>()
```

`PickPackShipGetSplitsOptimization.cs` was created experimentally to replace this conversion in Pack mode while preserving joins, assigned/unassigned handling, processed separation, and warehouse ordering. `ProfilerLog_9` confirmed that the override executed, but the identical LINQ warning remained once per scan and during profiler shutdown. SQL calls, select operations, and the two 1,808-row split loads were effectively unchanged. The experiment therefore did not address the active fallback and was removed from the deployed customization. Investigation then shifted to the `_CustomMethod -> PXView.InvokeDelegate -> PXGrid.PerformSelect` path and the relevant ASPX `DataMember`.

`ProfilerLog_9` averaged 2.44 seconds per scan versus 1.79 seconds in `ProfilerLog_8`. CPU was slightly lower, but SQL time increased from 0.38 to 1.01 seconds, indicating database timing variation rather than a demonstrated benefit from the override. One scan also encountered an `SOShipment` lock violation handled by the existing retry logic.

### Packed view optimization

**Updated:** August 6, 2026 at 9:17 AM EDT

The remaining fallback was traced to standard Acumatica's `PickPackShip.PackMode.Logic.packed()` delegate used by the Package Content grid. `PackModePackedViewOptimization.cs` replaced its full `PickedForPack` enumeration with a direct package-content join. `ProfilerLog_010` confirmed the override was active, returned only 1-3 package rows per scan, and eliminated all LINQ warnings and exceptions. Average scan time was 2.55 seconds versus 2.44 seconds in `ProfilerLog_9`; SQL remained unusually slow, so no timing improvement was demonstrated even though the targeted fallback was removed.

| Metric | Log 9 | Log 010 |
|---|---:|---:|
| Average scan time | 2.44 sec | 2.55 sec |
| CPU time | 1.48 sec | 1.57 sec |
| SQL time | 1.01 sec | 1.03 sec |
| SQL calls | 192 | 198 |
| LINQ fallback | Present | Eliminated |
| Exceptions | 1 | 0 |

The following customizations were deactivated during the `ProfilerLog_5`, `ProfilerLog_6`, and `ProfilerLog_7` isolation tests and have been ruled out:

- `ConsignmentOrdersBE`
- `SP_DBCostUpdates1`
- `ASCJewelryLibrary[v1.2.2]`
- `TRUECOMMERCE[25.193.0171][9.0.1.137]`
- `ASCIStarWMSCustomization[June19]`
- `AsgardLabels[Basic][25.201.0213][6.4.2.2]`
- `AsgardLabels[RomanSunStone][25.200.0248][1.0.0]`
- `OneUCCPerPackage`
- `OneLabelPerPackage`
- `UserRoleExtender`
- `AsgardButtonControl[06.09.2026]`
- `iStarCustomizations[25.201][05.18.2026]v1`
- `iStarCustomizations[25.201][July1]`
- `MasterPackISV[25.201[06.25.2026]`
- `MasterPackISV[25.201[07.01.2026]`
- `MasterPackISV[25.201[07.09.2026]`
- `MasterPackExtension[07.10.2026][1]`
- `MasterPackExtension[07.22.2026][1]`
- `CustomWMSManualPackTransfer[07.22.2026][1]`
- `POReceiptLineAdditionalColumn[06.19.2026][1]`
- `Velixo[25R2]`
- `SplitGIsAndReports[24.209.0013][April426]`
- `iStarShippingRestrictionsCustomizations[06.30.2026]`
- `MonthlyForecastReferenceTable[06.22.2026][1]`
- `MonthlyForecastReferenceTable[06.30.2026][1]`
- `iStarManufacturingCost[07.28.2026][1]`
- `FlexManufacturing25R201v260422`

The identical `SOShipLineSplit` LINQ fallback remained present during every measured scan after these customizations were deactivated. In `ProfilerLog_8`, deactivating `MasterPackExtension[07.22.2026][1]` removed some ancillary UI, event, note, item, customer, site, and package-related work, but it did not remove or change the fallback. The two safe full split loads also remained unchanged. These customizations were therefore ruled out as the source of the fallback later resolved by `PackModePackedViewOptimization.cs`.

## SQL priorities identified after `ProfilerLog_010`

**Updated:** August 6, 2026 at 11:23 AM EDT

| Rank | Query | Total SQL time | Executions | Meaning |
|---:|---|---:|---:|---|
| 1 | `FF246783` | 661 ms | 8 | Full assigned shipment-split loads |
| **2 - Completed** | **`6519B47D`** | **439 ms** | **107** | **Per-split `GetQtyThreshold()` lookups** |
| **3 - Completed** | **`B5446270`** | **356 ms** | **11** | **Repeated full `SOShipLine` loads** |
| 4 | `E93AD83C` | 150 ms | 174 | Repeated package-split lookups during packing |
| 5 | `11914AC2` | 100 ms | 6 | Master Pack actual package/style totals |

`FF246783` originally loaded approximately 1,808 shipment splits three times per scan. The request-scoped `pickedForPack()` cache reduced this to two loads: one before `PackSplit()` changes package quantities and one afterward to refresh `CanPack` and command state with current data. The cache is explicitly invalidated after confirmation to prevent stale packed quantities. Reducing these two loads to one would require invasive synchronization of Acumatica's cached split results after every packing mutation and could produce incorrect quantities or command states. The first-ranked query is therefore considered optimized as far as practical without materially changing standard behavior.

`6519B47D` was selected because its 107 executions exposed an N+1 pattern in `SOShipmentEntry.GetQtyThreshold(SOShipLineSplit)`.

### `GetQtyThreshold()` finding and plan

**Updated:** August 6, 2026 at 9:44 AM EDT

dnSpy confirmed that `GetQtyThreshold()` queries the `SOLine` associated with each shipment split, reads `CompleteQtyMax`, divides it by 100, and defaults to `1.0`. `GetSplitsToPack()` invokes this calculation for every eligible split, producing 6, 45, and 56 separate queries across the three measured scans.

`PackModeQtyThresholdOptimization.cs` replaced these individual lookups with one request-scoped batch query per shipment and scanned inventory. It builds a `SOShipLine.LineNbr -> CompleteQtyMax / 100` dictionary and reuses it during the scan; missing entries call the original method. This preserves standard `TargetQty()`, `HasPick`, wave/batch picking, Master Pack ordering, and behavior outside Pick/Pack/Ship.

### `GetQtyThreshold()` verified result

**Updated:** August 6, 2026 at 11:23 AM EDT

`ProfilerLog_011` confirmed that `6519B47D` was eliminated. Its 107 individual calls and 439 ms of SQL time were replaced by three batched `B730E2F8` queries totaling 30 ms—one query per scan and a 93% reduction in threshold SQL time.

| Metric | Log 010 | Log 011 | Result |
|---|---:|---:|---:|
| Threshold queries | 107 | 3 | 97% fewer |
| Threshold SQL time | 439 ms | 30 ms | 93% lower |
| Average scan time | 2.55 sec | 1.72 sec | 33% faster |
| Average CPU time | 1.57 sec | 1.37 sec | 13% lower |
| Average SQL calls | 198 | 161 | 19% fewer |
| LINQ fallback | Eliminated | Eliminated | Remains resolved |
| Exceptions | 0 | 0 | No errors |

The batch queries returned 6, 45, and 56 rows, matching the records previously retrieved separately. At that point, the next visible SQL candidate was `B5446270`, which repeatedly loaded all `SOShipLine` records.

### `B5446270` investigation target

**Updated:** August 6, 2026 at 11:32 AM EDT

The investigation next focused on `B5446270`, the repeated full `SOShipLine` load. In `ProfilerLog_011`, it executed nine times across three scans, returned 16,272 rows, and consumed approximately 213 ms of scan SQL time. It was selected because `FF246783` had already been reduced to the two state-correct loads required around packing mutations, while `E93AD83C` consumed only approximately 31 ms despite its high execution count.

### `SOShipment_RowSelected` verified result

**Updated:** August 6, 2026 at 1:18 PM EDT

`PickPackShipShipmentRowSelectedOptimization.cs` preserved the standard Pick/Pack/Ship `SOShipment_RowSelected` behavior while replacing the full `Transactions.Select().Count` load with a limited existence query. `ProfilerLog_012` confirmed that `B5446270` was eliminated without exceptions.

| Metric | Log 011 | Log 012 | Change |
|---|---:|---:|---:|
| Average scan time | 1.72 sec | 1.55 sec | 10% faster |
| Average CPU time | 1.37 sec | 1.29 sec | 6% lower |
| Rows returned per scan | 11,125 | 5,705 | 49% fewer |
| Exceptions | 0 | 0 | No regression |
| `B5446270` executions | 9 | 0 | Eliminated |
| Target-related rows | 16,272 | 9 | Nearly eliminated |
| Target-related SQL time | 213 ms | 50 ms | 76% lower |

The replacement stack was confirmed at `PickPackShipShipmentRowSelectedOptimization.SOShipment_RowSelected`. The existence check was subsequently changed from `.SelectWindowed(...).Count` to `.SelectWindowed(...).TopFirst == null` for clarity.

### `TopFirst` verification

**Updated:** August 6, 2026 at 1:53 PM EDT

`ProfilerLog_013` confirmed that `B5446270` remains eliminated and no exceptions occurred. Average scan time improved from 1.55 to 1.37 seconds, although the targeted replacement pattern was unchanged: `6F2A12FA` still executed six times across three scans, returned nine rows, and consumed approximately 48 ms. The `TOP (2)` and `TOP (1)` statements therefore represent two separate `SOShipment_RowSelected` evaluations per scan, not duplicate queries caused by `.Count`. The additional timing improvement should be treated as capture variation rather than an effect proven to result from `TopFirst`.

| Metric | Log 012 | Log 013 | Change |
|---|---:|---:|---:|
| Average scan time | 1.55 sec | 1.37 sec | 11% faster |
| Average CPU time | 1.29 sec | 1.16 sec | 10% lower |
| Average SQL calls | 163.7 | 159.0 | 3% fewer |
| Average SQL time | 380 ms | 331 ms | 13% lower |
| `B5446270` executions | 0 | 0 | Remains eliminated |
| Exceptions | 0 | 0 | No regression |

## Functional regression testing

**Added:** August 6, 2026 at 1:53 PM EDT

**Status:** In progress

Profiler results confirm performance improvements but do not prove that every warehouse workflow remains functionally correct. The following scenarios must be tested before production deployment:

- Scan a valid item and confirm that its packed quantity increases correctly.
- Scan the same item repeatedly and verify each quantity increment.
- Scan an item that is not on the shipment and confirm that it is rejected.
- Scan an invalid barcode and confirm that the expected validation message appears.
- Pack items using alternate barcodes or item cross-references.
- Pack items into multiple boxes and verify the contents of each box.
- Change the selected box and confirm subsequent scans use the correct package.
- Confirm a package and verify its status and quantities.
- Remove or unpack an item and verify that quantities decrease correctly.
- Test lot-controlled and serial-controlled inventory.
- Scan the final required item and verify that the shipment becomes fully packed.
- Confirm that `CanPack`, package confirmation, and related commands enable or disable immediately and correctly.
- Refresh or reopen the shipment and verify that displayed quantities match persisted quantities.
- Test both shipment `0000787` and a normal-sized shipment.
- Test standard shipments, transfer shipments, and any special picking modes used by the business.
- Confirm that all expected Pick, Pack, and Ship labels still print with the correct shipment, package, item, quantity, and printer information.
- Test label reprinting and verify that optimizations do not produce missing, duplicate, or stale labels.
- Have multiple users scan concurrently in Pick, Pack, and Ship. Test different shipments first, then test the same shipment only if that workflow is supported. Confirm correct locking, quantities, package assignments, labels, command states, and user-facing conflict messages.

After every quantity-changing test, verify that the displayed packed quantity, package-content quantity, and persisted database quantity agree. Any stale quantity, incorrect command state, duplicate label, or unexplained concurrency error must be resolved before production deployment.

### Deferred label-context defect

**Added:** August 6, 2026 at 4:28 PM EDT

Label testing identified an existing package-context defect on shipment `0000787`. When package line 30 was selected, the package contained the expected `UsrTCUCC128` value `00007168380000001132`, but the printed label used `00007168380000000845`, which belongs to the first package in the shipment.

Profiler messages confirmed that the intended package reached the printing entry point:

```text
[PKG-CHECK] LineNbr=30, UsrTCUCC128=00007168380000001132
[PRINT] Using selected package: 30
[QUEUE] QueuePrintForPackage called: Shipment=0000787, Package=30
```

The same incorrect label behavior was reproduced after disabling `IStarScanPerformance[08042026][1]`. This is strong evidence that the performance customization does not cause the defect. The current suspected failure occurs later, when Advanced Labels resolves `Packages.UsrTCUCC128` and appears to lose the selected-package context or fall back to the first package.

The all-customizations profiler capture also recorded warnings for missing `RHLineToggleLabelPrint` and `RHPackageToggleLabelPrint` wrappers under `ALLabelFolder`. These warnings are not exceptions but may be relevant to the label customization chain.

The label defect will not be investigated further during the current performance-testing phase. A later investigation should trace:

```text
QueuePrintForPackage
-> queued label parameters
-> Advanced Labels data-source creation
-> Packages.Current and package-line filtering
-> Packages.UsrTCUCC128 evaluation
```

The future fix must ensure that the selected `PackageLineNbr` remains explicit through queueing and rendering. Until resolved, label output must be checked carefully during regression testing.

## Current performance boundary

**Updated:** August 6, 2026 at 2:02 PM EDT

`ProfilerLog_013` averaged 1.37 seconds of server time, 1.16 seconds of application-server CPU, 331 ms of SQL time, approximately 8,433 cache/select operations, and approximately 779 ms of select-processing time per scan, with no reported wait time or exceptions. These counters overlap and must not be added together, but they show that the next investigation should target application CPU rather than general SQL tuning.

The remaining `E93AD83C` pattern executes 58 times per scan because 29 distinct packages are each checked twice. It consumes only approximately 9.54 ms of SQL time per scan and returns very few package-content records. Package-state caching is therefore a low-priority micro-optimization unless CPU profiling proves that its non-SQL framework processing is material.

## Deployed performance customization files

- `PickPackShipTranQtyPerformanceExt.cs`
- `PackModeBarcodeLookupOptimization.cs`
- `PackModePickedForPackRequestCache.cs`
- `PackModePackedViewOptimization.cs`
- `PackModeQtyThresholdOptimization.cs`
- `PickPackShipShipmentRowSelectedOptimization.cs`

`PickPackShipGetSplitsOptimization.cs` was an unsuccessful diagnostic experiment and is not part of the deployed solution.

## Recommended next steps

**Updated:** August 6, 2026 at 4:28 PM EDT

1. Continue functional regression testing with all customizations enabled.
2. Complete the planned concurrent multi-user Pick, Pack, and Ship test.
3. Record label results during testing, but defer correction of the selected-package UCC defect to a later work item.
4. After functional testing, capture one controlled scan with dotTrace.
5. Identify the methods consuming the remaining application-server CPU.
6. If full split processing dominates, evaluate only state-safe ways to reduce its in-memory work.
7. If `IsPackageEmpty` appears materially in the CPU call tree, design a bulk cache based on `SOShipLineSplitPackage` that preserves dirty cache records, parent-box behavior, and mutation invalidation.
8. Do not convert packing queries to read-only or disable database event logging without measurements proving that the change is both safe and worthwhile.

No additional performance customization should be implemented until CPU profiling identifies a specific method with meaningful optimization potential.

## Overall result

The completed optimizations reduced average scan time from approximately **5.19 seconds to 1.37 seconds** in the latest capture, an improvement of approximately **74%**. SQL calls fell from approximately **1,850 to 159 per scan**, a reduction of approximately **91%**.

The Advanced Labels lookup, repeated Master Pack barcode lookups, Package Content LINQ fallback, per-split `GetQtyThreshold()` queries, and repeated full `SOShipLine` query `B5446270` have been eliminated or consolidated. Request-scoped reuse also removed one of the three repeated `pickedForPack` split loads while preserving the required post-mutation reload.
