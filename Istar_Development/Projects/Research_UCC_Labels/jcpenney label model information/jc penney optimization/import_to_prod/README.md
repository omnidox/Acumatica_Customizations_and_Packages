# JCPenney UCC-128 carton label: production import

Updates the production label **iStar-8A-Packing for JCPenny** to the version reviewed against JCPenney's GS1-128
evaluation (T64600) and tested in the local SnapshotTest instance.

## What changes on the label

| Area | Before (production) | After |
|---|---|---|
| Ship-to postal barcode (420) | Shared postal barcode element | Label-owned element 189 (GS1-128, 100 dots) |
| Mark For (91) | Store/DC as stored | DC padded to 6 digits with leading zeros (5-digit values only; never truncated), readable `(91) 0xxxxx` and a new 305-dot (1.5 in) GS1-128 barcode |
| SSCC-18 (00) | Previous elements | Readable in JCPenney's spaced format `(00) 0 0614611 000000004 7` and a 305-dot GS1-128 barcode |
| From block | Stanley Creations, Inc. | **SGG INC** |
| New fixed text | | `080267` (vendor number), `- PACKING LIST INSIDE -` |
| Layout | | Positions, fonts (0-030, 0-110) and section lines adjusted to the JCPenney layout; 2 old rows switched off |

The label keeps production's own IDs: the label itself, its customer rule ("Enabled when") and the print rule
`SO302000-Packages-is-Master`. Nothing needs re-selecting after the import.

## What is in this folder

| Step | File | Screen | What it adds or updates |
|---|---|---|---|
| 1 | `1_ALBarcode-JCP-GS1-Code128-305-and-100.zip` | **Barcodes** (AL204000) | `GS1-Code128-305-NoHRI` (new); `GS1-Code128-100-NoHRI` (already in production; included so it is restored if missing, values identical) |
| 2 | `2_ALSubstitution-JCP.zip` | **Substitutions** (AL206500) | 3 new: `JCP-SSCC-00-Paren-Readable`, `JCP-MarkFor-Pad6-91-Barcode-1step`, `JCP-MarkFor-Pad6-91-Readable-1step`. 3 shared, identical to production, included as a safety restore: `Prepend-420-Readable`, `Prepend-420-Barcode`, `JOIN_BY_SPACE` |
| 3 | `3_ALDataElement-JCP-189-192-196-197-198.zip` | **Data Elements** | 5 new data elements: ship-to postal (189), SSCC barcode (192) and readable (196), Mark For barcode (197) and readable (198) |
| 4 | `4_ALModel-iStar-8A-Packing for JCPenny.zip` | **Label Models** (AL201000) | The label. It also carries font `0-110` and justification `C-000-061`, which the import creates |

Do the steps **in order**: each file uses records created by the one before it.

## Before you start

1. **Set the import mode.** Open **Label Basic Preferences (AL101000)** and set **Record Import Mode** to
   **Insert Update Intersection**, then save. In this mode an import only inserts and updates; it never deletes.
   With the other modes, an import deletes linked records that are not in the file. That has deleted shared barcodes
   and substitutions before; the missing `Code128-100-noHRI` barcode on the Target label is likely an example.
2. **Use "Import ALL as ZIP" for every file.** Do **not** use "Import from XML": it always replaces, whatever the
   setting above says.
3. **Backup:** export the current label first (Label Models > `iStar-8A-Packing for JCPenny` > clipboard menu >
   Export as XML). `../Orginal_Template_Model/` holds the production export from Sep 30.

No customization code is needed.

## Steps

For each ZIP, open the screen, open the clipboard menu, choose **Import ALL as ZIP**, and pick the file.
The result message should say "1 file has been imported".

| Step | Screen | File |
|---|---|---|
| 1 | Barcodes (AL204000) | `1_ALBarcode-JCP-GS1-Code128-305-and-100.zip` |
| 2 | Substitutions (AL206500) | `2_ALSubstitution-JCP.zip` |
| 3 | Data Elements | `3_ALDataElement-JCP-189-192-196-197-198.zip` |
| 4 | Label Models (AL201000) | `4_ALModel-iStar-8A-Packing for JCPenny.zip` |

## Check after the import

Open **iStar-8A-Packing for JCPenny** on Label Models (AL201000):
- **Enabled when** and **Prints when** (`SO302000-Packages-is-Master`) show the same rules as before.
- Row 3 reads **SGG INC**.

Print the label for a JCPenney carton and check:
- Ship to Postal Code: `(420) xxxxx` readable and its barcode.
- Mark For: `(91) 0xxxxx` (6 digits) readable and a tall barcode; the DC number in large type.
- SSCC: `(00) 0 0614611 000000004 7` style readable and a tall barcode at the bottom.
- `080267` and `- PACKING LIST INSIDE -` appear.
- Scan the three barcodes: (420) postal, (91) Mark For with the 6-digit value, (00) 18-digit SSCC.

Also confirm nothing shared was lost: another packing label (for example Target or Kohl's) still prints normally.

## Undo

1. On Label Models, import `../Orginal_Template_Model/ALModel-iStar-8A-Packing for JCPenny.xml` with
   **Import from XML**. Import from XML always replaces, which is what you want here. It restores the production
   label exactly as exported on Sep 30, including removing the 3 new text rows.
2. The new barcode, substitutions and data elements can stay; nothing else uses them.

## Notes

- The (91) readable text is padded to 6 digits, to match the barcode. In the Asgard email thread (Sep 25), Wayne
  suggested padding only the barcode data and leaving the readable text unpadded. This package does not change
  that; review before import if needed.
- Source of every record: the local SnapshotTest company, where the label renders correctly. All shared records
  were compared with production exports and are identical.
