@echo off
:: ========================================================================
:: Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
:: File Name    : Install_Thai_UI.bat
:: Version      : 1.4
:: Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
:: File Location: OpenUtau/Install_Thai_UI.bat
:: Description  : Drag and drop the target OpenUtau "Strings" folder onto this
::                .bat file to automatically apply the Thai UI Localization patch.
:: ========================================================================

title OpenUtau Thai UI Localization Installer V1.4
color 0B
chcp 65001 >nul

echo =======================================================
echo    OpenUtau Thai UI Localization Patcher (DELTA SYNTH)
echo =======================================================
echo.

if "%~1" == "" (
    echo [ERROR] Please drag and drop the OpenUtau "Strings" folder onto this .bat file.
    pause
    exit /b 1
)

cd /d "%~1" 2>nul
if errorlevel 1 (
    echo [ERROR] Could not access the dropped folder. Please verify the path.
    pause
    exit /b 1
)

echo [INFO] Working Directory: %CD%
echo [INFO] Applying th.axaml localization patch...

copy /y "%~dp0th.axaml" "th.axaml" >nul

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [SUCCESS] Localization patch applied successfully. The Thai UI is now ready.
) else (
    echo.
    echo [FAILED] Could not apply the patch. Please check file permissions.
)

echo.
echo =======================================================
pause
exit /b 0