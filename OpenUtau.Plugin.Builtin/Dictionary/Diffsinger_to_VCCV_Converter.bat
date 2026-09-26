@echo off
REM ============================================================================
REM Made And Checked By DELTA SYNTH & Gemini AI
REM Original by Patiphat Wongyai (Delta)
REM File Name: Diffsinger_to_VCCV_Converter_v1.3.bat
REM Version: v1.4 | Date: 2026-07-28
REM Description: Convert DiffSinger Dictionary and output directly as TH_VCCV_Dict.txt
REM ============================================================================

:: v1.3 - 18/07/2026 - Force active console codepage to 65001 for UTF-8 compatibility
chcp 65001 >nul
:: v1.3 - 18/07/2026 - Enable delayed variable expansion to properly update variables inside loops
setlocal enabledelayedexpansion

:: v1.3 - 18/07/2026 - Set the title of the command prompt window for clear identification
title DELTA SYNTH - DiffSinger to VCCV Dictionary Converter v1.3
:: v1.3 - 18/07/2026 - Change console colors to Black background and Light Aqua text
color 0B

:: v1.3 - 18/07/2026 - Display top border for the program UI
echo ============================================================================
:: v1.3 - 18/07/2026 - Display the main application header
echo [OptiLink System] Thai DiffSinger to Thai VCCV Phonemizer Converter
:: v1.3 - 18/07/2026 - Display bottom border for the program UI
echo ============================================================================
:: v1.3 - 18/07/2026 - Print an empty line for spacing
echo.

:: v1.3 - 18/07/2026 - Execute PowerShell command to fetch the current system time
for /f "tokens=*" %%a in ('powershell -NoProfile -Command "Get-Date -Format 'HH:mm:ss'"') do set "START_TIME=%%a"
:: v1.3 - 18/07/2026 - Print the starting time to the console
echo [SYSTEM] Process started at: %START_TIME%
:: v1.3 - 18/07/2026 - Print an empty line for spacing
echo.

:: v1.3 - 18/07/2026 - Initialize the variable to hold input file paths
set "TARGET_FILES="
:: v1.3 - 18/07/2026 - Check if the user has dragged and dropped any files onto the script
if "%~1" neq "" (
    :: v1.3 - 18/07/2026 - Assign all dragged files to the target variable
    set "TARGET_FILES=%*"
) else (
    :: v1.3 - 18/07/2026 - If no drag-and-drop, check if words_th_dict.txt exists in the current folder
    if exist "words_th_dict.txt" (
        :: v1.3 - 18/07/2026 - Assign the default dictionary file to the target variable
        set "TARGET_FILES="words_th_dict.txt""
    ) else (
        :: v1.3 - 18/07/2026 - Print an error message if no input file is found
        echo [ERROR] No input files provided and 'words_th_dict.txt' not found.
        :: v1.3 - 18/07/2026 - Instruct the user on how to use the script properly
        echo [INFO] Please drag and drop dictionary files onto this script.
        :: v1.3 - 18/07/2026 - Print an empty line for spacing
        echo.
        :: v1.3 - 18/07/2026 - Pause the execution to allow the user to read the error
        pause
        :: v1.3 - 18/07/2026 - Exit the script with an error code 1 indicating failure
        exit /b 1
    )
)

:: v1.3 - 18/07/2026 - Begin defining the PowerShell script string; suppress non-terminating errors
set "PS_SCRIPT=$ErrorActionPreference = 'Stop'; $failed = $false; "
:: v1.3 - 18/07/2026 - Create a case-sensitive Generic Dictionary to safely map phonemes
set "PS_SCRIPT=!PS_SCRIPT! $map = New-Object 'System.Collections.Generic.Dictionary[String,String]' ([System.StringComparer]::Ordinal); "
:: v1.3 - 18/07/2026 - Map consonants and vowels mapping (adding phonemes mapping logic)
set "PS_SCRIPT=!PS_SCRIPT! $map.Add('ng', 'g'); $map.Add('kk', 'k'); $map.Add('k', 'kh'); $map.Add('pp', 'p'); $map.Add('p', 'ph'); $map.Add('tt', 't'); $map.Add('t', 'th'); "
set "PS_SCRIPT=!PS_SCRIPT! $map.Add('A', '@'); $map.Add('O', 'Q'); $map.Add('E', '3'); $map.Add('Ua', '6'); $map.Add('U', '1'); $map.Add('au', 'aw'); "
set "PS_SCRIPT=!PS_SCRIPT! $map.Add('M', 'm'); $map.Add('Y', 'y'); $map.Add('W', 'w'); $map.Add('K', 'k'); $map.Add('B', 'b'); $map.Add('D', 'd'); "

:: v1.3 - 18/07/2026 - Iterate through each input file passed to the PowerShell script
set "PS_SCRIPT=!PS_SCRIPT! foreach ($file in @($env:OPENUTAU_CONVERTER_FILE)) { "
:: v1.3 - 18/07/2026 - Verify that the target file actually exists on the disk
set "PS_SCRIPT=!PS_SCRIPT!   if (-Not (Test-Path -LiteralPath $file)) { Write-Host \"[ERROR] File not found: $file\" -ForegroundColor Red; $failed = $true; continue; } "

:: v1.3 - 18/07/2026 - Extract the directory path of the current file being processed
set "PS_SCRIPT=!PS_SCRIPT!   $dir = [System.IO.Path]::GetDirectoryName($file); "
:: v1.3 - 18/07/2026 - Fallback to the current directory if no specific path is captured
set "PS_SCRIPT=!PS_SCRIPT!   if ([string]::IsNullOrEmpty($dir)) { $dir = '.'; } "
:: v1.3 - 18/07/2026 - Construct the final exact output path named strictly as 'TH_VCCV_Dict.txt'
set "PS_SCRIPT=!PS_SCRIPT!   $outPath = [System.IO.Path]::Combine($dir, 'TH_VCCV_Dict.txt'); "

:: v1.3 - 18/07/2026 - Print an informational message indicating which file is being read
set "PS_SCRIPT=!PS_SCRIPT!   Write-Host \"[PROCESSING] Reading: $file\" -ForegroundColor Cyan; "
:: v1.3 - 18/07/2026 - Read all lines from the file while ensuring proper UTF-8 encoding
set "PS_SCRIPT=!PS_SCRIPT!   $lines = Get-Content -Path $file -Encoding UTF8; "
:: v1.3 - 18/07/2026 - Initialize an empty array to collect the converted output lines
set "PS_SCRIPT=!PS_SCRIPT!   $outData = @(); "
:: v1.3 - 18/07/2026 - Loop through each individual line extracted from the input file
set "PS_SCRIPT=!PS_SCRIPT!   foreach ($line in $lines) { "
:: v1.3 - 18/07/2026 - Skip processing if the line is completely empty or just whitespace
set "PS_SCRIPT=!PS_SCRIPT!     if ([string]::IsNullOrWhiteSpace($line)) { continue; } "
:: v1.3 - 18/07/2026 - Split the line into segments based on tabs, spaces, or equals signs
set "PS_SCRIPT=!PS_SCRIPT!     $parts = $line -split '[\t =]+'; "
:: v1.3 - 18/07/2026 - Skip lines that do not have at least a word and a phoneme element
set "PS_SCRIPT=!PS_SCRIPT!     if ($parts.Length -lt 2) { continue; } "
:: v1.3 - 18/07/2026 - Extract the Thai word which is always the first element in the array
set "PS_SCRIPT=!PS_SCRIPT!     $thaiWord = $parts[0]; "
:: v1.3 - 18/07/2026 - Initialize an array to hold the newly mapped VCCV phonemes
set "PS_SCRIPT=!PS_SCRIPT!     $convertedPhonemes = @(); "
:: v1.3 - 18/07/2026 - Iterate through the phoneme tokens on the current line starting from index 1
set "PS_SCRIPT=!PS_SCRIPT!     for ($i = 1; $i -lt $parts.Length; $i++) { "
:: v1.3 - 18/07/2026 - Store the current token for matching and conversion
set "PS_SCRIPT=!PS_SCRIPT!       $token = $parts[$i]; "
:: v1.3 - 18/07/2026 - Check if the token exists in the dictionary and apply conversion if true
set "PS_SCRIPT=!PS_SCRIPT!       if ($map.ContainsKey($token)) { $convertedPhonemes += $map[$token]; } else { $convertedPhonemes += $token; } "
:: v1.3 - 18/07/2026 - End of the token processing loop
set "PS_SCRIPT=!PS_SCRIPT!     } "
:: v1.3 - 18/07/2026 - Rejoin the converted phonemes using a single space as a delimiter
set "PS_SCRIPT=!PS_SCRIPT!     $finalPhonemes = $convertedPhonemes -join ' '; "
:: v1.3 - 18/07/2026 - Reconstruct the full line combining the Thai word and new phonemes
set "PS_SCRIPT=!PS_SCRIPT!     $newLine = \"$thaiWord=$finalPhonemes\"; "
:: v1.3 - 18/07/2026 - Append the completed line to the final output array
set "PS_SCRIPT=!PS_SCRIPT!     $outData += $newLine; "
:: v1.3 - 18/07/2026 - Print the before and after transformation for real-time tracking
set "PS_SCRIPT=!PS_SCRIPT!     Write-Host \"   [CONVERTED] $line  ->  $newLine\" -ForegroundColor DarkGray; "
:: v1.3 - 18/07/2026 - End of the line processing loop
set "PS_SCRIPT=!PS_SCRIPT!   } "
:: v1.3 - 18/07/2026 - Write the entire collected array to the target TH_VCCV_Dict.txt file using UTF-8
set "PS_SCRIPT=!PS_SCRIPT!   [System.IO.File]::WriteAllLines($outPath, $outData, [System.Text.Encoding]::UTF8); "
:: v1.3 - 18/07/2026 - Display a success message detailing the exact location of the saved file
set "PS_SCRIPT=!PS_SCRIPT!   Write-Host \"[SUCCESS] Saved to: $outPath`n\" -ForegroundColor Green; "
:: v1.3 - 18/07/2026 - End of the main file iteration loop
set "PS_SCRIPT=!PS_SCRIPT! } if ($failed) { exit 1 } "

:: v1.3 - 18/07/2026 - Launch PowerShell in NoProfile mode and execute the assembled string script
set "CONVERT_EXIT_CODE=0"
for %%F in (%TARGET_FILES%) do (
    set "OPENUTAU_CONVERTER_FILE=%%~fF"
    powershell -NoProfile -Command "!PS_SCRIPT!"
    if errorlevel 1 set "CONVERT_EXIT_CODE=!errorlevel!"
)
set "OPENUTAU_CONVERTER_FILE="
if not "!CONVERT_EXIT_CODE!"=="0" (
    echo [ERROR] One or more dictionary conversions failed.
    pause
    exit /b !CONVERT_EXIT_CODE!
)

:: v1.3 - 18/07/2026 - Execute PowerShell command again to capture the exact completion time
for /f "tokens=*" %%a in ('powershell -NoProfile -Command "Get-Date -Format 'HH:mm:ss'"') do set "END_TIME=%%a"

:: v1.3 - 18/07/2026 - Display top border for the completion summary
echo ============================================================================
:: v1.3 - 18/07/2026 - Show the recorded time when all tasks successfully completed
echo [SYSTEM] Process completed at: %END_TIME%
:: v1.3 - 18/07/2026 - Confirm that no errors were encountered during the process
echo [SYSTEM] All conversions finished successfully with 0 errors.
:: v1.3 - 18/07/2026 - Display bottom border for the completion summary
echo ============================================================================
:: v1.3 - 18/07/2026 - Print an empty line before pausing
echo.
:: v1.3 - 18/07/2026 - Pause the console so the user can read the final output
pause
:: v1.3 - 18/07/2026 - Terminate the batch script with a success exit code
exit /b 0
