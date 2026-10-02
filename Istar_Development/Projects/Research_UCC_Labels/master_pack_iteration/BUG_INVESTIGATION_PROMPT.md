# Prompt: find the regression caused by MasterPackExtension 014/015

A bug appeared in Acumatica 25R2 (25.201.0213) after we published the customization project **MasterPackExtension**
with two new code files. Find what the new code breaks and propose the smallest fix that keeps both features working.
Start by reproducing or locating the bug. Do not change code until you can explain the cause.

## Environment

- Acumatica ERP 25R2, local site `C:\Program Files\Acumatica ERP\AcumaticaERP`, database `.\SQLEXPRESS` / `AcumaticaDB`,
  test company `CompanyID = 3` (SnapshotTest).
- Published code lands in `App_RuntimeCode\`; Request Profiler and Trace (SM205070) are available.
- Third-party packages involved:
  - **WMS** (`Bin\WMS.dll`, namespace `WMS`), from the customization MasterPackISV. It adds:
    - `SOPackageDetailExt` on `SOPackageDetail`: `UsrIsParentBox`, `UsrCartonNbr`, `UsrSelectedParentBox` (the master
      carton a carton is packed into), `UsrStoreNbr`;
    - the table `SelectedPackageContents`: planned carton contents, with `ShipmentNbr`, `PackageLineNbr`, `StoreNbr`,
      `BasePackedQty`, etc.
  - **Asgard Labels**, a ZPL label engine that reads `SOShipmentEntry` views by name.
- Other projects also extend `SOShipmentEntry` and `SOPackageDetail`, for example OneUCCPerPackage, ASCIStarWMSCustomization
  and TRUECOMMERCE.
- Source folder: `Current_Customizations\RH_Customizations\Customization_Code\MasterPackExtension\custom_code\`
  - `Custom_Code_013_Persisted_Skip`: the last version before our changes.
  - `Custom_Code_014_Master_Pack_Units`: 013 + `SOPackageDetail_MasterPackUnitsExt.cs`.
  - `Custom_Code_015_Master_Pack_Stores`: 014 + `SOShipmentEntry_MasterPackStores.cs`.
  - All other files in 015 are byte-identical to 013.

## Change 1 (version 014): `SOPackageDetail_MasterPackUnitsExt.cs`

**Why:** the Kohl's master pack label needed a "Units" value per master carton: the total quantity of the child
cartons assigned to it. The shipment total was wrong.

**What it does:**
- `SOPackageDetailEx_MasterPackUnitsExt`: a `PXCacheExtension<SOPackageDetailEx>` with an unbound field
  `UsrMasterPackUnits` (`[PXDecimal(0)]`, read-only UI field). It has no database column.
- `SOShipmentEntryExt_MasterPackUnits`: a `PXGraphExtension<SOShipmentEntry>` with
  `Events.RowSelecting<SOPackageDetailEx>`.
  - It runs only for master cartons: `UsrIsParentBox == true` with a carton number.
  - Inside `using (new PXConnectionScope())`, it runs a read-only BQL aggregate:
    `Sum(SelectedPackageContents.BasePackedQty)` joined to `SOPackageDetail` on shipment + `PackageLineNbr = LineNbr`,
    where `SOPackageDetailExt.UsrSelectedParentBox = <master carton nbr>`.
  - It then calls `e.Cache.SetValue<usrMasterPackUnits>(row, units)`.
- **Risk points:**
  - a database query inside RowSelecting, so it runs for every master package row read anywhere `SOPackageDetailEx`
    is selected through `SOShipmentEntry`: screens, processing, API, confirm shipment, mass processes;
  - `PXConnectionScope` nesting;
  - an extra cache extension on `SOPackageDetailEx`, which other customizations also extend.

## Change 2 (version 015): `SOShipmentEntry_MasterPackStores.cs`

**Why:** the Asgard label `iStar-5A-MasterPackingCartonLabel` printed every store on the shipment instead of only the
stores of the cartons inside the master carton being printed. Its store sub-model read the `OrderList` view with no
filter. We needed a list of (master carton, store) that Asgard can read by view name and filter with a rule.

**What it does:**
- `MasterPackStore`: a new `[PXHidden]` unbound DAC (`PXBqlTable, IBqlTable`) with two string key fields,
  `MasterCartonNbr` and `StoreNbr` (`[PXString(30, IsUnicode = true, IsKey = true)]`). It has no table.
- `SOShipmentEntryExt_MasterPackStores`: a `PXGraphExtension<SOShipmentEntry>`, `IsActive()` always true, which adds:
  ```csharp
  [PXVirtualDAC]
  [PXCopyPasteHiddenView]
  public SelectFrom<MasterPackStore>.View.ReadOnly MasterPackStores;
  protected virtual IEnumerable masterPackStores() { ... }
  ```
  - The delegate reads `Base.Document.Current?.ShipmentNbr`. If there is none, it returns an empty list.
  - Otherwise it runs `SelectFrom<SOPackageDetail>.LeftJoin<SelectedPackageContents>` (on shipment + package line)
    `.Where<shipmentNbr = @P and SOPackageDetailExt.usrSelectedParentBox IsNotNull>.View.ReadOnly`.
  - It collects distinct (master, store) pairs in a `SortedSet<(string, string)>`. The store comes from the contents'
    `StoreNbr`, falling back to the carton's `UsrStoreNbr`.
  - It returns new `MasterPackStore` objects, which are not inserted into any cache.
- **Label changes that go with it:**
  - an Asgard rule `MasterPackStores.MasterCartonNbr == Packages.UsrCartonNbr`;
  - data element 212 `MasterPackStores.StoreNbr`;
  - the sub-model `iStar-5A-MasterPackingCartonLabel-Iterator`, re-based from `OrderList` to `MasterPackStores`.
- **Risk points:**
  - a new view and cache on `SOShipmentEntry`, visible to anything that enumerates `graph.Views` or
    `graph.Caches`: Asgard, import/export scenarios, the contract-based API, copy-paste, Generic Inquiry, the
    workflow engine, and other customizations reflecting over views;
  - a new DAC type registered in the graph, plus `PXVirtualDAC` on a read-only view with a delegate;
  - the delegate's query whenever the view is selected (Asgard selects every view it resolves);
  - `SortedSet` of tuples, which uses culture-sensitive string comparison.
- **Operational note:** after publishing, the application had to be restarted. Asgard caches each graph's view list
  in a static dictionary keyed by the graph type name; without a restart it reported "Could not find a view named
  'MasterPackStores'". A missed restart or a stale cache in another component is a possible symptom source.

## What was verified (before the bug report)

- Both files compile together with the rest of the project.
- Master Pack Units renders the right value on the Kohl's master pack label: 8 for a master carton holding 4 + 2 + 2.
- The 5A label prints only the stores of the printed master carton, including a second label when there are more
  than 24 stores.
- Nothing else was tested: other screens, processes, API or WMS flows.

## How to investigate

1. Get the exact symptom: screen or process, steps, error text, and a Trace or Request Profiler entry with the
   stack trace.
2. Bisect by version. Publish 013 → 014 → 015 (or remove one file at a time) and see which version introduces the
   bug.
3. If it's 014, check RowSelecting side effects:
   - every place `SOPackageDetailEx` rows are read through `SOShipmentEntry` (including `PackagesForRates`,
     `ALPackages`/`ALiStarPackages`, confirm shipment, carrier rates, pick-pack-ship);
   - the `PXConnectionScope` inside RowSelecting during persist or long operations;
   - performance (one query per master carton row).
4. If it's 015, check what enumerates graph views or caches: the API or endpoints, import scenarios, Asgard printing
   of *other* labels on SO302000, and `Document.Current` being null or a different shipment in processing graphs.
5. Search other customizations for code that iterates `Base.Views`/`Caches` or relies on view counts or order on
   `SOShipmentEntry`.
6. Propose the smallest fix. Options might include:
   - guarding the extension to a narrower context;
   - moving the RowSelecting query to `FieldSelecting` or a lazy calculation;
   - reading `PXCache.Current` differently;
   - narrowing `IsActive()`.

   Explain why the fix resolves the bug and keeps both labels working.
