@echo off
setlocal

title iStar Acumatica Customization Export

rem Always switch to the directory containing this BAT file.
cd /d "%~dp0"

set "PS_SCRIPT=%~dp0Export-All-iStar-Acumatica-Customizations.ps1"
set "PROJECT_CSV=%~dp0CustomizationProjects.csv"
set "ENV_FILE=%~dp0.env"

echo ============================================================
echo  iStar Acumatica Customization Export
echo ============================================================
echo.
echo PowerShell script:
echo %PS_SCRIPT%
echo.
echo Project CSV:
echo %PROJECT_CSV%
echo.
echo Environment file:
echo %ENV_FILE%
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
    pause
    exit /b 1
)

rem Validate that the .env file exists.
if not exist "%ENV_FILE%" (
    echo ERROR: The credential file was not found:
    echo %ENV_FILE%
    echo.
    echo Create a file named .env in this folder with:
    echo.
    echo ACUMATICA_USERNAME=your_username
    echo ACUMATICA_PASSWORD=your_password
    echo ACUMATICA_BASE_URL=https://istar.privatecloudcorp.com/AcumaticaERP
    echo ACUMATICA_COMPANY=Company
    echo ACUMATICA_BRANCH=
    echo ACUMATICA_LOCALE=en-US
    echo.
    pause
    exit /b 1
)

powershell.exe ^
    -NoLogo ^
    -NoProfile ^
    -ExecutionPolicy Bypass ^
    -File "%PS_SCRIPT%" ^
    -EnvFilePath "%ENV_FILE%" ^
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