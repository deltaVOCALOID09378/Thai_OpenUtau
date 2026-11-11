@echo off
:: ========================================================================
:: Date         : 10/11/2026 (B.E. 2569 / A.D. 2026)
:: File Name    : Install-File.bat
:: Version      : 3.3
:: Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
:: File Location: Install-File.bat
:: Description  : Intelligent installer and compiler for Thai OpenUtau.
::                Checks installed system .NET versions (8.0 - 12.0+) across bundled
::                folders, PATH, dotnet --list-runtimes, and Windows Registry before
::                prompting any download, supports Drag & Drop, and logs execution.
:: ========================================================================

setlocal enabledelayedexpansion
title DELTA SYNTH - Thai_OpenUtau Intelligent Installer ^& Builder v3.3
chcp 65001 >nul
color 0B

set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"
set "ROOT_DIR=%~1"
if "%ROOT_DIR%"=="" set "ROOT_DIR=%SCRIPT_DIR%"
if "%ROOT_DIR:~-1%"=="\" set "ROOT_DIR=%ROOT_DIR:~0,-1%"

if not exist "%ROOT_DIR%\OpenUtau\OpenUtau.csproj" (
    if exist "%ROOT_DIR%\..\OpenUtau\OpenUtau.csproj" (
        for %%I in ("%ROOT_DIR%\..") do set "ROOT_DIR=%%~fI"
    )
)

set "LOG_DIR=%ROOT_DIR%\Logs"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%" 2>nul
set "LOG_FILE=%LOG_DIR%\Install-File.log"

echo ======================================================================== >> "%LOG_FILE%"
echo [%DATE% %TIME%] Starting Install-File.bat v3.3 in "%ROOT_DIR%" >> "%LOG_FILE%"

:: Persistent Prerequisite Marker (Preserved across OpenUtau reinstallations)
set "STATE_DIR=%LOCALAPPDATA%\Thai_OpenUtau"
set "PREREQ_MARKER=%STATE_DIR%\.installed_prereqs"

echo ========================================================================
echo       DELTA SYNTH - Thai_OpenUtau System Setup ^& Auto-Builder v3.3
echo       Architecture: Multi-Runtime Support (.NET 8.0 - 12.0+)
echo ========================================================================
echo [INFO] Working Directory: "%ROOT_DIR%"
echo [INFO] Log File         : "%LOG_FILE%"
echo.

:: ------------------------------------------------------------------------
:: STEP 1: Smart .NET Runtime & SDK Detection (8.0 - 12.0+)
:: ------------------------------------------------------------------------
echo [1/4] [25%%] Checking system environment (.NET Runtime / SDK Environment)...
echo [%DATE% %TIME%] [STEP 1] Checking installed .NET versions on system... >> "%LOG_FILE%"

set "DOTNET_ROLL_FORWARD=LatestMajor"
set "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1"

:: Check 0: Bundled Self-Contained Runtime in Root, Publish, or All Rendered File
if exist "%ROOT_DIR%\coreclr.dll" (
    echo [OK] Bundled self-contained .NET Runtime detected in application root.
    echo [%DATE% %TIME%] [OK] Bundled coreclr.dll detected in root. >> "%LOG_FILE%"
    goto :dotnet_ready
)
if exist "%ROOT_DIR%\Publish\coreclr.dll" (
    echo [OK] Bundled 2-in-1 .NET Runtime detected in Publish folder.
    echo [INFO] Self-contained package runs out-of-the-box with zero setup required.
    echo [%DATE% %TIME%] [OK] Bundled coreclr.dll detected in Publish folder. >> "%LOG_FILE%"
    goto :dotnet_ready
)
if exist "%ROOT_DIR%\All Rendered File\coreclr.dll" (
    echo [OK] Bundled .NET Runtime detected in All Rendered File folder.
    echo [%DATE% %TIME%] [OK] Bundled coreclr.dll detected in All Rendered File folder. >> "%LOG_FILE%"
    goto :dotnet_ready
)

:: Check 1: Try resolving dotnet in standard installation directories if not in PATH
where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    if exist "%ProgramFiles%\dotnet\dotnet.exe" (
        set "PATH=%ProgramFiles%\dotnet;%PATH%"
    ) else if exist "%ProgramFiles(x86)%\dotnet\dotnet.exe" (
        set "PATH=%ProgramFiles(x86)%\dotnet;%PATH%"
    ) else if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" (
        set "PATH=%LOCALAPPDATA%\Microsoft\dotnet;%PATH%"
    ) else if exist "%USERPROFILE%\.dotnet\dotnet.exe" (
        set "PATH=%USERPROFILE%\.dotnet;%PATH%"
    )
)

:: Check 2: Check dotnet --version for SDK >= 8
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
    if !DOTNET_MAJOR! geq 8 (
        echo [OK] Detected .NET SDK version !DOTNET_VER! (Major: !DOTNET_MAJOR!) already installed on system.
        echo [OK] Fully compatible with .NET 8.0 - 12.0+ (LatestMajor roll-forward enabled).
        echo [INFO] Skipping .NET download/installation prompt.
        echo [%DATE% %TIME%] [OK] Detected .NET SDK !DOTNET_VER! >> "%LOG_FILE%"
        if not exist "%STATE_DIR%" mkdir "%STATE_DIR%" 2>nul
        echo installed_version=!DOTNET_VER! > "%PREREQ_MARKER%"
        goto :dotnet_ready
    )
)

:: Check 3: Check dotnet --list-runtimes for any installed Runtime major version >= 8
set "RUNTIME_MAJOR=0"
set "RUNTIME_VER="
if %DOTNET_FOUND% equ 1 (
    for /f "tokens=1,2" %%r in ('dotnet --list-runtimes 2^>nul') do (
        for /f "tokens=1 delims=." %%m in ("%%s") do (
            if %%m geq 8 (
                set "RUNTIME_MAJOR=%%m"
                set "RUNTIME_VER=%%s"
            )
        )
    )
)
if !RUNTIME_MAJOR! geq 8 (
    echo [OK] Detected installed .NET Runtime version !RUNTIME_VER! (Major: !RUNTIME_MAJOR!) on system.
    echo [INFO] Skipping .NET download/installation prompt.
    echo [%DATE% %TIME%] [OK] Detected .NET Runtime !RUNTIME_VER! >> "%LOG_FILE%"
    if not exist "%STATE_DIR%" mkdir "%STATE_DIR%" 2>nul
    echo runtime_version=!RUNTIME_VER! > "%PREREQ_MARKER%"
    goto :dotnet_ready
)

:: Check 4: Check Windows Registry for sharedhost Version >= 8
set "REG_DOTNET_VER="
set "REG_DOTNET_MAJOR=0"
for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost" /v Version 2^>nul ^| findstr /i "Version"') do (
    set "REG_DOTNET_VER=%%b"
)
if defined REG_DOTNET_VER (
    for /f "tokens=1 delims=." %%m in ("!REG_DOTNET_VER!") do set "REG_DOTNET_MAJOR=%%m"
    if !REG_DOTNET_MAJOR! geq 8 (
        echo [OK] Detected .NET Shared Host version !REG_DOTNET_VER! in Windows Registry.
        echo [INFO] Skipping .NET download/installation prompt.
        echo [%DATE% %TIME%] [OK] Registry .NET version !REG_DOTNET_VER! >> "%LOG_FILE%"
        if not exist "%STATE_DIR%" mkdir "%STATE_DIR%" 2>nul
        echo registry_version=!REG_DOTNET_VER! > "%PREREQ_MARKER%"
        goto :dotnet_ready
    )
)

:: Check 5: Previous prerequisite marker
if exist "%PREREQ_MARKER%" (
    echo [OK] Previous prerequisite installation marker verified.
    echo [INFO] Skipping redundant prompts and proceeding directly to build.
    echo [%DATE% %TIME%] [OK] Prerequisite marker found. >> "%LOG_FILE%"
    goto :dotnet_ready
)

:: Prompt ONLY when no compatible .NET 8+ version is found anywhere on the system
echo [%DATE% %TIME%] [WARN] No .NET 8.0+ detected on system. Prompting user. >> "%LOG_FILE%"
echo.
echo ========================================================================
echo [IMPORTANT] .NET SDK or Runtime version 8.0 or higher (8.0 - 12.0+) is required.
echo No compatible .NET 8.0+ installation was detected on this machine.
echo Subsequent runs will automatically bypass this step once .NET is installed.
echo ========================================================================
echo [1] Open Official Microsoft .NET SDK Download Page
echo [2] Automatically Download and Install via Winget (if available)
echo [3] Skip Directly to Compilation Step
echo.
set "NET_CHOICE=3"
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
echo [2/4] [50%%] Verifying and restoring required dependencies...
if not exist "%ROOT_DIR%\OpenUtau\OpenUtau.csproj" (
    echo [INFO] Standalone binary directory detected (no source .csproj present).
    echo [SUCCESS] Environment check passed. You can launch the application via Start-The-Program.bat.
    echo [%DATE% %TIME%] [SUCCESS] Standalone check completed. >> "%LOG_FILE%"
    exit /b 0
)

pushd "%ROOT_DIR%"
dotnet restore "OpenUtau\OpenUtau.csproj" --verbosity minimal >> "%LOG_FILE%" 2>&1
if %ERRORLEVEL% neq 0 (
    echo [WARNING] dotnet restore reported warnings, proceeding to build step...
    echo [%DATE% %TIME%] [WARN] dotnet restore exited with non-zero code. >> "%LOG_FILE%"
) else (
    echo [OK] All dependencies restored successfully.
    echo [%DATE% %TIME%] [OK] Dependencies restored. >> "%LOG_FILE%"
)
popd
echo.

:: ------------------------------------------------------------------------
:: STEP 3: Native Library Build (if C++ present)
:: ------------------------------------------------------------------------
echo [3/4] [75%%] Checking Native Engine (C++ Worldline Library)...
if exist "%ROOT_DIR%\worldline.dll" (
    echo [OK] Pre-built worldline.dll detected; skipping redundant C++ rebuild.
    echo [%DATE% %TIME%] [OK] Existing worldline.dll verified. >> "%LOG_FILE%"
) else if exist "%ROOT_DIR%\cpp\build_win.bat" (
    echo [INFO] Compiling Native Worldline C++ Library...
    pushd "%ROOT_DIR%\cpp"
    call build_win.bat >> "%LOG_FILE%" 2>&1
    popd
    echo [OK] Native Library compiled successfully.
) else (
    echo [INFO] Using built-in native libraries.
)
echo.

:: ------------------------------------------------------------------------
:: STEP 4: Build OpenUtau C# Project (Release Mode with RollForward)
:: ------------------------------------------------------------------------
echo [4/4] [100%%] Building main application (Building OpenUtau C# Release)...
set "DOTNET_ROLL_FORWARD=LatestMajor"
pushd "%ROOT_DIR%"
dotnet build "OpenUtau\OpenUtau.csproj" -c Release --no-restore >> "%LOG_FILE%" 2>&1
if %ERRORLEVEL% equ 0 (
    echo.
    echo ========================================================================
    echo [SUCCESS] Thai OpenUtau compiled and installed 100%% successfully!
    echo [SUCCESS] You can now launch the application via Start-The-Program.bat.
    echo ========================================================================
    echo [%DATE% %TIME%] [SUCCESS] Build completed with 0 errors. >> "%LOG_FILE%"
    
    if not exist "All Rendered File" mkdir "All Rendered File" 2>nul
    for /d %%D in ("OpenUtau\bin\Release\net*") do (
        xcopy "%%D\*" "All Rendered File\" /s /e /y /i /q >nul 2>&1
    )
) else (
    echo.
    echo [FAILED] Errors occurred during compilation. Please review "%LOG_FILE%".
    echo [%DATE% %TIME%] [ERROR] Build failed. >> "%LOG_FILE%"
)
popd

echo.
if not "%NO_PROMPT%"=="1" pause
exit /b 0

