@echo off
setlocal enabledelayedexpansion
title Starting Thai_OpenUtau...
chcp 65001 >nul
set "DOTNET_ROLL_FORWARD=LatestMajor"

set "ROOT_DIR=%~dp0"
if "%ROOT_DIR:~-1%"=="\" set "ROOT_DIR=%ROOT_DIR:~0,-1%"

set "APP_EXE="
set "APP_DIR="

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
pause
exit /b 1

:launch
echo [INFO] Launching Thai OpenUtau: "%APP_EXE%"
cd /d "%APP_DIR%"
start "" "%APP_EXE%"
exit /b 0
