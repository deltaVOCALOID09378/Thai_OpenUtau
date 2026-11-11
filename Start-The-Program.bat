@echo off
:: ========================================================================
:: Date         : 10/11/2026 (B.E. 2569 / A.D. 2026)
:: File Name    : Start-The-Program.bat
:: Version      : 2.1
:: Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
:: File Location: Start-The-Program.bat
:: Description  : Smart application launcher for Thai OpenUtau supporting standalone root,
::                Publish, All Rendered File, and Release/Debug build directories.
:: ========================================================================
setlocal enabledelayedexpansion
title Starting Thai_OpenUtau...
chcp 65001 >nul

set "ROOT_DIR=%~dp0"
if "%ROOT_DIR:~-1%"=="\" set "ROOT_DIR=%ROOT_DIR:~0,-1%"

set "LOG_DIR=%ROOT_DIR%\Logs"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%" 2>nul
set "LOG_FILE=%LOG_DIR%\Start-The-Program.log"

set "APP_EXE="
set "APP_DIR="

:: Check Direct Standalone Root (Priority 0)
if exist "%ROOT_DIR%\OpenUtau.exe" (
    set "APP_EXE=%ROOT_DIR%\OpenUtau.exe"
    set "APP_DIR=%ROOT_DIR%"
    goto :launch
)

:: Check Publish (Self-Contained Release - Priority 1)
if exist "%ROOT_DIR%\Publish\OpenUtau.exe" (
    set "APP_EXE=%ROOT_DIR%\Publish\OpenUtau.exe"
    set "APP_DIR=%ROOT_DIR%\Publish"
    goto :launch
)

:: Check All Rendered File
if exist "%ROOT_DIR%\All Rendered File\OpenUtau.exe" (
    set "APP_EXE=%ROOT_DIR%\All Rendered File\OpenUtau.exe"
    set "APP_DIR=%ROOT_DIR%\All Rendered File"
    goto :launch
)

:: Search Release builds dynamically across any .NET version (net8.0, net9.0, net10.0, etc.)
for /d %%D in ("%ROOT_DIR%\OpenUtau\bin\Release\net*-windows") do (
    if exist "%%D\OpenUtau.exe" (
        set "APP_EXE=%%D\OpenUtau.exe"
        set "APP_DIR=%%D"
        goto :launch
    )
)

for /d %%D in ("%ROOT_DIR%\OpenUtau\bin\Release\net*") do (
    if exist "%%D\OpenUtau.exe" (
        set "APP_EXE=%%D\OpenUtau.exe"
        set "APP_DIR=%%D"
        goto :launch
    )
)

:: Search Debug builds
for /d %%D in ("%ROOT_DIR%\OpenUtau\bin\Debug\net*-windows") do (
    if exist "%%D\OpenUtau.exe" (
        set "APP_EXE=%%D\OpenUtau.exe"
        set "APP_DIR=%%D"
        goto :launch
    )
)

for /d %%D in ("%ROOT_DIR%\OpenUtau\bin\Debug\net*") do (
    if exist "%%D\OpenUtau.exe" (
        set "APP_EXE=%%D\OpenUtau.exe"
        set "APP_DIR=%%D"
        goto :launch
    )
)

echo [ERROR] OpenUtau executable not found.
echo [INFO] Please run Install-File.bat first to build the program.
echo [%DATE% %TIME%] [ERROR] OpenUtau.exe not found in "%ROOT_DIR%". >> "%LOG_FILE%"
pause
exit /b 1

:launch
echo [INFO] Launching Thai OpenUtau: "%APP_EXE%"
echo [%DATE% %TIME%] [INFO] Launching "%APP_EXE%" >> "%LOG_FILE%"
cd /d "%APP_DIR%"
if "%~1"=="" (
    start "" "%APP_EXE%"
) else (
    start "" "%APP_EXE%" "%~1"
)
exit /b 0
