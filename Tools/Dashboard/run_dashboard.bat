@echo off
title DELTA SYNTH - Voicebank Analytics Dashboard
chcp 65001 >nul

echo ======================================================================
echo    DELTA SYNTH - Voicebank & Thai Phoneme Analytics Dashboard
echo ======================================================================
echo.

python -m pip install streamlit pandas plotly -q
if %errorlevel% neq 0 (
    echo [WARNING] Dependency installation failed. Ensure Python is installed.
)

streamlit run "%~dp0app.py"
pause
