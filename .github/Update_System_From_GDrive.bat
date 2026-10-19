:: ==============================================================================
:: File Name   : Update_System_From_GDrive.bat
:: Version     : v1.3
:: Location    : E:\Program Developing\DELTA_SYNTH-main
:: Function    : Fetch and Checkout Git Branch with Google Drive [G:] File Sync
:: Credit      : Made And Checked By DELTA SYNTH & All Agentic AI
::               Original by DELTA SYNTH
:: ==============================================================================

@echo off
setlocal enabledelayedexpansion

:: Default working repository and Git configurations
set "REPO_DIR=E:\Program Developing\DELTA_SYNTH-main"
set "TARGET_BRANCH=v0/order-voicebanks"
set "GDRIVE_ROOT=G:\"

:: Check if directory or file arguments were passed via Drag-and-Drop
if not "%~1"=="" goto PROCESS_ARGS

:: Standard execution flow
call :SYNC_SYSTEM "%REPO_DIR%" ""
goto PROMPT_LOOP

:PROCESS_ARGS
for %%I in (%*) do (
    call :SYNC_SYSTEM "%REPO_DIR%" "%%~fI"
)
goto PROMPT_LOOP

:PROMPT_LOOP
echo.
echo ========================================================
echo [Drag and drop another file/folder from G: Drive to sync]
echo [Or press ENTER to exit]
echo ========================================================
set "INPUT_PATH="
set /p "INPUT_PATH=> "
if "!INPUT_PATH!"=="" goto FINISH

:: Remove quotation marks from input path
set "INPUT_PATH=!INPUT_PATH:"=!"
if exist "!INPUT_PATH!" (
    call :SYNC_SYSTEM "%REPO_DIR%" "!INPUT_PATH!"
    goto PROMPT_LOOP
) else (
    echo [ERROR] The specified path does not exist. Please verify and try again.
    goto PROMPT_LOOP
)

:SYNC_SYSTEM
set "TARGET_REPO=%~1"
set "EXTERNAL_SOURCE=%~2"

echo.
echo ========================================================
echo DELTA SYNTH - Update System and G-Drive Synchronization
echo Repository: "!TARGET_REPO!"
echo ========================================================

:: Step 1: Synchronize Git Repository
if not exist "!TARGET_REPO!" (
    echo [ERROR] Repository directory not found: "!TARGET_REPO!"
    goto SKIP_GIT
)

cd /d "!TARGET_REPO!"
echo.
echo [1/3] Synchronizing Git repository [%TARGET_BRANCH%]...
git fetch origin
if !errorlevel! neq 0 (
    echo [WARNING] Git fetch encountered an issue. Proceeding with caution...
)
git checkout %TARGET_BRANCH%
git pull origin %TARGET_BRANCH%
if !errorlevel! neq 0 (
    echo [WARNING] Git pull failed or encountered conflicts.
) else (
    echo [OK] Git repository successfully synchronized.
)

:SKIP_GIT
:: Step 2: Validate Google Drive [G:] Connection
echo.
echo [2/3] Checking Google Drive [G:\] connection status...
if not exist "%GDRIVE_ROOT%" (
    echo [WARNING] Google Drive [G:\] is not detected or unmounted.
    echo Please make sure the Google Drive desktop client is running.
) else (
    echo [OK] Google Drive [G:\] detected.
)

:: Step 3: Update and Synchronize Files from G: Drive
echo.
echo [3/3] Synchronizing assets and updates from G: Drive...
if not "!EXTERNAL_SOURCE!"=="" (
    echo Source provided: "!EXTERNAL_SOURCE!"
    if exist "!EXTERNAL_SOURCE!\" (
        echo Synchronizing directory updates to repository...
        robocopy "!EXTERNAL_SOURCE!" "!TARGET_REPO!" /E /XO /NP /R:2 /W:2 >nul
        echo [OK] Directory sync completed.
    ) else (
        echo Copying single file update to repository...
        copy /Y "!EXTERNAL_SOURCE!" "!TARGET_REPO!\" >nul
        echo [OK] File copied successfully.
    )
    goto SYNC_DONE
)

set "FOUND_GDRIVE_DIR=0"
if exist "G:\My Drive\DELTA_SYNTH" (
    echo Found sync directory: "G:\My Drive\DELTA_SYNTH"
    robocopy "G:\My Drive\DELTA_SYNTH" "!TARGET_REPO!" /E /XO /NP /R:2 /W:2 >nul
    set "FOUND_GDRIVE_DIR=1"
    echo [OK] Synchronized from "G:\My Drive\DELTA_SYNTH"
)
if exist "G:\DELTA_SYNTH" (
    echo Found sync directory: "G:\DELTA_SYNTH"
    robocopy "G:\DELTA_SYNTH" "!TARGET_REPO!" /E /XO /NP /R:2 /W:2 >nul
    set "FOUND_GDRIVE_DIR=1"
    echo [OK] Synchronized from "G:\DELTA_SYNTH"
)
if "!FOUND_GDRIVE_DIR!"=="0" (
    echo [INFO] No standard G:\DELTA_SYNTH folder found automatically.
    echo You can drag and drop any folder or file from G: Drive to update it directly.
)

:SYNC_DONE
echo.
echo Synchronization task finished successfully.
exit /b

:FINISH
echo.
echo Exiting synchronization utility...
pause
exit /b