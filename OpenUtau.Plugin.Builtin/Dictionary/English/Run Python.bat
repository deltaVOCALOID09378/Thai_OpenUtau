@echo off
:: ============================================================================
:: Made And Checked By DELTA SYNTH & Gemini AI
:: Original by Patiphat Wongyai (Delta)
:: File Name: Diffsinger_to_VCCV_Converter_v1.4.bat
:: Version: v1.5 | Date: 2026-07-28
:: Description: Drag and drop launcher for the Advanced Grammar Python engine
:: ============================================================================

:: v1.4 - 2026-07-18 - Force active console codepage to 65001 to ensure UTF-8 stability
chcp 65001 >nul

:: v1.4 - 2026-07-18 - Enable delayed variable expansion to properly handle dynamic file paths
setlocal enabledelayedexpansion

:: v1.4 - 2026-07-18 - Set the title of the command prompt window for interface clarity
title DELTA SYNTH - DiffSinger to VCCV Converter v1.4 (Harmony Engine)

:: v1.4 - 2026-07-18 - Change console colors to Black background and Light Aqua text for aesthetic appeal
color 0B

:: v1.4 - 2026-07-18 - Check if Python execution engine is properly installed on the host system
python --version >nul 2>&1
if errorlevel 1 (
    :: v1.4 - 2026-07-18 - Display a comprehensive error if the Python environment is missing
    echo [ERROR] Python is not recognized as an internal or external command.
    echo [INFO] Please install Python 3.x and ensure it is added to your system PATH variables.
    echo.
    :: v1.4 - 2026-07-18 - Pause execution to allow the user to read the troubleshooting message
    pause
    :: v1.4 - 2026-07-18 - Exit the batch script safely with an error code 1
    exit /b 1
)

:: v1.4 - 2026-07-18 - Resolve the absolute path to the Python script assuming it resides in the same directory
set "PYTHON_SCRIPT=%~dp0Diffsinger_to_VCCV_Converter_v1.4.py"

:: v1.4 - 2026-07-18 - Verify the physical existence of the Python script file before attempting execution
if not exist "%PYTHON_SCRIPT%" (
    :: v1.4 - 2026-07-18 - Output a file missing error alert to the user interface
    echo [ERROR] Core Python script not found: %PYTHON_SCRIPT%
    echo [INFO] Please verify that the .py file is correctly placed in the exact same directory as this .bat file.
    echo.
    :: v1.4 - 2026-07-18 - Halt execution temporarily for reading
    pause
    :: v1.4 - 2026-07-18 - Terminate the script gracefully
    exit /b 1
)

:: v1.4 - 2026-07-18 - Execute the Python script and pass any dragged dictionary files as runtime arguments
python "%PYTHON_SCRIPT%" %*
set "PYTHON_EXIT_CODE=%ERRORLEVEL%"

:: v1.4 - 2026-07-18 - Await user acknowledgment before dismissing the terminal session window
pause
:: v1.5 - 2026-07-28 - Preserve the Python process result for automation and diagnostics
exit /b %PYTHON_EXIT_CODE%
