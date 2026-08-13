import dotenv from "dotenv";
import path from "node:path";
import fs from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { spawn } from "node:child_process";
import ExcelJS from "exceljs";

dotenv.config();

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// ============================================================================
// CLI argument parsing
// ============================================================================
function getArg(flag) {
  const index = process.argv.indexOf(flag);
  return index !== -1 ? process.argv[index + 1] : null;
}

const shipmentNbr = getArg("--shipment");

if (!shipmentNbr) {
  console.error("Usage: node barcode_generator.js --shipment <ShipmentNbr>");
  process.exit(1);
}

// ============================================================================
// Config
// ============================================================================
function requireEnv(name) {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing required environment variable: ${name}. Check your .env file.`);
  }
  return value;
}

const ACUMATICA_URL = requireEnv("ACUMATICA_URL");
const ACUMATICA_LOGIN_PATH = requireEnv("ACUMATICA_LOGIN_PATH");
const ACUMATICA_LOGOUT_PATH = requireEnv("ACUMATICA_LOGOUT_PATH");
const ACUMATICA_SHIPMENT_INFO_PATH = requireEnv("ACUMATICA_SHIPMENT_INFO_PATH");
const ACUMATICA_NAME = requireEnv("ACUMATICA_NAME");
const ACUMATICA_PASSWORD = requireEnv("ACUMATICA_PASSWORD");
const ACUMATICA_TENANT = requireEnv("ACUMATICA_TENANT");

const TEMPLATE_PATH = path.resolve(__dirname, process.env.BARCODE_TEMPLATE_PATH || "templates/Barcode_Template.xlsx");
const OUTPUT_DIR = path.resolve(__dirname, process.env.BARCODE_OUTPUT_DIRECTORY || "output");
const LIBREOFFICE_PATH = process.env.LIBREOFFICE_PATH || "libreoffice";

const BARCODE_FONT_NAME = "IDAutomationHC39M Free Version";
const TEMPLATE_SHEET_NAME = "Barcode_Print";
const COLUMNS = 3;
const ID_START_ROW = 4;

// ============================================================================
// Acumatica auth (cookie-session, same pattern as carton_scanner/utils/auth.js)
// ============================================================================
async function acumaticaLogin() {
  const url = `${ACUMATICA_URL}/${ACUMATICA_LOGIN_PATH}`;

  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      name: ACUMATICA_NAME,
      password: ACUMATICA_PASSWORD,
      company: ACUMATICA_TENANT,
    }),
  });

  if (!response.ok || response.status !== 204) {
    const errorText = await response.text().catch(() => "");
    throw new Error(`Acumatica login failed: ${response.status} ${response.statusText}\n${errorText}`);
  }

  const cookieHeaders = response.headers.getSetCookie();
  if (!cookieHeaders || cookieHeaders.length === 0) {
    throw new Error("No session cookie received from Acumatica login response.");
  }

  const cookie = cookieHeaders
    .map((cookieString) => {
      const match = cookieString.match(/^(.*?);/);
      return match ? match[0] : "";
    })
    .filter((c) => c.length > 0)
    .join(" ");

  if (!cookie) {
    throw new Error("Failed to parse session cookie from Acumatica login response.");
  }

  return cookie;
}

async function acumaticaLogout(sessionCookie) {
  try {
    const url = `${ACUMATICA_URL}/${ACUMATICA_LOGOUT_PATH}`;
    await fetch(url, { method: "POST", headers: { Cookie: sessionCookie } });
  } catch (err) {
    console.warn(`Acumatica logout failed (non-fatal): ${err.message}`);
  }
}

// ============================================================================
// Fetch shipment info from Acumatica
// ============================================================================
async function fetchShipmentInfo(shipmentNbr, sessionCookie) {
  const url = `${ACUMATICA_URL}/${ACUMATICA_SHIPMENT_INFO_PATH}?$expand=shipinfoobjectDetails`;

  const response = await fetch(url, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      Cookie: sessionCookie,
    },
    body: JSON.stringify({ shipment_nbr: { value: shipmentNbr } }),
  });

  if (!response.ok) {
    const errorText = await response.text().catch(() => "");
    throw new Error(`Acumatica shipinfoobject fetch failed: ${response.status} ${response.statusText}\n${errorText}`);
  }

  const data = await response.json();
  const details = data.shipinfoobjectDetails || [];

  if (details.length === 0) {
    throw new Error(`No shipinfoobjectDetails found for Shipment #: ${shipmentNbr}`);
  }

  return details.map((row) => ({
    shipmentNbr: row.ShipmentNbr?.value ?? shipmentNbr,
    carton: row.Carton?.value ?? null,
    inventoryId: row.InventoryID?.value ?? null,
    ucc: row.GS1128?.value ?? null,
  }));
}

/**
 * Groups flat rows into one entry per Carton, in first-seen order.
 * @returns {Array<{carton: string, ucc: string, inventoryIds: string[]}>}
 */
function groupByCarton(rows) {
  const cartonMap = new Map();

  for (const row of rows) {
    if (!row.carton) continue;

    if (!cartonMap.has(row.carton)) {
      cartonMap.set(row.carton, { carton: row.carton, ucc: row.ucc, inventoryIds: [] });
    }

    const group = cartonMap.get(row.carton);
    if (row.inventoryId) group.inventoryIds.push(row.inventoryId);

    // Sanity check from Design_Document.md \u00a74.2: GS1128 should be
    // identical across every row sharing the same Carton.
    if (row.ucc && group.ucc && row.ucc !== group.ucc) {
      console.warn(
        `Warning: Carton ${row.carton} has inconsistent GS1128 values ("${group.ucc}" vs "${row.ucc}"). Using the first value seen.`,
      );
    }
  }

  return Array.from(cartonMap.values());
}

// ============================================================================
// Excel worksheet cloning (same proven approach as BOL Generator's
// cloneWorksheet() \u2014 exceljs has no built-in clone API)
// ============================================================================
function cloneWorksheet(workbook, sourceSheetName, newSheetName) {
  const sourceSheet = workbook.getWorksheet(sourceSheetName);
  if (!sourceSheet) {
    throw new Error(`Cannot clone: source sheet "${sourceSheetName}" not found`);
  }

  const newSheet = workbook.addWorksheet(newSheetName, {
    properties: { ...sourceSheet.properties },
    pageSetup: {
      ...sourceSheet.pageSetup,
      // Override the template's saved fit-to-page scale, which was
      // computed for the original 3-sample-ID layout. Cartons with more
      // IDs need more vertical space than that scale allows \u2014 forcing
      // everything onto one page would shrink the barcodes small enough
      // to become unscannable. fitToHeight: 0 lets a carton's sheet spill
      // onto additional pages instead, keeping every barcode at a fixed,
      // reliably scannable size regardless of how many IDs a carton has.
      // fitToWidth: 1 stays, since 3 columns always fits one page wide.
      fitToWidth: 1,
      fitToHeight: 0,
      scale: undefined,
    },
    views: sourceSheet.views,
  });

  newSheet.columns = sourceSheet.columns.map((col) => ({ width: col.width, style: col.style }));

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

  const merges = sourceSheet.model.merges || [];
  merges.forEach((range) => newSheet.mergeCells(range));

  return newSheet;
}

/**
 * Excel worksheet names must be <= 31 chars and cannot contain: \ / ? * [ ] :
 */
function generateUniqueSheetName(workbook, baseLabel) {
  const MAX_LENGTH = 31;
  const sanitized = String(baseLabel).replace(/[\\/?*[\]:]/g, "-");
  const prefix = "Carton_";
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

// ============================================================================
// Populates one carton's cloned sheet with real barcode values
// ============================================================================
function populateCartonSheet(sheet, cartonGroup, shipmentNbr) {
  // Barcode sizing, shared by both the header row and the ID grid so the
  // whole page stays visually consistent. Row height scaled proportionally
  // with the font size to avoid the row-overlap bug fixed earlier.
  const BARCODE_FONT_SIZE = 31.46; // 28.6 * 1.1 (compounded: 26 -> 28.6 -> 31.46)
  const BARCODE_ROW_HEIGHT = 184.2225; // 167.475 * 1.1

  // Row 1 (labels) is already correct from the template \u2014 no changes needed.
  // Row 2: Shipment # / Carton # / UCC # barcode values. Font size and row
  // height set explicitly (rather than left inherited from the template's
  // original 26pt) so the header matches the ID grid's enlarged size below.
  sheet.getCell("A2").value = `*${String(shipmentNbr).trim()}*`;
  sheet.getCell("B2").value = `*${String(cartonGroup.carton).trim()}*`;
  sheet.getCell("C2").value = cartonGroup.ucc
    ? `*${String(cartonGroup.ucc).trim()}*`
    : "*UCC_NOT_FOUND*";
  ["A2", "B2", "C2"].forEach((addr) => {
    sheet.getCell(addr).font = { name: BARCODE_FONT_NAME, size: BARCODE_FONT_SIZE };
  });
  sheet.getRow(2).height = BARCODE_ROW_HEIGHT;

  // Row 3 ("Inventory #'s" header): add a bit of breathing room between the
  // Shipment#/Carton#/UCC# barcode block above and this label. Row 3 has
  // no explicit vertical alignment in the template (defaults to bottom),
  // so increasing the row's height and pinning vertical:"bottom" adds the
  // extra space above the text (next to row 2's barcodes) rather than
  // pushing the label away from the ID grid it's labeling below it.
  sheet.getRow(3).height = 165;
  sheet.getCell("A3").alignment = { horizontal: "center", vertical: "bottom" };

  // Row 4+: Inventory ID barcodes, 3 columns, row-major fill (\u00a75.3).
  // Row height must be set explicitly for every ID row, not inherited from
  // the template clone \u2014 cloneWorksheet() only copies rows that physically
  // existed in the template (rows 4-6, sized for 9 sample IDs). Any carton
  // needing more than 9 IDs spills into brand-new rows with no explicit
  // height, defaulting to the sheet's tiny default height \u2014 far too short
  // for a 26pt barcode glyph, causing rows to visually overlap. Setting the
  // height on every row as it's populated (regardless of whether it was
  // cloned or newly created) guarantees consistent spacing at any ID count.
  // Height/size constants declared once at the top of this function, shared
  // with the header row above.

  cartonGroup.inventoryIds.forEach((id, index) => {
    const row = ID_START_ROW + Math.floor(index / COLUMNS);
    const col = (index % COLUMNS) + 1; // 1=A, 2=B, 3=C
    const cell = sheet.getCell(row, col);
    cell.value = `*${String(id).trim()}*`;
    cell.font = { name: BARCODE_FONT_NAME, size: BARCODE_FONT_SIZE };
    sheet.getRow(row).height = BARCODE_ROW_HEIGHT;
  });
}

// ============================================================================
// PDF conversion via headless LibreOffice
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
    proc.stdout.on("data", (data) => { stdout += data.toString(); });
    proc.stderr.on("data", (data) => { stderr += data.toString(); });
    proc.on("error", reject);

    proc.on("close", async (exitCode) => {
      if (exitCode !== 0) {
        reject(new Error(`LibreOffice failed with exit code ${exitCode}.\n${stderr}`));
        return;
      }

      const expectedPdf = path.join(outputDirectory, `${path.parse(excelPath).name}.pdf`);
      try {
        await fs.access(expectedPdf);
        resolve(expectedPdf);
      } catch {
        reject(new Error(`LibreOffice did not create the expected PDF:\n${expectedPdf}\nstdout: ${stdout}\nstderr: ${stderr}`));
      }
    });
  });
}

// ============================================================================
// Main
// ============================================================================
async function main() {
  console.log(`Starting barcode generation for Shipment #: ${shipmentNbr}`);
  await fs.mkdir(OUTPUT_DIR, { recursive: true });

  console.log("Logging into Acumatica...");
  const sessionCookie = await acumaticaLogin();

  let cartonGroups;
  try {
    console.log("Fetching shipment info...");
    const rows = await fetchShipmentInfo(shipmentNbr, sessionCookie);
    console.log(`Retrieved ${rows.length} rows from Acumatica.`);

    cartonGroups = groupByCarton(rows);
    console.log(`Grouped into ${cartonGroups.length} carton(s).`);
  } finally {
    await acumaticaLogout(sessionCookie);
  }

  if (cartonGroups.length === 0) {
    throw new Error("No cartons found; nothing to generate.");
  }

  console.log("Loading template workbook...");
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(TEMPLATE_PATH);

  console.log(`Creating ${cartonGroups.length} carton sheet(s)...`);
  for (const cartonGroup of cartonGroups) {
    const sheetName = generateUniqueSheetName(workbook, cartonGroup.carton);
    const newSheet = cloneWorksheet(workbook, TEMPLATE_SHEET_NAME, sheetName);
    populateCartonSheet(newSheet, cartonGroup, shipmentNbr);
    console.log(`  Carton ${cartonGroup.carton}: ${cartonGroup.inventoryIds.length} Inventory ID(s)`);
  }

  // Remove the original template sheet (still holds placeholder values) \u2014
  // it's not part of the printable output, same reasoning as BOL Generator
  // removing its unused template sheets.
  const templateSheet = workbook.getWorksheet(TEMPLATE_SHEET_NAME);
  if (templateSheet) workbook.removeWorksheet(templateSheet.id);

  const timestamp = Date.now();
  const excelPath = path.join(OUTPUT_DIR, `Barcode_${shipmentNbr}_${timestamp}.xlsx`);
  await workbook.xlsx.writeFile(excelPath);
  console.log(`Saved Excel workbook: ${excelPath}`);

  console.log("Converting to PDF...");
  const pdfPath = await convertToPdf(excelPath, OUTPUT_DIR);
  console.log(`Saved PDF: ${pdfPath}`);

  console.log("Barcode generation complete!");
}

main().catch((err) => {
  console.error("Error during barcode generation:", err.message);
  console.error(err.stack);
  process.exit(1);
});
