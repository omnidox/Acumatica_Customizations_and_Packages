@echo off
setlocal

cd /d "%~dp0"

echo ================================================
echo iStar Acumatica Customization Export
echo ================================================
echo.
echo Server: https://istar.privatecloudcorp.com/AcumaticaERP
echo Company: Company
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Export-All-iStar-Acumatica-Customizations.ps1"

set "EXIT_CODE=%ERRORLEVEL%"

echo.
if "%EXIT_CODE%"=="0" (
    echo Export completed successfully.
) else (
    echo Export completed with one or more errors.
    echo Review the Export-Report.csv file for details.
)

echo.
pause
exit /b %EXIT_CODE%