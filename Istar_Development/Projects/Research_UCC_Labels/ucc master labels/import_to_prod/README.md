# Kohl's master pack label: production import

Installs the **masterpack KOHLS U.C.C.-128 label** (Kohl's master pack carton label) in production,
exactly as it works in the local SnapshotTest instance.

## What is in this folder

| Step | File | Where it goes | What it adds |
|---|---|---|---|
| 1 | `1_customization_code/SOPackageDetail_MasterPackUnitsExt.cs` | Customization project **MasterPackExtension** | "Master Pack Units": the total quantity of the cartons inside a master carton |
| 2 | `2_ALBarcode-MasterPack.zip` | **Barcodes** (AL204000) | `GS1-Code128-150-NoHRI`, `Code128-100-noHRI-Auto` |
| 3 | `3_ALSubstitution-MasterPack.zip` | **Substitutions** (AL206500) | 8 substitutions (pad-to-6 Mark For, `(420) `/`(91) ` with a space, "Invoice No. ", `STRIP_SPACES`, `NO_DECIMAL`, `JOIN_BY_SPACE`, `Prepend-420-Barcode`) |
| 4 | `4_ALDataElement-MasterPack-201-211.zip` | **Data Elements** | 9 data elements (lines 201, 203, 205-211) |
| 5 | `5_ALModel-masterpack_KOHLS_128.zip` | **Label Models** (AL201000) | The label itself |

Do the steps **in order**: each file uses records created by the one before it.

## Before you start

1. **Set the import mode.** Open **Label Basic Preferences (AL101000)** and set
   **Record Import Mode** to **Insert Update Intersection**, then save.
   In this mode an import only inserts and updates; it never deletes. With the default modes, an import
   deletes linked records that are not in the file, which has deleted shared data elements and substitutions before.
2. **Use "Import ALL as ZIP" for every file below.** Do **not** use "Import from XML": it always replaces,
   whatever the setting above says.
3. **Optional backup:** if the label already exists in production, export it first (Label Models > clipboard menu > Export as XML).

## Steps

### 1. Customization code
1. Open **Customization Projects** and the production **MasterPackExtension** project.
2. In **Code**, add a new code file named `SOPackageDetail_MasterPackUnitsExt` and paste the content of
   `1_customization_code/SOPackageDetail_MasterPackUnitsExt.cs`.
   Add only this one file; leave the project's other files as they are.
3. Save and **Publish**.

The file adds a read-only, calculated field (no database column). It needs the Master Pack package (`WMS`)
to be installed, which it already is.

### 2-5. Label records
For each ZIP, open the screen, open the clipboard menu, choose **Import ALL as ZIP**, and pick the file.
The result message should say "1 file has been imported".

| Step | Screen | File |
|---|---|---|
| 2 | Barcodes (AL204000) | `2_ALBarcode-MasterPack.zip` |
| 3 | Substitutions (AL206500) | `3_ALSubstitution-MasterPack.zip` |
| 4 | Data Elements | `4_ALDataElement-MasterPack-201-211.zip` |
| 5 | Label Models (AL201000) | `5_ALModel-masterpack_KOHLS_128.zip` |

## Check after the import

Open the label on **Label Models (AL201000)**:
- **Enabled when** (filter rule) shows **CustomerIsKohls**. If it is blank, select `CustomerIsKohls`.
- **Prints when** (print rule) shows **SO302000-Packages-is-Master**, with **Not** unchecked. If it is blank, the
  rule is missing: create it on **Rules (AL203500)** with the expression `Packages.UsrIsParentBox == 'true'`
  (screen SO302000), then select it.

Render or print a Kohl's master carton and check:
- FOR block: `(91) 000875`-style readable (6 digits), the barcode, and the location number centered on the right.
- **Units** shows the master carton's own quantity (for example 8 for a master carton holding cartons of 4 + 2 + 2),
  not the shipment total.
- Dept, BOL, Invoice No. and the invoice barcode show the shipment's values; Pro# shows the master carton's tracking number.
- A regular (non-master) Kohl's carton does **not** get this label.

Then confirm nothing shared was lost: on **Substitutions**, `JOIN_BY_SPACE` and `Prepend-420-Readable` still exist,
and another packing label (for example the regular JCPenney or Kohl's label) still renders normally.

## What this does not change

- The shared postal barcode data element used by the other labels (line 154) is **not** touched.
  The Kohl's master pack uses its own (line 211).
- No other label model is imported or modified.

## Data the shipment needs

| Label field | Where it comes from |
|---|---|
| Carrier | Shipment, User Defined Field 1 |
| Pro# | Master carton package, Tracking Number |
| Bill of Lading, Invoice No., invoice barcode | Shipment, User Defined Field 3 (numeric, up to 9 digits fits) |
| Dept | Shipment, EDI Department Number; if empty, User Defined Field 4 |
| FOR / Mark For | Shipment, customer Location ID |
| Units | Master Pack Units (calculated from the cartons assigned to the master carton) |

## Undo

To stop the label from printing, clear **Active** on the label in Label Models (AL201000).
Removing the customization file (step 1) also requires changing the Units row, which uses it.
