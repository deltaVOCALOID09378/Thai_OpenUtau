:: Made And Checked By DELTA SYNTH & Gemini AI
:: Original by Patiphat Wongyai
:: Version: 2.0 (Multi-Runtime .NET 8.0 - 12.0+ Support)

@echo off
setlocal enabledelayedexpansion
set "DOTNET_ROLL_FORWARD=LatestMajor"

echo ========================================================
echo DELTA SYNTH MULTI-PLATFORM PACKAGING SYSTEM (v2.0)
echo Supported Runtime: .NET 8.0 - 12.0+ (RollForward Active)
echo ========================================================
echo.

:: Find the next No.X folder
set "EXPORT_BASE=Export File"
if not exist "%EXPORT_BASE%" mkdir "%EXPORT_BASE%"

set NEXT_NUM=1
:LOOP
if exist "%EXPORT_BASE%\No.!NEXT_NUM!" (
    set /a NEXT_NUM+=1
    goto LOOP
)
set "EXPORT_DIR=%EXPORT_BASE%\No.!NEXT_NUM!"

echo [INFO] Creating output directory: %EXPORT_DIR%
mkdir "%EXPORT_DIR%"

echo.
echo [INFO] Building for Windows (x64) ...
dotnet publish "OpenUtau.sln" -c Release -r win-x64 --self-contained true -o "%EXPORT_DIR%\Windows_x64"
tar -a -c -f "%EXPORT_DIR%\Windows_x64.zip" -C "%EXPORT_DIR%\Windows_x64" *

echo.
echo [INFO] Building for Windows (x86) ...
dotnet publish "OpenUtau.sln" -c Release -r win-x86 --self-contained true -o "%EXPORT_DIR%\Windows_x86"
tar -a -c -f "%EXPORT_DIR%\Windows_x86.zip" -C "%EXPORT_DIR%\Windows_x86" *

echo.
echo [INFO] Building for Windows (ARM64) ...
dotnet publish "OpenUtau.sln" -c Release -r win-arm64 --self-contained true -o "%EXPORT_DIR%\Windows_ARM64"
tar -a -c -f "%EXPORT_DIR%\Windows_ARM64.zip" -C "%EXPORT_DIR%\Windows_ARM64" *

echo.
echo [INFO] Building for macOS (x64 / Intel) ...
dotnet publish "OpenUtau.sln" -c Release -r osx-x64 --self-contained true -o "%EXPORT_DIR%\macOS_x64"
tar -a -c -f "%EXPORT_DIR%\macOS_x64.zip" -C "%EXPORT_DIR%\macOS_x64" *

echo.
echo [INFO] Building for macOS (ARM64 / Apple Silicon) ...
dotnet publish "OpenUtau.sln" -c Release -r osx-arm64 --self-contained true -o "%EXPORT_DIR%\macOS_ARM64"
tar -a -c -f "%EXPORT_DIR%\macOS_ARM64.zip" -C "%EXPORT_DIR%\macOS_ARM64" *

echo.
echo [INFO] Building for Linux / Steam OS / Chrome OS (x64) ...
dotnet publish "OpenUtau.sln" -c Release -r linux-x64 --self-contained true -o "%EXPORT_DIR%\Linux_x64"
tar -c -z -f "%EXPORT_DIR%\Linux_x64.tar.gz" -C "%EXPORT_DIR%\Linux_x64" *

echo.
echo [INFO] Building for Linux / Chrome OS (ARM64) ...
dotnet publish "OpenUtau.sln" -c Release -r linux-arm64 --self-contained true -o "%EXPORT_DIR%\Linux_ARM64"
tar -c -z -f "%EXPORT_DIR%\Linux_ARM64.tar.gz" -C "%EXPORT_DIR%\Linux_ARM64" *

echo.
echo ========================================================
echo [SUCCESS] Multi-Platform Packaging Complete!
echo [SUCCESS] All executable files and archives have been exported to:
echo %CD%\%EXPORT_DIR%
echo ========================================================
pause
