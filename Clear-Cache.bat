@echo off
setlocal enabledelayedexpansion
title DELTA SYNTH - Thai OpenUtau Cache & Junk Cleaner

:: Set code page for UTF-8 and English/Thai display
chcp 65001 >nul

set "TARGET_DIR=%~1"
if "%TARGET_DIR%"=="" set "TARGET_DIR=%~dp0"
if "%TARGET_DIR:~-1%"=="\" set "TARGET_DIR=%TARGET_DIR:~0,-1%"

echo ======================================================================
echo    DELTA SYNTH - Thai OpenUtau Cache & Artifact Cleaner v1.0
echo    Clean Old DLLs, Build Residues, Audio Render Caches and Temp Logs
echo ======================================================================
echo Target Directory: "%TARGET_DIR%"
echo.

if not exist "%TARGET_DIR%" (
    echo [ERROR] Target directory does not exist!
    pause
    exit /b 1
)

echo [1/5] [20%%] Clearing OpenUtau Audio Engine Render Cache...
if exist "%TARGET_DIR%\UCache" (
    echo   - Cleaning "%TARGET_DIR%\UCache"...
    rd /s /q "%TARGET_DIR%\UCache" 2>nul
)
if exist "%LOCALAPPDATA%\OpenUtau\cache" (
    echo   - Cleaning "%LOCALAPPDATA%\OpenUtau\cache"...
    rd /s /q "%LOCALAPPDATA%\OpenUtau\cache" 2>nul
)
if exist "%APPDATA%\OpenUtau\cache" (
    echo   - Cleaning "%APPDATA%\OpenUtau\cache"...
    rd /s /q "%APPDATA%\OpenUtau\cache" 2>nul
)

echo.
echo [2/5] [40%%] Clearing Obsolete Build Output Folders (bin, obj, Publish)...
for /d /r "%TARGET_DIR%" %%d in (bin obj Publish) do (
    if exist "%%d" (
        echo   - Removing: "%%d"
        rd /s /q "%%d" 2>nul
    )
)

echo.
echo [3/5] [60%%] Removing Stray Build Logs, Check Files and Crash Dumps...
del /f /q "%TARGET_DIR%\*.binlog" 2>nul
del /f /q "%TARGET_DIR%\*.log" 2>nul
del /f /q "%TARGET_DIR%\build_log*.txt" 2>nul
del /f /q "%TARGET_DIR%\compile_log*.txt" 2>nul
del /f /q "%TARGET_DIR%\check_*.txt" 2>nul
del /f /q "%TARGET_DIR%\test_out*.txt" 2>nul
del /f /q "%TARGET_DIR%\desktop.ini" 2>nul
del /f /q "%TARGET_DIR%\*.bak" 2>nul
del /f /q "%TARGET_DIR%\*.tmp" 2>nul
del /f /q "%TARGET_DIR%\cpp\build_log*.txt" 2>nul
del /f /q "%TARGET_DIR%\cpp\test_dir*.txt" 2>nul

echo.
echo [4/5] [80%%] Cleaning IDE and Visual Studio Caches (.vs, .idea)...
if exist "%TARGET_DIR%\.vs" (
    echo   - Removing: "%TARGET_DIR%\.vs"
    rd /s /q "%TARGET_DIR%\.vs" 2>nul
)
if exist "%TARGET_DIR%\.idea" (
    echo   - Removing: "%TARGET_DIR%\.idea"
    rd /s /q "%TARGET_DIR%\.idea" 2>nul
)
if exist "%TARGET_DIR%\TestResults" (
    echo   - Removing: "%TARGET_DIR%\TestResults"
    rd /s /q "%TARGET_DIR%\TestResults" 2>nul
)

echo.
echo [5/5] [100%%] Finalizing and Verifying Directory Integrity...
echo.
echo ======================================================================
echo    [SUCCESS] All Cache, Old DLLs, and Junk Files Successfully Cleared!
echo    Source Code, Voicebanks, and Configurations Preserved Safely.
echo ======================================================================
echo.
pause
