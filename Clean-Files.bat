@echo off
setlocal enabledelayedexpansion
title Cleaning Junk Files...

:: Check if a target directory or file was dragged and dropped onto this script
set "TARGET_DIR=%~1"
if "%TARGET_DIR%"=="" set "TARGET_DIR=%~dp0"

:: Remove trailing backslash if present
if "%TARGET_DIR:~-1%"=="\" set "TARGET_DIR=%TARGET_DIR:~0,-1%"

echo ===================================================
echo   DELTA SYNTH - Thai_OpenUtau Junk File Cleaner
echo ===================================================
echo Target: "%TARGET_DIR%"
echo.

if not exist "%TARGET_DIR%" (
    echo Error: Target directory does not exist!
    pause
    exit /b 1
)

echo Progress: 10%% - Removing build artifacts (bin and obj)...
for /d /r "%TARGET_DIR%" %%d in (bin obj) do (
    if exist "%%d" (
        echo Deleting: "%%d"
        rd /s /q "%%d" 2>nul
    )
)

echo Progress: 30%% - Removing Visual Studio and cache folders (.vs, UCache)...
if exist "%TARGET_DIR%\.vs" (
    echo Deleting: "%TARGET_DIR%\.vs"
    rd /s /q "%TARGET_DIR%\.vs" 2>nul
)
if exist "%TARGET_DIR%\UCache" (
    echo Deleting: "%TARGET_DIR%\UCache"
    rd /s /q "%TARGET_DIR%\UCache" 2>nul
)
if exist "%TARGET_DIR%\.tmp.driveupload" (
    echo Deleting: "%TARGET_DIR%\.tmp.driveupload"
    rd /s /q "%TARGET_DIR%\.tmp.driveupload" 2>nul
)

echo Progress: 60%% - Removing temporary log files...
del /f /q "%TARGET_DIR%\*.log" 2>nul
del /f /q "%TARGET_DIR%\*_log.txt" 2>nul
del /f /q "%TARGET_DIR%\build_out.txt" 2>nul
del /f /q "%TARGET_DIR%\compile_log.txt" 2>nul
del /f /q "%TARGET_DIR%\log.txt" 2>nul
del /f /q "%TARGET_DIR%\*.tmp" 2>nul
del /f /q "%TARGET_DIR%\cpp\build_log.txt" 2>nul
del /f /q "%TARGET_DIR%\cpp\build_out.txt" 2>nul
del /f /q "%TARGET_DIR%\cpp\test_dir.txt" 2>nul

echo Progress: 90%% - Removing test results and benchmarks...
for /d /r "%TARGET_DIR%" %%d in (TestResults BenchmarkDotNet.Artifacts) do (
    if exist "%%d" (
        echo Deleting: "%%d"
        rd /s /q "%%d" 2>nul
    )
)

echo Progress: 100%% - Cleanup completed successfully!
echo.
pause
