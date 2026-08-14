import path from "node:path";
import fs from "node:fs/promises";
import { spawn } from "node:child_process";
import ExcelJS from "exceljs";
import { createReadStream } from "node:fs";
import { parse } from "csv-parse";
import "dotenv/config";

const TEMPLATE_PATH = path.resolve(process.env.BOL_TEMPLATE_PATH || "templates/BOL_Template.xlsx");
const OUTPUT_DIRECTORY = path.resolve(process.env.BOL_OUTPUT_DIRECTORY || "output");
const LIBREOFFICE_PATH = process.env.LIBREOFFICE_PATH || "libreoffice";

/**
 * Resolves the CSV input path from the command line, falling back to .env.
 * Supports:
 *   node bol_generator_wip.js "path/to/file.csv"
 *   node bol_generator_wip.js --csv "path/to/file.csv"
 * @returns {string} Absolute path to the CSV file
 */
function resolveCsvPath() {
  const args = process.argv.slice(2);

  const flagIndex = args.indexOf("--csv");
  const flagValue = flagIndex !== -1 ? args[flagIndex + 1] : null;
  const positionalValue = args.find((arg) => !arg.startsWith("--"));

  const csvPath = flagValue || positionalValue || process.env.BOL_CSV_INPUT_PATH;

  if (!csvPath) {
    throw new Error(
      "No CSV input provided. Pass it as an argument " +
        '(node bol_generator_wip.js "path/to/file.csv" or --csv "path/to/file.csv") ' +
        "or set BOL_CSV_INPUT_PATH in .env.",
    );
  }

  return path.resolve(csvPath);
}

const ACUMATICA_BASE_URL = requireEnv("ACUMATICA_BASE_URL");
const ACUMATICA_USERNAME = requireEnv("ACUMATICA_USERNAME");
const ACUMATICA_PASSWORD = requireEnv("ACUMATICA_PASSWORD");
const ACUMATICA_COMPANY = process.env.ACUMATICA_COMPANY || "";

const ACUMATICA_LOGIN_ENDPOINT = `${ACUMATICA_BASE_URL}/entity/auth/login`;
const ACUMATICA_LOGOUT_ENDPOINT = `${ACUMATICA_BASE_URL}/entity/auth/logout`;
const ACUMATICA_ENDPOINT = `${ACUMATICA_BASE_URL}/entity/iStarBOL/25.200.001/BOLShipmentInquiry?$expand=BOLShipmentInquiryDetails`;

/**
 * Reads a required environment variable or throws a clear error.
 * @param {string} name
 * @returns {string}
 */
function requireEnv(name) {
  const value = process.env[name];
  if (!value) {
    throw new Error(
      `Missing required environment variable: ${name}. Check your .env file.`,
    );
  }
  return value;
}

// ============================================================================
// DATA LOADING & PARSING
// ============================================================================

/**
 * Reads and parses the CSV file into an array of order objects.
 * @returns {Promise<Array>} Array of parsed CSV rows
 */
async function loadCsvData(csvPath) {
  return new Promise((resolve, reject) => {
    const records = [];
    const parser = parse({
      columns: true,
      skip_empty_lines: true,
    });

    parser.on("readable", function () {
      let record;
      while ((record = parser.read()) !== null) {
        records.push(record);
      }
    });

    parser.on("error", (err) => reject(err));
    parser.on("end", () => resolve(records));

    createReadStream(csvPath).pipe(parser);
  });
}

/**
 * Logs into Acumatica using username/password and returns the session cookie
 * to attach to subsequent requests.
 * @returns {Promise<string>} Cookie header value (e.g. ".ASPXAUTH=...; ...")
 */
async function acumaticaLogin() {
  const response = await fetch(ACUMATICA_LOGIN_ENDPOINT, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
    },
    body: JSON.stringify({
      name: ACUMATICA_USERNAME,
      password: ACUMATICA_PASSWORD,
      ...(ACUMATICA_COMPANY ? { company: ACUMATICA_COMPANY } : {}),
    }),
  });

  if (!response.ok) {
    const bodyText = await response.text().catch(() => "");
    throw new Error(
      `Acumatica login failed: ${response.status} ${response.statusText}\n${bodyText}`,
    );
  }

  const setCookieHeader = response.headers.get("set-cookie");
  if (!setCookieHeader) {
    throw new Error("Acumatica login succeeded but no session cookie was returned.");
  }

  // Node's fetch may combine multiple Set-Cookie values with a comma; split and
  // keep only the "name=value" portion of each cookie for the outgoing header.
  const cookiePairs = setCookieHeader
    .split(/,(?=[^;]+?=)/)
    .map((c) => c.split(";")[0].trim())
    .filter(Boolean);

  return cookiePairs.join("; ");
}

/**
 * Logs out of the Acumatica session (best-effort cleanup).
 * @param {string} sessionCookie
 */
async function acumaticaLogout(sessionCookie) {
  try {
    await fetch(ACUMATICA_LOGOUT_ENDPOINT, {
      method: "POST",
      headers: { Cookie: sessionCookie },
    });
  } catch {
    // Non-fatal; session will expire on its own.
  }
}

/**
 * Calls the Acumatica BOLShipmentInquiry API for a given customer order number.
 * Endpoint/contract: BOL_Generator_API_Contract_v0.1.md
 * @param {string} customerOrderNbr - e.g. "10001971908-3811"
 * @param {string} sessionCookie - Cookie header value from acumaticaLogin()
 * @returns {Promise<Object>} Flattened shipment detail fields
 */
async function fetchAcumaticaShipmentData(customerOrderNbr, sessionCookie) {
  const response = await fetch(ACUMATICA_ENDPOINT, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      Cookie: sessionCookie,
    },
    body: JSON.stringify({
      Customer_order_NBR: {
        value: customerOrderNbr,
      },
    }),
  });

  if (!response.ok) {
    const bodyText = await response.text().catch(() => "");
    throw new Error(
      `Acumatica API error for order ${customerOrderNbr}: ${response.status} ${response.statusText}\n${bodyText}`,
    );
  }

  const data = await response.json();
  const detail = data.BOLShipmentInquiryDetails?.[0];

  if (!detail) {
    throw new Error(`No BOLShipmentInquiryDetails found for order: ${customerOrderNbr}`);
  }

  // Acumatica returns each field as { value: ... }; unwrap them here.
  return {
    ShipmentNbr: detail.ShipmentNbr?.value ?? null,
    CustomerOrderNbr: detail.CustomerOrderNbr?.value ?? customerOrderNbr,
    BOLNumber: detail.BOLNumber?.value ?? null,
    CustomerName: detail.CustomerName?.value ?? null,
    LocationName: detail.LocationName?.value ?? null,
    AddressLine1: detail.AddressLine1?.value ?? null,
    City: detail.City?.value ?? null,
    State: detail.State?.value ?? null,
    PostalCode: detail.PostalCode?.value ?? null,
    Packages: detail.Packages?.value ?? null,
    ShippedWeight: detail.ShippedWeight?.value ?? null,
    ShipViaDescription: detail.ShipViaDescription?.value ?? null,
    Status: detail.Status?.value ?? null,
  };
}

/**
 * Resolves the Customer Order Nbr for a CSV row. If the CSV has a
 * non-empty "Customer Order Nbr" column, that value is used as-is
 * (verbatim override). Otherwise it's computed as
 * "Purchase Order Number" + "-" + zero-padded "Destination".
 * @param {Object} csvRow
 * @returns {string}
 */
function resolveCustomerOrderNbr(csvRow) {
  const override = csvRow["Customer Order Nbr"];
  if (override && String(override).trim() !== "") {
    return String(override).trim();
  }
  return `${csvRow["Purchase Order Number"]}-${String(csvRow["Destination"]).padStart(4, "0")}`;
}

/**
 * Transforms CSV row + Acumatica API response into a normalized order object.
 * @param {Object} csvRow - Single row from CSV
 * @param {Object} apiData - Response from Acumatica API
 * @returns {Object} Normalized order data
 */
function enrichOrderData(csvRow, apiData) {
  const customerOrderNbr = resolveCustomerOrderNbr(csvRow);

  return {
    csvRow,
    apiData,
    customerOrderNbr,
    bolNumber: apiData.BOLNumber || "NO_BOL_NUM_FOUND",
    cartons: parseInt(csvRow["Cartons"], 10),
    weight: Math.round(apiData.ShippedWeight ?? parseFloat(csvRow["Weight"])),
    loadNumber: csvRow["Load Number"],
    scac: csvRow["Assigned SCAC"],
    proNumber: csvRow["PRO"],
    shipVia: apiData.ShipViaDescription || csvRow["Vendor Name"],
    destination: csvRow["Destination"],
    shipToName: apiData.LocationName,
    shipToAddress: apiData.AddressLine1,
    shipToCity: apiData.City,
    shipToState: apiData.State,
    shipToZip: apiData.PostalCode,
  };
}

// ============================================================================
// EXCEL WORKBOOK SETUP
// ============================================================================

/**
 * Loads and prepares the BOL template workbook.
 * @returns {Promise<ExcelJS.Workbook>}
 */
async function loadTemplateWorkbook() {
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(TEMPLATE_PATH);
  return workbook;
}

/**
 * Configures page setup and print area for a worksheet.
 * @param {ExcelJS.Worksheet} worksheet
 * @param {string} printArea - Excel range (e.g., "A1:M49")
 */
function configureWorksheetPrinting(worksheet, printArea = "A1:M49") {
  worksheet.pageSetup = {
    paperSize: 1, // US Letter
    orientation: "portrait",
    fitToPage: true,
    fitToWidth: 1,
    fitToHeight: 1,
    horizontalCentered: true,
    verticalCentered: false,
    margins: {
      left: 0.2,
      right: 0.2,
      top: 0.2,
      bottom: 0.2,
      header: 0,
      footer: 0,
    },
  };
  worksheet.pageSetup.printArea = printArea;
}

// ============================================================================
// MASTER BOL POPULATION
// ============================================================================

function formatSystemDate() {
  const today = new Date();
  return `Date: ${String(today.getMonth() + 1).padStart(2, "0")}/${String(today.getDate()).padStart(2, "0")}/${today.getFullYear()}`;
}

/**
 * Excel worksheet names must be <= 31 chars and cannot contain: \ / ? * [ ] :
 * Builds a short, unique, valid sheet name for a given order, using an
 * incrementing counter to guarantee uniqueness even if truncation collides.
 * @param {ExcelJS.Workbook} workbook
 * @param {string} baseLabel - Desired label (e.g. order number), pre-truncation
 * @returns {string} A valid, unique worksheet name
 */
function generateUniqueSheetName(workbook, baseLabel) {
  const MAX_LENGTH = 31;
  const sanitized = String(baseLabel).replace(/[\\/?*[\]:]/g, "-");
  const prefix = "Supp_BOL_";
  let candidate = `${prefix}${sanitized}`.slice(0, MAX_LENGTH);

  let counter = 1;
  while (workbook.getWorksheet(candidate)) {
    const suffix = `_${counter}`;
    const truncatedBase = `${prefix}${sanitized}`.slice(0, MAX_LENGTH - suffix.length);
    candidate = `${truncatedBase}${suffix}`;
    counter++;
  }

  return candidate;
}

/**
 * Clones a worksheet's layout (columns, rows, cell values/styles, merged
 * cells, page setup) into a new worksheet. exceljs has no built-in "clone"
 * API, so this copies everything by hand.
 * @param {ExcelJS.Workbook} workbook
 * @param {string} sourceSheetName - Name of the sheet to clone from
 * @param {string} newSheetName - Name for the new sheet
 * @returns {ExcelJS.Worksheet}
 */
function cloneWorksheet(workbook, sourceSheetName, newSheetName) {
  const sourceSheet = workbook.getWorksheet(sourceSheetName);
  if (!sourceSheet) {
    throw new Error(`Cannot clone: source sheet "${sourceSheetName}" not found`);
  }

  const newSheet = workbook.addWorksheet(newSheetName, {
    properties: { ...sourceSheet.properties },
    pageSetup: { ...sourceSheet.pageSetup },
    views: sourceSheet.views,
  });

  // Copy column widths/styles
  newSheet.columns = sourceSheet.columns.map((col) => ({
    width: col.width,
    style: col.style,
  }));

  // Copy every row: values, styles, and height
  sourceSheet.eachRow({ includeEmpty: true }, (row, rowNumber) => {
    const newRow = newSheet.getRow(rowNumber);
    row.eachCell({ includeEmpty: true }, (cell, colNumber) => {
      const newCell = newRow.getCell(colNumber);
      newCell.value = cell.value;
      newCell.style = cell.style;
    });
    newRow.height = row.height;
    newRow.commit();
  });

  // Copy merged cell ranges
  const merges = sourceSheet.model.merges || [];
  merges.forEach((range) => {
    newSheet.mergeCells(range);
  });

  return newSheet;
}

/**
 * Populates the MasterBOL sheet with the first 8 orders (rows 22-29).
 * Grand total cells reflect ALL orders across MasterBOL + Supplemental_Master_BOL
 * combined, not just this sheet's own 8 rows.
 * @param {ExcelJS.Worksheet} worksheet - MasterBOL sheet
 * @param {Array<Object>} orders - Array of enriched order objects
 * @param {number} grandTotalCartons - Combined total packages across all sheets
 * @param {number} grandTotalWeight - Combined total weight across all sheets
 * @returns {Array<Object>} Remaining orders that didn't fit (8+)
 */
function populateMasterBOL(worksheet, orders, grandTotalCartons, grandTotalWeight) {
  const masterBolCapacity = 8; // Rows 22-29
  const ordersForMaster = orders.slice(0, masterBolCapacity);
  const overflowOrders = orders.slice(masterBolCapacity);

  worksheet.getCell("B1").value = formatSystemDate();

  const firstOrder = ordersForMaster[0];
  if (firstOrder) {
    worksheet.getCell("I4").value = firstOrder.csvRow["Purchase Order Number"]; // Master Bill of Lading Number
    worksheet.getCell("K7").value = firstOrder.shipVia;      // Carrier name
    worksheet.getCell("J10").value = firstOrder.scac;         // SCAC
    worksheet.getCell("K11").value = firstOrder.proNumber;    // Pro number
    worksheet.getCell("F16").value = firstOrder.loadNumber;   // Target Load ID
  }

  // Customer orders (rows 22-29)
  ordersForMaster.forEach((order, index) => {
    const rowNum = 22 + index;
    worksheet.getCell(`A${rowNum}`).value = order.bolNumber;
    worksheet.getCell(`E${rowNum}`).value = order.cartons;
    worksheet.getCell(`F${rowNum}`).value = order.weight;
    worksheet.getCell(`G${rowNum}`).value = order.weight;    // Duplicate column
    worksheet.getCell(`H${rowNum}`).value = "Y";             // Pallet indicator
  });

  // Grand totals (row 30) - combined across MasterBOL + Supplemental_Master_BOL
  worksheet.getCell("E30").value = grandTotalCartons;
  worksheet.getCell("F30").value = grandTotalWeight;
  worksheet.getCell("G30").value = grandTotalWeight;

  // Carrier detail section (row 34) - reflects the same combined grand total
  worksheet.getCell("C34").value = grandTotalCartons;
  worksheet.getCell("E34").value = grandTotalWeight;

  // Carrier detail totals (row 42) - sums any additional line items on rows 34-41
  worksheet.getCell("C42").value = { formula: "SUM(C34:C41)" };
  worksheet.getCell("E42").value = { formula: "SUM(E34:E41)" };

  configureWorksheetPrinting(worksheet, "A1:M49");

  return overflowOrders;
}

// ============================================================================
// SUPPLEMENTAL MASTER BOL POPULATION
// ============================================================================

/**
 * Creates and populates Supplemental_Master_BOL sheets for overflow orders.
 * If more than 45 orders remain (rows 5-49), creates multiple iterations.
 * Grand total cells reflect ALL orders across MasterBOL + Supplemental_Master_BOL
 * combined (same combined total written to every iteration), not just the
 * rows on that particular sheet.
 * @param {ExcelJS.Workbook} workbook
 * @param {Array<Object>} orders - Overflow orders from MasterBOL
 * @param {number} grandTotalCartons - Combined total packages across all sheets
 * @param {number} grandTotalWeight - Combined total weight across all sheets
 */
function populateSupplementalMasterBOLs(workbook, orders, grandTotalCartons, grandTotalWeight) {
  if (orders.length === 0) return;

  const supplementalCapacity = 45; // Rows 5-49
  let iterationNum = 1;
  let ordersRemaining = [...orders];

  while (ordersRemaining.length > 0) {
    const ordersForThisSheet = ordersRemaining.slice(0, supplementalCapacity);
    ordersRemaining = ordersRemaining.slice(supplementalCapacity);

    let sheet;
    if (iterationNum === 1) {
      sheet = workbook.getWorksheet("Supplemental_Master_BOL");
    } else {
      sheet = cloneWorksheet(
        workbook,
        "Supplemental_Master_BOL",
        `Supp_Master_BOL_${iterationNum}`,
      );
    }

    if (!sheet) {
      throw new Error("Supplemental_Master_BOL template sheet not found");
    }

    sheet.getCell("B1").value = formatSystemDate();

    // Populate order rows (starting at row 5)
    ordersForThisSheet.forEach((order, index) => {
      const rowNum = 5 + index;
      sheet.getCell(`A${rowNum}`).value = order.bolNumber;
      sheet.getCell(`E${rowNum}`).value = order.cartons;
      sheet.getCell(`F${rowNum}`).value = order.weight;
      sheet.getCell(`G${rowNum}`).value = order.weight;
    });

    // Grand totals (row 50) - combined across MasterBOL + Supplemental_Master_BOL
    sheet.getCell("E50").value = grandTotalCartons;
    sheet.getCell("F50").value = grandTotalWeight;

    configureWorksheetPrinting(sheet, "A1:G50");

    iterationNum++;
  }
}

// ============================================================================
// SUPPLEMENTAL BOL POPULATION (PER-ORDER)
// ============================================================================

/**
 * Creates and populates one Supplemental_BOL sheet per order.
 * @param {ExcelJS.Workbook} workbook
 * @param {Array<Object>} orders - All orders (will create one sheet per order)
 */
function populateSupplementalBOLs(workbook, orders) {
  orders.forEach((order) => {
    const templateSheet = workbook.getWorksheet("Supplemental_BOL");
    if (!templateSheet) {
      throw new Error("Supplemental_BOL template sheet not found");
    }

    const sheetName = generateUniqueSheetName(workbook, order.customerOrderNbr);
    const newSheet = cloneWorksheet(workbook, "Supplemental_BOL", sheetName);

    newSheet.getCell("B1").value = formatSystemDate();

    // Ship To fields
    newSheet.getCell("B8").value = order.shipToName;
    newSheet.getCell("B9").value = order.shipToAddress;
    newSheet.getCell("C10").value = `${order.shipToCity}, ${order.shipToState} ${order.shipToZip}`;

    // Target Load ID
    newSheet.getCell("F16").value = order.loadNumber;

    // Carrier information
    newSheet.getCell("K7").value = order.shipVia;
    newSheet.getCell("J10").value = order.scac;
    newSheet.getCell("K11").value = order.proNumber;

    // Bill of Lading Number (rendered as a Code 39 barcode via the
    // IDAutomationHC39M Free Version font, which also shows the
    // human-readable number below the bars; parentheses hide the
    // start/stop asterisks from the readable text)
    newSheet.getCell("I4").value = `(${order.bolNumber})`;
    newSheet.getCell("I4").font = { name: "IDAutomationHC39M Free Version", size: 13 };
    // I4 is merged across rows 4-6 (template default totals only ~39pt:
    // 12.75 + 12.75 + 13.5). A 24pt HC-variant Code 39 glyph draws bars
    // AND a human-readable text line stacked in one glyph, which needs
    // noticeably more vertical room than plain 24pt text — bump the merged
    // region's total height so LibreOffice's PDF export doesn't spill the
    // glyph into row 7. Font size reduced from 24 to 14 so the smaller
    // human-readable text line (drawn beneath the bars, at a fixed
    // proportion of the barcode's overall glyph height) isn't squeezed out
    // of the available vertical space.
    newSheet.getRow(4).height = 22;
    newSheet.getRow(5).height = 22;
    newSheet.getRow(6).height = 22;

    // Single customer order row (row 22)
    newSheet.getCell("A22").value = order.customerOrderNbr;
    newSheet.getCell("E22").value = order.cartons;
    newSheet.getCell("F22").value = order.weight;
    newSheet.getCell("G22").value = order.weight;
    newSheet.getCell("H22").value = "Y"; // Pallet indicator

    // Grand totals (row 30)
    newSheet.getCell("E30").value = order.cartons;
    newSheet.getCell("F30").value = order.weight;

    // Carrier detail section (row 34) - both A34 and C34 get package qty per mapping
    newSheet.getCell("A34").value = order.cartons;
    newSheet.getCell("C34").value = order.cartons;
    newSheet.getCell("E34").value = order.weight;

    // Carrier detail totals (row 42) - sums any additional line items on rows 34-41
    newSheet.getCell("A42").value = { formula: "SUM(A34:A41)" };
    newSheet.getCell("C42").value = { formula: "SUM(C34:C41)" };
    newSheet.getCell("E42").value = { formula: "SUM(E34:E41)" };

    configureWorksheetPrinting(newSheet, "A1:M49");
  });
}

// ============================================================================
// EXCEL WRITE & PDF CONVERSION
// ============================================================================

async function convertToPdf(excelPath, outputDirectory) {
  return new Promise((resolve, reject) => {
    const proc = spawn(
      LIBREOFFICE_PATH,
      [
        "--headless",
        "--nologo",
        "--nodefault",
        "--nolockcheck",
        "--nofirststartwizard",
        "--convert-to",
        "pdf:calc_pdf_Export",
        "--outdir",
        outputDirectory,
        excelPath,
      ],
      { stdio: ["ignore", "pipe", "pipe"] },
    );

    let stdout = "";
    let stderr = "";

    proc.stdout.on("data", (data) => {
      stdout += data.toString();
    });

    proc.stderr.on("data", (data) => {
      stderr += data.toString();
    });

    proc.on("error", reject);

    proc.on("close", async (exitCode) => {
      if (exitCode !== 0) {
        reject(new Error(`LibreOffice failed with exit code ${exitCode}.\n${stderr}`));
        return;
      }

      const expectedPdf = path.join(
        outputDirectory,
        `${path.parse(excelPath).name}.pdf`,
      );

      try {
        await fs.access(expectedPdf);
        resolve({ pdfPath: expectedPdf, stdout, stderr });
      } catch {
        reject(
          new Error(
            `LibreOffice did not create the expected PDF:\n${expectedPdf}\n` +
              `stdout: ${stdout}\nstderr: ${stderr}`,
          ),
        );
      }
    });
  });
}

// ============================================================================
// MAIN ORCHESTRATION
// ============================================================================

async function main() {
  try {
    console.log("Starting BOL generation...");

    await fs.mkdir(OUTPUT_DIRECTORY, { recursive: true });

    const csvPath = resolveCsvPath();
    console.log(`Loading CSV data from ${csvPath}...`);
    const csvRecords = await loadCsvData(csvPath);
    console.log(`Loaded ${csvRecords.length} orders from CSV`);

    console.log("Logging into Acumatica...");
    const sessionCookie = await acumaticaLogin();

    let enrichedOrders;
    const skippedOrders = [];
    try {
      console.log("Fetching Acumatica data for each order...");
      enrichedOrders = [];
      for (const csvRow of csvRecords) {
        const customerOrderNbr = resolveCustomerOrderNbr(csvRow);
        try {
          const apiData = await fetchAcumaticaShipmentData(customerOrderNbr, sessionCookie);
          console.log(`\n--- Raw Acumatica response for ${customerOrderNbr} ---`);
          console.log(JSON.stringify(apiData, null, 2));

          const enriched = enrichOrderData(csvRow, apiData);
          console.log(`--- Enriched fields for ${customerOrderNbr} ---`);
          console.log({
            bolNumber: enriched.bolNumber,
            shipToName: enriched.shipToName,
            shipToAddress: enriched.shipToAddress,
            shipToCity: enriched.shipToCity,
            shipToState: enriched.shipToState,
            shipToZip: enriched.shipToZip,
            shipVia: enriched.shipVia,
            scac: enriched.scac,
            proNumber: enriched.proNumber,
            loadNumber: enriched.loadNumber,
            cartons: enriched.cartons,
            weight: enriched.weight,
          });

          enrichedOrders.push(enriched);
        } catch (orderError) {
          console.warn(`  Skipping order ${customerOrderNbr}: ${orderError.message}`);
          skippedOrders.push({ customerOrderNbr, reason: orderError.message });
        }
      }
    } finally {
      await acumaticaLogout(sessionCookie);
    }

    console.log(`Resolved ${enrichedOrders.length} of ${csvRecords.length} orders (${skippedOrders.length} skipped).`);

    if (enrichedOrders.length === 0) {
      throw new Error("No orders could be resolved via Acumatica; nothing to generate.");
    }

    console.log("Loading Excel template...");
    const workbook = await loadTemplateWorkbook();

    console.log("Populating MasterBOL sheet...");
    const masterBolSheet = workbook.getWorksheet("MasterBOL");
    if (!masterBolSheet) {
      throw new Error('Worksheet "MasterBOL" not found in template');
    }

    // Grand total across MasterBOL + Supplemental_Master_BOL combined (all orders).
    const grandTotalCartons = enrichedOrders.reduce((sum, o) => sum + o.cartons, 0);
    const grandTotalWeight = enrichedOrders.reduce((sum, o) => sum + o.weight, 0);

    const overflowOrders = populateMasterBOL(masterBolSheet, enrichedOrders, grandTotalCartons, grandTotalWeight);

    if (overflowOrders.length > 0) {
      console.log(`Populating Supplemental_Master_BOL sheet(s) with ${overflowOrders.length} overflow orders...`);
      populateSupplementalMasterBOLs(workbook, overflowOrders, grandTotalCartons, grandTotalWeight);
    }

    console.log(`Creating ${enrichedOrders.length} individual Supplemental_BOL sheets...`);
    populateSupplementalBOLs(workbook, enrichedOrders);

    // Remove reference/template sheets that aren't part of the printable BOL
    // output. Note: we REMOVE (not just hide) these, since LibreOffice's
    // headless PDF conversion does not reliably respect Excel's hidden-sheet
    // flag and will print hidden sheets anyway.
    const sheetsToRemove = ["ADDRESS", "Customer Order Info", "Supplemental_BOL"];
    if (overflowOrders.length === 0) {
      sheetsToRemove.push("Supplemental_Master_BOL");
    }
    for (const name of sheetsToRemove) {
      const sheet = workbook.getWorksheet(name);
      if (sheet) workbook.removeWorksheet(sheet.id);
    }

    const timestamp = Date.now();
    const outputExcelPath = path.join(OUTPUT_DIRECTORY, `BOL_Generated_${timestamp}.xlsx`);
    console.log(`Saving Excel workbook to ${outputExcelPath}...`);
    await workbook.xlsx.writeFile(outputExcelPath);

    console.log("Converting to PDF...");
    const pdfResult = await convertToPdf(outputExcelPath, OUTPUT_DIRECTORY);

    console.log("\nBOL generation complete!");
    console.log(`Excel: ${outputExcelPath}`);
    console.log(`PDF: ${pdfResult.pdfPath}`);
    console.log(`Orders included: ${enrichedOrders.length} of ${csvRecords.length}`);
    if (skippedOrders.length > 0) {
      console.log(`\nSkipped orders (${skippedOrders.length}):`);
      for (const { customerOrderNbr, reason } of skippedOrders) {
        console.log(`  - ${customerOrderNbr}: ${reason}`);
      }
    }
  } catch (error) {
    console.error("Error during BOL generation:", error.message);
    console.error(error);
    process.exitCode = 1;
  }
}

main();
