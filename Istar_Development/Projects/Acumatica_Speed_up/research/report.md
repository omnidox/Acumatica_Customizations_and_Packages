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

## Current remaining bottleneck

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

These calls are primarily part of Acumatica's standard scan workflow. The third-party `PackModeLogicExt` processes and sorts the results, but no separate customization was found creating additional calls.

The behavior is functionally valid but inefficient for unusually large shipments. Acumatica loads and processes the same 1,808 splits independently for validation, state selection, and command enablement.

The three evaluations are initiated by Acumatica's standard scan workflow to validate the item, select the next scan state, and determine command availability. The third-party `PackModeLogicExt` participates by processing and sorting the results, but it does not create the three consumers. This repeated work is generally insignificant for small shipments but becomes costly for shipment `0000787`, which contains approximately 1,808 splits. One evaluation also enables `PackAllIntoBoxCommand`, even though worksheet picking is not currently used.

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

The profiler continues to report the following application-side LINQ fallback during scan and related grid callbacks:

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

`PickPackShipGetSplitsOptimization.cs` was created to replace this conversion in Pack mode while preserving joins, assigned/unassigned handling, processed separation, and warehouse ordering. `ProfilerLog_9` confirmed that the override executed, but the identical LINQ warning remained once per scan and during profiler shutdown. SQL calls, select operations, and the two 1,808-row split loads were effectively unchanged. This proves that another grid view delegate independently generates the reported fallback; the `_CustomMethod -> PXView.InvokeDelegate -> PXGrid.PerformSelect` path and the relevant ASPX `DataMember` are the next investigation targets.

`ProfilerLog_9` averaged 2.44 seconds per scan versus 1.79 seconds in `ProfilerLog_8`. CPU was slightly lower, but SQL time increased from 0.38 to 1.01 seconds, indicating database timing variation rather than a demonstrated benefit from the override. One scan also encountered an `SOShipment` lock violation handled by the existing retry logic.

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

The identical `SOShipLineSplit` LINQ fallback remained present during every measured scan after these customizations were deactivated. In `ProfilerLog_8`, deactivating `MasterPackExtension[07.22.2026][1]` removed some ancillary UI, event, note, item, customer, site, and package-related work, but it did not remove or change the fallback. The two safe full split loads also remained unchanged. These customizations are therefore not considered the source of the current scan-related LINQ fallback.

## Overall result

The completed, verified optimizations reduced average scan time from approximately **5.19 seconds to 2.08 seconds** in the comparable cache test, an improvement of approximately **60%**. SQL calls fell from approximately **1,850 to about 211 per comparable scan**, a reduction of approximately **89%**. The later `ProfilerLog_9` capture averaged 2.44 seconds because SQL execution was substantially slower during that test.

The primary per-split Advanced Labels lookup and repeated Master Pack barcode lookups have both been eliminated. Request-scoped reuse has also removed one of the three repeated `pickedForPack` split loads while preserving a required reload after packing quantities change. The previously identified LINQ fallback remains a separate optimization opportunity.
