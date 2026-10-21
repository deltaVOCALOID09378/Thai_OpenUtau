@echo off
:: ============================================================================
:: Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
:: File Name    : Install_ThaiVCCV_Delta.bat
:: Version      : 1.2
:: Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
:: File Location: Install_ThaiVCCV_Delta.bat
:: Description  : Standalone installation script for ThaiVCCVPhonemizer.cs with
::                direct Drag-and-Drop and interactive prompt support.
:: ============================================================================
title DELTA SYNTH - OpenUtau Plugin Installer V1.2
color 0B
chcp 65001 >nul

echo =======================================================
echo      DELTA SYNTH - Thai VCCV Phonemizer Installer
echo =======================================================
echo.

set "target_dir=%~1"
if "%target_dir%"=="" (
    echo Please drag and drop the OpenUtau root directory here.
    set /p target_dir="Target Path: "
)

set "target_dir=%target_dir:"=%"

if not exist "%target_dir%\OpenUtau.sln" (
    if not exist "%target_dir%\Thai OpenUtau.sln" (
        echo [ERROR] The specified folder is not a valid OpenUtau project root.
        pause
        exit /b 1
    )
)

set "plugin_dir=%target_dir%\OpenUtau.Plugin.Builtin"
if not exist "%plugin_dir%" mkdir "%plugin_dir%"

echo [INFO] Copying ThaiVCCVPhonemizer.cs to Plugin.Builtin directory...
copy /Y "%~dp0ThaiVCCVPhonemizer.cs" "%plugin_dir%\ThaiVCCVPhonemizer.cs" >nul

if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] File successfully copied.
    echo [INFO] Building project in Release mode...
    cd /d "%target_dir%"
    dotnet build --configuration Release
    if %ERRORLEVEL% EQU 0 (
        echo [SUCCESS] Compilation completed flawlessly!
    ) else (
        echo [FAILED] Compilation errors detected. Check the log.
    )
) else (
    echo [ERROR] Failed to copy the file. Please check permissions.
)

echo.
echo =======================================================
pause
exit /b 0