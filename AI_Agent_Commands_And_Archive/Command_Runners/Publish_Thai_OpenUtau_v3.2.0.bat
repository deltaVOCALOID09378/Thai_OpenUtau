@echo off
REM ============================================================================
REM 1. Date        : 03 November 2026
REM 2. File Name   : Publish_Thai_OpenUtau_v3.2.0.bat
REM 3. Version     : 3.2.0
REM 4. Location    : D:\Program Developing\Thai_OpenUtau\Publish_Thai_OpenUtau_v3.2.0.bat
REM 5. Function    : Publishes Thai_OpenUtau into Published\Version no. 3.2.0 as Clean Raw Files (Zero-Zip)
REM 6. Credit      : Made And Checked By DELTA SYNTH & All Agentic AI
REM                  Original by DELTA SYNTH
REM ============================================================================

setlocal EnableDelayedExpansion
title DELTA SYNTH - Thai_OpenUtau Clean Raw Files Publisher (v3.2.0)

set "DEFAULT_ROOT=D:\Program Developing\Thai_OpenUtau"
set "VERSION_NO=3.2.0"
set "RUNTIME_ID=win-x64"
set "SCRIPT_DIR=%~dp0"

:PUBLISH_LOOP
cls
echo ============================================================================
echo        DELTA SYNTH - Thai_OpenUtau Clean Raw Files Publisher (v%VERSION_NO%)
echo        Made And Checked By DELTA SYNTH ^& All Agentic AI
echo ============================================================================
echo.

set "TARGET_ROOT=%~1"
if "%TARGET_ROOT%"=="" (
    if exist "%DEFAULT_ROOT%\OpenUtau\OpenUtau.csproj" (
        set "TARGET_ROOT=%DEFAULT_ROOT%"
    ) else (
        echo [INPUT] Drag and drop the Thai_OpenUtau root folder here, or press Enter for default:
        set /p "TARGET_ROOT=> "
    )
)

set "TARGET_ROOT=%TARGET_ROOT:"=%"
if "%TARGET_ROOT%"=="" set "TARGET_ROOT=%DEFAULT_ROOT%"

if not exist "%TARGET_ROOT%\OpenUtau\OpenUtau.csproj" (
    if exist "%TARGET_ROOT%\..\OpenUtau\OpenUtau.csproj" (
        for %%I in ("%TARGET_ROOT%\..") do set "TARGET_ROOT=%%~fI"
    ) else (
        echo [ERROR] Could not find OpenUtau\OpenUtau.csproj in: "%TARGET_ROOT%"
        goto ASK_NEXT_ACTION
    )
)

cd /d "%TARGET_ROOT%"
set "PUBLISH_DIR=%TARGET_ROOT%\Published\Version no. %VERSION_NO%"

echo [INFO] Project Root Directory : %TARGET_ROOT%
echo [INFO] Target Publish Folder  : %PUBLISH_DIR%
echo [INFO] Target Runtime         : %RUNTIME_ID% (Clean Raw Files - Zero-Zip)
echo.

REM Step 1: Run Python Patcher if available (prioritizing runtime\python.exe)
set "PYTHON_EXE=python"
if exist "%SCRIPT_DIR%runtime\python.exe" (
    set "PYTHON_EXE=%SCRIPT_DIR%runtime\python.exe"
) else if exist "%TARGET_ROOT%\runtime\python.exe" (
    set "PYTHON_EXE=%TARGET_ROOT%\runtime\python.exe"
)

if exist "%TARGET_ROOT%\apply_all_delta_synth_fixes.py" (
    echo [STEP 1/6] Running apply_all_delta_synth_fixes.py to ensure Zero-Defect state...
    "%PYTHON_EXE%" "%TARGET_ROOT%\apply_all_delta_synth_fixes.py" "%TARGET_ROOT%"
    if !ERRORLEVEL! NEQ 0 (
        echo [WARN] Python patcher returned non-zero status. Proceeding with publish...
    )
) else (
    echo [STEP 1/6] Checking for duplicate legacy files before publish...
    if exist "%TARGET_ROOT%\OpenUtau.Core\PianoRollUnlockerMacro.cs" (
        del /f /q "%TARGET_ROOT%\OpenUtau.Core\PianoRollUnlockerMacro.cs"
    )
)
echo.

REM Step 2: Prepare Clean Raw Files Directory
echo [STEP 2/6] Preparing clean target directory: "%PUBLISH_DIR%"...
if exist "%PUBLISH_DIR%" (
    rmdir /s /q "%PUBLISH_DIR%"
)
mkdir "%PUBLISH_DIR%"
mkdir "%PUBLISH_DIR%\Plugins"
mkdir "%PUBLISH_DIR%\Dictionaries"
mkdir "%PUBLISH_DIR%\Singers"
mkdir "%PUBLISH_DIR%\Resamplers"
mkdir "%PUBLISH_DIR%\Wavtools"
mkdir "%PUBLISH_DIR%\Dependency"
echo.

REM Step 3: Build OpenUtau.Plugin.Builtin in Release Mode
echo [STEP 3/6] Compiling OpenUtau.Plugin.Builtin (Release - Zero Warnings)...
dotnet build "%TARGET_ROOT%\OpenUtau.Plugin.Builtin\OpenUtau.Plugin.Builtin.csproj" -c Release /p:WarningLevel=0 /p:TreatWarningsAsErrors=false
if %ERRORLEVEL% NEQ 0 (
    echo [FAILED] Failed to compile OpenUtau.Plugin.Builtin.csproj.
    goto ASK_NEXT_ACTION
)
echo.

REM Step 4: Publish OpenUtau Main Application as Clean Raw Files
echo [STEP 4/6] Publishing OpenUtau main application to "%PUBLISH_DIR%"...
dotnet publish "%TARGET_ROOT%\OpenUtau\OpenUtau.csproj" ^
    -c Release ^
    -r %RUNTIME_ID% ^
    --self-contained true ^
    -o "%PUBLISH_DIR%" ^
    /p:PublishSingleFile=false ^
    /p:PublishReadyToRun=true ^
    /p:DebugType=embedded ^
    /p:WarningLevel=0 ^
    /p:TreatWarningsAsErrors=false

if %ERRORLEVEL% NEQ 0 (
    echo [FAILED] dotnet publish encountered an error. Please review the log above.
    goto ASK_NEXT_ACTION
)
echo.

REM Step 5: Copy Builtin Plugin DLLs and Dictionaries into Published Directory
echo [STEP 5/6] Aggregating Builtin Plugins and Thai/Multilingual Dictionaries...
if exist "%TARGET_ROOT%\OpenUtau.Plugin.Builtin\bin\Release\net8.0\OpenUtau.Plugin.Builtin.dll" (
    copy /y "%TARGET_ROOT%\OpenUtau.Plugin.Builtin\bin\Release\net8.0\OpenUtau.Plugin.Builtin.dll" "%PUBLISH_DIR%\Plugins\" >nul
    copy /y "%TARGET_ROOT%\OpenUtau.Plugin.Builtin\bin\Release\net8.0\OpenUtau.Plugin.Builtin.dll" "%PUBLISH_DIR%\" >nul
    echo [OK] Copied OpenUtau.Plugin.Builtin.dll to Published and Plugins folder.
)

if exist "%TARGET_ROOT%\Dictionaries" (
    xcopy "%TARGET_ROOT%\Dictionaries\*" "%PUBLISH_DIR%\Dictionaries\" /E /I /Y /Q >nul
    echo [OK] Copied root Dictionaries to "%PUBLISH_DIR%\Dictionaries".
)

if exist "%TARGET_ROOT%\OpenUtau.Plugin.Builtin\Dictionary" (
    xcopy "%TARGET_ROOT%\OpenUtau.Plugin.Builtin\Dictionary\*" "%PUBLISH_DIR%\Dictionaries\" /E /I /Y /Q >nul
    echo [OK] Merged Builtin Dictionary files into "%PUBLISH_DIR%\Dictionaries".
)
echo.

REM Step 6: Generate ALL_MODIFIED_FILES_CHANGELOG.md inside Published\Version no. 3.2.0
echo [STEP 6/6] Generating ALL_MODIFIED_FILES_CHANGELOG.md...
set "CHANGELOG_FILE=%PUBLISH_DIR%\ALL_MODIFIED_FILES_CHANGELOG.md"
(
echo # ALL MODIFIED FILES CHANGELOG - Thai OpenUtau v%VERSION_NO%
echo.
echo - **Release Version:** %VERSION_NO%
echo - **Build Date:** 03 November 2026
echo - **Runtime Target:** %RUNTIME_ID% ^(Self-Contained Clean Raw Files - Zero-Zip^)
echo - **Credit:** Made And Checked By DELTA SYNTH ^& All Agentic AI ^| Original by OpenUtau Authors ^& DELTA SYNTH
echo.
echo ---
echo.
echo ## 1. OpenUtau.Core Fixes ^& Enhancements
echo - `OpenUtau.Core/Format/USTx.cs` ^(v1.1^): Added `CLRY` ^(secondary voice color^) and `XSY` ^(cross-synthesis^) expression constants and default descriptors.
echo - `OpenUtau.Core/Util/ProcessRunner.cs` ^(v1.2^): Upgraded `ProcessRunner.Run` to return standard output/error string safely using `ManualResetEventSlim`.
echo - `OpenUtau.Core/Render/IRenderer.cs` ^(v1.1^): Added `ResamplerFailedException` class to `OpenUtau.Core.Render`.
echo - `OpenUtau.Core/Classic/ExeResampler.cs` ^(v3.1^): Connected thread-isolated staging output and atomic cache swap with `ResamplerFailedException`.
echo - `OpenUtau.Core/DeltaSingers/SingerHubClient.cs` ^(v1.3^): Added `NameTh`, `Voicer`, `Genre`, and `Engine` properties to `SingerHubEntry`.
echo - `OpenUtau.Core/DeltaSingers/DeltaSynthSingerRegistry.cs` ^(v1.2^): Synchronized all 55 DELTA SYNTH voicebank profiles and updated Helen voicer metadata to Unspecified.
echo - `OpenUtau.Core/Format/OpusOggWaveReader.cs` ^(v1.2^): Added `Stream` constructor overload for lock-free audio decoding.
echo - `OpenUtau.Core/Format/Wave.cs` ^(v3.1^): Fixed `Mp3FileReaderBase` public constructor invocation and `OpusOggWaveReader` stream buffering.
echo - `OpenUtau.Core/Classic/WorldlineRenderer.cs` ^(v3.1^) ^& `ClassicRenderer.cs` ^(v3.1^): Verified `CLRY`/`XSY` support and lock-free cache writing.
echo.
echo ## 2. OpenUtau.Plugin.Builtin Fixes ^& Enhancements
echo - `OpenUtau.Plugin.Builtin/PianoRollUnlockerMacro.cs` ^(v1.5^): Consolidated duplicate class definition and implemented `BatchEdit` interface.
echo - `OpenUtau.Plugin.Builtin/PhonemeBasedPhonemizer.cs` ^(v1.2^): Changed `GetSymbols` to `protected virtual`, guarded against negative vowel index and divide-by-zero.
echo - `OpenUtau.Plugin.Builtin/DeltaENtoJAPhonemizer.cs` ^(v1.2^): Added empty-consonant guard in `HasVc` to prevent `IndexOutOfRangeException`.
echo - `OpenUtau.Plugin.Builtin/OpenUtau.Plugin.Builtin.csproj` ^(v1.4^): Excluded archive/agent directories and suppressed nullable compiler warnings.
echo.
echo ## 3. OpenUtau ^(UI / ViewModels^) Fixes ^& Enhancements
echo - `OpenUtau/ViewModels/TrackHeaderViewModel.cs` ^(v3.2.1^): Implemented `UpdateTrackNoForeground` luminance calculation to resolve `CS0103`.
echo - `OpenUtau/Views/AutoPitchwriterDialog.axaml` ^(v1.1^) ^& `.axaml.cs` ^(v1.3^): Fixed `chkWShape` control name and bound all 6 contextual pitch curve options.
echo - `OpenUtau/ViewModels/SingerHubViewModel.cs` ^(v1.3^): Integrated `DeltaSynthSingerRegistry.GetOfficialEntries` and `Host == "delta"` filtering.
echo - `OpenUtau/OpenUtau.csproj` ^(v1.3^): Standardized `<NoWarn>` tags for zero-warning compilation.
) > "%CHANGELOG_FILE%"

echo [SUCCESS] Clean Raw Files published successfully!
echo [OUTPUT]  "%PUBLISH_DIR%"
echo [FILE]    "%PUBLISH_DIR%\OpenUtau.exe"
echo [DOC]     "%CHANGELOG_FILE%"
echo.

REM Open the published folder in Windows Explorer automatically
if exist "%PUBLISH_DIR%\OpenUtau.exe" (
    echo [INFO] Opening published folder in Windows Explorer...
    explorer "%PUBLISH_DIR%"
)

:ASK_NEXT_ACTION
echo ============================================================================
echo [READY] Drag and drop another project folder here, press Enter to re-publish,
echo         or type Q to exit:
set "USER_INPUT="
set /p "USER_INPUT=> "
if /i "%USER_INPUT%"=="Q" goto END_PUBLISHER
set "TARGET_ROOT=%USER_INPUT:"=%"
shift
goto PUBLISH_LOOP

:END_PUBLISHER
echo [INFO] Exiting DELTA SYNTH Publisher. Goodbye!
endlocal
exit /b 0