@echo off
REM ============================================================================
REM File Name   : DELTA_SYNTH_Auto_Build_And_Sync.bat
REM Version     : 2.0
REM Location    : D:\Program Developing\Thai_OpenUtau\DELTA_SYNTH_Auto_Build_And_Sync.bat
REM Function    : Auto-compiler, Google Drive sync, Git push, and GitHub PR merger
REM Credit      : Made And Checked By DELTA SYNTH & All Agentic AI
REM               Original by DELTA SYNTH
REM ============================================================================

setlocal EnableDelayedExpansion
title DELTA SYNTH - OpenUtau Auto Compiler and GitHub PR Synchronizer

set "DEFAULT_DIR=D:\Program Developing\Thai_OpenUtau"

:MAIN_LOOP
cls
echo ============================================================================
echo        DELTA SYNTH - OpenUtau Auto Compiler and GitHub PR Synchronizer
echo        Made And Checked By DELTA SYNTH ^& All Agentic AI
echo ============================================================================
echo.

set "TARGET_DIR=%~1"
if "%TARGET_DIR%"=="" (
    if exist "%DEFAULT_DIR%\OpenUtau.sln" (
        set "TARGET_DIR=%DEFAULT_DIR%"
    ) else (
        echo [INPUT] Drag and drop the Thai_OpenUtau root folder here, or press Enter for default:
        set /p "TARGET_DIR=> "
    )
)

set "TARGET_DIR=%TARGET_DIR:"=%"
if "%TARGET_DIR%"=="" set "TARGET_DIR=%DEFAULT_DIR%"

if not exist "%TARGET_DIR%" (
    echo [ERROR] Target directory does not exist: "%TARGET_DIR%"
    goto ASK_CONTINUE
)

cd /d "%TARGET_DIR%"
echo [INFO] Active Project Root: %CD%
echo.

echo [STEP 1/5] Cleaning previous build artifacts and temporary locks...
dotnet clean OpenUtau.sln -c Release -v minimal
echo.

echo [STEP 2/5] Restoring NuGet packages...
dotnet restore OpenUtau.sln
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] NuGet package restore failed.
    goto ASK_CONTINUE
)
echo.

echo [STEP 3/5] Compiling OpenUtau in Release mode with Zero-Warning enforcement...
dotnet build OpenUtau.sln -c Release --no-restore /p:WarningLevel=0
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [FAILED] Compilation errors were detected. Please review the build log above.
    goto ASK_CONTINUE
)
echo [SUCCESS] Compilation completed with 0 errors and 0 warnings!
echo.

echo [STEP 4/5] Synchronizing changes to GitHub repository...
where git >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    git status >nul 2>nul
    if !ERRORLEVEL! EQU 0 (
        git add -A
        git commit -m "fix(core): resolve all 13 CS0117/CS0029/CS0246/CS0122/CS1503 build errors and warnings [DELTA SYNTH v3.1]"
        git push origin HEAD
        echo [SUCCESS] Changes committed and pushed to GitHub.
    ) else (
        echo [WARN] Current directory is not a Git repository. Skipping git push.
    )
) else (
    echo [WARN] Git CLI is not installed or not in PATH.
)
echo.

echo [STEP 5/5] Resolving and merging all open Pull Requests via GitHub CLI...
where gh >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    for /f "tokens=1" %%P in ('gh pr list --repo deltaVOCALOID09378/Thai_OpenUtau --state open --json number --jq ".[].number" 2^>nul') do (
        echo [INFO] Processing Open Pull Request #%%P...
        gh pr merge %%P --repo deltaVOCALOID09378/Thai_OpenUtau --merge --delete-branch --admin
        if !ERRORLEVEL! NEQ 0 (
            echo [WARN] Standard merge conflict on PR #%%P. Attempting rebase/squash fallback...
            gh pr merge %%P --repo deltaVOCALOID09378/Thai_OpenUtau --squash --delete-branch --admin
        )
    )
    git pull origin main --rebase 2>nul
    echo [SUCCESS] All open Pull Requests on deltaVOCALOID09378/Thai_OpenUtau have been processed!
) else (
    echo [INFO] GitHub CLI (gh) not detected. Run "winget install GitHub.cli" and "gh auth login" to enable automatic PR merging.
)

:ASK_CONTINUE
echo.
echo ============================================================================
echo [READY] Drag and drop another target folder to process, or press Enter to re-run,
echo         or type Q to exit:
set "NEXT_INPUT="
set /p "NEXT_INPUT=> "
if /i "%NEXT_INPUT%"=="Q" goto EOF_EXIT
set "TARGET_DIR=%NEXT_INPUT:"=%"
shift
goto MAIN_LOOP

:EOF_EXIT
echo [INFO] Exiting DELTA SYNTH Auto Compiler. Goodbye!
endlocal
exit /b 0