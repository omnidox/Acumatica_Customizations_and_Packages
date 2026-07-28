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

  // Replace these with the actual cells from the template.
  worksheet.getCell("F4").value = "MBOL-TEST-000001";
  worksheet.getCell("F5").value = "BOL-TEST-000001";
  worksheet.getCell("F6").value = "LOAD-123456";
  worksheet.getCell("B10").value = "CUSTOMER-ORDER-98765";
  worksheet.getCell("F10").value = new Date("2026-07-27");
  worksheet.getCell("B14").value = "Test Transportation Company";
  worksheet.getCell("F14").value = "TEST";
  worksheet.getCell("B26").value = 120;
  worksheet.getCell("D26").value = 6;
  worksheet.getCell("F26").value = 2450.75;

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