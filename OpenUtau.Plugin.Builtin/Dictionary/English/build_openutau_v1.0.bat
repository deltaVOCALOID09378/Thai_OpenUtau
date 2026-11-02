@echo off
:: ============================================================================
:: Made And Checked By DELTA SYNTH & Gemini AI
:: Original by Patiphat Wongyai (Delta)
:: Version: v1.1 | Date: 2026-07-28
:: Description: Automated compilation script for the OpenUtau Plugin Suite.
:: Designed for maximum stability and strict English-only console output.
:: ============================================================================

:: Force UTF-8 encoding for safe processing
chcp 65001 >nul
setlocal enabledelayedexpansion

:: ----------------------------------------------------------------------------
:: Configuration Variables
:: BASE_DIR : The absolute path to the central project directory.
:: CONFIG   : The build configuration mode (Release or Debug).
:: ----------------------------------------------------------------------------
for %%i in ("%~dp0..\..") do set "BASE_DIR=%%~fi"
set "CONFIG=Release"

title DELTA SYNTH - Automated Builder v1.0
color 0B

echo =======================================================
echo [SYSTEM] Initializing OpenUtau Project Compilation
echo =======================================================
echo.

:: Execute the build function for each specific target directory
call :BuildProject "%BASE_DIR%\OpenUtau\OpenUtau.csproj"
if errorlevel 1 exit /b !errorlevel!
call :BuildProject "%BASE_DIR%\OpenUtau.Core\OpenUtau.Core.csproj"
if errorlevel 1 exit /b !errorlevel!
call :BuildProject "%BASE_DIR%\OpenUtau.Plugin.Builtin\OpenUtau.Plugin.Builtin.csproj"
if errorlevel 1 exit /b !errorlevel!
call :BuildProject "%BASE_DIR%\OpenUtau.Test\OpenUtau.Test.csproj"
if errorlevel 1 exit /b !errorlevel!

echo.
echo =======================================================
echo [SYSTEM] All compilation processes successfully finished!
echo =======================================================
pause
exit /b 0

:: ============================================================================
:: Subroutine: BuildProject
:: Parameters: %1 - The full path to the target project directory.
:: Function  : Checks for .csproj files and executes the dotnet build command.
:: ============================================================================
:BuildProject
echo [PROCESSING] Scanning directory: %~1

:: Check if the directory contains any C# project files
if exist "%~1" (
    :: Execute the dotnet build command with the specified configuration
    dotnet build "%~1" -c %CONFIG%
    
    :: Validate the exit code of the dotnet command
    if !errorlevel! equ 0 (
        echo    [SUCCESS] Compiled perfectly: %~1
    ) else (
        set "PROJECT_EXIT_CODE=!errorlevel!"
        echo    [FAILED] Critical error during compilation: %~1
        exit /b !PROJECT_EXIT_CODE!
    )
) else (
    echo    [FAILED] No .csproj file found at: %~1
    exit /b 1
)
echo -------------------------------------------------------
exit /b 0
