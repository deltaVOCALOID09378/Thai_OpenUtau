@echo off
setlocal enabledelayedexpansion
title DELTA SYNTH - Moresampler Master Tool Coordinator
chcp 65001 >nul
color 0B

:: ========================================================================
:: Date         : 06/11/2026 (B.E. 2569 / A.D. 2026)
:: File Name    : Moresampler-Tools.bat
:: Version      : 1.0
:: Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
:: File Location: Resamplers/moresampler/Moresampler-Tools.bat
:: Description  : Interactive launcher and pipeline runner for Moresampler source tools.
::                Supports Drag and Drop of voicebank directories, logging to tools/logs,
::                and automated dispatcher invocation.
:: ========================================================================

set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

set "TOOLS_DIR=%SCRIPT_DIR%\tools"
set "LOGS_DIR=%TOOLS_DIR%\logs"
if not exist "%LOGS_DIR%" mkdir "%LOGS_DIR%" >nul 2>&1

for /f %%t in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmmss"') do set "STAMP=%%t"
set "LOG_FILE=%LOGS_DIR%\session_%STAMP%.log"

echo ======================================================================== >> "%LOG_FILE%"
echo   Session Started: %DATE% %TIME% >> "%LOG_FILE%"
echo ======================================================================== >> "%LOG_FILE%"

echo ========================================================================
echo       DELTA SYNTH - Moresampler Master Tool Coordinator v1.0
echo       Primary Resampler Source Engine, Analyzers ^& Voicebank Suite
echo ========================================================================
echo.

where python >nul 2>&1
if %ERRORLEVEL% neq 0 (
    color 0C
    echo [ERROR] Python 3 was not found in System PATH.
    echo Please install Python 3.10+ from https://www.python.org/
    echo [ERROR] Python not found >> "%LOG_FILE%"
    pause
    exit /b 1
)

set "TARGET_PATH=%~1"

if not "%TARGET_PATH%"=="" (
    echo [INFO] Detected Drag and Drop input: "%TARGET_PATH%"
    echo [INFO] Detected Drag and Drop input: "%TARGET_PATH%" >> "%LOG_FILE%"
    echo.
    echo Select an action for this voicebank:
    echo   [1] Run Full End-to-End Pipeline (Repair, Sync, Optimize, Tighten, Audit, Pack)
    echo   [2] Optimize Thai Oto (VCCV / CVVC)
    echo   [3] Balance VCV Multi-Pitch Oto
    echo   [4] Balance Arpasing Oto
    echo   [5] Repair WAV Extensions ^& Audio Headers
    echo   [6] Audit Voicebank Integrity
    echo   [7] Pack Voicebank to ZIP
    echo.
    set /p "ACT_CHOICE=Select an action (1-7): "

    if "!ACT_CHOICE!"=="1" (
        python "%TOOLS_DIR%\Program.moresampler_coordinator.py" pipeline-all --path "%TARGET_PATH%" 2>&1 | tee-fallback
    ) else if "!ACT_CHOICE!"=="2" (
        python "%TOOLS_DIR%\Program.moresampler_coordinator.py" optimize-thai --path "%TARGET_PATH%"
    ) else if "!ACT_CHOICE!"=="3" (
        python "%TOOLS_DIR%\Program.moresampler_coordinator.py" balance-vcv --path "%TARGET_PATH%"
    ) else if "!ACT_CHOICE!"=="4" (
        python "%TOOLS_DIR%\Program.moresampler_coordinator.py" balance-arpasing --path "%TARGET_PATH%"
    ) else if "!ACT_CHOICE!"=="5" (
        python "%TOOLS_DIR%\Program.moresampler_coordinator.py" repair-wav --path "%TARGET_PATH%"
    ) else if "!ACT_CHOICE!"=="6" (
        python "%TOOLS_DIR%\Program.moresampler_coordinator.py" audit --path "%TARGET_PATH%"
    ) else if "!ACT_CHOICE!"=="7" (
        python "%TOOLS_DIR%\Program.moresampler_coordinator.py" pack --path "%TARGET_PATH%"
    ) else (
        echo [ERROR] Invalid selection.
    )
    goto end_session
)

python "%TOOLS_DIR%\Program.moresampler_coordinator.py" --interactive

:end_session
echo.
echo ========================================================================
echo [INFO] Session logged to: "%LOG_FILE%"
echo ========================================================================
echo.
pause
exit /b 0
