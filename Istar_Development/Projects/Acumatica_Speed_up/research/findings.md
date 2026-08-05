I found a clear root cause: the scans are slow primarily because of application-level N+1 queries and repeated grid/view evaluation—not because one SQL statement is intrinsically slow.

## What the profiler shows

Across four scan requests on shipment `0000787`:

| Metric | Observed |
|---|---:|
| Average scan time | 5.19 seconds |
| Range | 4.20–5.82 seconds |
| SQL calls per scan | 1,101–2,110 |
| Cache/select operations | 13,558–15,716 |
| SQL time per scan | 1.30–1.91 seconds |
| CPU time per scan | 3.14–4.45 seconds |
| Shipment splits returned | approximately 1,808 |

The server spends much of the request repeatedly constructing DAC records, firing formulas and events, and executing individual lookups.

## Priority 1: eliminate the `SOShipLine` N+1 lookup

The biggest issue is SQL hash `959AAB12`:

```sql
SELECT TOP (1) ...
FROM SOShipLine
WHERE CompanyID = @CompanyID
  AND ShipmentNbr = @ShipmentNbr
  AND LineNbr = @LineNbr
```

It executed:

- Scan 37: 812 times
- Scan 38: 1,810 times
- Scan 39: 1,808 times
- Scan 40: 1,814 times
- Total across four scans: 6,244 queries
- Total database time: 2.25 seconds

The stack trace identifies the precise source:

```text
AA.Objects.Labels.TranQty.ALTranQty.ForSOShipLineSplit.Evaluate
PX.Objects.SO.SOShipLine.PK.Find
PXFormulaAttribute.FormulaDefaulting
PXFormulaAttribute formulaRowSelecting
```

`ALTranQty.ForSOShipLineSplit` performs `SOShipLine.PK.Find()` for every `SOShipLineSplit` while Acumatica is materializing the grid.

### Recommended fix

Change the `ALTranQty` formula so it does not query `SOShipLine` during `RowSelecting`.

Best options, in order:

1. Calculate the value directly from fields already available on `SOShipLineSplit`.
2. Add `SOShipLine` to the primary view’s BQL join and calculate from the joined result.
3. Preload all shipment lines once into a dictionary keyed by `(ShipmentNbr, LineNbr)`.
4. Persist the calculated value on the split and update it only when its input fields change.

A request-scoped cache would conceptually look like:

```csharp
var lines = SelectFrom<SOShipLine>
    .Where<SOShipLine.shipmentNbr.IsEqual<@P.AsString>>
    .View.Select(graph, shipmentNbr)
    .RowCast<SOShipLine>()
    .ToDictionary(
        x => (x.ShipmentNbr, x.LineNbr),
        x => x);

lines.TryGetValue(
    (split.ShipmentNbr, split.LineNbr),
    out SOShipLine line);
```

Do this once per request or shipment—not inside a formula called once per split.

This change alone should remove roughly 1,800 SQL round trips from a typical scan. Because these calls also cause formula, cache, DAC hydration, and stack-processing overhead, the benefit should be considerably greater than the reported SQL duration alone.

## Priority 2: fix the LINQ fallback

Every scan reports:

```text
LINQ fallback! Consider rewriting your query.
SQLQueryable<PXResult<SOShipLineSplit>>
SelectManyIterator
WhereSelectEnumerableIterator
OfTypeIterator
ToList
PXView.InvokeDelegate
```

This indicates a custom view delegate is applying unsupported LINQ operations to an Acumatica `SQLQueryable`. Acumatica then falls back to application-side processing. It appears late in the request and is probably involved in rebuilding/rendering the splits grid.

Search the customization implementing the `SOShipLineSplit` view for a chain resembling:

```csharp
query.SelectMany(...)
     .Where(...)
     .Select(...)
     .OfType<...>()
     .ToList();
```

Rewrite it so filtering and joins are expressed in BQL/Fluent BQL:

```csharp
SelectFrom<SOShipLineSplit>
    .InnerJoin<SOShipLine>
        .On<SOShipLine.shipmentNbr
            .IsEqual<SOShipLineSplit.shipmentNbr>
        .And<SOShipLine.lineNbr
            .IsEqual<SOShipLineSplit.lineNbr>>>
    .Where<SOShipLineSplit.shipmentNbr
        .IsEqual<@P.AsString>
      .And<SOShipLineSplit.isUnassigned
        .IsEqual<False>>>
```

If a small amount of LINQ is unavoidable, explicitly materialize the correctly filtered BQL result first and then run LINQ on that in-memory list. Avoid enumerating an Acumatica `SQLQueryable` through `SelectMany`, `OfType`, or `Convert()`.

## Priority 3: stop reloading all 1,808 splits

SQL hash `FF246783` loads all assigned splits joined to `SOShipLine` and `INLocation`:

- Executed 11 times
- Returned 19,888 rows total
- Consumed 846.6 ms
- Approximately 1,808 rows on every execution

Its principal call path is:

```text
PickPackShip.GetSplits
PackMode.Logic.pickedForPack
WMS.PackModeLogicExt.pickedForPack
PackMode.Logic.get_CanPack
PackAllIntoBoxCommand.IsEnabled
ActualizeCommandActions
RowSelected
```

The scan callback is repeatedly evaluating whether “Pack All Into Box” is enabled. That check reloads all splits for the shipment.

Recommended changes:

- Do not call `GetSplits()` from `RowSelected` or an action’s `IsEnabled` calculation.
- Cache the result for the duration of the scan request.
- Invalidate it only after a split/package mutation.
- If the code only needs to know whether an eligible split exists, replace full loading with an existence query using `SelectWindowed(0, 1)` or equivalent.
- If it needs a total, perform a SQL-side aggregate rather than retrieving 1,808 DAC records.
- Avoid recalculating command availability multiple times during the same callback.

For example, if the condition is simply “is anything picked and unpacked?”, query only one matching record:

```csharp
var exists = SelectFrom<SOShipLineSplit>
    .Where<SOShipLineSplit.shipmentNbr.IsEqual<@P.AsString>
      .And<SOShipLineSplit.isUnassigned.IsEqual<False>>
      .And<SOShipLineSplit.basePickedQty
          .IsGreater<SOShipLineSplit.basePackedQty>>>
    .View.SelectWindowed(graph, 0, 1, shipmentNbr)
    .Any();
```

Adjust the condition to match your actual packing rules.

## Priority 4: reduce secondary repeated queries

Several smaller N+1 patterns also occur on every scan:

- `InventoryItem`/`INItemXRef`: 64 executions per scan
- `SOShipLineSplitPackage`: 29–58 executions per scan
- `SOLine` joined to `SOShipLine`: approximately 27 executions per scan
- `Note` and `NoteDoc`: dozens of executions

Recommended treatment:

- Load barcode cross-references once for the scanned inventory/barcode.
- Cache negative cross-reference results too; all 256 captured `INItemXRef` executions returned no rows.
- Load package content once by `(ShipmentNbr, PackageLineNbr)`.
- Avoid requesting notes/files unless those UI fields are actually visible or required during scanning.
- Ensure the scan callback refreshes only the controls that changed instead of refreshing every shipment grid and toolbar state.

## Priority 5: review customization event handlers

Two customization paths appear in the profiler:

```text
WMS.PackModeLogicExt.pickedForPack
TCAddon.TCSOShipmentEntry_Extension.SOShipment_RowSelected
```

The `TCAddon` row-selected handler contributes to a full `SOShipLine` load by shipment. The profiler shows that query 11 times, returning 19,888 total rows and consuming 322 ms.

Review every `RowSelected` handler for:

- Database selects
- Iterating the entire shipment
- Setting field values
- Calling `View.RequestRefresh()`
- Evaluating action state through expensive queries
- Repeating logic whose inputs did not change

`RowSelected` can run many times during one scan callback. Keep it limited to cheap UI-state operations based on data already in the graph.

## Index recommendations

Indexes are not the primary solution here. The worst `SOShipLine` query is already individually fast; executing it 1,800 times is the problem.

After removing repeated calls, validate execution plans for the remaining queries. Potential supporting indexes are:

```text
SOShipLineSplit:
CompanyID, ShipmentNbr, IsUnassigned, LineNbr, SplitLineNbr
INCLUDE LocationID, InventoryID, PickedQty, PackedQty

SOShipLineSplitPackage:
CompanyID, ShipmentNbr, PackageLineNbr
INCLUDE ShipmentLineNbr, ShipmentSplitLineNbr, PackedQty

INItemXRef:
CompanyID, InventoryID, AlternateID
```

Check Acumatica’s existing indexes before adding these—some or all may already exist. Do not add overlapping indexes without comparing current definitions and SQL execution plans.

## Non-performance warnings

Each scan also emits:

```text
[LabelButtonWrapper] Missing folder or wrapper:
ALLabelFolder / RHLineToggleLabelPrint

[LabelButtonWrapper] Missing folder or wrapper:
ALLabelFolder / RHPackageToggleLabelPrint
```

These are probably not responsible for seconds of latency, but they indicate label customization code is searching for UI controls that are absent. Guard that initialization by screen/mode and stop retrying it on every callback.

## Recommended implementation sequence

1. Fix `ALTranQty.ForSOShipLineSplit` so it never calls `SOShipLine.PK.Find()` during formula evaluation.
2. Rewrite the custom `SOShipLineSplit` view delegate causing the LINQ fallback.
3. Cache or replace `pickedForPack()` so `GetSplits()` is not executed repeatedly.
4. Remove database work from `SOShipment_RowSelected`.
5. Cache barcode, package, and negative `INItemXRef` lookups.
6. Capture a new profiler session and target:
   - Under 100–200 SQL calls per scan
   - No LINQ fallback
   - No per-split `SOShipLine.PK.Find`
   - No more than one full split query per scan
   - Initial target below 2 seconds, with sub-second scans plausible after the event/formula work is removed

The logs are here: [ProfilerLog](C:/Users/Procomm/Documents/GitHub/Acumatica_Customizations_and_Packages/Istar_Development/Projects/Acumatica_Speed_up/ProfilerLog). The folder currently contains only profiler artifacts, so I could identify the offending classes and methods from their stack traces but could not patch the customization source itself.