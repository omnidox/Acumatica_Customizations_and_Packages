@echo off
setlocal

title iStar Acumatica Customization Export

rem Always switch to the directory containing this BAT file.
cd /d "%~dp0"

set "PS_SCRIPT=%~dp0Export-All-iStar-Acumatica-Customizations.ps1"
set "PROJECT_CSV=%~dp0CustomizationProjects.csv"

echo ============================================================
echo  iStar Acumatica Customization Export
echo ============================================================
echo.
echo Acumatica:
echo https://istar.privatecloudcorp.com/AcumaticaERP
echo.
echo Company:
echo Company
echo.
echo Project CSV:
echo %PROJECT_CSV%
echo.

rem Validate that the PowerShell script exists.
if not exist "%PS_SCRIPT%" (
    echo ERROR: The PowerShell script was not found:
    echo %PS_SCRIPT%
    echo.
    pause
    exit /b 1
)

rem Validate that the CSV file exists.
if not exist "%PROJECT_CSV%" (
    echo ERROR: The customization-project CSV was not found:
    echo %PROJECT_CSV%
    echo.
    echo Keep these three files in the same folder:
    echo   Export-All-iStar-Acumatica-Customizations.ps1
    echo   Run-Customization-Export.bat
    echo   CustomizationProjects.csv
    echo.
    pause
    exit /b 1
)

powershell.exe ^
    -NoLogo ^
    -NoProfile ^
    -ExecutionPolicy Bypass ^
    -File "%PS_SCRIPT%" ^
    -ProjectCsvPath "%PROJECT_CSV%"

set "EXIT_CODE=%ERRORLEVEL%"

echo.
echo ============================================================

if "%EXIT_CODE%"=="0" (
    echo  Export completed successfully.
) else (
    echo  Export completed with errors.
    echo  Review Export-Report.csv in the generated export folder.
)

echo ============================================================
echo.
pause

exit /b %EXIT_CODE%