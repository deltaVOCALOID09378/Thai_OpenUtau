@echo off
rem ======================================================================
rem Date         : 27/10/2026 (B.E. 2569 / A.D. 2026)
rem File Name    : Release_Cycle.bat
rem Version      : 1.0
rem Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
rem File Location: Root Directory of the project (Thai_OpenUtau\Release_Cycle.bat)
rem Description  : One-click release cycle to run before EVERY program packaging.
rem                Phases: Preflight, Version, Build (0 warnings gate), Tests,
rem                Publish (Compile_Full_Program.bat), Package (Package_Program.bat),
rem                Verify (7-Zip test + SHA256), Record (Release_History.csv).
rem                Supports Drag and Drop of the project folder onto this file.
rem Usage        : Release_Cycle.bat [project folder] [/V:x.y.z] [/BUMP] [/SKIPTESTS]
rem                                  [/SKIPPACK] [/DRYRUN] [/NOPAUSE]
rem                Version order: /V:x.y.z, env RELEASE_VERSION, csproj Version
rem                (falls back to the newest "Published\Version no. X" folder when
rem                the csproj major version is 10 or higher, i.e. BumpVersion drift).
rem ======================================================================

setlocal EnableExtensions EnableDelayedExpansion
title DELTA SYNTH - Thai OpenUtau Release Cycle v1.0
color 0B
chcp 65001 >nul
set "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1"

set "PROJECT_ROOT=%~dp0"
if "%PROJECT_ROOT:~-1%"=="\" set "PROJECT_ROOT=%PROJECT_ROOT:~0,-1%"
set "OPT_DRYRUN=0"
set "OPT_BUMP=0"
set "OPT_SKIPTESTS=0"
set "OPT_SKIPPACK=0"
set "OPT_NOPAUSE=0"
set "REL_VER=%RELEASE_VERSION%"
set "VER_SRC=env RELEASE_VERSION"
if not defined REL_VER set "VER_SRC="
set "R_BUILD=SKIPPED"
set "R_TEST=SKIPPED"
set "R_PUB=SKIPPED"
set "R_PACK=SKIPPED"
set "R_VERIFY=SKIPPED"

:parse_args
if "%~1"=="" goto args_done
set "ARG=%~1"
if /i "%ARG%"=="/DRYRUN" set "OPT_DRYRUN=1" & goto next_arg
if /i "%ARG%"=="/BUMP" set "OPT_BUMP=1" & goto next_arg
if /i "%ARG%"=="/SKIPTESTS" set "OPT_SKIPTESTS=1" & goto next_arg
if /i "%ARG%"=="/SKIPPACK" set "OPT_SKIPPACK=1" & goto next_arg
if /i "%ARG%"=="/NOPAUSE" set "OPT_NOPAUSE=1" & goto next_arg
if /i "%ARG:~0,3%"=="/V:" set "REL_VER=%ARG:~3%" & set "VER_SRC=argument /V:" & goto next_arg
if exist "%ARG%\" set "PROJECT_ROOT=%ARG%" & goto next_arg
echo [Release_Cycle] Unknown argument ignored: %ARG%
:next_arg
shift
goto parse_args

:args_done
if "%PROJECT_ROOT:~-1%"=="\" set "PROJECT_ROOT=%PROJECT_ROOT:~0,-1%"
cd /d "%PROJECT_ROOT%" 2>nul
if errorlevel 1 (
    set "FAIL_MSG=Cannot open project folder %PROJECT_ROOT%. Suggested action: drop the Thai_OpenUtau folder onto this file."
    goto fail
)

echo ======================================================================
echo       DELTA SYNTH - Thai OpenUtau Release Cycle v1.0
echo       Preflight - Version - Build - Test - Publish - Package - Verify
echo ======================================================================
echo.

rem ---------------------------------------------------------------- Phase 0
echo [0/7] Preflight checks...
if not exist "Thai OpenUtau.sln" (
    set "FAIL_MSG=Thai OpenUtau.sln not found in %CD%. Suggested action: run from the project root or drag the project folder onto this file."
    goto fail
)
if not exist "OpenUtau\OpenUtau.csproj" (
    set "FAIL_MSG=OpenUtau\OpenUtau.csproj not found. Suggested action: verify the project folder structure."
    goto fail
)
if not exist "Compile_Full_Program.bat" (
    set "FAIL_MSG=Compile_Full_Program.bat is missing. Suggested action: restore it from git history; do not rename root build scripts."
    goto fail
)
if not exist "Package_Program.bat" (
    set "FAIL_MSG=Package_Program.bat is missing. Suggested action: restore it from git history; do not rename root build scripts."
    goto fail
)
where dotnet >nul 2>&1
if errorlevel 1 (
    set "FAIL_MSG=dotnet CLI not found in PATH. Suggested action: install the .NET 8.0 SDK from https://dotnet.microsoft.com/download/dotnet/8.0"
    goto fail
)
for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set "DOTNET_VER=%%v"
echo  - Project Root : %CD%
echo  - .NET SDK     : %DOTNET_VER%

set "SEVENZIP="
if exist "C:\Program Files\7-Zip\7z.exe" set "SEVENZIP=C:\Program Files\7-Zip\7z.exe"
if not defined SEVENZIP for /f "tokens=*" %%p in ('where 7z.exe 2^>nul') do if not defined SEVENZIP set "SEVENZIP=%%p"
if defined SEVENZIP (echo  - 7-Zip        : %SEVENZIP%) else echo  - 7-Zip        : NOT FOUND - archive test will be skipped
set "RAREXE="
if exist "C:\Program Files\WinRAR\Rar.exe" set "RAREXE=C:\Program Files\WinRAR\Rar.exe"
if not defined RAREXE for /f "tokens=*" %%p in ('where rar.exe 2^>nul') do if not defined RAREXE set "RAREXE=%%p"
if defined RAREXE (echo  - WinRAR       : %RAREXE%) else echo  - WinRAR       : NOT FOUND - SFX installer will be skipped

set "GIT_CHANGES=n/a"
where git >nul 2>&1
if not errorlevel 1 for /f %%c in ('git status --porcelain 2^>nul ^| find /c /v ""') do set "GIT_CHANGES=%%c"
echo  - Git changes  : %GIT_CHANGES% file(s) in working tree
echo.

rem ---------------------------------------------------------------- Phase 1
echo [1/7] Resolving release version...
if not defined REL_VER (
    for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "([xml](Get-Content -LiteralPath 'OpenUtau\OpenUtau.csproj' -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1"`) do set "REL_VER=%%v"
    set "VER_SRC=OpenUtau.csproj"
    for /f "tokens=1 delims=." %%m in ("!REL_VER!") do set "REL_MAJOR=%%m"
    if not defined REL_VER (
        set "FAIL_MSG=Could not read Version from OpenUtau\OpenUtau.csproj. Suggested action: pass /V:x.y.z explicitly."
        goto fail
    )
    if !REL_MAJOR! GEQ 10 (
        echo [WARNING] csproj Version !REL_VER! looks like BumpVersion drift. Falling back to the newest Published folder version.
        set "REL_VER="
        for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "Get-ChildItem -LiteralPath 'Published' -Directory -Filter 'Version no. *' -ErrorAction SilentlyContinue | ForEach-Object { $v = $null; if ([version]::TryParse(($_.Name -replace '^Version no\. ',''), [ref]$v)) { $v } } | Sort-Object -Descending | Select-Object -First 1"`) do set "REL_VER=%%v"
        set "VER_SRC=newest Published folder"
    )
)
if not defined REL_VER (
    set "FAIL_MSG=No release version could be resolved. Suggested action: run again with /V:x.y.z, for example /V:3.3.0"
    goto fail
)
echo !REL_VER!| findstr /r /b /e "[0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*" >nul
if errorlevel 1 (
    set "FAIL_MSG=Release version !REL_VER! is not in x.y.z format. Suggested action: use /V:3.3.0 style values."
    goto fail
)
set "PKG_VER=!REL_VER!"
echo  - Release version : !REL_VER!  [source: !VER_SRC!]
echo  - Bump version    : %OPT_BUMP%   [0 = keep version stable, 1 = run BumpVersion.ps1]
echo.

if "%OPT_DRYRUN%"=="1" (
    echo [DRYRUN] Preflight and version are valid. The following would run:
    echo   [2/7] dotnet build "Thai OpenUtau.sln" -c Release --no-incremental   ^(gate: 0 warnings, 0 errors^)
    echo   [3/7] dotnet test OpenUtau.Test\OpenUtau.Test.csproj -c Release --no-build   ^(skip=%OPT_SKIPTESTS%^)
    echo   [4/7] Compile_Full_Program.bat  with SKIP_BUMP and NO_PROMPT
    echo   [5/7] Package_Program.bat  with PKG_VER=!REL_VER!   ^(skip=%OPT_SKIPPACK%^)
    echo   [6/7] 7-Zip integrity test + SHA256SUMS.txt
    echo   [7/7] Append result row to Published\Release_Logs\Release_History.csv
    set "FAIL_MSG="
    goto finish_ok
)

for /f %%t in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmmss"') do set "STAMP=%%t"
set "LOG_DIR=%PROJECT_ROOT%\Published\Release_Logs"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%" >nul 2>&1
set "LOG_BUILD=%LOG_DIR%\%STAMP%_1_build.log"
set "LOG_TEST=%LOG_DIR%\%STAMP%_2_test.log"
set "LOG_PUB=%LOG_DIR%\%STAMP%_3_publish.log"
set "LOG_PACK=%LOG_DIR%\%STAMP%_4_package.log"
set "LOG_VERIFY=%LOG_DIR%\%STAMP%_5_verify.log"

rem ---------------------------------------------------------------- Phase 2
echo [2/7] Building solution in Release mode - gate: 0 warnings, 0 errors...
dotnet build "Thai OpenUtau.sln" -c Release --no-incremental > "%LOG_BUILD%" 2>&1
set "RC=!errorlevel!"
findstr /c:"Warning(s)" /c:"Error(s)" "%LOG_BUILD%"
if not "!RC!"=="0" (
    set "R_BUILD=FAILED"
    set "FAIL_MSG=dotnet build exited with code !RC!. Suggested action: open %LOG_BUILD% and fix the first error."
    goto fail
)
findstr /r /c:"^ *0 Warning(s)" "%LOG_BUILD%" >nul
if errorlevel 1 (
    set "R_BUILD=WARNINGS"
    set "FAIL_MSG=Build produced warnings. Suggested action: fix the root cause listed in %LOG_BUILD% - do not suppress them."
    goto fail
)
findstr /r /c:"^ *0 Error(s)" "%LOG_BUILD%" >nul
if errorlevel 1 (
    set "R_BUILD=FAILED"
    set "FAIL_MSG=Build reported errors. Suggested action: open %LOG_BUILD%."
    goto fail
)
set "R_BUILD=OK"
echo [OK] Build clean: 0 warnings, 0 errors.
echo.

rem ---------------------------------------------------------------- Phase 3
if "%OPT_SKIPTESTS%"=="1" (
    echo [3/7] Unit tests skipped by /SKIPTESTS.
    set "R_TEST=SKIPPED"
    echo.
    goto phase_publish
)
echo [3/7] Running unit tests - this can take several minutes...
dotnet test "OpenUtau.Test\OpenUtau.Test.csproj" -c Release --no-build > "%LOG_TEST%" 2>&1
set "RC=!errorlevel!"
findstr /c:"Passed!" /c:"Failed!" /c:"Total tests" "%LOG_TEST%"
if not "!RC!"=="0" (
    set "R_TEST=FAILED"
    set "FAIL_MSG=Unit tests failed with code !RC!. Suggested action: open %LOG_TEST% and fix the failing test at its root cause."
    goto fail
)
set "R_TEST=OK"
echo [OK] All unit tests passed.
echo.

rem ---------------------------------------------------------------- Phase 4
:phase_publish
echo [4/7] Publishing standalone program via Compile_Full_Program.bat...
set "SKIP_BUMP=1"
if "%OPT_BUMP%"=="1" set "SKIP_BUMP=0"
set "NO_PROMPT=1"
call "%PROJECT_ROOT%\Compile_Full_Program.bat" "%PROJECT_ROOT%" > "%LOG_PUB%" 2>&1 < nul
set "RC=!errorlevel!"
if not "!RC!"=="0" (
    set "R_PUB=FAILED"
    set "FAIL_MSG=Compile_Full_Program.bat exited with code !RC!. Suggested action: open %LOG_PUB%."
    goto fail
)
if not exist "%PROJECT_ROOT%\Publish\OpenUtau.exe" (
    set "R_PUB=FAILED"
    set "FAIL_MSG=Publish\OpenUtau.exe was not produced. Suggested action: open %LOG_PUB%."
    goto fail
)
set "R_PUB=OK"
echo [OK] Publish folder ready: %PROJECT_ROOT%\Publish
echo.

rem ---------------------------------------------------------------- Phase 5
if "%OPT_SKIPPACK%"=="1" (
    echo [5/7] Packaging skipped by /SKIPPACK.
    set "R_PACK=SKIPPED"
    echo.
    goto phase_record
)
echo [5/7] Packaging ZIP and SFX installer via Package_Program.bat - version !PKG_VER!...
call "%PROJECT_ROOT%\Package_Program.bat" "%PROJECT_ROOT%\Publish" > "%LOG_PACK%" 2>&1 < nul
set "RC=!errorlevel!"
if not "!RC!"=="0" (
    set "R_PACK=FAILED"
    set "FAIL_MSG=Package_Program.bat exited with code !RC!. Suggested action: open %LOG_PACK%."
    goto fail
)
set "EXPORT_DIR=%PROJECT_ROOT%\Published\Version no. !PKG_VER!"
set "PKG_ZIP=!EXPORT_DIR!\Thai_OpenUtau_v!PKG_VER!_Standalone.zip"
set "PKG_EXE=!EXPORT_DIR!\Thai_OpenUtau_Installer_v!PKG_VER!.exe"
if not exist "!PKG_ZIP!" (
    set "R_PACK=FAILED"
    set "FAIL_MSG=Expected archive not found: !PKG_ZIP!. Suggested action: open %LOG_PACK%."
    goto fail
)
set "R_PACK=OK"
echo [OK] Packages created in !EXPORT_DIR!
echo.

rem ---------------------------------------------------------------- Phase 6
echo [6/7] Verifying package integrity...
> "%LOG_VERIFY%" echo Verification started %STAMP% for version !PKG_VER!
set "VERIFY_OK=1"
if not exist "%PROJECT_ROOT%\Publish\DirectML.dll" (
    echo [WARNING] Publish\DirectML.dll is missing - DiffSinger GPU path will fall back to CPU.
    >> "%LOG_VERIFY%" echo WARN DirectML.dll missing
)
if not exist "%PROJECT_ROOT%\Publish\Dictionaries" (
    echo [WARNING] Publish\Dictionaries is missing - Thai phonemizer dictionaries are not bundled.
    >> "%LOG_VERIFY%" echo WARN Dictionaries missing
)
if defined SEVENZIP (
    "%SEVENZIP%" t "!PKG_ZIP!" >> "%LOG_VERIFY%" 2>&1
    findstr /c:"Everything is Ok" "%LOG_VERIFY%" >nul
    if errorlevel 1 (
        set "VERIFY_OK=0"
        echo [ERROR] 7-Zip integrity test failed for !PKG_ZIP!
    ) else (
        echo [OK] 7-Zip integrity test: Everything is Ok
    )
) else (
    echo [NOTICE] 7-Zip not found - archive integrity test skipped.
)
powershell -NoProfile -Command "Get-ChildItem -LiteralPath '!EXPORT_DIR!' -File | Where-Object { $_.Extension -in '.zip','.exe' } | ForEach-Object { '{0}  {1}' -f (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash, $_.Name } | Set-Content -LiteralPath '!EXPORT_DIR!\SHA256SUMS.txt' -Encoding ASCII"
if exist "!EXPORT_DIR!\SHA256SUMS.txt" echo [OK] SHA256SUMS.txt written.
if "!VERIFY_OK!"=="0" (
    set "R_VERIFY=FAILED"
    set "FAIL_MSG=Package verification failed. Suggested action: open %LOG_VERIFY% and rebuild the package."
    goto fail
)
set "R_VERIFY=OK"
echo.

rem ---------------------------------------------------------------- Phase 7
:phase_record
echo [7/7] Recording result...
set "CSV=%LOG_DIR%\Release_History.csv"
if not exist "%CSV%" > "%CSV%" echo stamp,version,git_changes,build,tests,publish,package,verify
>> "%CSV%" echo %STAMP%,!PKG_VER!,%GIT_CHANGES%,%R_BUILD%,%R_TEST%,%R_PUB%,%R_PACK%,%R_VERIFY%
echo [OK] Row appended to %CSV%
set "FAIL_MSG="
goto finish_ok

:fail
color 0C
echo.
echo ======================================================================
echo [Release_Cycle] FAILED
echo %FAIL_MSG%
echo ======================================================================
echo  Build=%R_BUILD%  Tests=%R_TEST%  Publish=%R_PUB%  Package=%R_PACK%  Verify=%R_VERIFY%
if not "%OPT_NOPAUSE%"=="1" pause
endlocal & exit /b 1

:finish_ok
color 0A
echo.
echo ======================================================================
echo  [SUCCESS] Release cycle completed.
echo ======================================================================
echo  Version : !REL_VER!
echo  Build=%R_BUILD%  Tests=%R_TEST%  Publish=%R_PUB%  Package=%R_PACK%  Verify=%R_VERIFY%
echo.
echo  Next: update About_Project.md Work History and add a Docs\History entry
echo        ^(see "Optimize for this plan.md", section 9^).
echo ======================================================================
if not "%OPT_NOPAUSE%"=="1" pause
endlocal & exit /b 0
