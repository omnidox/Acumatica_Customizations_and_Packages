# 5A master packing carton label: production import

Fixes **iStar-5A-MasterPackingCartonLabel** so that each master carton label lists only the stores of the cartons
assigned to that master carton, not every store on the shipment. This is the issue in the "Urgent! RE: Ucc128 label
for JCP" thread: "We expect to see only 6 stores on 1 label."

- Each store is listed once, in number order, 24 per label (4 columns).
- If a master carton holds more than 24 stores, Asgard prints additional labels, each with the same PO #, Master Pack #
  and Dept #.
- Stores come from the cartons' planned contents. If a carton has no planned contents with a store, its own Store
  Nbr is used.

Tested locally (SnapshotTest):
- master carton 00005480: 17 stores, 1 label;
- master carton 00008097 with 3 cartons: 3 stores;
- master carton 00008097 with 30 cartons: 29 stores, 2 labels (24 + 5).

## What is in this folder

| Step | File | Where it goes | What it adds |
|---|---|---|---|
| 1 | `1_customization_code/SOShipmentEntry_MasterPackStores.cs` | Customization project **MasterPackExtension** | The read-only list `MasterPackStores`: one row per master carton and store. Nothing is stored in the database. |
| 2 | `2_ALRule-SO302000-MasterPackStores-of-Printed-Master.zip` | **Rules** (AL203500) | The rule `MasterPackStores.MasterCartonNbr == Packages.UsrCartonNbr`: keeps only the stores of the master carton being printed |
| 3 | `3_ALDataElement-MasterPackStores-StoreNbr-212.zip` | **Data Elements** | Data element line 212, `MasterPackStores.StoreNbr` |
| 4 | `4_ALModel-iStar-5A-MasterPackingCartonLabel-Iterator.zip` | **Label Models** (AL201000) | Updates the store sub-model `iStar-5A-MasterPackingCartonLabel-Iterator`: Based On View `OrderList` becomes `MasterPackStores`, it gets the rule from step 2, and its row prints the element from step 3 |

The main label, **iStar-5A-MasterPackingCartonLabel**, is not changed. Its row 5 already prints the sub-model.

Do the steps **in order**: each one uses what the previous one added.

## Before you start

1. **Set the import mode.** Open **Label Basic Preferences (AL101000)** and set **Record Import Mode** to
   **Insert Update Intersection**, then save. In this mode an import only inserts and updates; it never deletes.
2. **Use "Import ALL as ZIP" for every file.** Do **not** use "Import from XML": it always replaces, whatever the
   setting above says.
3. **Optional backup:** export `iStar-5A-MasterPackingCartonLabel-Iterator` from Label Models (clipboard menu >
   Export as XML). `../original_labels/` already holds a production export of both 5A models.

## Steps

### 1. Customization code
1. Open **Customization Projects** and the production **MasterPackExtension** project.
2. In **Code**, add a new code file named `SOShipmentEntry_MasterPackStores` and paste the content of
   `1_customization_code/SOShipmentEntry_MasterPackStores.cs`. Add only this one file; leave the project's other
   files as they are.
3. Save and **Publish**.
4. **Restart the application:** open **Apply Updates (SM203510)** and click **Restart Application**.
   This is required. Asgard keeps its list of a screen's views in memory until the site restarts. Without a restart,
   printing fails with "Could not find a view named 'MasterPackStores' on graph '...SOShipmentEntry'", even though
   the publish succeeded.

> **Kohl's master pack package** (`ucc master labels/import_to_prod`): its code file,
> `SOPackageDetail_MasterPackUnitsExt.cs`, goes into the same MasterPackExtension project. The two files do not depend
> on each other, so either package can go first. If both are installed on the same day, add both files, then publish
> and restart once.

### 2-4. Label records
For each ZIP, open the screen, open the clipboard menu, choose **Import ALL as ZIP**, and pick the file.
The result message should say "1 file has been imported".

| Step | Screen | File |
|---|---|---|
| 2 | Rules (AL203500) | `2_ALRule-SO302000-MasterPackStores-of-Printed-Master.zip` |
| 3 | Data Elements | `3_ALDataElement-MasterPackStores-StoreNbr-212.zip` |
| 4 | Label Models (AL201000) | `4_ALModel-iStar-5A-MasterPackingCartonLabel-Iterator.zip` |

## Check after the import

Open **iStar-5A-MasterPackingCartonLabel-Iterator** on **Label Models (AL201000)**:
- **Based On View** is `MasterPackStores`.
- **Prints when** (print rule) is `SO302000-MasterPackStores-of-Printed-Master`, with **Not** unchecked.
  If it is blank, select it.
- Row 1 uses the data element **SO302000-MasterPack Stores (Store Nbr)**.

Then print the 5A label for a shipment with master cartons:
- Each master carton label lists only the stores of the cartons assigned to it, in number order, with no repeats.
- A master carton with more than 24 stores prints 2 or more labels.
- PO #, Master Pack # and Dept # are the same as before.

## If something goes wrong

| Symptom | Cause | Fix |
|---|---|---|
| "Could not find a view named 'MasterPackStores'" | The application was not restarted after publishing | Apply Updates (SM203510) > Restart Application |
| No stores printed | Print rule missing on the sub-model, or the cartons have no master carton assigned | Check "Prints when" on the sub-model; check "Contains In Master Pack Carton #" on the cartons |
| Every store on the shipment printed | Step 4 not imported (the sub-model still reads `OrderList`) | Import step 4 again |

## Undo

On **Label Models (AL201000)**, import `../original_labels/ALModel-iStar-5A-MasterPackingCartonLabel-Iterator.xml`
with **Import from XML**. That restores Based On View `OrderList` and removes the print rule (it prints all stores
again). The code file can stay; nothing else uses it.

## Data the shipment needs

| Label field | Where it comes from |
|---|---|
| Store # list | Cartons whose **Contains In Master Pack Carton #** is the printed master carton; store from each carton's planned contents, else the carton's **Store Nbr** |
| PO #, Dept # | Shipment (unchanged) |
| Master Pack # | Master carton's Carton Nbr (unchanged) |
