@echo off
setlocal enabledelayedexpansion
:: ==============================================================================
:: File Name: Build Worldline.bat
:: Version: 1.1
:: Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
::         Original by Worldline Project
:: Description: Automated Bazel build script for multiple Windows architectures.
::              Hardened with strict error handling and path safety.
:: ==============================================================================
title DELTA SYNTH - Worldline Bazel Builder
color 0B

echo ========================================================
echo      WORLDLINE MULTI-ARCHITECTURE BUILDER
echo      Made And Checked By DELTA SYNTH
echo ========================================================
echo.

goto :MAIN

:BUILD
echo [INFO] ========================================================
echo [INFO] Starting build for Architecture: %~1 (CPU: %~2)
echo [INFO] ========================================================

:: Safely create target directory if it does not exist
if not exist "..\runtimes\%~1\native" (
    echo [INFO] Creating directory: ..\runtimes\%~1\native
    mkdir "..\runtimes\%~1\native"
)

:: Execute Bazel build
echo [INFO] Executing Bazel build command...
bazel build //worldline:worldline -c opt --cpu=%~2

:: Strict error checking for Bazel process
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Bazel build failed for %~1!
    exit /b %ERRORLEVEL%
)

:: Verify output file before manipulating attributes and copying
if exist "bazel-bin\worldline\worldline.dll" (
    echo [INFO] Removing read-only attribute...
    attrib -r "bazel-bin\worldline\worldline.dll"
    
    echo [INFO] Copying DLL to target directory...
    copy /Y "bazel-bin\worldline\worldline.dll" "..\runtimes\%~1\native\"
    
    echo [SUCCESS] Build and copy completed for %~1.
) else (
    echo [ERROR] Output file 'bazel-bin\worldline\worldline.dll' not found!
    exit /b 1
)
echo.
EXIT /B 0

:MAIN
echo [INFO] Initiating build sequence...
echo.

:: Build for win-x64
call :BUILD win-x64 x64_windows
if %ERRORLEVEL% NEQ 0 goto :FAIL

:: Build for win-x86
call :BUILD win-x86 x64_x86_windows
if %ERRORLEVEL% NEQ 0 goto :FAIL

:: Build for win-arm64
call :BUILD win-arm64 arm64_windows
if %ERRORLEVEL% NEQ 0 goto :FAIL

echo ========================================================
echo [SUCCESS] ALL BUILDS COMPLETED SUCCESSFULLY.
echo ========================================================
pause
exit /b 0

:FAIL
echo ========================================================
echo [CRITICAL ERROR] BUILD SEQUENCE HALTED DUE TO ERRORS.
echo ========================================================
pause
exit /b 1