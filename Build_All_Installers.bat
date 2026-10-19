@echo off
:: ============================================================================
:: Made And Checked By DELTA SYNTH & Gemini AI
:: Version: v2.0
:: Date: 2026-07-31
:: Description: Builds Thai OpenUtau By DELTA SYNTH. Packages .EXE, .MSI, 
::              and .ZIP with self-contained .NET 10.
:: ============================================================================

chcp 65001 >nul
setlocal EnableExtensions EnableDelayedExpansion

echo ====================================================================
echo Thai OpenUtau By DELTA SYNTH - All-in-One Builder (EXE, MSI, ZIP)
echo ====================================================================
echo.

:: 1. Check .NET SDK
echo [0/5] Checking .NET SDK installation...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] .NET SDK was not found on this system!
    pause
    exit /b 1
)
for /f "tokens=*" %%v in ('dotnet --version 2^>^&1') do set DOTNET_VER=%%v
echo [OK] Found .NET SDK Version: %DOTNET_VER%

:: 2. Check WiX Toolset
set "WIX_EXE=%USERPROFILE%\.dotnet\tools\wix.exe"
if not exist "%WIX_EXE%" (
    echo [ERROR] WiX Toolset was not found at "%WIX_EXE%".
    echo Please install by running: dotnet tool install --global wix
    pause
    exit /b 1
)
echo [OK] Found WiX Toolset.

:: 3. Reading Version
echo.
echo [1/5] Reading Next Version...
set "NEW_VER="
for /f "usebackq delims=" %%v in (`powershell -NoProfile -ExecutionPolicy Bypass -File "BumpVersion.ps1" -Preview`) do set "NEW_VER=%%v"
if not defined NEW_VER (
    echo [ERROR] Failed to calculate the next version.
    pause
    exit /b 1
)
echo Package Version: %NEW_VER%

:: 4. Setup Export Directory
set "EXPORT_BASE=Export File"
set "EXPORT_DIR=%EXPORT_BASE%\Thai OpenUtau By DELTA SYNTH version %NEW_VER%"
if not exist "%EXPORT_DIR%" mkdir "%EXPORT_DIR%"

:: 5. Compile Project
echo.
echo [2/5] Compiling Project for win-x64 (self-contained)...
set "PUBLISH_DIR=bin\PublishOutput_winx64"
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"

dotnet publish OpenUtau\OpenUtau.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=true -p:Version="%NEW_VER%" -o "%PUBLISH_DIR%"
if errorlevel 1 (
    echo [ERROR] Publish failed!
    pause
    exit /b 1
)

:: Prepare DirectML and extra folders
powershell -NoProfile -ExecutionPolicy Bypass -File "PrepareDirectML.ps1" -Architecture x64 -Destination "%PUBLISH_DIR%"
if exist "Resamplers" xcopy "Resamplers" "%PUBLISH_DIR%\Resamplers" /E /I /Y >nul
if not exist "%PUBLISH_DIR%\Singers" mkdir "%PUBLISH_DIR%\Singers"
if exist "Singers" xcopy "Singers" "%PUBLISH_DIR%\Singers" /E /I /Y >nul

:: 6. Package ZIP
echo.
echo [3/5] Packaging ZIP Archive...
powershell -NoProfile -Command "Compress-Archive -Path '%PUBLISH_DIR%\*' -DestinationPath '%EXPORT_DIR%\Thai_OpenUtau_v%NEW_VER%.zip' -Force"
echo [OK] ZIP created: %EXPORT_DIR%\Thai_OpenUtau_v%NEW_VER%.zip

:: 7. Package MSI
echo.
echo [4/5] Packaging MSI Installer...
"%WIX_EXE%" build Package.wxs -arch x64 -d AppVersion="%NEW_VER%" -d PublishDir="%PUBLISH_DIR%" -o "%EXPORT_DIR%\Thai_OpenUtau_v%NEW_VER%.msi"
if errorlevel 1 (
    echo [ERROR] MSI Creation failed!
    pause
    exit /b 1
)
echo [OK] MSI created: %EXPORT_DIR%\Thai_OpenUtau_v%NEW_VER%.msi

:: 8. Package EXE (WinRAR)
echo.
echo [5/5] Packaging EXE Installer (WinRAR SFX)...
set "WINRAR=C:\Program Files\WinRAR\WinRAR.exe"
if exist "%WINRAR%" (
    echo Path=Thai OpenUtau By DELTA SYNTH> sfx_config.txt
    echo SavePath>> sfx_config.txt
    echo Setup=OpenUtau - DELTA SYNTH.exe>> sfx_config.txt
    echo Silent=1>> sfx_config.txt
    echo Overwrite=1>> sfx_config.txt

    "%WINRAR%" a -r -ep1 -sfx -z"sfx_config.txt" "%EXPORT_DIR%\Thai_OpenUtau_Installer_v%NEW_VER%.exe" "%PUBLISH_DIR%\*"
    del sfx_config.txt
    echo [OK] EXE created: %EXPORT_DIR%\Thai_OpenUtau_Installer_v%NEW_VER%.exe
) else (
    echo [WARNING] WinRAR not found. Skipping EXE SFX build.
)

:: 9. Persist Version
set "PERSISTED_VER="
for /f "usebackq delims=" %%v in (`powershell -NoProfile -ExecutionPolicy Bypass -File "BumpVersion.ps1"`) do set "PERSISTED_VER=%%v"
if /i not "%PERSISTED_VER%"=="%NEW_VER%" (
    echo [WARNING] Persisted version %PERSISTED_VER% does not match package version %NEW_VER%.
)

echo.
echo ====================================================================
echo BUILD SUCCESSFUL! All packages are in:
echo "%EXPORT_DIR%"
echo ====================================================================
:: Do not pause in automated run
:: pause
exit /b 0
