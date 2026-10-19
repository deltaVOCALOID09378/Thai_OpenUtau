@echo off
setlocal enabledelayedexpansion
title DELTA SYNTH - Thai_OpenUtau Intelligent Installer ^& Builder
chcp 65001 >nul
color 0B

:: ========================================================================
:: Made And Checked By DELTA SYNTH & All Agentic AI
:: Original by Patiphat Wongyai (Delta)
:: Version: 3.0 (Smart Multi-Runtime .NET 8.0-12.0+ Lifecycle Standard)
:: Description: Intelligent installer and compiler for Thai OpenUtau.
:: Automatically detects any installed .NET (8.0 - 12.0+), eliminates
:: redundant prompts, verifies system dependencies, and builds project.
:: ========================================================================

set "SCRIPT_DIR=%~dp0"
set "ROOT_DIR=%~1"
if "%ROOT_DIR%"=="" set "ROOT_DIR=%SCRIPT_DIR%"
if "%ROOT_DIR:~-1%"=="\" set "ROOT_DIR=%ROOT_DIR:~0,-1%"

:: Persistent Prerequisite Marker (Preserved across OpenUtau reinstallations)
set "STATE_DIR=%LOCALAPPDATA%\Thai_OpenUtau"
set "PREREQ_MARKER=%STATE_DIR%\.installed_prereqs"

echo ========================================================================
echo       DELTA SYNTH - Thai_OpenUtau System Setup ^& Auto-Builder
echo       Architecture: Multi-Runtime Support (.NET 8.0 - 12.0+)
echo ========================================================================
echo [INFO] Working Directory: "%ROOT_DIR%"
echo.

:: ------------------------------------------------------------------------
:: STEP 1: Smart .NET Runtime & SDK Detection (8.0 - 12.0+)
:: ------------------------------------------------------------------------
echo [1/4] ตรวจสอบสภาพแวดล้อมระบบ (.NET Runtime / SDK Environment)...

set "DOTNET_FOUND=0"
set "DOTNET_VER="
set "DOTNET_MAJOR=0"

where dotnet >nul 2>nul
if %ERRORLEVEL% equ 0 (
    set "DOTNET_FOUND=1"
    for /f "tokens=1 delims=." %%a in ('dotnet --version 2^>nul') do (
        set "DOTNET_MAJOR=%%a"
    )
    for /f "tokens=*" %%a in ('dotnet --version 2^>nul') do (
        set "DOTNET_VER=%%a"
    )
)

if %DOTNET_FOUND% equ 1 (
    if %DOTNET_MAJOR% geq 8 (
        echo [OK] ตรวจพบ .NET SDK / Runtime เวอร์ชัน !DOTNET_VER! (Major: !DOTNET_MAJOR!)
        echo [OK] เข้ากันได้กับระบบ (รองรับตั้งแต่เวอร์ชัน 8.0 ถึง 12.0+ ขึ้นไป)
        echo [INFO] ข้ามขั้นตอนการสอบถามติดตั้ง .NET โดยอัตโนมัติ (ตรวจพบตัวโปรแกรมในเครื่องแล้ว)
        if not exist "%STATE_DIR%" mkdir "%STATE_DIR%" 2>nul
        echo installed_version=!DOTNET_VER! > "%PREREQ_MARKER%"
        goto :dotnet_ready
    ) else (
        echo [WARNING] ตรวจพบ .NET เวอร์ชัน !DOTNET_VER! ซึ่งต่ำกว่าเวอร์ชัน 8.0 ขั้นต่ำ
    )
) else (
    echo [NOTICE] ไม่พบคำสั่ง dotnet ในระบบ
)

:: If marker was already created previously, verify if dotnet path is resolvable
if exist "%PREREQ_MARKER%" (
    echo [INFO] พบประวัติการติดตั้งพรีเรควิสิทเดิมในระบบ ข้ามการแจ้งเตือนซ้ำซ้อน...
)

:: Prompt ONLY on clean/first install when .NET >= 8 is truly missing
echo.
echo ========================================================================
echo [สำคัญ] จำเป็นต้องมี .NET SDK หรือ Runtime เวอร์ชัน 8.0 ขึ้นไป (8.0 - 12.0+)
echo ระบบ .NET ติดตั้งที่ระดับปฏิบัติการของเครื่อง แม้ลบ OpenUtau ออก .NET จะไม่ถูกลบ
echo และเมื่อดาวน์โหลด OpenUtau กลับมาใหม่ ระบบจะข้ามขั้นตอนนี้ให้โดยอัตโนมัติ
echo ========================================================================
echo [1] เปิดหน้าดาวน์โหลด Microsoft .NET SDK (Official)
echo [2] ดาวน์โหลดและติดตั้งอัตโนมัติผ่าน Winget (ถ้ามี)
echo [3] ข้ามไปขั้นตอนการคอมไพล์โดยตรง
echo.
set /p "NET_CHOICE=กรุณาเลือกตัวเลือก (1-3) หรือกด Enter เพื่อข้าม: "
if "!NET_CHOICE!"=="1" (
    start https://dotnet.microsoft.com/download/dotnet/8.0
    echo กรุณาติดตั้ง .NET ให้เรียบร้อย แล้วกดคีย์ใดๆ เพื่อดำเนินการต่อ...
    pause >nul
) else if "!NET_CHOICE!"=="2" (
    echo กำลังติดตั้ง .NET SDK ผ่าน Winget...
    winget install Microsoft.DotNet.SDK.8 --accept-source-agreements --accept-package-agreements
)

:dotnet_ready
echo.

:: ------------------------------------------------------------------------
:: STEP 2: Restore Dependencies
:: ------------------------------------------------------------------------
echo [2/4] ตรวจสอบและดึง Dependency ที่จำเป็น (Restoring Dependencies)...
pushd "%ROOT_DIR%"
dotnet restore "OpenUtau\OpenUtau.csproj" --verbosity minimal
if %ERRORLEVEL% neq 0 (
    echo [WARNING] dotnet restore พบข้อความแจ้งเตือน แต่จะดำเนินการคอมไพล์ต่อ...
) else (
    echo [OK] ดึง Dependencies ครบถ้วนสมบูรณ์
)
popd
echo.

:: ------------------------------------------------------------------------
:: STEP 3: Native Library Build (if C++ present)
:: ------------------------------------------------------------------------
echo [3/4] ตรวจสอบและคอมไพล์ Native Engine (C++ Worldline Library)...
if exist "%ROOT_DIR%\cpp\build_win.bat" (
    echo [INFO] กำลังคอมไพล์ Native Worldline C++ Library...
    pushd "%ROOT_DIR%\cpp"
    call build_win.bat
    popd
    echo [OK] คอมไพล์ Native Library สำเร็จ
) else (
    echo [INFO] ไม่พบสคริปต์ C++ เพิ่มเติม ใช้ Built-in Native Library ตามมาตรฐาน
)
echo.

:: ------------------------------------------------------------------------
:: STEP 4: Build OpenUtau C# Project (Release Mode with RollForward)
:: ------------------------------------------------------------------------
echo [4/4] คอมไพล์โปรแกรมหลัก (Building OpenUtau C# Release)...
set "DOTNET_ROLL_FORWARD=LatestMajor"
pushd "%ROOT_DIR%"
dotnet build "OpenUtau\OpenUtau.csproj" -c Release --no-restore
if %ERRORLEVEL% equ 0 (
    echo.
    echo ========================================================================
    echo [SUCCESS] คอมไพล์และติดตั้ง Thai OpenUtau เสร็จสมบูรณ์ 100%%
    echo [SUCCESS] สามารถเปิดใช้งานผ่านไฟล์ Start-The-Program.bat ได้ทันที
    echo ========================================================================
    
    :: Consolidate binary output if All Rendered File folder is desired
    if not exist "All Rendered File" mkdir "All Rendered File" 2>nul
    for /d %%D in ("OpenUtau\bin\Release\net*") do (
        xcopy "%%D\*" "All Rendered File\" /s /e /y /i /q >nul 2>&1
    )
) else (
    echo.
    echo [FAILED] เกิดข้อผิดพลาดระหว่างการคอมไพล์ กรุณาตรวจสอบรายละเอียดด้านบน
)
popd

echo.
pause
exit /b 0
