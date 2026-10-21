@echo off
rem ======================================================================
rem Date         : 11/10/2026 (B.E. 2569 / A.D. 2026)
rem File Name    : Package_Program.bat
rem Version      : 1.0
rem Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
rem Original by  : DELTA SYNTH
rem Location     : Root and Scripts Directory
rem Description  : Professional Standalone Packaging Pipeline for Thai OpenUtau.
rem                Creates Standalone ZIP Archive and Executable SFX Installer.
rem                Supports Drag & Drop of any published directory.
rem ======================================================================

setlocal enabledelayedexpansion
title DELTA SYNTH - Thai OpenUtau Packaging Pipeline v1.0
color 0B
chcp 65001 >nul

echo ======================================================================
echo       DELTA SYNTH - Thai OpenUtau Standalone Packaging Pipeline
echo       Generates Standalone ZIP and Self-Extracting GUI Installer
echo ======================================================================
echo.

rem Step 1: Detect Project Root and Target Publish Directory
set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

set "PROJECT_ROOT=%SCRIPT_DIR%"
if exist "%PROJECT_ROOT%\..\Publish" (
    set "PROJECT_ROOT=%PROJECT_ROOT%\.."
)

set "TARGET_DIR=%PROJECT_ROOT%\Publish"
if not "%~1"=="" (
    if exist "%~1" (
        set "TARGET_DIR=%~1"
        echo [INFO] Custom target directory specified by Drag and Drop:
        echo        %TARGET_DIR%
        echo.
    )
)

if not exist "%TARGET_DIR%\OpenUtau.exe" (
    color 0C
    echo [ERROR] Could not find OpenUtau.exe in target directory:
    echo         %TARGET_DIR%
    echo Please build the project first using Compile_Full_Program.bat
    echo.
    pause
    exit /b 1
)

echo [1/3] Target directory verified:
echo  - Target Path : %TARGET_DIR%
echo  - Executable  : %TARGET_DIR%\OpenUtau.exe
echo.

rem Step 2: Prepare Export Directories
set "EXPORT_DIR=%PROJECT_ROOT%\Published\Version no. 3.0.0"
if not exist "%EXPORT_DIR%" mkdir "%EXPORT_DIR%" >nul 2>&1

echo [2/3] Validating compression and installer toolchains...

set "SEVENZIP=C:\Program Files\7-Zip\7z.exe"
if not exist "%SEVENZIP%" (
    where 7z.exe >nul 2>&1
    if !errorlevel! equ 0 for /f "tokens=*" %%p in ('where 7z.exe') do set "SEVENZIP=%%p"
)

set "RAR_EXE=C:\Program Files\WinRAR\Rar.exe"
if not exist "%RAR_EXE%" (
    where rar.exe >nul 2>&1
    if !errorlevel! equ 0 for /f "tokens=*" %%p in ('where rar.exe') do set "RAR_EXE=%%p"
)

set "PACKAGED_COUNT=0"

rem Step 3: Create Standalone ZIP Archive
echo.
echo [3/3] Packaging distribution assets...
if exist "%SEVENZIP%" (
    echo [*] Creating Standalone ZIP Archive with 7-Zip (Ultra Compression)...
    "%SEVENZIP%" a -tzip -mx=9 "%EXPORT_DIR%\Thai_OpenUtau_v3.0.0_Standalone.zip" "%TARGET_DIR%\*" >nul 2>&1
    if !errorlevel! equ 0 (
        copy /y "%EXPORT_DIR%\Thai_OpenUtau_v3.0.0_Standalone.zip" "%PROJECT_ROOT%\" >nul 2>&1
        echo [OK] Standalone ZIP created successfully:
        echo      - %EXPORT_DIR%\Thai_OpenUtau_v3.0.0_Standalone.zip
        echo      - %PROJECT_ROOT%\Thai_OpenUtau_v3.0.0_Standalone.zip
        set /a PACKAGED_COUNT+=1
    ) else (
        echo [WARNING] Failed to create 7-Zip standalone archive.
    )
) else (
    echo [NOTICE] 7-Zip was not found. Creating fallback ZIP with PowerShell...
    powershell -NoProfile -Command "Compress-Archive -Path '%TARGET_DIR%\*' -DestinationPath '%EXPORT_DIR%\Thai_OpenUtau_v3.0.0_Standalone.zip' -Force"
    copy /y "%EXPORT_DIR%\Thai_OpenUtau_v3.0.0_Standalone.zip" "%PROJECT_ROOT%\" >nul 2>&1
    echo [OK] Fallback ZIP created successfully.
    set /a PACKAGED_COUNT+=1
)

rem Step 4: Create Standalone Self-Extracting Installer (SFX)
if exist "%RAR_EXE%" (
    echo.
    echo [*] Creating Standalone GUI Installer (SFX) with RAR Engine...
    (
        echo Path=Thai OpenUtau
        echo SavePath
        echo Setup=Start-The-Program.bat
        echo Silent=0
        echo Overwrite=1
        echo Title=DELTA SYNTH - Thai OpenUtau v3.0.0 Installer
        echo Text
        echo {
        echo ยินดีต้อนรับสู่ตัวติดตั้ง Thai OpenUtau v3.0.0 โดย DELTA SYNTH
        echo ระบบประมวลผลเสียงภาษาไทยครบวงจร ^(VCCV, VCV, CpV, Arpasing^)
        echo พร้อมเอนจิน Native Worldline DSP, Moresampler และ AutoPitchwriter
        echo }
        echo Shortcut=D, "Start-The-Program.bat", "", "Thai OpenUtau โดย DELTA SYNTH", "Thai OpenUtau"
        echo Shortcut=P, "Start-The-Program.bat", "", "Thai OpenUtau โดย DELTA SYNTH", "Thai OpenUtau"
    ) > "%TEMP%\sfx_config.txt"

    "%RAR_EXE%" a -r -ep1 -sfx -m5 -z"%TEMP%\sfx_config.txt" -y "%EXPORT_DIR%\Thai_OpenUtau_Installer_v3.0.0.exe" "%TARGET_DIR%\*" >nul 2>&1
    if !errorlevel! equ 0 (
        copy /y "%EXPORT_DIR%\Thai_OpenUtau_Installer_v3.0.0.exe" "%PROJECT_ROOT%\" >nul 2>&1
        echo [OK] Standalone Installer EXE created successfully:
        echo      - %EXPORT_DIR%\Thai_OpenUtau_Installer_v3.0.0.exe
        echo      - %PROJECT_ROOT%\Thai_OpenUtau_Installer_v3.0.0.exe
        set /a PACKAGED_COUNT+=1
    ) else (
        echo [WARNING] Failed to create SFX installer.
    )
    del "%TEMP%\sfx_config.txt" >nul 2>&1
) else (
    echo [NOTICE] RAR engine not found. Skipping SFX installer generation.
)

color 0A
echo.
echo ======================================================================
echo  [SUCCESS] Packaging process completed (%PACKAGED_COUNT% package targets ready)!
echo ======================================================================
echo.
echo  Distribution Packages Available:
echo   1. Standalone Installer : %EXPORT_DIR%\Thai_OpenUtau_Installer_v3.0.0.exe
echo   2. Standalone ZIP Pack  : %EXPORT_DIR%\Thai_OpenUtau_v3.0.0_Standalone.zip
echo   3. Root Mirrors         : %PROJECT_ROOT%\Thai_OpenUtau_Installer_v3.0.0.exe
echo                             %PROJECT_ROOT%\Thai_OpenUtau_v3.0.0_Standalone.zip
echo ======================================================================
echo.

exit /b 0
