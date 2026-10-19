@echo off
:: ========================================================================
:: Made And Checked By DELTA SYNTH & Gemini AI
:: Original by DELTA SYNTH
:: Version: 1.0 | Date: 2026-08-25
:: Description: Drag and drop the target folder to extract TrackHeader.axaml
:: and TrackHeader.axaml.cs for AI analysis to restore Type 5 UI elements.
:: ========================================================================
title DELTA SYNTH - TrackHeader Files Extractor v1.0
color 0B
chcp 65001 >nul

echo =======================================================
echo    DELTA SYNTH - TrackHeader Extractor Tool
echo =======================================================
echo.

if "%~1"=="" (
    echo [ERROR] Please drag and drop the OpenUtau project folder onto this .bat file.
    pause
    exit /b 1
)

set "TARGET_DIR=%~1"
cd /d "%TARGET_DIR%" 2>nul

echo [INFO] Working Directory: %CD%
echo [INFO] Searching for TrackHeader files...

set "OUTPUT_FILE=%TARGET_DIR%\All Editing File For Agent\Extracted_TrackHeader.txt"
if not exist "%TARGET_DIR%\All Editing File For Agent" mkdir "%TARGET_DIR%\All Editing File For Agent"

echo ======================================================= > "%OUTPUT_FILE%"
echo Extracted Date and Time: %date% %time% >> "%OUTPUT_FILE%"
echo ======================================================= >> "%OUTPUT_FILE%"

:: Find and dump TrackHeader.axaml
for /r %%i in (TrackHeader.axaml) do (
    if exist "%%i" (
        echo [SUCCESS] Found: %%i
        echo. >> "%OUTPUT_FILE%"
        echo [FILE: TrackHeader.axaml] >> "%OUTPUT_FILE%"
        echo ======================================================= >> "%OUTPUT_FILE%"
        type "%%i" >> "%OUTPUT_FILE%"
        echo. >> "%OUTPUT_FILE%"
    )
)

:: Find and dump TrackHeader.axaml.cs
for /r %%i in (TrackHeader.axaml.cs) do (
    if exist "%%i" (
        echo [SUCCESS] Found: %%i
        echo. >> "%OUTPUT_FILE%"
        echo [FILE: TrackHeader.axaml.cs] >> "%OUTPUT_FILE%"
        echo ======================================================= >> "%OUTPUT_FILE%"
        type "%%i" >> "%OUTPUT_FILE%"
        echo. >> "%OUTPUT_FILE%"
    )
)

echo [INFO] Extraction complete. Opening the text file for you...
start "" "%OUTPUT_FILE%"

echo.
echo [SUCCESS] Process finished! Please copy the contents to the AI.
echo =======================================================
pause
exit /b 0