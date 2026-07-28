import path from "node:path";
import fs from "node:fs/promises";
import { spawn } from "node:child_process";
import ExcelJS from "exceljs";

const TEMPLATE_PATH = path.resolve("templates/BOL_Template.xlsx");
const OUTPUT_DIRECTORY = path.resolve("output");

async function populateTemplate(outputExcelPath) {
  const workbook = new ExcelJS.Workbook();

  await workbook.xlsx.readFile(TEMPLATE_PATH);

  const worksheet = workbook.getWorksheet("BOL");

  if (!worksheet) {
    throw new Error('Worksheet "BOL" was not found.');
  }

  // ---------------------------------------------------------
  // PDF / PRINT CONFIGURATION
  // ---------------------------------------------------------
  worksheet.pageSetup = {
    paperSize: 1, // US Letter (8.5 x 11 inches)
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
  worksheet.pageSetup.printArea = "A1:M49";

  // ---------------------------------------------------------
  // FIELD POPULATION
  // ---------------------------------------------------------
  // Carrier information (right side, rows 7-11)
  worksheet.getCell("K7").value = "XPO Logistics";        // Carrier name
  worksheet.getCell("K8").value = "TRL-2025-000001";      // Trailer number
  worksheet.getCell("K9").value = "SL-98765";             // Seal number
  worksheet.getCell("J10").value = "XPOL";                // SCAC
  worksheet.getCell("K11").value = "PRO-2025-123456";     // Pro number

  // Third party freight charges bill to (rows 13-15)
  worksheet.getCell("B13").value = "Test Transportation Company";
  worksheet.getCell("B14").value = "456 Commerce Drive, Suite 200";
  worksheet.getCell("B15").value = "Atlanta, GA 30303";

  // Special instructions / Target Load ID (row 16)
  worksheet.getCell("F16").value = "LOAD-123456";

  // Customer order info (data rows start at 22)
  worksheet.getCell("A22").value = "CUSTOMER-ORDER-98765";
  worksheet.getCell("E22").value = 120;        // # PKGS
  worksheet.getCell("F22").value = 2450.75;    // WEIGHT
  worksheet.getCell("G22").value = 2450.75;    // WEIGHT (duplicate col)
  worksheet.getCell("H22").value = "Y";        // Pallet Y/N
  worksheet.getCell("I22").value = "Pallet";

  // ---------------------------------------------------------
  // HIDE UNUSED SHEETS
  // ---------------------------------------------------------
  // Only show the BOL sheet in the PDF; hide all others
  for (const sheet of workbook.worksheets) {
    sheet.state = sheet.name === "BOL" ? "visible" : "hidden";
  }

  await workbook.xlsx.writeFile(outputExcelPath);
}

function convertToPdf(excelPath, outputDirectory) {
  return new Promise((resolve, reject) => {
    const process = spawn(
      "libreoffice",
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
      {
        stdio: ["ignore", "pipe", "pipe"],
      },
    );

    let stdout = "";
    let stderr = "";

    process.stdout.on("data", (data) => {
      stdout += data.toString();
    });

    process.stderr.on("data", (data) => {
      stderr += data.toString();
    });

    process.on("error", reject);

    process.on("close", async (exitCode) => {
      if (exitCode !== 0) {
        reject(
          new Error(
            `LibreOffice failed with exit code ${exitCode}.\n${stderr}`,
          ),
        );
        return;
      }

      const expectedPdf = path.join(
        outputDirectory,
        `${path.parse(excelPath).name}.pdf`,
      );

      try {
        await fs.access(expectedPdf);
        resolve({
          pdfPath: expectedPdf,
          stdout,
          stderr,
        });
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

async function main() {
  await fs.mkdir(OUTPUT_DIRECTORY, { recursive: true });

  const outputExcelPath = path.join(
    OUTPUT_DIRECTORY,
    `BOL_Test_${Date.now()}.xlsx`,
  );

  await populateTemplate(outputExcelPath);

  const result = await convertToPdf(
    outputExcelPath,
    OUTPUT_DIRECTORY,
  );

  console.log("Excel created:", outputExcelPath);
  console.log("PDF created:", result.pdfPath);
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});