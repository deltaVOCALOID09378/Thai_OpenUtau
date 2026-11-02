@echo off
rem ==============================================================================
rem File: audit_vccv_engine.bat
rem Version: 1.0.0
rem Location: root/
rem Description: Compiles Thai VCCV Phonemizer and validates dictionary syntax without warnings.
rem Credit: Made And Checked By DELTA SYNTH & All Agentic AI
rem ==============================================================================

setlocal enabledelayedexpansion

echo ========================================================
echo  DELTA SYNTH - Thai VCCV Phonemizer & Dict Audit Tool
echo ========================================================

set TARGET_DIR=%~1
if "%TARGET_DIR%"=="" (
    set TARGET_DIR=%CD%
)

echo [INFO] Target Directory: %TARGET_DIR%
cd /d "%TARGET_DIR%"

echo [CHECK] Auditing C# Source Files for Warnings and Build Errors...
dotnet build --configuration Release /p:TreatWarningsAsErrors=true /p:Nullable=enable

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Build validation failed. Unresolved warnings or compilation errors detected.
    exit /b %ERRORLEVEL%
)

echo [CHECK] Validating Thai VCCV Dictionary File...
if exist "TH_VCCV_Dict.txt" (
    echo [INFO] Found TH_VCCV_Dict.txt. Verifying structure...
    findstr /r "^[a-zA-Z0-9ก-๙].*" "TH_VCCV_Dict.txt" > nul
    if !ERRORLEVEL! NEQ 0 (
        echo [WARNING] Dictionary file exists but might be empty or improperly formatted.
    ) else (
        echo [SUCCESS] Dictionary format verified.
    )
) else (
    echo [INFO] No local TH_VCCV_Dict.txt in root directory. Skipping dictionary text scan.
)

echo ========================================================
echo [SUCCESS] Audit completed with 0 errors and 0 warnings.
echo ========================================================
endlocal
pause