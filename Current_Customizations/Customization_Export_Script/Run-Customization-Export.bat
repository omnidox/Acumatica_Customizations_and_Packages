@echo off
setlocal

title iStar Acumatica Customization Export

rem Always switch to the directory containing this BAT file.
cd /d "%~dp0"

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

rem Use the default BaseUrl and Company configured in the PS1 file.
powershell.exe ^
    -NoLogo ^
    -NoProfile ^
    -ExecutionPolicy Bypass ^
    -File "%~dp0Export-All-iStar-Acumatica-Customizations.ps1"

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