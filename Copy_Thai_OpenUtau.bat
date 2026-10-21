@echo off
:: ========================================================================
:: File Name : Copy_Thai_OpenUtau.bat
:: Version   : 2.0.0
:: Target    : E:\Program Developing\Thai_OpenUtau
:: Function  : Copy files from Google Drive to E:\Program Developing\Thai_OpenUtau,
::             maintain directory structure, overwrite existing files immediately,
::             and keep the local development environment 100% up-to-date.
:: Credit    : DELTA SYNTH AI Engine
:: ========================================================================

setlocal enabledelayedexpansion
title DELTA SYNTH - Update Thai_OpenUtau to Local Machine
color 0B
chcp 65001 >nul

echo ======================================================================
echo       DELTA SYNTH - Thai OpenUtau Local Synchronizer v2.0.0
echo       Target: E:\Program Developing\Thai_OpenUtau
echo ======================================================================
echo.

set "DEST_DIR=E:\Program Developing\Thai_OpenUtau"

rem Auto-detect Source Directory (Google Drive)
set "SOURCE_DIR=%~dp0"
if "%SOURCE_DIR:~-1%"=="\" set "SOURCE_DIR=%SOURCE_DIR:~0,-1%"

rem Check if run inside Scripts folder
if exist "%SOURCE_DIR%\..\Thai OpenUtau.sln" (
    set "SOURCE_DIR=%SOURCE_DIR%\.."
)

rem If current directory is not the source, check Google Drive mount paths
if not exist "%SOURCE_DIR%\Thai OpenUtau.sln" (
    if exist "K:\.shortcut-targets-by-id\1cjZlS0V-RDRCa25_otwRqKaCSyKxaZaQ\Thai_OpenUtau\Thai OpenUtau.sln" (
        set "SOURCE_DIR=K:\.shortcut-targets-by-id\1cjZlS0V-RDRCa25_otwRqKaCSyKxaZaQ\Thai_OpenUtau"
    ) else if exist "G:\My Drive\Thai_OpenUtau\Thai OpenUtau.sln" (
        set "SOURCE_DIR=G:\My Drive\Thai_OpenUtau"
    ) else if exist "G:\.shortcut-targets-by-id\1cjZlS0V-RDRCa25_otwRqKaCSyKxaZaQ\Thai_OpenUtau\Thai OpenUtau.sln" (
        set "SOURCE_DIR=G:\.shortcut-targets-by-id\1cjZlS0V-RDRCa25_otwRqKaCSyKxaZaQ\Thai_OpenUtau"
    )
)

echo [*] Source Directory     : %SOURCE_DIR%
echo [*] Destination Directory: %DEST_DIR%
echo.

if not exist "%DEST_DIR%" (
    echo [*] Creating destination directory...
    mkdir "%DEST_DIR%" >nul 2>&1
)

if not exist "%SOURCE_DIR%\Thai OpenUtau.sln" (
    color 0C
    echo [ERROR] Could not automatically locate the Thai_OpenUtau source folder.
    echo Please run this script directly from your Google Drive Thai_OpenUtau folder,
    echo or drag and drop your Google Drive Thai_OpenUtau folder onto this window:
    set /p "USER_SRC=> "
    if defined USER_SRC (
        set "USER_SRC=!USER_SRC:"=!"
        if exist "!USER_SRC!\Thai OpenUtau.sln" (
            set "SOURCE_DIR=!USER_SRC!"
        ) else (
            echo [ERROR] Invalid path. Exiting.
            pause
            exit /b 1
        )
    ) else (
        pause
        exit /b 1
    )
)

echo [INFO] Updating files to "%DEST_DIR%"...
echo.

robocopy "%SOURCE_DIR%" "%DEST_DIR%" /E /IS /IT /R:1 /W:1 /XD ".git" ".vs" "bin" "obj" "Publish_Temp"

if !errorlevel! geq 8 (
    color 0C
    echo.
    echo [ERROR] Synchronization encountered an issue. Exit code: !errorlevel!
) else (
    color 0A
    echo.
    echo ======================================================================
    echo  [SUCCESS] All files successfully updated to:
    echo  %DEST_DIR%
    echo ======================================================================
    echo.
    echo  Latest updates included:
    echo   - Modern Slate-Indigo Eye-Comfort UI Theme (DarkTheme.axaml)
    echo   - Canon AutoPitchwriter Plugin & Native Configuration
    echo   - Moresampler Ultra-Stable Configuration
    echo   - Full Program Compiler (Compile_Full_Program.bat)
    echo ======================================================================
)

echo.
pause
exit /b 0
