import path from "node:path";
import fs from "node:fs/promises";
import { spawn } from "node:child_process";
import ExcelJS from "exceljs";
import { createReadStream } from "node:fs";
import { parse } from "csv-parse";
import "dotenv/config";

const TEMPLATE_PATH = path.resolve(process.env.BOL_TEMPLATE_PATH || "templates/BOL_Template.xlsx");
const CSV_PATH = path.resolve(process.env.BOL_CSV_INPUT_PATH || "data/Target Original Values PO 10001971908.csv");
const OUTPUT_DIRECTORY = path.resolve(process.env.BOL_OUTPUT_DIRECTORY || "output");
const LIBREOFFICE_PATH = process.env.LIBREOFFICE_PATH || "libreoffice";

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
    ShipVia: detail.ShipVia?.value ?? null,
    Status: detail.Status?.value ?? null,
  };
}

/**
 * Transforms CSV row + Acumatica API response into a normalized order object.
 * @param {Object} csvRow - Single row from CSV
 * @param {Object} apiData - Response from Acumatica API
 * @returns {Object} Normalized order data
 */
function enrichOrderData(csvRow, apiData) {
  const customerOrderNbr = `${csvRow["Purchase Order Number"]}-${String(csvRow["Destination"]).padStart(4, "0")}`;

  return {
    csvRow,
    apiData,
    customerOrderNbr,
    bolNumber: apiData.BOLNumber || customerOrderNbr,
    cartons: parseInt(csvRow["Cartons"], 10),
    weight: apiData.ShippedWeight ?? parseFloat(csvRow["Weight"]),
    loadNumber: csvRow["Load Number"],
    scac: csvRow["Assigned SCAC"],
    proNumber: csvRow["PRO"],
    shipVia: apiData.ShipVia || csvRow["Vendor Name"],
    destination: csvRow["Destination"],
    shipToName: apiData.CustomerName,
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
 * Populates the MasterBOL sheet with the first 8 orders (rows 22-29).
 * Returns overflow orders for Supplemental_Master_BOL.
 * @param {ExcelJS.Worksheet} worksheet - MasterBOL sheet
 * @param {Array<Object>} orders - Array of enriched order objects
 * @returns {Array<Object>} Remaining orders that didn't fit (8+)
 */
function populateMasterBOL(worksheet, orders) {
  const masterBolCapacity = 8; // Rows 22-29
  const ordersForMaster = orders.slice(0, masterBolCapacity);
  const overflowOrders = orders.slice(masterBolCapacity);

  worksheet.getCell("B1").value = formatSystemDate();

  const firstOrder = ordersForMaster[0];
  if (firstOrder) {
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

  // Grand totals (row 30)
  const totalCartons = ordersForMaster.reduce((sum, o) => sum + o.cartons, 0);
  const totalWeight = ordersForMaster.reduce((sum, o) => sum + o.weight, 0);
  worksheet.getCell("E30").value = totalCartons;
  worksheet.getCell("F30").value = totalWeight;
  worksheet.getCell("G30").value = totalWeight;

  // Carrier detail section (row 34)
  worksheet.getCell("C34").value = totalCartons;
  worksheet.getCell("E34").value = totalWeight;

  configureWorksheetPrinting(worksheet, "A1:M49");

  return overflowOrders;
}

// ============================================================================
// SUPPLEMENTAL MASTER BOL POPULATION
// ============================================================================

/**
 * Creates and populates Supplemental_Master_BOL sheets for overflow orders.
 * If more than 45 orders remain (rows 5-49), creates multiple iterations.
 * @param {ExcelJS.Workbook} workbook
 * @param {Array<Object>} orders - Overflow orders from MasterBOL
 */
function populateSupplementalMasterBOLs(workbook, orders) {
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
      // TODO: clone formatting/merged cells from the template sheet properly
      sheet = workbook.addWorksheet(`Supplemental_Master_BOL_${iterationNum}`);
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

    // Grand totals (row 50)
    const totalCartons = ordersForThisSheet.reduce((sum, o) => sum + o.cartons, 0);
    const totalWeight = ordersForThisSheet.reduce((sum, o) => sum + o.weight, 0);
    sheet.getCell("E50").value = totalCartons;
    sheet.getCell("F50").value = totalWeight;

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

    // TODO: clone formatting/merged cells from the template sheet properly
    const newSheet = workbook.addWorksheet(`Supplemental_BOL_${order.customerOrderNbr}`);

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

    // Bill of Lading Number
    newSheet.getCell("I4").value = order.bolNumber;

    // Single customer order row (row 22)
    newSheet.getCell("A22").value = order.bolNumber;
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

    console.log("Loading CSV data...");
    const csvRecords = await loadCsvData(CSV_PATH);
    console.log(`Loaded ${csvRecords.length} orders from CSV`);

    console.log("Logging into Acumatica...");
    const sessionCookie = await acumaticaLogin();

    let enrichedOrders;
    try {
      console.log("Fetching Acumatica data for each order...");
      enrichedOrders = [];
      for (const csvRow of csvRecords) {
        const customerOrderNbr = `${csvRow["Purchase Order Number"]}-${String(csvRow["Destination"]).padStart(4, "0")}`;
        const apiData = await fetchAcumaticaShipmentData(customerOrderNbr, sessionCookie);
        enrichedOrders.push(enrichOrderData(csvRow, apiData));
      }
    } finally {
      await acumaticaLogout(sessionCookie);
    }

    console.log("Loading Excel template...");
    const workbook = await loadTemplateWorkbook();

    console.log("Populating MasterBOL sheet...");
    const masterBolSheet = workbook.getWorksheet("MasterBOL");
    if (!masterBolSheet) {
      throw new Error('Worksheet "MasterBOL" not found in template');
    }
    const overflowOrders = populateMasterBOL(masterBolSheet, enrichedOrders);

    if (overflowOrders.length > 0) {
      console.log(`Populating Supplemental_Master_BOL sheet(s) with ${overflowOrders.length} overflow orders...`);
      populateSupplementalMasterBOLs(workbook, overflowOrders);
    }

    console.log(`Creating ${enrichedOrders.length} individual Supplemental_BOL sheets...`);
    populateSupplementalBOLs(workbook, enrichedOrders);

    // Hide all sheets except MasterBOL (adjust later if full multi-sheet export is desired)
    for (const sheet of workbook.worksheets) {
      sheet.state = sheet.name === "MasterBOL" ? "visible" : "hidden";
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
  } catch (error) {
    console.error("Error during BOL generation:", error.message);
    console.error(error);
    process.exitCode = 1;
  }
}

main();
