@echo off
setlocal enabledelayedexpansion

:: ========================================================================
:: Build-Capture-Tools.bat
:: Version: 1.0
:: Author: DELTA SYNTH & All Code Agentic AI Engine
:: Description: Automated build script for Standby Capture DLL (100% English)
:: ========================================================================

set "SCRIPT_DIR=%~dp0"
set "LOG_DIR=%SCRIPT_DIR%..\Logs"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

set "LOG_FILE=%LOG_DIR%\Build_Capture_Tools_%date:~10,4%%date:~4,2%%date:~7,2%.log"
echo [INFO] Starting build for OpenUtau.Media.Capture DLL at %time% >> "%LOG_FILE%"
echo [INFO] Working directory: %SCRIPT_DIR% >> "%LOG_FILE%"

echo ========================================================
echo   DELTA SYNTH - OpenUtau Standby Tools Compiler
echo ========================================================
echo.

set "PROJ_PATH=%SCRIPT_DIR%OpenUtau.Media.Capture\OpenUtau.Media.Capture.csproj"
set "OUT_DIR=%SCRIPT_DIR%bin\Release"

if not exist "%PROJ_PATH%" (
    echo [ERROR] Project file not found: %PROJ_PATH%
    echo [ERROR] Project file not found: %PROJ_PATH% >> "%LOG_FILE%"
    exit /b 1
)

echo [INFO] Compiling OpenUtau.Media.Capture in Release configuration...
dotnet build "%PROJ_PATH%" -c Release -o "%OUT_DIR%" >> "%LOG_FILE%" 2>&1

if %ERRORLEVEL% equ 0 (
    echo [SUCCESS] Build succeeded! DLL generated at:
    echo           %OUT_DIR%\OpenUtau.Media.Capture.dll
    echo [SUCCESS] OpenUtau.Media.Capture.dll compiled successfully at %time% >> "%LOG_FILE%"
    
    :: Standby sync to Standalone if available
    set "STANDALONE_DIR=A:\Program Files\Thai_OpenUtau_v3.9.4_Standalone"
    if exist "!STANDALONE_DIR!" (
        echo [INFO] Copying standby DLL to Standalone folder...
        copy /y "%OUT_DIR%\OpenUtau.Media.Capture.dll" "!STANDALONE_DIR!\" >> "%LOG_FILE%" 2>&1
        echo [SUCCESS] Standby DLL deployed to Standalone.
    )
) else (
    echo [ERROR] Compilation failed! Please inspect log: %LOG_FILE%
    exit /b 1
)

echo.
echo ========================================================
echo   Build process completed successfully.
echo ========================================================
exit /b 0
