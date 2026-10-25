@echo off
rem ======================================================================
rem Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
rem File Name    : Compile_Full_Program.bat
rem Version      : 3.3.0 (Full Standalone Release Builder)
rem Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
rem File Location: Root Directory of the Project (E:\Program Developing\Thai_OpenUtau\)
rem Description  : Professional full-stack compilation and publishing pipeline for Thai OpenUtau.
rem                Builds OpenUtau in Release mode and produces a fully consolidated,
rem                ready-to-run application in the "Publish" folder.
rem ======================================================================

setlocal enabledelayedexpansion
title DELTA SYNTH - Thai OpenUtau Full Program Compiler v3.3.0
color 0B
chcp 65001 >nul
set "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1"

echo ======================================================================
echo       DELTA SYNTH - Thai OpenUtau Full Program Compiler v3.1.0
echo       Compiles the entire solution into a ready-to-run package
echo       Target Workspace: E:\Program Developing\Thai_OpenUtau\
echo ======================================================================
echo.

rem Step 1: Detect Project Directory Structure
set "PROJECT_ROOT=%~dp0"
if "%PROJECT_ROOT:~-1%"=="\" set "PROJECT_ROOT=%PROJECT_ROOT:~0,-1%"

if not "%~1"=="" (
    if exist "%~1" set "PROJECT_ROOT=%~1"
)

cd /d "%PROJECT_ROOT%" 2>nul

if exist "OpenUtau\OpenUtau.csproj" (
    set "TARGET_CSPROJ=OpenUtau\OpenUtau.csproj"
) else if exist "OpenUtau.csproj" (
    cd ..
    set "PROJECT_ROOT=%CD%"
    set "TARGET_CSPROJ=OpenUtau\OpenUtau.csproj"
) else if exist "Thai OpenUtau.sln" (
    set "TARGET_CSPROJ=OpenUtau\OpenUtau.csproj"
) else (
    color 0C
    echo [ERROR] Could not locate OpenUtau project or solution files.
    echo Please ensure this script is run from: E:\Program Developing\Thai_OpenUtau\
    echo Current Directory: %CD%
    echo.
    pause
    exit /b 1
)

echo [1/4] Project Environment Verified:
echo  - Project Root : %PROJECT_ROOT%
echo  - Target Csproj: %TARGET_CSPROJ%
echo.

rem Step 2: Validate .NET SDK Toolchain
echo [2/4] Verifying .NET SDK environment...
where dotnet >nul 2>&1
if !errorlevel! neq 0 (
    color 0C
    echo [ERROR] The "dotnet" CLI tool was not found in System PATH.
    echo Please install the .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set "DOTNET_VER=%%v"
echo [*] .NET SDK Version detected: %DOTNET_VER%
echo [OK] Toolchain confirmed ready.
echo.

rem Step 2.5: Automated Version Bumping (DELTA SYNTH Formula)
if exist "%PROJECT_ROOT%\BumpVersion.ps1" (
    echo [*] Executing DELTA SYNTH automated version bumper...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%PROJECT_ROOT%\BumpVersion.ps1"
    echo.
)

rem Step 3: Full Application Compilation and Publishing
set "OUTPUT_DIR=%PROJECT_ROOT%\Publish"
echo [3/4] Compiling and publishing full program to Release mode...
echo [*] Output Destination: %OUTPUT_DIR%
echo.

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%" >nul 2>&1

rem Execute publish with runtime identifier for Windows x64
dotnet publish "%TARGET_CSPROJ%" -c Release -r win-x64 --self-contained false -o "%OUTPUT_DIR%"

if !errorlevel! neq 0 (
    echo [*] Retrying publish with default target platform...
    dotnet publish "%TARGET_CSPROJ%" -c Release -o "%OUTPUT_DIR%"
)

if !errorlevel! neq 0 (
    color 0C
    echo.
    echo [ERROR] Compilation failed with errors. Please review the build log above.
    pause
    exit /b 1
)

echo.
echo [OK] Core compilation completed successfully.
echo.

rem Step 4: Asset Consolidation & Native Library Deployment
echo [4/4] Consolidating native libraries, plugins, and dependencies...

rem Consolidate Legacy Plugins (including Canon AutoPitchwriter)
if exist "%PROJECT_ROOT%\Plugins" (
    if not exist "%OUTPUT_DIR%\Plugins" mkdir "%OUTPUT_DIR%\Plugins" >nul 2>&1
    xcopy "%PROJECT_ROOT%\Plugins\*" "%OUTPUT_DIR%\Plugins\" /s /e /y /q >nul 2>&1
    echo [*] Plugins directory consolidated - Canon AutoPitchwriter ready.
)

rem Consolidate Builtin Plugin
if exist "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\bin\Release\net8.0\OpenUtau.Plugin.Builtin.dll" (
    copy /y "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\bin\Release\net8.0\OpenUtau.Plugin.Builtin.dll" "%OUTPUT_DIR%\" >nul 2>&1
    if not exist "%OUTPUT_DIR%\Plugins" mkdir "%OUTPUT_DIR%\Plugins" >nul 2>&1
    copy /y "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\bin\Release\net8.0\OpenUtau.Plugin.Builtin.dll" "%OUTPUT_DIR%\Plugins\" >nul 2>&1
    echo [*] OpenUtau.Plugin.Builtin.dll deployed.
)

rem Consolidate Worldline and SDL3 Native Libraries
if exist "%PROJECT_ROOT%\worldline.dll" (
    copy /y "%PROJECT_ROOT%\worldline.dll" "%OUTPUT_DIR%\" >nul 2>&1
    echo [*] worldline.dll consolidated.
)
if exist "%PROJECT_ROOT%\SDL3.dll" (
    copy /y "%PROJECT_ROOT%\SDL3.dll" "%OUTPUT_DIR%\" >nul 2>&1
    echo [*] SDL3.dll consolidated.
)

rem Consolidate Wavtools and Resamplers if present
if exist "%PROJECT_ROOT%\Wavtools" (
    if not exist "%OUTPUT_DIR%\Wavtools" mkdir "%OUTPUT_DIR%\Wavtools" >nul 2>&1
    xcopy "%PROJECT_ROOT%\Wavtools\*" "%OUTPUT_DIR%\Wavtools\" /s /e /y /q >nul 2>&1
    echo [*] Wavtools directory consolidated.
)
if exist "%PROJECT_ROOT%\Resamplers" (
    if not exist "%OUTPUT_DIR%\Resamplers" mkdir "%OUTPUT_DIR%\Resamplers" >nul 2>&1
    xcopy "%PROJECT_ROOT%\Resamplers\*" "%OUTPUT_DIR%\Resamplers\" /s /e /y /q >nul 2>&1
    echo [*] Resamplers directory consolidated.
)

rem Consolidate Thai Dictionaries
if exist "%PROJECT_ROOT%\Dictionaries" (
    if not exist "%OUTPUT_DIR%\Dictionaries" mkdir "%OUTPUT_DIR%\Dictionaries" >nul 2>&1
    xcopy "%PROJECT_ROOT%\Dictionaries\*" "%OUTPUT_DIR%\Dictionaries\" /s /e /y /q >nul 2>&1
    echo [*] Dictionaries consolidated to Publish.
)
if exist "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\Dictionary" (
    if not exist "%OUTPUT_DIR%\Dictionary" mkdir "%OUTPUT_DIR%\Dictionary" >nul 2>&1
    xcopy "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\Dictionary\*" "%OUTPUT_DIR%\Dictionary\" /s /e /y /q >nul 2>&1
    if not exist "%OUTPUT_DIR%\Dictionaries" mkdir "%OUTPUT_DIR%\Dictionaries" >nul 2>&1
    xcopy "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\Dictionary\*" "%OUTPUT_DIR%\Dictionaries\" /s /e /y /q >nul 2>&1
    echo [*] Builtin Dictionary consolidated to Publish.
)

rem Consolidate to All Rendered File if present
set "ALL_RENDERED=%PROJECT_ROOT%\All Rendered File"
if exist "%ALL_RENDERED%" (
    if not exist "%ALL_RENDERED%\Dictionaries" mkdir "%ALL_RENDERED%\Dictionaries" >nul 2>&1
    if exist "%PROJECT_ROOT%\Dictionaries" xcopy "%PROJECT_ROOT%\Dictionaries\*" "%ALL_RENDERED%\Dictionaries\" /s /e /y /q >nul 2>&1
    if exist "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\Dictionary" xcopy "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\Dictionary\*" "%ALL_RENDERED%\Dictionaries\" /s /e /y /q >nul 2>&1
    echo [*] Dictionaries deployed to All Rendered File.
)

rem Deploy Quick Launcher in Publish Folder
if exist "%PROJECT_ROOT%\Start-The-Program.bat" (
    copy /y "%PROJECT_ROOT%\Start-The-Program.bat" "%OUTPUT_DIR%\" >nul 2>&1
) else (
    (
        echo @echo off
        echo title DELTA SYNTH - Thai OpenUtau
        echo echo Starting Thai OpenUtau...
        echo start "" "OpenUtau.exe" %%*
        echo exit
    ) > "%OUTPUT_DIR%\Start-The-Program.bat"
)

color 0A
echo.
echo ======================================================================
echo  [SUCCESS] Thai OpenUtau compiled and published successfully!
echo ======================================================================
echo.
echo  Application Location:
echo   - Executable: %OUTPUT_DIR%\OpenUtau.exe
echo   - Launcher  : %OUTPUT_DIR%\Start-The-Program.bat
echo.
echo  The program is ready to run with all native libraries and plugins.
echo ======================================================================
echo.

set /p RUN_CHOICE="Do you want to launch Thai OpenUtau now? (Y/N): "
if /i "%RUN_CHOICE%"=="Y" (
    echo Launching Thai OpenUtau...
    cd /d "%OUTPUT_DIR%"
    start "" "OpenUtau.exe"
)

exit /b 0
