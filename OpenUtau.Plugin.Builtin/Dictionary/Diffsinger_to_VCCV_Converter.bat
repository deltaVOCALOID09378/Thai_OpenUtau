@echo off
REM ============================================================================
REM 1. Date: 29/10/2569 (2026-10-29)
REM 2. File Name: Diffsinger_Phoneme_Converter_v1.0.bat
REM 3. Version: 1.4
REM 4. Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by Patiphat Wongyai (Delta) ]
REM 5. Location: Root/Tools/
REM 6. Description: Advanced Drag & Drop Dictionary Converter with dedicated output folder support.
REM ============================================================================

chcp 65001 >nul
setlocal enabledelayedexpansion

title DELTA SYNTH - Advanced Phoneme Converter v1.4
color 0B

echo ============================================================================
echo [OptiLink System] Advanced Thai DiffSinger to VCCV ^& Arpasing Converter
echo ============================================================================
echo.

set "START_TIME=%TIME%"
echo [SYSTEM] Process started at: %START_TIME%
echo.

set "TARGET_FILE=%~1"
if "%TARGET_FILE%"=="" (
    if exist "dict.txt" (
        set "TARGET_FILE=dict.txt"
    ) else (
        echo [ERROR] No input file detected.
        echo [INFO] Please drag and drop 'dict.txt' or any dictionary file onto this script.
        echo.
        pause
        exit /b 1
    )
)

REM Detect Python Engine Runtime
set "PYTHON_EXEC=python"
if exist "runtime\python.exe" (
    set "PYTHON_EXEC=runtime\python.exe"
)

REM Verify Python Script Existence
set "PY_SCRIPT=Diffsinger_to_VCCV_Converter.py"
if not exist "%PY_SCRIPT%" (
    echo [ERROR] Core processing script '%PY_SCRIPT%' not found.
    echo [INFO] Please ensure the Python script is placed in the same central tools directory.
    echo.
    pause
    exit /b 1
)

echo [INFO] Utilizing Python Engine: %PYTHON_EXEC%
echo [INFO] Target Dictionary: %TARGET_FILE%
echo.

"%PYTHON_EXEC%" "%PY_SCRIPT%" "%TARGET_FILE%"
set "CONVERT_EXIT_CODE=!errorlevel!"

if not "!CONVERT_EXIT_CODE!"=="0" (
    echo.
    echo [ERROR] Dictionary conversion encountered an issue.
    pause
    exit /b !CONVERT_EXIT_CODE!
)

echo ============================================================================
echo [SYSTEM] Process completed successfully.
echo ============================================================================
echo.
pause
exit /b 0