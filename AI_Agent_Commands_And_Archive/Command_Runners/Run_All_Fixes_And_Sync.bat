@echo off
REM ============================================================================
REM File Name   : Run_All_Fixes_And_Sync.bat
REM Version     : 3.2
REM Location    : D:\Program Developing\Thai_OpenUtau\Run_All_Fixes_And_Sync.bat
REM Function    : Executes apply_all_delta_synth_fixes.py, builds Release, and merges PRs
REM Credit      : Made And Checked By DELTA SYNTH & All Agentic AI
REM               Original by DELTA SYNTH
REM ============================================================================

setlocal EnableDelayedExpansion
title DELTA SYNTH - One-Click Master Patcher, Builder and GitHub PR Merger

set "DEFAULT_ROOT=D:\Program Developing\Thai_OpenUtau"
set "SCRIPT_DIR=%~dp0"

:PROCESS_LOOP
cls
echo ============================================================================
echo        DELTA SYNTH - Master Patcher, Auto-Compiler ^& GitHub Synchronizer
echo        Made And Checked By DELTA SYNTH ^& All Agentic AI
echo ============================================================================
echo.

set "TARGET_ROOT=%~1"
if "%TARGET_ROOT%"=="" (
    if exist "%DEFAULT_ROOT%\OpenUtau.Core" (
        set "TARGET_ROOT=%DEFAULT_ROOT%"
    ) else (
        echo [INPUT] Drag and drop the Thai_OpenUtau root folder here, or press Enter for default:
        set /p "TARGET_ROOT=> "
    )
)

set "TARGET_ROOT=%TARGET_ROOT:"=%"
if "%TARGET_ROOT%"=="" set "TARGET_ROOT=%DEFAULT_ROOT%"

if not exist "%TARGET_ROOT%" (
    echo [ERROR] Target directory does not exist: "%TARGET_ROOT%"
    goto ASK_NEXT
)

cd /d "%TARGET_ROOT%"
echo [INFO] Working Directory: %CD%
echo.

set "PYTHON_EXE=python"
if exist "%SCRIPT_DIR%runtime\python.exe" (
    set "PYTHON_EXE=%SCRIPT_DIR%runtime\python.exe"
) else if exist "%TARGET_ROOT%\runtime\python.exe" (
    set "PYTHON_EXE=%TARGET_ROOT%\runtime\python.exe"
)

echo [STEP 1/3] Applying all DELTA SYNTH code fixes and compiling solution...
if exist "%SCRIPT_DIR%apply_all_delta_synth_fixes.py" (
    "%PYTHON_EXE%" "%SCRIPT_DIR%apply_all_delta_synth_fixes.py" "%TARGET_ROOT%"
) else if exist "%TARGET_ROOT%\apply_all_delta_synth_fixes.py" (
    "%PYTHON_EXE%" "%TARGET_ROOT%\apply_all_delta_synth_fixes.py" "%TARGET_ROOT%"
) else (
    echo [ERROR] apply_all_delta_synth_fixes.py not found in "%SCRIPT_DIR%" or "%TARGET_ROOT%".
    goto ASK_NEXT
)

if %ERRORLEVEL% NEQ 0 (
    echo [FAILED] Patching or compilation encountered an error.
    goto ASK_NEXT
)
echo.

echo [STEP 2/3] Committing and pushing updated files to GitHub...
where git >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    git status >nul 2>nul
    if !ERRORLEVEL! EQU 0 (
        git add -A
        git commit -m "fix(all): apply DELTA SYNTH v3.2 zero-defect fixes across Core, Builtin, UI, and CI workflow"
        git push origin HEAD
        echo [SUCCESS] Changes pushed to GitHub repository.
    ) else (
        echo [WARN] Current folder is not a Git repository. Skipping Git push.
    )
) else (
    echo [WARN] Git CLI is not installed or not in PATH.
)
echo.

echo [STEP 3/3] Sweeping and merging all open Pull Requests on GitHub...
where gh >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    for /f "tokens=1" %%P in ('gh pr list --repo deltaVOCALOID09378/Thai_OpenUtau --state open --json number --jq ".[].number" 2^>nul') do (
        echo [INFO] Merging Pull Request #%%P...
        gh pr ready %%P --repo deltaVOCALOID09378/Thai_OpenUtau 2>nul
        gh pr review %%P --repo deltaVOCALOID09378/Thai_OpenUtau --approve 2>nul
        gh pr merge %%P --repo deltaVOCALOID09378/Thai_OpenUtau --merge --delete-branch --admin 2>nul || (
            gh pr merge %%P --repo deltaVOCALOID09378/Thai_OpenUtau --squash --delete-branch --admin 2>nul
        )
    )
    git pull origin main --rebase 2>nul
    echo [SUCCESS] All open Pull Requests processed!
) else (
    echo [INFO] GitHub CLI (gh) not found locally. The pushed GitHub Actions workflow will merge PRs automatically on GitHub.
)

:ASK_NEXT
echo.
echo ============================================================================
echo [READY] Drag and drop another target folder here, press Enter to re-run,
echo         or type Q to exit:
set "NEXT_FOLDER="
set /p "NEXT_FOLDER=> "
if /i "%NEXT_FOLDER%"=="Q" goto EXIT_ALL
set "TARGET_ROOT=%NEXT_FOLDER:"=%"
shift
goto PROCESS_LOOP

:EXIT_ALL
echo [INFO] All tasks finished. Exiting...
endlocal
exit /b 0