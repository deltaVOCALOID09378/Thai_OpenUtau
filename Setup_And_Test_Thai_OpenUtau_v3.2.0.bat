@echo off
rem ======================================================================
rem Date         : 22/10/2026 (B.E. 2569 / A.D. 2026)
rem File Name    : Setup_And_Test_Thai_OpenUtau_v3.2.0.bat
rem Version      : 3.2.0
rem Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
rem Description  : One-click In-Place Source Updater, Full Release Builder,
rem                Installer Generator, Shortcut Creator, and Test Launcher.
rem ======================================================================

setlocal enabledelayedexpansion
title DELTA SYNTH - Thai OpenUtau v3.2.0 Setup and Test Suite
color 0B
chcp 65001 >nul

echo ======================================================================
echo   DELTA SYNTH - Thai OpenUtau v3.2.0 Complete Setup and Test Suite
echo ======================================================================
echo.

set "PAYLOAD_DIR=%~dp0"
if "%PAYLOAD_DIR:~-1%"=="\" set "PAYLOAD_DIR=%PAYLOAD_DIR:~0,-1%"

set "PROJECT_ROOT=D:\Program Developing\Thai_OpenUtau"
if not exist "%PROJECT_ROOT%\OpenUtau" set "PROJECT_ROOT=E:\Program Developing\Thai_OpenUtau"
if not exist "%PROJECT_ROOT%\OpenUtau" (
    if exist "%PAYLOAD_DIR%\OpenUtau\OpenUtau.csproj" (
        set "PROJECT_ROOT=%PAYLOAD_DIR%"
    ) else if exist "%PAYLOAD_DIR%\..\OpenUtau\OpenUtau.csproj" (
        for %%I in ("%PAYLOAD_DIR%\..") do set "PROJECT_ROOT=%%~fI"
    )
)

echo [1/4] Deploying tuned v3.2.0 source code and scripts in-place...
if exist "%PROJECT_ROOT%\OpenUtau" (
    echo  - Target Project Folder: %PROJECT_ROOT%
    if not "%PAYLOAD_DIR%"=="%PROJECT_ROOT%" (
        if exist "%PAYLOAD_DIR%\OpenUtau.Core" xcopy "%PAYLOAD_DIR%\OpenUtau.Core\*" "%PROJECT_ROOT%\OpenUtau.Core\" /s /e /y /q >nul 2>&1
        if exist "%PAYLOAD_DIR%\OpenUtau" xcopy "%PAYLOAD_DIR%\OpenUtau\*" "%PROJECT_ROOT%\OpenUtau\" /s /e /y /q >nul 2>&1
        if exist "%PAYLOAD_DIR%\Resamplers" xcopy "%PAYLOAD_DIR%\Resamplers\*" "%PROJECT_ROOT%\Resamplers\" /s /e /y /q >nul 2>&1
        if exist "%PAYLOAD_DIR%\Compile_Full_Program.bat" copy /y "%PAYLOAD_DIR%\Compile_Full_Program.bat" "%PROJECT_ROOT%\" >nul 2>&1
        if exist "%PAYLOAD_DIR%\Package_Program.bat" copy /y "%PAYLOAD_DIR%\Package_Program.bat" "%PROJECT_ROOT%\" >nul 2>&1
        if exist "%PAYLOAD_DIR%\Thai_OpenUtau_Launcher.exe" (
            copy /y "%PAYLOAD_DIR%\Thai_OpenUtau_Launcher.exe" "%PROJECT_ROOT%\" >nul 2>&1
            if exist "%PROJECT_ROOT%\All Rendered File" copy /y "%PAYLOAD_DIR%\Thai_OpenUtau_Launcher.exe" "%PROJECT_ROOT%\All Rendered File\" >nul 2>&1
        )
    )
    echo [OK] All tuned source files and scripts updated in-place.
) else (
    echo [INFO] Standalone test mode detected.
)

echo.
echo [2/4] Compiling and packaging full application...
where dotnet >nul 2>&1
if !errorlevel! equ 0 (
    if exist "%PROJECT_ROOT%\Compile_Full_Program.bat" (
        call "%PROJECT_ROOT%\Compile_Full_Program.bat" "%PROJECT_ROOT%" --auto
    )
) else (
    echo [NOTICE] .NET SDK CLI not found in PATH; using pre-rendered binaries in All Rendered File.
    if exist "%PROJECT_ROOT%\Package_Program.bat" (
        call "%PROJECT_ROOT%\Package_Program.bat" "%PROJECT_ROOT%\All Rendered File" --auto
    )
)

echo.
echo [3/4] Creating Desktop shortcut for immediate testing...
set "APP_EXE=%PROJECT_ROOT%\Publish\OpenUtau.exe"
if not exist "!APP_EXE!" set "APP_EXE=%PROJECT_ROOT%\All Rendered File\OpenUtau.exe"
if exist "!APP_EXE!" (
    for %%A in ("!APP_EXE!") do set "APP_WORK_DIR=%%~dpA"
    powershell -NoProfile -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut([Environment]::GetFolderPath('Desktop') + '\Thai OpenUtau v3.2.0 (DELTA SYNTH).lnk'); $s.TargetPath = '!APP_EXE!'; $s.WorkingDirectory = '!APP_WORK_DIR!'; $s.Save()" >nul 2>&1
    echo [OK] Desktop shortcut created: Thai OpenUtau v3.2.0 (DELTA SYNTH).lnk
)

echo.
echo [4/4] Launching Thai OpenUtau v3.2.0 for testing...
if exist "!APP_EXE!" (
    start "" /d "!APP_WORK_DIR!" "!APP_EXE!"
    echo [SUCCESS] Thai OpenUtau v3.2.0 launched!
) else (
    echo [INFO] Setup complete. Run Start-The-Program.bat or Compile_Full_Program.bat to start.
)

exit /b 0
