# Acumatica Scan Performance Investigation Report

## Initial problem

Warehouse scans on shipment `0000787`, containing approximately 1,808 shipment splits, were taking more than five seconds on average. Profiler analysis showed unusually high SQL, CPU, and cache activity during every scan.

### Initial profiler evidence

Across four scan requests:

| Metric | Observed |
|---|---:|
| Average scan time | 5.19 seconds |
| Range | 4.20–5.82 seconds |
| SQL calls per scan | 1,101–2,110 |
| Cache/select operations | 13,558–15,716 |
| SQL time per scan | 1.30–1.91 seconds |
| CPU time per scan | 3.14–4.45 seconds |
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

It uses Acumatica’s supported extension-of-extension structure to run after `WMS.PackModeLogicExt`. It replaces only the third-party barcode handler and leaves the remainder of the Master Pack functionality unchanged.

The optimized process is:

```text
Look up the scanned barcode
→ identify the inventory item
→ confirm the item exists on the shipment
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
→ pickedForPack
→ GetSplits
```

2. Scanner state selection:

```text
ShipmentState.SetNextState
→ CanPack
→ pickedForPack
→ GetSplits
```

3. Command enablement:

```text
PackAllIntoBoxCommand.IsEnabled
→ CanPack
→ pickedForPack
→ GetSplits
```

These calls are primarily part of Acumatica’s standard scan workflow. The third-party `PackModeLogicExt` processes and sorts the results, but no separate customization was found creating additional calls.

The behavior is functionally valid but inefficient for unusually large shipments. Acumatica loads and processes the same 1,808 splits independently for validation, state selection, and command enablement.

## Proposed next optimization

The proposed next step is a request-scoped cache for:

```text
WMS.PackModeLogicExt.pickedForPack()
```

The objective is to reuse the materialized split results during one scan request instead of querying the same data three times.

Research confirmed that packing quantities are modified through:

```text
Confirm()
→ PackSplit()
→ SOShipLineSplitPackage.PackedQty
```

Therefore, the cache must be invalidated after confirmation or any packing mutation. Reusing a pre-update result after `PackSplit()` could cause stale quantities or incorrect command states.

Expected behavior:

- Read-only scan processing could reduce three full split loads to one.
- A request that changes packing quantities may require one load before the update and another after invalidation.
- Cached data must never persist across separate HTTP requests.

## Overall result

The completed optimizations have reduced average scan time from approximately **5.19 seconds to 2.43 seconds**, a total improvement of approximately **53%**. SQL calls have fallen from approximately **1,850 to 208 per scan**, a reduction of approximately **89%**.

The primary per-split Advanced Labels lookup and repeated Master Pack barcode lookups have both been eliminated. The remaining optimization opportunity is repeated loading of the same shipment splits by standard Acumatica validation and state-management logic.