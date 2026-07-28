Yes—this significantly clarifies the project, and it changes the scope in an important way.

Your conclusion is essentially correct: **replacing RFGen’s BOL generation requires replacing both the document-generation behavior and the BOL-number assignment behavior.** However, based on Gal’s email and the SOP, RFGen appears to generate **both** the individual BOL numbers and the numbered blank BOL used as the Master BOL.

## What the current process appears to be

### 1. Target creates the transportation-routing data

The original file comes from ShipIQ in Target’s Partners Online portal. ShipIQ is Target’s application for domestic collect shipments; suppliers review shipment details such as cartons, weight, cube, and pallet space, after which Target routes the shipment and assigns pickup information. ([SPS Commerce][1])

The original CSV confirms that ShipIQ supplies data such as:

* Purchase Order Number
* Shipment ID
* Load Number
* Assigned SCAC
* Base PRO number
* Destination
* Pickup Date
* Cartons
* Weight
* Cube
* Pallet Spaces

Importantly, its `bol` column is blank. Therefore, **ShipIQ is not supplying the final BOL numbers in this example.**

### 2. Gal processes the ShipIQ export

I compared the two CSV files. Both contain exactly **29 destination rows**, all associated with:

* PO `10001971908`
* Load Number `76298131`
* SCAC `RBTW`
* Base PRO `560806310`

The processed file is not merely a cleaned export. Gal appears to perform several transformations:

* Sorts the rows by Destination.
* Removes fields that RFGen does not require.
* Adds a separate `DC Code`.
* Retains the Target Destination number.
* Converts the shared base PRO into a unique value for each destination:

  * `560806310`
  * `560806310A`
  * `560806310B`
  * …
  * `560806310Z`
  * `560806310AA`
  * `560806310AB`

That tells us the processed file is effectively an **RFGen input worksheet** or operator preparation sheet.

It also reveals two business rules we still need to document precisely:

1. Where does the `DC Code` mapping come from?
2. Is appending letters to the PRO number a required business/carrier rule, or simply an internal technique for making each DC’s PRO unique?

Those rules will need to be automated or made editable.

## 3. RFGen generates each individual BOL

The SOP shows the operator entering:

* PO number
* Deliver-To number
* Carton count
* Total weight
* Carrier
* PRO number
* Freight terms
* Pallet information
* Additional POs for the same DC

RFGen then asks whether to print the BOL. The SOP also contains a reprint workflow based on customer, DC number, SCAC, number of POs, DC name, and freight terms. 

Gal’s email confirms that, after the operator submits the job, RFGen produces the individual BOL with a BOL number already assigned.

Therefore, replacing RFGen cannot consist only of populating a PDF template. The new application must also:

* Assign a unique individual BOL number.
* Persist the number.
* Associate it with the relevant customer, load, PO or POs, destination/DC, and generated document.
* Support reprinting without creating a new number.
* Prevent duplicate numbers during simultaneous requests or retries.

## 4. RFGen also appears to provide the Master BOL number

Gal said:

> When it comes time to make the Master Bill, I generate a blank BOL … and use the number on the blank bill as the number for the Master.

The SOP corroborates this:

* The operator selects **Print BLANK BOL**.
* Later, when filling the Master BOL workbook, the operator scans the individual BOLs and the blank BOL.
* The SOP instructs the operator to remove the leading `402` from the scanned sequence. 

So the current flow is likely:

```text
RFGen generates numbered individual BOLs
                    +
RFGen generates one numbered blank BOL
                    ↓
Blank BOL's number becomes Master BOL number
                    ↓
Numbers are entered/scanned into Master BOL Excel template
```

This means the Master BOL number is **not the Load ID** and is not directly assigned by Target. It is another RFGen-generated BOL identifier that the warehouse repurposes as the Master BOL number.

## The project now has four major functions

### A. Import and normalize routing data

Input:

```text
Target ShipIQ CSV
```

Output:

```text
Normalized transportation-load records
```

This includes:

* PO
* Load Number
* SCAC
* Destination
* DC Code
* Cartons
* Weight
* PRO
* Pickup date
* Shipment ID

### B. Prepare the RFGen-equivalent BOL input

The application must reproduce Gal’s current Excel processing:

* Filter the relevant load or pickup.
* Sort/group destinations.
* Add DC mappings.
* Generate destination-specific PRO references.
* Allow corrections before generation.
* Combine multiple POs when permitted for the same DC.

### C. Generate and persist BOL numbers

The system needs at least two identifiers:

```text
Individual BOL Number
Master BOL Number
```

These should probably come from the same numbering service or compatible numbering sequences, but we should not define the exact format until we understand RFGen’s existing number structure.

The leading `402` is especially important. Before duplicating the numbering logic, we should determine:

* Is `402` a GS1 application identifier, company prefix, barcode prefix, document type, or RFGen-specific prefix?
* How many digits comprise the actual BOL number?
* Is there a check digit?
* Does RFGen use a database sequence, JDE next-number table, SSCC-style numbering, or another algorithm?
* Must future numbers remain within the same historical sequence?
* Do carriers or customers validate the number format?

We should **not invent a new numbering format** until those questions are answered.

### D. Generate the individual and Master PDFs

Only after the records and numbers exist should the templates be populated.

The Excel-and-LibreOffice approach remains viable, but it is now only the final rendering stage:

```text
ShipIQ CSV
    ↓
Validation and transformation
    ↓
BOL records and numbering
    ↓
Populate individual BOL template(s)
    ↓
Populate Master BOL template
    ↓
LibreOffice headless conversion
    ↓
PDF packet
```

## Recommended first implementation

Your standalone approach is still the right first phase, but I would redefine the proof of concept.

### Phase 1A — Reproduce Gal’s CSV processing

Build a standalone screen that:

1. Uploads the ShipIQ CSV.
2. Displays the detected PO, Load Number, SCAC, destinations, cartons, and weights.
3. Applies the DC-code mapping.
4. Generates the suffixed PRO numbers.
5. Allows Gal to review and correct the rows.
6. Exports a result matching `07-17-2026 Target Pickup.csv`.

This establishes that we correctly understand his data-preparation work.

### Phase 1B — Generate documents with test numbers

Use clearly labeled test numbers, not production numbers:

```text
TEST-BOL-000001
TEST-MBOL-000001
```

Then populate the Excel templates and convert them using LibreOffice.

This validates rendering without risking duplicate operational BOL numbers.

### Phase 1C — Design the production numbering mechanism

Before activating production numbering, investigate RFGen/JDE’s current logic and historical records. Then implement:

* atomic sequence allocation;
* unique database constraint;
* BOL status;
* generation timestamp;
* generated-by user;
* reprint count;
* void/cancel behavior;
* original PDF retention;
* relationship to Load ID, PO, DC, and shipment.

### Phase 2 — Connect it to Acumatica

Once the standalone workflow is verified by Gal, Acumatica can send shipment/order identifiers to the service or launch the external screen with the relevant context.

## My overall assessment

The newly supplied information makes the project more concrete, but also somewhat larger than originally understood.

The project is not merely:

> Fill BOL templates from Acumatica.

It is closer to:

> Replace the Target BOL portion of an RFGen/JDE warehouse workflow, including import preparation, destination mapping, PRO assignment, BOL numbering, document generation, persistence, and reprinting.

The good news is that the two CSV files provide an unusually strong before-and-after example. They give us enough information to begin coding the **ShipIQ import and transformation portion immediately**. The next critical discovery should be the exact RFGen/JDE BOL-number format and sequence mechanism, because that is the one component we should not approximate.

[1]: https://www.spscommerce.com/community/articles/targets-new-process-shipiq?utm_source=chatgpt.com "Target's New Process for Collect Suppliers: ShipIQ"
