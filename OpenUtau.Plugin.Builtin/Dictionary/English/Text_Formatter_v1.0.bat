@echo off
REM ============================================================================
REM 1. Date: 29/10/2569 (2026-10-29)
REM 2. File Name: Text_Formatter_v1.0.bat
REM 3. Version: 1.0
REM 4. Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by Patiphat Wongyai (Delta) ]
REM 5. Location: Root/Tools/
REM 6. Description: Convert text to lowercase and remove all numbers via Drag & Drop.
REM ============================================================================

chcp 65001 >nul
setlocal enabledelayedexpansion

title DELTA SYNTH - Text Formatter v1.0
color 0B

echo ============================================================================
echo [OptiLink System] Text Formatter (Lowercase ^& Remove Numbers)
echo ============================================================================
echo.

for /f "tokens=*" %%a in ('powershell -NoProfile -Command "Get-Date -Format 'HH:mm:ss'"') do set "START_TIME=%%a"
echo [SYSTEM] Process started at: %START_TIME%
echo.

set "TARGET_FILES=%*"
if "%~1"=="" (
    echo [ERROR] No input files provided.
    echo [INFO] Please drag and drop .txt files or folders onto this script.
    echo.
    pause
    exit /b 1
)

:: Build PowerShell Script to handle Regex and String Manipulation securely
set "PS_SCRIPT=$ErrorActionPreference = 'Stop'; $failed = $false; "
set "PS_SCRIPT=!PS_SCRIPT! $inputPath = $env:TARGET_FILE; "
set "PS_SCRIPT=!PS_SCRIPT! $filesToProcess = @(); "
set "PS_SCRIPT=!PS_SCRIPT! if (Test-Path -Path $inputPath -PathType Container) { $filesToProcess = Get-ChildItem -Path $inputPath -Filter *.txt | Select-Object -ExpandProperty FullName; } else { $filesToProcess += $inputPath; } "

set "PS_SCRIPT=!PS_SCRIPT! foreach ($file in $filesToProcess) { "
set "PS_SCRIPT=!PS_SCRIPT!   if (-Not (Test-Path -LiteralPath $file)) { Write-Host \"[ERROR] File not found: $file\" -ForegroundColor Red; $failed = $true; continue; } "
set "PS_SCRIPT=!PS_SCRIPT!   $dir = [System.IO.Path]::GetDirectoryName($file); "
set "PS_SCRIPT=!PS_SCRIPT!   $name = [System.IO.Path]::GetFileNameWithoutExtension($file); "
set "PS_SCRIPT=!PS_SCRIPT!   $ext = [System.IO.Path]::GetExtension($file); "
:: Append '_Formatted' to preserve the original file
set "PS_SCRIPT=!PS_SCRIPT!   $outFile = [System.IO.Path]::Combine($dir, \"$name\" + '_Formatted' + $ext); "
set "PS_SCRIPT=!PS_SCRIPT!   Write-Host \"[PROCESSING] Reading: $file\" -ForegroundColor Cyan; "

:: Read raw content in UTF8, convert to lowercase, and replace numbers 0-9 with empty string
set "PS_SCRIPT=!PS_SCRIPT!   $content = Get-Content -Path $file -Encoding UTF8 -Raw; "
set "PS_SCRIPT=!PS_SCRIPT!   if ($null -ne $content) { "
set "PS_SCRIPT=!PS_SCRIPT!     $content = $content.ToLowerInvariant() -replace '[0-9]', ''; "
set "PS_SCRIPT=!PS_SCRIPT!     [System.IO.File]::WriteAllText($outFile, $content, [System.Text.Encoding]::UTF8); "
set "PS_SCRIPT=!PS_SCRIPT!     Write-Host \"[SUCCESS] Exported: $outFile`n\" -ForegroundColor Green; "
set "PS_SCRIPT=!PS_SCRIPT!   } else { Write-Host \"[WARNING] File is empty or cannot be read: $file`n\" -ForegroundColor Yellow; } "
set "PS_SCRIPT=!PS_SCRIPT! } if ($failed) { exit 1 } "

set "CONVERT_EXIT_CODE=0"
for %%F in (%TARGET_FILES%) do (
    set "TARGET_FILE=%%~fF"
    powershell -NoProfile -Command "!PS_SCRIPT!"
    if errorlevel 1 set "CONVERT_EXIT_CODE=!errorlevel!"
)
set "TARGET_FILE="

if not "!CONVERT_EXIT_CODE!"=="0" (
    echo [ERROR] Text formatting encountered an issue.
    pause
    exit /b !CONVERT_EXIT_CODE!
)

for /f "tokens=*" %%a in ('powershell -NoProfile -Command "Get-Date -Format 'HH:mm:ss'"') do set "END_TIME=%%a"

echo ============================================================================
echo [SYSTEM] Process completed at: %END_TIME%
echo [SYSTEM] All tasks finished successfully with 0 errors.
echo ============================================================================
echo.
pause
exit /b 0