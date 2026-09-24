Here is the flow from start to finish.

## 1. User selects a package row

**File:** `SOShipmentEntry_AsgardExt.cs`
**Class:** `SOShipmentEntry_AsgardExt`
**Method:** `PrintForPackage(...)`

This starts from the Shipments screen. The selected package comes from:

```csharp
SOPackageDetail currentSelected = Base.Packages.Current;
int? selectedBeforeSave = selectedPackageLineNbr ?? currentSelected?.LineNbr;
```

This captures the package line number before saving because saving can disturb the selected row.

---

## 2. User clicks “Print Asgard Label”

**File:** `SOShipmentEntry_AsgardExt.cs`
**Method:** `printAsgardPackageLabel(...)`

```csharp
PrintForPackage(adapter);
```

This button calls `PrintForPackage(...)`.

---

## 3. Code queues a long operation

**File:** `SOShipmentEntry_AsgardExt.cs`
**Method:** `QueuePrintForPackage(...)`

```csharp
PXLongOperation.StartOperation(Base, delegate()
{
    PrintForPackageCore(shipmentNbr, packageLineNbr);
});
```

This moves printing into a fresh isolated long operation.

---

## 4. Fresh shipment graph is created

**File:** `SOShipmentEntry_AsgardExt.cs`
**Method:** `PrintForPackageCore(...)`

```csharp
SOShipmentEntry graph = PXGraph.CreateInstance<SOShipmentEntry>();
SOShipment shipmentInLongOp = SOShipment.PK.Find(graph, shipmentNbr);
graph.Document.Current = shipmentInLongOp;
```

This reloads the shipment in a clean `SOShipmentEntry` graph.

---

## 5. Correct Asgard model is selected dynamically

**File:** `AsgardLabelService.cs`
**Class:** `AsgardLabelService`
**Method:** `ResolveModelIdByAsgardRules(...)`

This checks active Asgard models based on:

```csharp
Packages
ALPackages
ALiStarPackages
```

Then evaluates rules like:

```text
Document.CustomerID.AcctName | string.Contains 'TARGET'
Document.CustomerID.AcctName | string.Contains 'BOSCOV'
```

using:

```csharp
NewScribanUtils.EvalExpr<bool>(scribanContext, rule.Expression, false);
```

One matching model is selected.

---

## 6. Package checkbox is cleared and selected package is checked

**File:** `AsgardLabelService.cs`
**Method:** `PrintSelectedPackageUsingNativeContext(...)`

First, all package print flags are cleared:

```csharp
ClearAllPackagePrintFlags(shipment.ShipmentNbr);
```

Then only the selected package is marked:

```csharp
SetOnlySelectedPackagePrintFlag(shipment.ShipmentNbr, selectedPackageLineNbr);
```

That sets:

```text
UsrALPrintLabel = true
```

only on the selected package.

Then the graph saves:

```csharp
_graph.Actions.PressSave();
```

---

## 7. Filter scope activates

**File:** `ALPackagesFilterScope.cs`
**Class:** `ALPackagesFilterScope`
**Method:** `Activate(...)`

```csharp
using (ALPackagesFilterScope.Activate(
    shipment.ShipmentNbr,
    new int?[] { selectedPackageLineNbr }))
{
    ...
}
```

Inside this `using` block, the system knows:

```text
Only allow this shipment number and this package line number.
```

Outside the block, filtering turns off.

---

## 8. Package views are intercepted

**File:** `SOShipmentEntry_AsgardViewFilterExt.cs`
**Class:** `SOShipmentEntry_AsgardViewFilterExt`
**Method:** `Initialize()`

At graph startup, these views are wrapped:

```csharp
Base.Views["Packages"]
Base.Views["ALPackages"]
Base.Views["ALiStarPackages"]
```

The important replacement is:

```csharp
Base.Views[viewName] = new PXView(
    Base,
    true,
    originalView.BqlSelect,
    new PXSelectDelegate(() => FilteredPackageView(viewName, originalView)));
```

This means whenever Asgard asks for package rows, it goes through your filter wrapper.

---

## 9. View filter returns only the selected package

**File:** `SOShipmentEntry_AsgardViewFilterExt.cs`
**Method:** `FilteredPackageView(...)`

If the filter scope is inactive:

```csharp
yield return row;
```

All packages are returned normally.

If the filter scope is active:

```csharp
if (!ALPackagesFilterScope.Matches(packageDetail.ShipmentNbr, packageDetail.LineNbr))
{
    continue;
}

yield return row;
```

Only the selected package row is returned.

Important: it returns the original `row`, so joined Asgard row shape is preserved.

---

## 10. Asgard print context is created

**File:** `AsgardLabelService.cs`
**Method:** `PrintSelectedPackageUsingNativeContext(...)`

```csharp
AcuLabelContext printContext = AcuLabelContext.CreatePrintContext(
    _graph.GetType(),
    shipment,
    modelId,
    false,
    adapter);
```

Because the filter scope is active, Asgard sees only the selected package.

---

## 11. Asgard prints the label

**File:** `AsgardLabelService.cs`
**Method:** `PrintSelectedPackageUsingNativeContext(...)`

```csharp
PrintResults results = _labelGenerator.PrintLabels(printContext);
```

Asgard renders the selected model, using the selected package row.

For example:

```text
1 'R&S UCC-128 - Target' > 'Test Printer'
```

or:

```text
1 'Stanley UCC-128 - Boscov' > 'Test Printer'
```

---

## 12. Cleanup happens

**File:** `AsgardLabelService.cs`
**Method:** `PrintSelectedPackageUsingNativeContext(...)`

In the `finally` block:

```csharp
ClearAllPackagePrintFlags(shipment.ShipmentNbr);
_graph.Actions.PressSave();
```

This clears `UsrALPrintLabel` from all packages so the shipment is left clean.

---

## Whole flow in one picture

```text
User selects package row
        ↓
SOShipmentEntry_AsgardExt.printAsgardPackageLabel()
        ↓
PrintForPackage()
        ↓
QueuePrintForPackage()
        ↓
PrintForPackageCore()
        ↓
AsgardLabelService.ResolveModelIdByAsgardRules()
        ↓
AsgardLabelService.PrintSelectedPackageUsingNativeContext()
        ↓
Clear all UsrALPrintLabel flags
        ↓
Set selected package UsrALPrintLabel = true
        ↓
Activate ALPackagesFilterScope
        ↓
SOShipmentEntry_AsgardViewFilterExt filters package views
        ↓
AcuLabelContext.CreatePrintContext()
        ↓
_labelGenerator.PrintLabels()
        ↓
Label prints
        ↓
Clear all UsrALPrintLabel flags again
```
