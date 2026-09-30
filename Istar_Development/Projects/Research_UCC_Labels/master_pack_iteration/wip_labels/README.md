# 5A master packing carton label: only the stores in the printed master carton

**Problem:** `iStar-5A-MasterPackingCartonLabel` listed every store on the shipment.
Its store sub-model (`iStar-5A-MasterPackingCartonLabel-Iterator`) read the shipment's order list (`OrderList`)
and had no print rule.

**Fix:**
- A new read-only list, `MasterPackStores` (customization MasterPackExtension, version 015), has one row per
  master carton and store: the distinct stores of the cartons assigned to that master carton. Stores come from the
  cartons' planned contents; if a carton has none, its own Store Nbr is used.
- The sub-model reads that list, and a print rule keeps only the rows of the master carton being printed.
- The main label is unchanged. It still prints 24 stores per label (4 columns). Asgard prints additional labels,
  with the same header, when a master carton has more than 24 stores.

## Files

| Step | File | Screen | What it does |
|---|---|---|---|
| 0 | `Custom_Code_015_Master_Pack_Stores/SOShipmentEntry_MasterPackStores.cs` | Customization project **MasterPackExtension** | Adds the `MasterPackStores` list |
| 1 | `1_ALRule-SO302000-MasterPackStores-of-Printed-Master.zip` | Rules (AL203500) | `MasterPackStores.MasterCartonNbr == Packages.UsrCartonNbr` |
| 2 | `2_ALDataElement-MasterPackStores-StoreNbr-212.zip` | Data Elements | Line 212: `MasterPackStores.StoreNbr` |
| 3 | `3_ALModel-iStar-5A-MasterPackingCartonLabel-Iterator.zip` | Label Models (AL201000) | Sub-model: Based On View `MasterPackStores`, the rule above, and row 1 prints element 212 |

The code file is in
`Current_Customizations/RH_Customizations/Customization_Code/MasterPackExtension/custom_code/Custom_Code_015_Master_Pack_Stores/`.
Version 015 is 014 plus this one new file.

The `.xml` files are the same content as the `.zip` files, for reading. Import the `.zip` files.

## Steps

1. **Code:** in **MasterPackExtension**, add a code file named `SOShipmentEntry_MasterPackStores`, paste the file's
   content, then save and **Publish**. Leave the other files as they are.
2. **Import mode:** check that **Label Basic Preferences (AL101000)** > Record Import Mode is
   **Insert Update Intersection**.
3. **Labels:** import files 1, 2, 3 in that order with **Import ALL as ZIP** on the screen listed for each.
4. **Check the sub-model** on Label Models: Based On View = `MasterPackStores`, and Prints when =
   `SO302000-MasterPackStores-of-Printed-Master` with Not unchecked.

## Test (local, shipment 0000775)

| Master carton | Stores | Expected labels |
|---|---|---|
| 00007473 | 16 | 1 |
| 00007474 | 19 | 1 |
| 00007475 | 30 | 2 (24 + 6) |
| 00007476 | 43 | 2 (24 + 19) |

Master carton 00008097 on 0000784 should show 01578, 01603 and 01604 (cartons with no store of their own, so the
stores come from their contents).

Only the selected master carton's stores should appear. PO #, Master Pack # and Dept # repeat on each label.

## Undo

Re-import `../original_labels/ALModel-iStar-5A-MasterPackingCartonLabel-Iterator.xml` with **Import from XML**
(Label Models). That restores Based On View `OrderList` and removes the print rule. Import from XML always
replaces, which is what you want for this undo.
