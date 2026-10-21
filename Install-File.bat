@echo off
setlocal enabledelayedexpansion
title DELTA SYNTH - Thai_OpenUtau Intelligent Installer ^& Builder
chcp 65001 >nul
color 0B

:: ========================================================================
:: Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
:: File Name    : Install-File.bat
:: Version      : 3.2
:: Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by Patiphat Wongyai (Delta) ]
:: File Location: Install-File.bat
:: Description  : Intelligent installer and compiler for Thai OpenUtau.
::                Automatically detects installed .NET (8.0 - 12.0+), supports
::                Drag & Drop, verifies dependencies, and builds the project.
:: ========================================================================

set "SCRIPT_DIR=%~dp0"
set "ROOT_DIR=%~1"
if "%ROOT_DIR%"=="" set "ROOT_DIR=%SCRIPT_DIR%"
if "%ROOT_DIR:~-1%"=="\" set "ROOT_DIR=%ROOT_DIR:~0,-1%"

:: Persistent Prerequisite Marker (Preserved across OpenUtau reinstallations)
set "STATE_DIR=%LOCALAPPDATA%\Thai_OpenUtau"
set "PREREQ_MARKER=%STATE_DIR%\.installed_prereqs"

echo ========================================================================
echo       DELTA SYNTH - Thai_OpenUtau System Setup ^& Auto-Builder
echo       Architecture: Multi-Runtime Support (.NET 8.0 - 12.0+)
echo ========================================================================
echo [INFO] Working Directory: "%ROOT_DIR%"
echo.

:: ------------------------------------------------------------------------
:: STEP 1: Smart .NET Runtime & SDK Detection (8.0 - 12.0+)
:: ------------------------------------------------------------------------
echo [1/4] Checking system environment (.NET Runtime / SDK Environment)...

set "DOTNET_FOUND=0"
set "DOTNET_VER="
set "DOTNET_MAJOR=0"

where dotnet >nul 2>nul
if %ERRORLEVEL% equ 0 (
    set "DOTNET_FOUND=1"
    for /f "tokens=1 delims=." %%a in ('dotnet --version 2^>nul') do (
        set "DOTNET_MAJOR=%%a"
    )
    for /f "tokens=*" %%a in ('dotnet --version 2^>nul') do (
        set "DOTNET_VER=%%a"
    )
)

if %DOTNET_FOUND% equ 1 (
    if %DOTNET_MAJOR% geq 8 (
        echo [OK] Detected .NET SDK / Runtime version !DOTNET_VER! (Major: !DOTNET_MAJOR!)
        echo [OK] Compatible with the system (supports versions 8.0 through 12.0+ and above).
        echo [INFO] Automatically skipping .NET installation prompt (runtime already installed).
        if not exist "%STATE_DIR%" mkdir "%STATE_DIR%" 2>nul
        echo installed_version=!DOTNET_VER! > "%PREREQ_MARKER%"
        goto :dotnet_ready
    ) else (
        echo [WARNING] Detected .NET version !DOTNET_VER!, which is below the minimum required version 8.0.
    )
) else (
    echo [NOTICE] The "dotnet" command was not found on this system.
)

:: If marker was already created previously, verify if dotnet path is resolvable
if exist "%PREREQ_MARKER%" (
    echo [INFO] Previous prerequisite installation marker found. Skipping redundant alerts...
)

:: Prompt ONLY on clean/first install when .NET >= 8 is truly missing
echo.
echo ========================================================================
echo [IMPORTANT] .NET SDK or Runtime version 8.0 or higher (8.0 - 12.0+) is required.
echo The .NET runtime installs at the OS level and persists across OpenUtau updates.
echo Subsequent runs will automatically bypass this step once .NET is installed.
echo ========================================================================
echo [1] Open Official Microsoft .NET SDK Download Page
echo [2] Automatically Download and Install via Winget (if available)
echo [3] Skip Directly to Compilation Step
echo.
set /p "NET_CHOICE=Select an option (1-3) or press Enter to skip: "
if "!NET_CHOICE!"=="1" (
    start https://dotnet.microsoft.com/download/dotnet/8.0
    echo Please complete the .NET installation, then press any key to continue...
    pause >nul
) else if "!NET_CHOICE!"=="2" (
    echo Installing .NET SDK via Winget...
    winget install Microsoft.DotNet.SDK.8 --accept-source-agreements --accept-package-agreements
)

:dotnet_ready
echo.

:: ------------------------------------------------------------------------
:: STEP 2: Restore Dependencies
:: ------------------------------------------------------------------------
echo [2/4] Verifying and restoring required dependencies (Restoring Dependencies)...
pushd "%ROOT_DIR%"
dotnet restore "OpenUtau\OpenUtau.csproj" --verbosity minimal
if %ERRORLEVEL% neq 0 (
    echo [WARNING] dotnet restore reported warnings, proceeding to build step...
) else (
    echo [OK] All dependencies restored successfully.
)
popd
echo.

:: ------------------------------------------------------------------------
:: STEP 3: Native Library Build (if C++ present)
:: ------------------------------------------------------------------------
echo [3/4] Checking and compiling Native Engine (C++ Worldline Library)...
if exist "%ROOT_DIR%\cpp\build_win.bat" (
    echo [INFO] Compiling Native Worldline C++ Library...
    pushd "%ROOT_DIR%\cpp"
    call build_win.bat
    popd
    echo [OK] Native Library compiled successfully.
) else (
    echo [INFO] No external C++ build script required; using built-in native libraries.
)
echo.

:: ------------------------------------------------------------------------
:: STEP 4: Build OpenUtau C# Project (Release Mode with RollForward)
:: ------------------------------------------------------------------------
echo [4/4] Building main application (Building OpenUtau C# Release)...
set "DOTNET_ROLL_FORWARD=LatestMajor"
pushd "%ROOT_DIR%"
dotnet build "OpenUtau\OpenUtau.csproj" -c Release --no-restore
if %ERRORLEVEL% equ 0 (
    echo.
    echo ========================================================================
    echo [SUCCESS] Thai OpenUtau compiled and installed 100%% successfully!
    echo [SUCCESS] You can now launch the application via Start-The-Program.bat.
    echo ========================================================================
    
    :: Consolidate binary output if All Rendered File folder is desired
    if not exist "All Rendered File" mkdir "All Rendered File" 2>nul
    for /d %%D in ("OpenUtau\bin\Release\net*") do (
        xcopy "%%D\*" "All Rendered File\" /s /e /y /i /q >nul 2>&1
    )
) else (
    echo.
    echo [FAILED] Errors occurred during compilation. Please review the build log above.
)
popd

echo.
pause
exit /b 0
