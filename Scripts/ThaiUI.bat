@echo off
:: ========================================================================
:: Date         : 10/11/2026 (B.E. 2569 / A.D. 2026)
:: File Name    : ThaiUI.bat
:: Version      : 2.1
:: Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
:: File Location: Scripts/ThaiUI.bat
:: Description  : Drag and drop the target OpenUtau "Strings" folder onto this
::                .bat file to automatically apply the Thai UI Localization & Font patch.
:: ========================================================================

title OpenUtau Thai UI Localization Patcher V2.1 (Leelawadee UI)
color 0B
chcp 65001 >nul

set "LOG_DIR=%~dp0..\Logs"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%" 2>nul
set "LOG_FILE=%LOG_DIR%\ThaiUI.log"
echo [%DATE% %TIME%] Starting Scripts/ThaiUI.bat v2.1 >> "%LOG_FILE%"

echo =======================================================
echo    OpenUtau Thai UI Localization Patcher (V2.1)
echo    Font Standard: Leelawadee UI (Standardized UI)
echo =======================================================
echo.

set "TARGET_DIR=%~1"
if "%TARGET_DIR%"=="" (
    if exist "Strings" (
        set "TARGET_DIR=%CD%\Strings"
    ) else if exist "..\Strings" (
        set "TARGET_DIR=%CD%\..\Strings"
    ) else (
        echo [ERROR] Please drag and drop the OpenUtau "Strings" folder onto this .bat file.
        echo [%DATE% %TIME%] [ERROR] No target Strings folder provided. >> "%LOG_FILE%"
        pause
        exit /b 1
    )
)

cd /d "%TARGET_DIR%" 2>nul
if errorlevel 1 (
    echo [ERROR] Could not access the target folder. Please verify the path and try again.
    echo [%DATE% %TIME%] [ERROR] Failed to access target folder: "%TARGET_DIR%" >> "%LOG_FILE%"
    pause
    exit /b 1
)

echo [INFO] Working Directory: %CD%
echo [INFO] Applying Thai UI localization and Leelawadee UI font configuration...

if exist "%~dp0Strings.th-TH.axaml" (
    copy /y "%~dp0Strings.th-TH.axaml" "Strings.th-TH.axaml" >nul
    echo [OK] Updated Strings.th-TH.axaml successfully.
)
if exist "%~dp0unhandled_strings.json" (
    copy /y "%~dp0unhandled_strings.json" "unhandled_strings.json" >nul
    echo [OK] Updated unhandled_strings.json (Leelawadee UI default) successfully.
)
if exist "%~dp0th.axaml" (
    copy /y "%~dp0th.axaml" "th.axaml" >nul
    echo [OK] Updated th.axaml successfully.
)

echo.
echo [SUCCESS] Localization patch applied successfully! Leelawadee UI is now active.
echo [%DATE% %TIME%] [SUCCESS] Localization patch applied in "%CD%". >> "%LOG_FILE%"
echo.
echo =======================================================
pause
exit /b 0