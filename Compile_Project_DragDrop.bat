@echo off
:: ==============================================================================
:: Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
:: File Name    : Compile_Project_DragDrop.bat
:: Version      : 1.3
:: Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
:: File Location: Compile_Project_DragDrop.bat
:: Description  : Drag-and-Drop Release compiler for Thai OpenUtau with delayed
::                expansion path safety and SDK validation.
:: ==============================================================================

chcp 65001 >nul
setlocal enabledelayedexpansion
set "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1"
title DELTA SYNTH - OpenUtau Auto Compiler V1.3
color 0B

echo =======================================================
echo      DELTA SYNTH - OpenUtau Project Auto Compiler
echo =======================================================
echo.

:: Check Drag & Drop folder argument (falls back to current script directory if omitted)
set "TARGET_DIR=%~1"
if "!TARGET_DIR!"=="" (
    echo [ERROR] Please drag and drop the project folder onto this .bat file.
    pause
    exit /b 1
)

:: Enter target folder safely
cd /d "!TARGET_DIR!" 2>nul
if errorlevel 1 (
    echo [ERROR] Unable to access the specified folder. Please verify the directory path.
    pause
    exit /b 1
)

echo [INFO] Working Directory: !CD!
echo [INFO] Verifying project files...

:: Verify core project or solution files
if not exist "*.csproj" (
    if not exist "*.sln" (
        echo [ERROR] No .csproj or .sln file found in this directory.
        pause
        exit /b 1
    )
)

:: Verify system dependencies
where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] The "dotnet" command was not found on this system. Please install the .NET SDK first.
    pause
    exit /b 1
)

echo [INFO] Starting compilation process in Release mode...
echo.

dotnet build --configuration Release

:: Evaluate build result
if !ERRORLEVEL! EQU 0 (
    echo.
    echo [SUCCESS] Compilation completed successfully! The system is stable and ready for use.
) else (
    echo.
    echo [FAILED] Compilation errors were detected. Please review the build log above.
)

echo.
echo =======================================================
pause
exit /b 0