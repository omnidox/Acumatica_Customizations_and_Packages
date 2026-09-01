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

### Conditional-reuse experiment and final cache decision

**Final decision:** August 10, 2026 at 3:06 PM EDT

Version 2 diagnostics tested whether `UpdateShipmentLine()` updated the same assigned-split objects held by the request cache. Four representative ordinary Pack-mode scans each reported 1,808 assigned splits, 1,808 identical object references, zero different or missing references, no split insertions or deletions, no packed-quantity mismatches, and `SafeCandidate=True`. This supported a guarded Version 3 experiment that reused the first materialized collection only when every diagnostic condition remained valid and otherwise fell back to the standard invalidation behavior.

After restoring a clean snapshot, `ProfilerLog_017` confirmed that Version 3 reduced full `FF246783` loads from two to one per scan and lowered rows and select operations by approximately 31% and 20%, respectively. However, average server time remained essentially unchanged at 1.40 seconds versus approximately 1.41 seconds before Version 3; CPU and SQL time did not improve, and the change depended on undocumented object-reference and cache-membership behavior requiring substantially broader regression coverage.

The production design will therefore remain **Version 1**, which unconditionally invalidates the request cache after confirmation and performs a fresh post-mutation load. Versions 2 and 3 are retained as diagnostic research only. Version 1 provides the safer state-consistency boundary, has already received broader functional testing, and delivers virtually the same user-visible performance. This is the final active scan optimization because further changes now offer diminishing returns while increasing correctness and maintenance risk.

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

`FF246783` originally loaded approximately 1,808 shipment splits three times per scan. Production Version 1 reduces this to two loads: one before `PackSplit()` and one after explicit invalidation to refresh `CanPack` and command state. Version 3 proved that a guarded one-load path was structurally possible for the tested ordinary scans, but it produced no meaningful wall-clock improvement and carried a larger state-correctness and regression burden. Version 1 is therefore the final deployed design.

### Concluded `FF246783` and `CanPack` research

**Added:** August 10, 2026 at 9:24 AM EDT

**Concluded:** August 10, 2026 at 3:06 PM EDT

dnSpy confirmed that `PickPackShip.PackMode.Logic.CanPack` is a virtual property and can be intercepted through Acumatica's supported getter override pattern:

```csharp
[PXOverride]
public bool get_CanPack(Func<bool> base_CanPack)
```

The standard implementation materializes `PickedForPack` and performs a simple existence test: when the shipment has not been picked, it checks whether any split has `PackedQty < Qty`; otherwise, it checks `PackedQty < PickedQty`. Standard `PaperlessOnlyPacking` also overrides the getter and, in Paperless Pack Only mode, excludes splits marked `RelatedPickListSplitForceCompleted`. Any optimization must preserve that override by calling the existing delegate for special or unsupported modes.

The `SOShipLineSplitPackage.PackedQty` and `BasePackedQty` fields are database-backed but do not contain a `PXFormula` or `SumCalc` that automatically updates the parent split. A database-only `TOP (1)` query is therefore not automatically state-correct during an active packing callback.

More importantly, `SOShipmentEntry.PackageDetail.UpdateShipmentLine()` was found to explicitly adjust the cached `SOShipLineSplit.PackedQty` after package content changes, then recalculate the parent `SOShipLine.PackedQty` and `SOShipment.PackedQty`. This means Acumatica may already synchronize the exact split objects retained by the first `pickedForPack()` load. If reference identity and collection membership are preserved, `CanPack` may be able to evaluate the already-loaded, newly updated objects and avoid the second full query without manually recreating Acumatica's quantity calculations.

Version 2 diagnostics subsequently confirmed identical reference identity for all 1,808 assigned splits across four representative ordinary scans. Version 3 then conditionally reused those objects only when the shipment, mode, membership, reference identity, and packed quantities passed a fail-closed validation. `ProfilerLog_016` and the post-snapshot `ProfilerLog_017` confirmed one `FF246783` load per scan, no exceptions, approximately 31% fewer returned rows, and approximately 20% fewer select operations.

Despite this structural reduction, Version 3 did not materially improve scan latency: post-snapshot scans averaged approximately 1.40 seconds, essentially matching the approximately 1.41-second pre-Version-3 baseline. CPU and SQL time also varied without a demonstrated improvement. Because unassigned, inserted, deleted, reassigned, and specialized-mode behavior would require extensive additional validation, the conditional design is not justified by its measured benefit. Version 1's explicit invalidation remains the production behavior.

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

## dotTrace CPU profiling results

**Added:** August 7, 2026 at 11:32 AM EDT

Sampling captures were attached to the `DefaultAppPool` IIS worker and scoped to `BarcodeDrivenStateMachine.ProcessSingleBarcode`. The first capture after an accidental worker termination took 7.324 seconds and included approximately 2.79 seconds in schema/query construction under `DbSchemaCache.TryGetLockedTableHeader`. That delay disappeared after warming the restarted application and is treated as cold-start initialization rather than a steady-state optimization target.

Three separate warmed scans produced the following results:

| Area | Scan 1 | Scan 2 | Scan 3 | Average |
|---|---:|---:|---:|---:|
| `ProcessSingleBarcode` | 834 ms | 388 ms | 1,026 ms | 749 ms |
| `CompleteFlow` | 425 ms | 175 ms | 470 ms | 357 ms |
| Confirmation chain | 342 ms | 176 ms | 416 ms | 311 ms |
| `PackSplit` | 252 ms | 144 ms | 337 ms | 244 ms |
| `PackAllIntoBoxCommand -> CanPack -> GetSplits` | 267 ms | 133 ms | 327 ms | 242 ms |
| `GetSplitsToPack` | 50 ms | 24 ms | 40 ms | 38 ms |

The warmed captures confirm that the optimized barcode resolver is no longer material: it required only 15-16 ms in scans two and three. `GetSplitsToPack` averaged 38 ms and is also not a worthwhile target. `PackSplit` averaged 244 ms, but it performs required package, shipment-line, formula, and parent-quantity maintenance and would be comparatively invasive to alter.

The clearest repeatable avoidable path was standard command-state evaluation:

```text
ActualizeCommandActions
-> WorksheetPicking.PackAllIntoBoxCommand.get_IsEnabled
-> PickPackShip.PackMode.Logic.get_CanPack
-> pickedForPack
-> GetSplits
```

This path consumed 31.9-34.2% of every warmed scan and averaged 242 ms. It initially made `PackAllIntoBoxCommand` the leading suspected avoidable consumer. The feature-isolation and Timeline results below supersede that initial interpretation.

### Advanced Picking isolation and full-request Timeline

**Updated:** August 7, 2026 at 2:54 PM EDT

Advanced Picking was enabled while Paperless Picking was disabled. Decompiled code confirmed that `WorksheetPicking.IsActive()` activates for Wave/Batch Picking or Paperless Picking, appends `PackAllIntoBoxCommand`, and evaluates `IsEnabled -> CanPack -> pickedForPack -> GetSplits` even though its action is hidden.

Disabling Advanced Picking removed that command stack, but did not reduce full assigned-split query `FF246783`: it remained at six executions across three scans and returned 10,848 rows. Total SQL time changed only from 462.7 ms to 450.4 ms. Clean Request Profiler scans, excluding one unrelated `SOShipment` lock-conflict request, changed as follows:

| Metric | Enabled | Disabled | Change |
|---|---:|---:|---:|
| Average server time | 1.437 sec | 1.413 sec | 1.7% faster |
| Average CPU time | 1.305 sec | 1.195 sec | 8.4% lower |
| Select-processing time | 843 ms | 749 ms | 11.2% lower |
| SQL time | 312 ms | 309 ms | Essentially unchanged |
| SQL calls | 162.5 | 163.5 | Essentially unchanged |

Three warmed Sampling captures with Advanced Picking disabled averaged 743 ms in `ProcessSingleBarcode`, compared with 749 ms while enabled. The command disappeared, but `SetNextState -> CanPack -> GetSplits` averaged 245 ms, effectively replacing the previously attributed 242 ms command path. Disabling the unused feature therefore produced no material steady-state barcode-processing improvement. A separate 7.316-second cold capture containing approximately 2.531 seconds of schema-cache initialization was excluded.

**Timeline captured:** August 7, 2026 at 2:16 PM EDT  
**Timeline analyzed:** August 7, 2026 at 2:36 PM EDT

A Timeline capture filtered to the single incoming `POST` request accounted for the full callback rather than only `ProcessSingleBarcode`:

| Area | Time | Share |
|---|---:|---:|
| Incoming HTTP request | 2,609 ms | 100% |
| `PXPage.ProcessRequest` | 2,573 ms | 98.6% |
| Running | 2,317 ms | 88.8% |
| Waiting | 292 ms | 11.2% |
| `ProcessSingleBarcode` | 1,642 ms | 62.9% |
| Work outside `ProcessSingleBarcode` | approximately 967 ms | 37.1% |
| SQL Queries event time | 75 ms | 2.9% |

Timeline and Sampling instrumentation differ, so their absolute method times should not be compared directly. Within the Timeline capture, the two remaining full split loads are fully accounted for:

| Phase | Path | Time |
|---|---|---:|
| Before barcode processing | `OnPreLoad -> PXGrid.LoadPostData -> SyncCurrentPosition -> SynchronizeGrid -> ExecuteSelect -> pickedForPack -> GetSplits` | approximately 484 ms |
| After packing mutation | `CompleteFlow -> ShipmentState.SetNextState -> CanPack -> pickedForPack -> GetSplits` | approximately 619 ms |

Other measured barcode work included `CompleteFlow` at 836 ms, `PackSplit` at 483 ms, `GetSplitsToPack` at 56 ms, and barcode resolution at 22 ms. `SOShipment_RowSelected` required only about 4.5 ms. The request path ran through `GetCallbackResult -> RenderClientData -> CollectDataControls -> DataBind -> ExecuteSelect -> scan`. SQL is no longer the dominant cost; most remaining time is framework processing, split materialization, formula/event handling, grid synchronization, and callback rendering.

One Sampling attempt caused a native `w3wp.exe` access violation in `ntdll.dll` (`0xc0000005`) while dotTrace was attached. This is recorded as a profiling incident, not an Acumatica customization failure or an Advanced Picking result. IIS recovered and the later Timeline capture completed successfully.

## Current performance boundary

**Updated:** August 10, 2026 at 3:06 PM EDT

`ProfilerLog_013` averaged 1.37 seconds of server time, 1.16 seconds of application-server CPU, 331 ms of SQL time, approximately 8,433 cache/select operations, and approximately 779 ms of select-processing time per scan, with no reported wait time or exceptions. These counters overlap and must not be added together, but they show that the next investigation should target application CPU rather than general SQL tuning.

The full Timeline request confirms that application processing, rather than SQL, is the principal remaining cost. ASPX review identified the pre-scan caller as `gridPacked`, whose `PickedForPack` level uses `SyncPosition="true"`. The barcode field performs a committed `Scan` callback, which causes Acumatica to synchronize the grid's current row and execute `PickedForPack` before barcode processing.

`SyncPosition` does not itself guarantee database freshness; saving, cache invalidation, and query execution provide that. Its role is to keep the browser-selected grid row synchronized with Acumatica's server-side current record. The Pack grid contains the row-dependent `ReopenLineQty` command with `DependOnGrid="gridPacked"`, and other current-row behavior may also depend on this synchronization. The business requires `SyncPosition="true"`, but later investigation established that its approximately 484 ms pre-scan full-result load may be replaceable with an exact one-row lookup without disabling synchronization. Version 1 continues to retain the post-mutation `SetNextState -> CanPack` refresh because Version 3's conditional reuse did not improve user-visible scan time enough to justify its additional risk.

The remaining `E93AD83C` pattern executes 58 times per scan because 29 distinct packages are each checked twice. It consumes only approximately 9.54 ms of SQL time per scan and returns very few package-content records. Package-state caching remains a low-priority micro-optimization unless focused profiling proves otherwise.

### Renewed grid-synchronization investigation

**Updated:** August 28, 2026 at 10:09 AM EDT

The gap from the previous August 10 entry reflects the earlier safe closeout and a later decision to resume research into one final narrowly scoped opportunity identified through full-request dotTrace analysis. No production optimization is implied by this new investigation.

A Timeline capture taken after Advanced Picking was disabled confirmed that `PackAllIntoBoxCommand` was removed, but the request still contained two full `pickedForPack -> GetSplits` evaluations: approximately 476 ms during pre-scan grid synchronization and approximately 491 ms during the required post-mutation `CanPack` refresh. The complete request measured 3,009 ms in this individual capture, so disabling Advanced Picking did not demonstrate a reliable elapsed-time improvement and does not remove the underlying two-load pattern.

Decompiled framework code now explains the pre-scan load more precisely. `PXBaseDataSource.SynchronizeGrid()` passes the selected grid key values to `ExecuteSelect()` with `startRow = 0` and `maximumRows = 1`. Acumatica therefore asks the `PickedForPack` view for one selected row. However, `PXView.Select()` invokes the custom `pickedForPack()` delegate before applying `SearchResult()` and the one-row limit. The existing delegate materializes its complete result, causing all approximately 1,808 splits to be loaded, joined, transformed, and ordered before Acumatica narrows the result to the requested row.

In practical terms, the grid asks Acumatica to restore one selected row, but the custom view first rebuilds the entire shipment-split list and only then searches that list for the requested row. This is similar to reading and sorting an entire filing cabinet before retrieving one known document. The proposed optimization would recognize this narrowly defined one-row request and retrieve the requested split directly, while retaining the existing full-list process for every other request.

The pre-scan synchronization load and post-scan `CanPack` load cannot simply share the same result. Grid synchronization occurs before the scan changes packed quantities, while `CanPack` executes afterward and must evaluate the updated state. Reusing the earlier result could expose stale quantities or incorrect command states. The investigation is therefore limited to reducing the size of the pre-scan selected-row lookup rather than reusing stale data across the packing mutation.

The remaining candidate was a guarded selected-row fast path inside `pickedForPack()`. It would be considered only when runtime diagnostics proved that the synchronization request supplied a complete, unambiguous split key, requested one row, and used no unsupported filters. In every other case, execution would fall back to the existing Version 1 cache/base behavior. Version 4 therefore captured `PXView.StartRow`, `PXView.MaximumRows`, `PXView.Searches`, `PXView.SortColumns`, `PXView.Descendings`, and `PXView.Filters` for synchronization and full-result calls.

### Version 5 one-row comparison validation

**Validated:** August 31, 2026 at 9:46 AM EDT  
**Recorded:** August 31, 2026 at 10:14 AM EDT

Version 4 traces confirmed that grid synchronization consistently used `StartRow=0`, `MaximumRows=1`, no filters, and exact `ShipmentNbr`, `LineNbr`, and `SplitLineNbr` search values. Full-result business consumers continued to use `MaximumRows=0` or `MaximumRows=201` and therefore remained outside the proposed fast path. The traces also showed that `PXView.Searches`, rather than `PXCache.Current`, identifies the row Acumatica is restoring; during item scans the current split changed while the synchronization search correctly remained on the browser-selected grid row.

Version 5 executed the proposed exact one-row joined query and the standard 1,808-row path, compared the relevant `SOShipLineSplit`, `SOShipLine`, and `INLocation` values, and continued returning the standard result. Six comparisons on package line 32 and six comparisons after changing to package line 33 all produced:

```text
Decision=Match
TargetRows=1
StandardMatches=1
StandardRows=1808
Equivalent=True
FastPathEnabled=False
```

The successful tests covered synchronization targets `1337/1338` and `1371/1372`, different scanned items, two cartons, initial database materialization, and subsequent request-cache reads. There were zero mismatches, ambiguous results, or unexpected fallbacks. These results establish functional equivalence for the tested ordinary assigned-split workflow; they do not by themselves prove every lot/serial, unassigned, transfer, removal, or specialized picking scenario.

### Version 6 one-row implementation test

**Prepared:** August 31, 2026 at 10:14 AM EDT

Version 6 enables the guarded fast path for performance testing. An eligible `MaximumRows=1` synchronization request now performs the exact assigned-split query and returns its one-row `PXResult<SOShipLineSplit, SOShipLine, INLocation>` before invoking the standard delegate. It does not execute the full 1,808-row query, perform Version 5 comparison work, or store the one-row result in the full-result request cache. Requests with incomplete keys, filters, nonzero start rows, unsupported unassigned state, missing rows, ambiguous rows, or any non-one-row shape continue through the Version 1 cache/base path. Full-result `CanPack` and grid consumers are unchanged.

Verbose Version 4 view diagnostics were disabled in Version 6 to avoid distorting performance measurements. A lightweight `[PFP-FASTPATH] Decision=Applied` or `Decision=Fallback` trace remained for controlled verification. Version 6 was prepared as a test candidate rather than a production decision; the following section records its measured outcome.

### Version 6 measured result and displaced-load finding

**Profiler captured:** August 31, 2026 at 10:29 AM EDT  
**Analyzed:** August 31, 2026 at 10:40 AM EDT

`ProfilerLog_018` confirmed that the fast path itself worked. Exact query `B79C6577` executed once per scan, returned one row, and averaged approximately 0.6 ms. Its stack originated from `PXBaseDataSource.SynchronizeGrid() -> pickedForPack() -> SelectTargetedRows()`, proving that the formerly full grid-synchronization lookup was successfully replaced without disabling `SyncPosition`.

However, the expected elapsed-time improvement did not occur:

| Metric | Version 1 baseline (`ProfilerLog_013`) | Version 6 (`ProfilerLog_018`) | Result |
|---|---:|---:|---:|
| Average server time | 1,371.7 ms | 1,535.2 ms | 11.9% slower |
| Average server CPU | 1,156.2 ms | 1,322.9 ms | 14.4% higher |
| Average SQL time | 330.8 ms | 353.4 ms | 6.8% higher |
| SQL calls per scan | 159.0 | 192.0 | 20.8% higher |
| SQL rows per scan | 5,702 | 5,775 | 1.3% higher |
| Select time per scan | 778.6 ms | 862.7 ms | 10.8% higher |
| Exceptions | 0 | 0 | No observed regression |

The three Version 6 scans measured 1,487.1, 1,551.0, and 1,567.4 ms, averaging 1,535.2 ms. These measurements do not establish a performance benefit. Comparison with other all-customization captures also remains within normal run-to-run variation rather than demonstrating a repeatable improvement.

SQL stacks explain the outcome. Full assigned-split query `FF246783` still executed twice per scan and returned 1,808 rows each time. The first call now originated from barcode item-presence validation:

```text
ProcessSingleBarcode
-> InjectItemPresenceValidation
-> IsItemMissing
-> pickedForPack
-> GetSplits
-> FF246783 (1,808 rows)
```

The second call remained the required post-mutation refresh:

```text
PackSplit changes quantities
-> CompleteFlow
-> ShipmentState.SetNextState
-> CanPack
-> pickedForPack
-> GetSplits
-> FF246783 (1,808 rows)
```

Under Version 1, synchronization materializes the full pre-mutation result and stores it in the request cache; `IsItemMissing()` subsequently reuses that collection. Version 6 correctly reduces synchronization to one row and deliberately avoids storing that partial result as the complete cache. `IsItemMissing()` therefore encounters no full collection and performs its own 1,808-row load. The fast path displaced the first full load rather than eliminating it:

| Implementation | Synchronization | Item-presence validation | Post-pack `CanPack` |
|---|---:|---:|---:|
| Version 1 | 1,808 rows, then cached | Reuses full cache | Reloads 1,808 current rows |
| Version 6 | One targeted row | Loads 1,808 rows | Reloads 1,808 current rows |

Version 6 therefore should not be retained as a standalone performance change. The next narrowly scoped research question is whether `IsItemMissing()` can safely use a targeted existence query that preserves inventory, alternate-ID, subitem, location, lot/serial, assigned/unassigned, and specialized picking behavior. Only if that full pre-mutation consumer can also be removed would the one-row synchronization path be expected to reduce the scan's full-load count. Until then, Version 1 remains the safer and at least equally fast implementation.

### `IsItemMissing()` investigation path

**Investigation path recorded:** August 31, 2026 at 11:05 AM EDT

dnSpy review confirmed that standard `PickPackShip.IsItemMissing()` answers a yes-or-no question by enumerating the complete supplied split view:

```text
splitView.SelectMain()
-> materialize every eligible shipment split
-> compare each split's InventoryID with the resolved InventoryItem
-> report whether the item is absent
```

For shipment `0000787`, this validation is responsible for the first remaining `FF246783` execution and processes approximately 1,808 assigned splits merely to determine whether at least one eligible matching item exists. This makes a targeted existence check a plausible optimization, but the method accepts a `PXSelectBase<SOShipLineSplit>` supplied by its caller. A replacement cannot safely assume that `ShipmentNbr + InventoryID` alone reproduces that view's complete meaning.

The next dnSpy investigation will therefore establish the effective contract before any implementation is written:

1. Use **Analyze -> Used By** on `PickPackShip.IsItemMissing()` and inspect the complete `InjectItemPresenceValidation` implementation, including the exact `splitView` passed at each call site.
2. Use **Analyze -> Overridden By** on `IsItemMissing()` to identify Acumatica feature extensions or installed customizations that alter item-presence behavior.
3. Inspect **Used By** and **Overridden By** for `InjectItemPresenceValidation` to determine which scan modes and validation chains invoke it.
4. Confirm that the validation executes strictly before `PackSplit()` and therefore does not require post-mutation packed quantities.
5. Search for other Acumatica implementations of `IsItemMissing()` or equivalent targeted-presence checks that provide a framework-supported query pattern.
6. Document all eligibility rules represented by the supplied view, including assigned and transformed unassigned splits, valid `SOShipLine` and `INLocation` joins, alternate-ID resolution, subitem, location, lot/serial, removal mode, cache state, and specialized picking modes.
7. Only after those rules are known, build a comparison-only diagnostic that evaluates the proposed targeted result alongside the standard result across representative workflows. The standard result must remain authoritative during this phase.

The desired safe sequence is:

```text
Grid synchronization -> one-row selected-split query
IsItemMissing         -> targeted eligible-item existence query
PackSplit             -> standard quantity and cache mutation
CanPack               -> one required full post-mutation refresh
```

If the caller and override analysis proves that a targeted query cannot reproduce the supplied view and active cache semantics, this candidate will be rejected rather than broadening it into an invasive replacement. No production change has yet been approved or implemented for `IsItemMissing()`.

### Rolling shipment-line cache proposal for `8659BCA6`

**Research proposal recorded:** August 31, 2026 at 3:37 PM EDT

The current Scan 12 timeline exposed three major 1,808-row executions: the first `FF246783` pre-pack split load, `8659BCA6` during `PackSplit`, and the second `FF246783` post-pack refresh. Query stack analysis established that `8659BCA6` is triggered when insertion of an `SOShipLineSplitPackage` record invokes `SOShipmentEntry.PackageDetail.UpdateParentShipmentLine() -> UpdateShipmentLine() -> UpdatePickPackInfoOf() -> PXParentAttribute.SelectChildren()`. It retrieves all 1,808 `SOShipLine` records into the Acumatica application server while Acumatica updates parent shipment packing information. In Scan 12 it executed once, consumed 42.6629 ms of SQL time, and represented approximately 13.2% of measured scan SQL time before accounting for DAC creation, cache merging, formulas, and event processing.

A possible future architecture is a rolling shipment-line cache initialized when the carton is scanned, provided that carton processing can be proven to retrieve the complete shipment's `SOShipLine` population rather than only package-specific contents. The proposed flow would be:

```text
Carton scan
-> load and validate the complete SOShipLine collection once
-> initialize shipment-line cache

Item scan
-> one-row grid synchronization
-> targeted IsItemMissing existence check
-> PackSplit
-> use validated cached SOShipLine records instead of executing 8659BCA6
-> retain the required second FF246783 post-mutation refresh
-> use its updated joined SOShipLine records to refresh the cache for the next scan
```

This would not populate or accelerate `8659BCA6`; it would bypass that database query when a valid cache is available and immediately fall back to standard behavior otherwise. If safe, the carton scan would pay the initial full-line cost once and the existing post-pack `FF246783` result would maintain a rolling snapshot for subsequent item scans. Combined with a targeted `IsItemMissing()` check, the theoretical steady-state result would reduce the current three major 1,808-row executions to the one post-mutation refresh that remains necessary for current quantities and command state.

The proposal has significant correctness requirements and is not approved for implementation. Cached records from the preceding request are pre-mutation state; the current `PackSplit` adjustment must update the same live `PXCache` instances or be applied explicitly before parent totals are calculated. The design must also validate complete line coverage, expected DAC identity and cache context, company, shipment, shipment type, row versions, and mutation state. It must invalidate or fall back on shipment/carton changes, pack, unpack, deletion, confirmation, exceptions, missing or duplicate keys, and any external or concurrent update. A user/session-local memory cache is also insufficient by itself in a multi-user or multi-IIS-node SaaS deployment because another user or node can change the shipment without updating the local snapshot.

Before any diagnostic implementation, the following evidence is required:

1. Confirm whether carton scanning actually loads all 1,808 `SOShipLine` records and identify the responsible query and method.
2. Confirm every field and aggregation consumed by `UpdatePickPackInfoOf()` and whether the calculation can accept an existing collection without changing event behavior.
3. Compare object identity and keys between the proposed cached records and the current graph's `PXCache<SOShipLine>` records.
4. Prove that the second `FF246783` contains the completed post-`PackSplit` values required to refresh the next-scan cache.
5. Define a lightweight version/timestamp validation and fail-safe fallback for concurrent changes.
6. Validate packing, unpacking, carton changes, errors, persistence, labels, and concurrent users before measuring performance.

This rolling-cache concept is more coherent than preserving the first full `FF246783` solely for reuse, because the planned `IsItemMissing()` optimization is intended to eliminate that pre-pack full load. It nevertheless remains a higher-risk architectural candidate than the current request-scoped Version 1 optimization and will be rejected if Acumatica cache identity, mutation ordering, concurrency, or multi-node correctness cannot be proven.

### Two independent remaining investigation paths

**Execution plan analyzed and paths separated:** August 31, 2026 at 4:06 PM EDT

The remaining pre-pack and parent-update loads originate from different Acumatica processes and must be investigated independently. Optimizing one will not automatically remove the other:

| Investigation path | Query | Scan stage | Current behavior | Research objective |
|---|---|---|---|---|
| Item-presence validation | First `FF246783` | Before packing | `IsItemMissing()` materializes approximately 1,808 eligible joined split records to answer whether one scanned inventory item exists | Replace the full enumeration with a targeted, behaviorally equivalent existence query |
| Parent shipment update | `8659BCA6` | During `PackSplit` | `UpdatePickPackInfoOf()` invokes `PXParentAttribute.SelectChildren()` and retrieves all 1,808 `SOShipLine` records while updating parent picked/packed information | Determine whether the calculation can safely use an incremental adjustment, narrower aggregate/projection, or validated rolling cache |

The first path is evidenced by:

```text
ProcessSingleBarcode
-> InjectItemPresenceValidation
-> IsItemMissing
-> pickedForPack
-> GetSplits
-> FF246783
```

The second path is evidenced by the independent stack:

```text
PackSplit
-> insert SOShipLineSplitPackage
-> RowInserted<SOShipLineSplitPackage>
-> UpdateParentShipmentLine
-> UpdateShipmentLine
-> UpdatePickPackInfoOf
-> PXParentAttribute.SelectChildren
-> 8659BCA6
```

The actual `8659BCA6` execution plan used `SOShipLine_PK` through a Clustered Index Seek on `CompanyID` and `ShipmentNbr`, with `ShipmentType` and `DatabaseRecordStatus` applied as additional filters. It returned 1,808 rows, performed 186 logical reads, used 15 ms of SQL CPU in the SSMS replay, and showed no table scan, index scan, key lookup, missing-index recommendation, or execution warning. This indicates that SQL Server already locates the shipment lines efficiently; the remaining cost is retrieving a wide complete line collection and processing it in Acumatica. A new index alone is therefore unlikely to eliminate this load.

The intended research sequence is:

1. Complete caller, override, and rule analysis for `IsItemMissing()` and validate a comparison-only targeted existence query.
2. Inspect the complete `UpdatePickPackInfoOf()` calculation to identify exactly why it requests every `SOShipLine` and which fields, cache events, and totals it consumes.
3. Evaluate the parent-update alternatives in increasing order of risk: targeted incremental adjustment, database aggregate or narrower projection, then validated rolling cache.
4. Preserve standard behavior as an immediate fallback whenever eligibility, cache identity, mutation state, or concurrency validation is uncertain.
5. Measure each path independently and together. Only the combined result can determine whether the current sequence of three major 1,808-row executions can be reduced to the one required post-mutation `FF246783` refresh.

## Deployed performance customization files

- `PickPackShipTranQtyPerformanceExt.cs`
- `PackModeBarcodeLookupOptimization.cs`
- `PackModePickedForPackRequestCache.cs` - Version 1 production implementation with explicit post-confirmation invalidation
- `PackModePackedViewOptimization.cs`
- `PackModeQtyThresholdOptimization.cs`
- `PickPackShipShipmentRowSelectedOptimization.cs`

`PickPackShipGetSplitsOptimization.cs` was an unsuccessful diagnostic experiment and is not part of the deployed solution. Versions 2 and 3 remain research artifacts. Version 4 is the PXView diagnostic build, Version 5 is the comparison-only validation build, and Version 6 is a structurally successful but performance-neutral implementation experiment. Only one version of `PackModePickedForPackRequestCache.cs` may be published at a time; Version 1 remains the production-safe selection unless a later combined `IsItemMissing()` optimization is independently validated.

## Current deployment and validation steps

**Updated:** August 31, 2026 at 10:40 AM EDT

1. Return to Version 1 as the proven production-safe baseline; Version 6 alone did not improve measured performance.
2. Retain Versions 4-6 as research evidence that the one-row synchronization path is structurally valid but cannot eliminate the pre-mutation full load while `IsItemMissing()` still requires it.
3. If research continues, inspect and diagnose `IsItemMissing()` before writing another implementation. Confirm every input and business rule that determines shipment-item presence.
4. Preserve standard behavior as the fallback for alternate IDs, subitems, locations, lot/serial inventory, unassigned splits, removal, and specialized picking modes.
5. Retain `gridPacked` with `SyncPosition="true"`; the synchronization requirement itself was not removed or bypassed.
6. Retain Version 1's post-confirmation invalidation and standard full-result path for `CanPack` so post-`PackSplit` quantities and command states remain current.
7. Require controlled Request Profiler improvement and full regression testing before retaining any combined one-row and item-presence optimization.

The one-row synchronization experiment is concluded as insufficient by itself. A targeted `IsItemMissing()` existence check is the only newly identified narrow follow-up candidate. More invasive alternatives—maintaining a mutable authoritative split snapshot, bypassing cache synchronization, altering `PackSplit`, or deferring database persistence across scans—remain outside the current implementation because of disproportionate stale-state, concurrency, validation, label, and maintenance risks.

## Overall result

**Baseline finalized:** August 10, 2026 at 3:06 PM EDT  
**Investigation reopened:** August 31, 2026 at 10:14 AM EDT  
**Version 6 result recorded:** August 31, 2026 at 10:40 AM EDT

The completed optimizations reduced average scan time from approximately **5.19 seconds to the 1.37-1.44 second range** in steady Request Profiler captures, an improvement of approximately **72-74%**. SQL calls fell from approximately **1,850 to about 159-164 per scan**, a reduction of approximately **91%**.

The Advanced Labels lookup, repeated Master Pack barcode lookups, Package Content LINQ fallback, per-split `GetQtyThreshold()` queries, and repeated full `SOShipLine` query `B5446270` have been eliminated or consolidated. Production Version 1 request-scoped reuse removes one of the original three repeated `pickedForPack` split loads while preserving state correctness. Its first full pre-mutation result serves both grid synchronization and later `IsItemMissing()` validation through the request cache; the second load refreshes `CanPack` after packing changes quantities.

Version 3 reduced the two loads to one under guarded conditions, but post-snapshot profiling showed essentially unchanged average server time. Version 6 successfully replaced synchronization with one exact row, but `IsItemMissing()` then performed the displaced full load and overall scan time did not improve. Version 1 therefore remains the production-safe baseline. Further work is justified only if a separate targeted item-presence check can remove that full load without weakening Acumatica's validation rules.
