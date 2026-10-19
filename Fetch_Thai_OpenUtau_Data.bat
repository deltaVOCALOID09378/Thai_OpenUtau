:: ==============================================================================
:: File Name   : Fetch_Thai_OpenUtau_Data.bat
:: Version     : v1.0
:: Location    : Workspace Directory
:: Function    : Fetch and synchronize all data from A:\Thai_OpenUtau
:: Credit      : Made And Checked By DELTA SYNTH & All Agentic AI
::               Original by DELTA SYNTH
:: ==============================================================================

@echo off
setlocal enabledelayedexpansion

:: Define the master source directory
set "SOURCE_DIR=A:\Thai_OpenUtau"

:: Check if destination directory arguments were passed via Drag-and-Drop
if not "%~1"=="" goto PROCESS_ARGS

:: Standard execution flow [Uses current directory as default destination]
call :SYNC_DATA "%~dp0"
goto PROMPT_LOOP

:PROCESS_ARGS
for %%I in (%*) do (
    call :SYNC_DATA "%%~fI"
)
goto PROMPT_LOOP

:PROMPT_LOOP
echo.
echo ========================================================
echo [Drag and drop a destination folder to fetch data into]
echo [Or press ENTER to exit]
echo ========================================================
set "INPUT_PATH="
set /p "INPUT_PATH=> "
if "!INPUT_PATH!"=="" goto FINISH

:: Strip quotation marks from input path
set "INPUT_PATH=!INPUT_PATH:"=!"
if exist "!INPUT_PATH!\" (
    call :SYNC_DATA "!INPUT_PATH!"
    goto PROMPT_LOOP
) else (
    echo [ERROR] The specified destination path does not exist.
    goto PROMPT_LOOP
)

:SYNC_DATA
set "TARGET_DIR=%~1"
echo.
echo ========================================================
echo DELTA SYNTH - Data Fetch Utility
echo Source      : "%SOURCE_DIR%"
echo Destination : "!TARGET_DIR!"
echo ========================================================

:: Validate Source Connection
if not exist "%SOURCE_DIR%\" (
    echo [ERROR] Source directory not found: "%SOURCE_DIR%"
    echo Please ensure drive A: is connected and accessible.
    goto SYNC_END
)

echo.
echo [INFO] Synchronizing data from source to destination...
:: Use robocopy to mirror data efficiently, skipping older files
robocopy "%SOURCE_DIR%" "!TARGET_DIR!" /E /XO /NP /R:2 /W:2 >nul

:: Check robocopy exit codes [Any code 8 or higher indicates a failure]
if !errorlevel! geq 8 (
    echo [ERROR] Synchronization failed. Please check permissions or disk space.
) else (
    echo [OK] All data fetched and synchronized successfully.
)

:SYNC_END
exit /b

:FINISH
echo.
echo Exiting data fetch utility...
pause
exit /b