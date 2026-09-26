@echo off
:: ========================================================================
:: Made And Checked By DELTA SYNTH & All Gemini AI Making
:: Original by DELTA SYNTH (Delta)
:: Version: 1.1 | Date: 2026-07-24
:: Description: Drag and drop the OpenUtau folder to compile the project seamlessly.
:: It builds the project in Release mode and securely packs all compiled 
:: outputs into a newly created folder named "All Rendered File".
:: ========================================================================

title DELTA SYNTH - OpenUtau Auto Compiler V1.1
color 0B
chcp 65001 >nul

echo =======================================================
echo      DELTA SYNTH - OpenUtau Project Auto Compiler
echo =======================================================
echo.

if "%~1" == "" (
    echo [ERROR] Please drag and drop the project folder onto this .bat file.
    pause
    exit /b 1
)

cd /d "%~1" 2>nul
if errorlevel 1 (
    echo [ERROR] Could not access the dropped folder. Please verify the path.
    pause
    exit /b 1
)

echo [INFO] Working Directory: %CD%
echo [INFO] Verifying project files...

if not exist "*.csproj" (
    if not exist "*.sln" (
        echo [ERROR] No .csproj or .sln file found in this directory.
        pause
        exit /b 1
    )
)

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] The "dotnet" command was not found. Please install the .NET SDK.
    pause
    exit /b 1
)

echo [INFO] Starting compilation in Release mode...
echo.

dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [SUCCESS] Compilation completed flawlessly! The system is stable and ready.
    echo [INFO] Packing all compiled files into "All Rendered File" folder...
    
    if not exist "All Rendered File" (
        mkdir "All Rendered File"
    )
    
    if exist "bin\Release" (
        xcopy "bin\Release\*" "All Rendered File\" /s /e /y /i /q >nul 2>&1
        echo [SUCCESS] All files have been successfully consolidated into "All Rendered File".
    ) else (
        echo [WARNING] Could not find the "bin\Release" directory to extract the files.
    )
    
) else (
    echo.
    echo [FAILED] Compilation errors were detected. Please check the logs above.
)

echo.
echo =======================================================
pause
exit /b 0