@echo off
:: ======================================================================
:: Date         : 10/11/2026 (B.E. 2569 / A.D. 2026)
:: File Name    : run_dashboard.bat
:: Version      : 1.1
:: Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
:: File Location: Tools/Dashboard/run_dashboard.bat
:: Description  : Launches the Voicebank Analytics Dashboard after checking installed Python packages first.
:: ======================================================================
setlocal enabledelayedexpansion
title DELTA SYNTH - Voicebank Analytics Dashboard v1.1
chcp 65001 >nul

set "LOG_DIR=%~dp0..\..\Logs"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%" 2>nul
set "LOG_FILE=%LOG_DIR%\run_dashboard.log"

echo ======================================================================
echo    DELTA SYNTH - Voicebank & Thai Phoneme Analytics Dashboard
echo ======================================================================
echo.

echo [%DATE% %TIME%] Checking installed Python packages (streamlit, pandas, plotly)... >> "%LOG_FILE%"
python -c "import streamlit, pandas, plotly" >nul 2>&1
if %errorlevel% equ 0 (
    echo [OK] Required Python packages (streamlit, pandas, plotly) are already installed.
    echo [%DATE% %TIME%] [OK] Packages already installed; skipping pip download. >> "%LOG_FILE%"
) else (
    echo [INFO] Installing missing dashboard dependencies (streamlit, pandas, plotly)...
    echo [%DATE% %TIME%] [INFO] Installing missing dependencies via pip... >> "%LOG_FILE%"
    python -m pip install streamlit pandas plotly -q >> "%LOG_FILE%" 2>&1
    if !errorlevel! neq 0 (
        echo [WARNING] Dependency installation failed. Ensure Python is installed.
        echo [%DATE% %TIME%] [WARN] pip install failed. >> "%LOG_FILE%"
    )
)

streamlit run "%~dp0app.py"
pause

