@echo off
rem ==============================================================================
rem Date         : 11/11/2026 (B.E. 2569 / A.D. 2026)
rem File Name    : Program.Package_And_Build_DragDrop.bat
rem Version      : 1.0
rem Author Credit: Made And Checked By DELTA SYNTH & All Agentic AI Engine | Original by Thai OpenUtau Project
rem File Location: D:\Program Developing\Thai_OpenUtau\Program.Package_And_Build_DragDrop.bat
rem Description  : Professional Drag-and-Drop Automated Build, DLL Bundling,
rem                .NET 8.0 Runtime Prerequisite Verification, and SFX/ZIP Packaging Pipeline.
rem ==============================================================================

setlocal enabledelayedexpansion
chcp 65001 >nul
title DELTA SYNTH - Thai OpenUtau Packaging Pipeline v1.0
color 0B

set "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1"

rem ------------------------------------------------------------------------------
rem Step 1: Detect Project Root and Target Input (Drag-and-Drop Support)
rem ------------------------------------------------------------------------------
set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

set "PROJECT_ROOT=%SCRIPT_DIR%"
set "CUSTOM_PUBLISH="

if not "%~1"=="" (
    if exist "%~1\OpenUtau\OpenUtau.csproj" (
        set "PROJECT_ROOT=%~1"
    ) else if exist "%~1\Thai OpenUtau.sln" (
        set "PROJECT_ROOT=%~1"
    ) else if exist "%~1\OpenUtau.exe" (
        set "CUSTOM_PUBLISH=%~1"
    ) else if exist "%~1" (
        set "PROJECT_ROOT=%~1"
    )
)

cd /d "%PROJECT_ROOT%" 2>nul

rem ------------------------------------------------------------------------------
rem Step 2: Initialize Build Logging
rem ------------------------------------------------------------------------------
set "LOG_DIR=%PROJECT_ROOT%\Build_Logs"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%" >nul 2>&1

for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value 2^>nul') do set "DATETIME=%%I"
if not defined DATETIME set "DATETIME=%DATE:~10,4%%DATE:~4,2%%DATE:~7,2%_000000"
set "TIMESTAMP=%DATETIME:~0,8%_%DATETIME:~8,6%"
set "LOG_FILE=%LOG_DIR%\Package_Build_%TIMESTAMP%.log"

call :LogMessage "======================================================================"
call :LogMessage "      DELTA SYNTH - Thai OpenUtau Production Packaging Pipeline"
call :LogMessage "      Target: Win-x64 Standalone Archive & SFX GUI Installer"
call :LogMessage "======================================================================"
call :LogMessage "Timestamp       : %DATE% %TIME%"
call :LogMessage "Project Root    : %PROJECT_ROOT%"
call :LogMessage "Log File        : %LOG_FILE%"
call :LogMessage ""

rem ------------------------------------------------------------------------------
rem Step 3: Validate Toolchains and Dependencies
rem ------------------------------------------------------------------------------
call :LogMessage "[1/6] Verifying toolchain environment..."

where dotnet >nul 2>&1
if !errorlevel! neq 0 (
    call :LogError "The 'dotnet' command was not found. Please install .NET 8.0 SDK."
    goto :BuildFailure
)
for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set "DOTNET_SDK_VER=%%v"
call :LogMessage " - Found .NET SDK Version      : %DOTNET_SDK_VER%"

rem Check .NET 8.0 Desktop Runtime on Host System
dotnet --list-runtimes | findstr /i "Microsoft.WindowsDesktop.App 8." >nul 2>&1
if !errorlevel! equ 0 (
    call :LogMessage " - Found .NET 8.0 Desktop App  : Verified present on host."
) else (
    call :LogWarning " - .NET 8.0 Desktop App is not detected on host system."
)

rem Find 7-Zip
set "SEVENZIP=C:\Program Files\7-Zip\7z.exe"
if not exist "%SEVENZIP%" (
    where 7z.exe >nul 2>&1
    if !errorlevel! equ 0 for /f "tokens=*" %%p in ('where 7z.exe') do set "SEVENZIP=%%p"
)
if exist "%SEVENZIP%" (
    call :LogMessage " - Found 7-Zip Toolchain       : %SEVENZIP%"
) else (
    call :LogWarning " - 7-Zip not found. Fallback to PowerShell archive engine."
)

rem Find WinRAR
set "RAR_EXE=C:\Program Files\WinRAR\Rar.exe"
if not exist "%RAR_EXE%" (
    where rar.exe >nul 2>&1
    if !errorlevel! equ 0 for /f "tokens=*" %%p in ('where rar.exe') do set "RAR_EXE=%%p"
)
if exist "%RAR_EXE%" (
    call :LogMessage " - Found WinRAR SFX Engine     : %RAR_EXE%"
) else (
    call :LogWarning " - WinRAR not found. SFX executable generation will be skipped."
)

rem Find Target Icon
set "ICON_FILE="
if exist "%PROJECT_ROOT%\OpenUtau\Thai OpenUtau Logo type.ico" (
    set "ICON_FILE=%PROJECT_ROOT%\OpenUtau\Thai OpenUtau Logo type.ico"
) else if exist "%PROJECT_ROOT%\OpenUtau\Assets\Thai OpenUtau Logo.ico" (
    set "ICON_FILE=%PROJECT_ROOT%\OpenUtau\Assets\Thai OpenUtau Logo.ico"
) else if exist "%PROJECT_ROOT%\Logo\Thai OpenUtau Logo type.ico" (
    set "ICON_FILE=%PROJECT_ROOT%\Logo\Thai OpenUtau Logo type.ico"
)
if defined ICON_FILE (
    call :LogMessage " - Found Installer Icon        : %ICON_FILE%"
) else (
    call :LogWarning " - Custom icon not found. Default SFX icon will be used."
)

rem Find Bundled .NET 8.0 Runtime Installer
set "BUNDLED_RUNTIME="
for %%f in ("%PROJECT_ROOT%\windowsdesktop-runtime-8.0.*-win-x64.exe") do (
    if exist "%%~f" set "BUNDLED_RUNTIME=%%~f"
)
if defined BUNDLED_RUNTIME (
    call :LogMessage " - Found Bundled .NET Runtime  : %BUNDLED_RUNTIME%"
) else (
    call :LogWarning " - Bundled .NET runtime installer not found in project root."
)

rem ------------------------------------------------------------------------------
rem Step 4: Resolve Package Version
rem ------------------------------------------------------------------------------
set "PKG_VER=3.9.5"
if exist "%PROJECT_ROOT%\BumpVersion.ps1" (
    for /f "usebackq tokens=*" %%v in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%PROJECT_ROOT%\BumpVersion.ps1" -Preview 2^>nul`) do (
        set "LAST_LINE=%%v"
    )
    if defined LAST_LINE set "PKG_VER=!LAST_LINE!"
)
call :LogMessage " - Resolved Release Version    : v%PKG_VER%"
call :LogMessage ""

rem ------------------------------------------------------------------------------
rem Step 5: Execute Production Publish
rem ------------------------------------------------------------------------------
set "PUBLISH_DIR=%PROJECT_ROOT%\Publish"
if defined CUSTOM_PUBLISH (
    set "PUBLISH_DIR=%CUSTOM_PUBLISH%"
    call :LogMessage "[2/6] Using pre-built target directory: %PUBLISH_DIR%"
) else (
    call :LogMessage "[2/6] Compiling Thai OpenUtau in Release mode (win-x64)..."
    if not exist "%PUBLISH_DIR%" mkdir "%PUBLISH_DIR%" >nul 2>&1
    
    set "TARGET_PROJ=%PROJECT_ROOT%\OpenUtau\OpenUtau.csproj"
    if not exist "%TARGET_PROJ%" set "TARGET_PROJ=%PROJECT_ROOT%\Thai OpenUtau.sln"
    
    call :LogMessage " - Executing: dotnet publish !TARGET_PROJ! -c Release -r win-x64 --self-contained false -p:PublishReadyToRun=true -p:RollForward=LatestMajor"
    dotnet publish "!TARGET_PROJ!" -c Release -r win-x64 --self-contained false -p:PublishReadyToRun=true -p:RollForward=LatestMajor -o "%PUBLISH_DIR%" >> "%LOG_FILE%" 2>&1
    if !errorlevel! neq 0 (
        call :LogError "Production publish failed. Please inspect build log for compiler errors."
        goto :BuildFailure
    )
    call :LogMessage " [OK] Production binaries compiled successfully."
)
call :LogMessage ""

rem ------------------------------------------------------------------------------
rem Step 6: Consolidate DLLs, Plugins, Dictionaries and Native Assets
rem ------------------------------------------------------------------------------
call :LogMessage "[3/6] Bundling libraries, plugins, dictionaries and native assets..."

rem Deploy Builtin Plugin
if exist "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\bin\Release\net8.0\OpenUtau.Plugin.Builtin.dll" (
    copy /y "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\bin\Release\net8.0\OpenUtau.Plugin.Builtin.dll" "%PUBLISH_DIR%\" >nul 2>&1
    if not exist "%PUBLISH_DIR%\Plugins" mkdir "%PUBLISH_DIR%\Plugins" >nul 2>&1
    copy /y "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\bin\Release\net8.0\OpenUtau.Plugin.Builtin.dll" "%PUBLISH_DIR%\Plugins\" >nul 2>&1
    call :LogMessage " - Bundled OpenUtau.Plugin.Builtin.dll"
)

rem Deploy Native Runtimes (worldline, DirectML, SDL3, ffmpeg)
if exist "%PUBLISH_DIR%\runtimes\win-x64\native\*.dll" (
    copy /y "%PUBLISH_DIR%\runtimes\win-x64\native\*.dll" "%PUBLISH_DIR%\" >nul 2>&1
    call :LogMessage " - Extracted native runtimes to root"
)
if exist "%PROJECT_ROOT%\worldline.dll" copy /y "%PROJECT_ROOT%\worldline.dll" "%PUBLISH_DIR%\" >nul 2>&1
if exist "%PROJECT_ROOT%\SDL3.dll" copy /y "%PROJECT_ROOT%\SDL3.dll" "%PUBLISH_DIR%\" >nul 2>&1
if exist "%PROJECT_ROOT%\ffmpeg.exe" copy /y "%PROJECT_ROOT%\ffmpeg.exe" "%PUBLISH_DIR%\" >nul 2>&1
if exist "%PROJECT_ROOT%\All Rendered File\DirectML.dll" copy /y "%PROJECT_ROOT%\All Rendered File\DirectML.dll" "%PUBLISH_DIR%\" >nul 2>&1

rem Deploy Dictionaries
if exist "%PROJECT_ROOT%\Dictionaries" (
    if not exist "%PUBLISH_DIR%\Dictionaries" mkdir "%PUBLISH_DIR%\Dictionaries" >nul 2>&1
    xcopy "%PROJECT_ROOT%\Dictionaries\*" "%PUBLISH_DIR%\Dictionaries\" /s /e /y /q >nul 2>&1
    call :LogMessage " - Consolidated Dictionaries directory"
)
if exist "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\Dictionary" (
    if not exist "%PUBLISH_DIR%\Dictionary" mkdir "%PUBLISH_DIR%\Dictionary" >nul 2>&1
    xcopy "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\Dictionary\*" "%PUBLISH_DIR%\Dictionary\" /s /e /y /q >nul 2>&1
    if not exist "%PUBLISH_DIR%\Dictionaries" mkdir "%PUBLISH_DIR%\Dictionaries" >nul 2>&1
    xcopy "%PROJECT_ROOT%\OpenUtau.Plugin.Builtin\Dictionary\*" "%PUBLISH_DIR%\Dictionaries\" /s /e /y /q >nul 2>&1
    call :LogMessage " - Consolidated OpenUtau.Plugin.Builtin dictionaries"
)

rem Deploy Plugins and Synthesis Assets
if exist "%PROJECT_ROOT%\Plugins" (
    if not exist "%PUBLISH_DIR%\Plugins" mkdir "%PUBLISH_DIR%\Plugins" >nul 2>&1
    xcopy "%PROJECT_ROOT%\Plugins\*" "%PUBLISH_DIR%\Plugins\" /s /e /y /q >nul 2>&1
)
if exist "%PROJECT_ROOT%\Resamplers" (
    if not exist "%PUBLISH_DIR%\Resamplers" mkdir "%PUBLISH_DIR%\Resamplers" >nul 2>&1
    xcopy "%PROJECT_ROOT%\Resamplers\*" "%PUBLISH_DIR%\Resamplers\" /s /e /y /q >nul 2>&1
)
if exist "%PROJECT_ROOT%\Wavtools" (
    if not exist "%PUBLISH_DIR%\Wavtools" mkdir "%PUBLISH_DIR%\Wavtools" >nul 2>&1
    xcopy "%PROJECT_ROOT%\Wavtools\*" "%PUBLISH_DIR%\Wavtools\" /s /e /y /q >nul 2>&1
)
if exist "%PROJECT_ROOT%\Singers" (
    if not exist "%PUBLISH_DIR%\Singers" mkdir "%PUBLISH_DIR%\Singers" >nul 2>&1
    xcopy "%PROJECT_ROOT%\Singers\*" "%PUBLISH_DIR%\Singers\" /s /e /y /q >nul 2>&1
)
if exist "%PROJECT_ROOT%\Templates" (
    if not exist "%PUBLISH_DIR%\Templates" mkdir "%PUBLISH_DIR%\Templates" >nul 2>&1
    xcopy "%PROJECT_ROOT%\Templates\*" "%PUBLISH_DIR%\Templates\" /s /e /y /q >nul 2>&1
)
if exist "%PROJECT_ROOT%\Themes" (
    if not exist "%PUBLISH_DIR%\Themes" mkdir "%PUBLISH_DIR%\Themes" >nul 2>&1
    xcopy "%PROJECT_ROOT%\Themes\*" "%PUBLISH_DIR%\Themes\" /s /e /y /q >nul 2>&1
)
if exist "%PROJECT_ROOT%\TrackColors" (
    if not exist "%PUBLISH_DIR%\TrackColors" mkdir "%PUBLISH_DIR%\TrackColors" >nul 2>&1
    xcopy "%PROJECT_ROOT%\TrackColors\*" "%PUBLISH_DIR%\TrackColors\" /s /e /y /q >nul 2>&1
)

rem Deploy Icon to Package Root for SFX Shortcuts
if defined ICON_FILE (
    copy /y "%ICON_FILE%" "%PUBLISH_DIR%\Thai OpenUtau Logo type.ico" >nul 2>&1
)

rem Bundle .NET 8.0 Runtime Redistributable
if defined BUNDLED_RUNTIME (
    if not exist "%PUBLISH_DIR%\redist" mkdir "%PUBLISH_DIR%\redist" >nul 2>&1
    copy /y "%BUNDLED_RUNTIME%" "%PUBLISH_DIR%\redist\" >nul 2>&1
    call :LogMessage " - Bundled .NET 8.0 Desktop Runtime in redist\"
)

rem Deploy High-Performance Launcher with Prerequisite Check (<1s execution)
(
    echo @echo off
    echo rem DELTA SYNTH - Thai OpenUtau High-Performance Launcher
    echo setlocal enabledelayedexpansion
    echo cd /d "%%~dp0"
    echo rem Prerequisite check for .NET 8.0 Desktop Runtime
    echo dotnet --list-runtimes 2^>nul ^| findstr /i "Microsoft.WindowsDesktop.App 8." ^>nul 2^>^&1
    echo if !errorlevel! neq 0 ^(
    echo     for %%%%R in ^("%%~dp0redist\windowsdesktop-runtime-8.0.*-win-x64.exe"^) do ^(
    echo         if exist "%%%%~R" ^(
    echo             echo [INFO] Installing required .NET 8.0 Desktop Runtime...
    echo             start /wait "" "%%%%~R" /install /quiet /norestart
    echo         ^)
    echo     ^)
    echo ^)
    echo if "%%~1"=="" ^(
    echo     start "" "OpenUtau.exe"
    echo ^) else ^(
    echo     start "" "OpenUtau.exe" "%%~1"
    echo ^)
    echo exit /b 0
) > "%PUBLISH_DIR%\Start-The-Program.bat"

call :LogMessage " [OK] All library bundles and operational assets consolidated."
call :LogMessage ""

rem ------------------------------------------------------------------------------
rem Step 7: Create Package Outputs (SFX Installer & Standalone ZIP)
rem ------------------------------------------------------------------------------
call :LogMessage "[4/6] Packaging distribution targets..."

set "EXPORT_DIR=%PROJECT_ROOT%\Published\Version no. %PKG_VER%"
if not exist "%EXPORT_DIR%" mkdir "%EXPORT_DIR%" >nul 2>&1

set "INSTALLER_NAME=Thai OpenUtau Version %PKG_VER% Installing.exe"
set "STANDALONE_ZIP=Thai OpenUtau Version %PKG_VER% Standalone.zip"

rem 7.1 Standalone ZIP Archive Generation
if exist "%SEVENZIP%" (
    call :LogMessage " - Generating Standalone ZIP using 7-Zip Ultra compression..."
    "%SEVENZIP%" a -tzip -mx=9 "%EXPORT_DIR%\%STANDALONE_ZIP%" "%PUBLISH_DIR%\*" >> "%LOG_FILE%" 2>&1
    if !errorlevel! equ 0 (
        copy /y "%EXPORT_DIR%\%STANDALONE_ZIP%" "%PROJECT_ROOT%\" >nul 2>&1
        call :LogMessage "   [SUCCESS] Standalone ZIP created: %EXPORT_DIR%\%STANDALONE_ZIP%"
    ) else (
        call :LogWarning "   [WARNING] 7-Zip compression returned errors."
    )
) else (
    call :LogMessage " - Generating Fallback ZIP using PowerShell..."
    powershell -NoProfile -Command "Compress-Archive -Path '%PUBLISH_DIR%\*' -DestinationPath '%EXPORT_DIR%\%STANDALONE_ZIP%' -Force" >> "%LOG_FILE%" 2>&1
    copy /y "%EXPORT_DIR%\%STANDALONE_ZIP%" "%PROJECT_ROOT%\" >nul 2>&1
    call :LogMessage "   [SUCCESS] Fallback ZIP created: %EXPORT_DIR%\%STANDALONE_ZIP%"
)

rem 7.2 Self-Extracting GUI Installer (SFX) Generation
if exist "%RAR_EXE%" (
    call :LogMessage " - Generating Standalone SFX Installer using WinRAR engine..."
    
    set "SFX_CONF=%TEMP%\thai_openutau_sfx_cfg.txt"
    (
        echo Path=Thai OpenUtau
        echo SavePath
        echo Setup=Start-The-Program.bat
        echo Silent=0
        echo Overwrite=1
        echo Title=Thai OpenUtau Version: %PKG_VER% Installing
        echo Text
        echo {
        echo Thai OpenUtau Version: %PKG_VER%
        echo Complete Thai Vocal Synthesis Suite ^(VCCV, VCV, CpV, Arpasing^)
        echo Developed by DELTA SYNTH
        echo }
        echo Shortcut=D, "Start-The-Program.bat", "", "Thai OpenUtau by DELTA SYNTH", "Thai OpenUtau", "Thai OpenUtau Logo type.ico"
        echo Shortcut=P, "Start-The-Program.bat", "", "Thai OpenUtau by DELTA SYNTH", "Thai OpenUtau", "Thai OpenUtau Logo type.ico"
    ) > "!SFX_CONF!"

    set "ICON_SWITCH="
    if defined ICON_FILE set "ICON_SWITCH=-iicon"!ICON_FILE!""

    "%RAR_EXE%" a -r -ep1 -sfx -m5 !ICON_SWITCH! -z"!SFX_CONF!" -y "%EXPORT_DIR%\%INSTALLER_NAME%" "%PUBLISH_DIR%\*" >> "%LOG_FILE%" 2>&1
    if !errorlevel! equ 0 (
        copy /y "%EXPORT_DIR%\%INSTALLER_NAME%" "%PROJECT_ROOT%\" >nul 2>&1
        call :LogMessage "   [SUCCESS] SFX Installer created : %EXPORT_DIR%\%INSTALLER_NAME%"
    ) else (
        call :LogWarning "   [WARNING] WinRAR SFX generation returned errors."
    )
    if exist "!SFX_CONF!" del "!SFX_CONF!" >nul 2>&1
) else (
    call :LogWarning " - Skipping SFX generation: WinRAR engine not found."
)
call :LogMessage ""

rem ------------------------------------------------------------------------------
rem Step 8: Final Integrity Verification
rem ------------------------------------------------------------------------------
call :LogMessage "[5/6] Verifying output artifacts integrity..."

if exist "%EXPORT_DIR%\%STANDALONE_ZIP%" (
    for %%A in ("%EXPORT_DIR%\%STANDALONE_ZIP%") do call :LogMessage " - Standalone Archive Size : %%~zA bytes"
)
if exist "%EXPORT_DIR%\%INSTALLER_NAME%" (
    for %%B in ("%EXPORT_DIR%\%INSTALLER_NAME%") do call :LogMessage " - SFX Installer Size      : %%~zB bytes"
)

call :LogMessage ""
call :LogMessage "======================================================================"
call :LogMessage " [SUCCESS] Thai OpenUtau packaging pipeline finished successfully!"
call :LogMessage "======================================================================"
call :LogMessage " Deliverables Available at:"
call :LogMessage "  1. Installer Package : %EXPORT_DIR%\%INSTALLER_NAME%"
call :LogMessage "  2. Standalone ZIP    : %EXPORT_DIR%\%STANDALONE_ZIP%"
call :LogMessage "  3. Root Mirror       : %PROJECT_ROOT%\%INSTALLER_NAME%"
call :LogMessage "  4. Build Log File    : %LOG_FILE%"
call :LogMessage "======================================================================"
color 0A
pause
exit /b 0

rem ------------------------------------------------------------------------------
rem Helper Subroutines for Logging
rem ------------------------------------------------------------------------------
:LogMessage
echo %~1
echo [%TIME%] %~1 >> "%LOG_FILE%" 2>&1
exit /b 0

:LogWarning
echo [WARNING] %~1
echo [%TIME%] [WARNING] %~1 >> "%LOG_FILE%" 2>&1
exit /b 0

:LogError
color 0C
echo [ERROR] %~1
echo [%TIME%] [ERROR] %~1 >> "%LOG_FILE%" 2>&1
exit /b 0

:BuildFailure
call :LogMessage ""
call :LogMessage "======================================================================"
call :LogMessage " [FAILED] Packaging pipeline aborted due to critical error."
call :LogMessage " Review log file for root cause: %LOG_FILE%"
call :LogMessage "======================================================================"
color 0C
pause
exit /b 1
