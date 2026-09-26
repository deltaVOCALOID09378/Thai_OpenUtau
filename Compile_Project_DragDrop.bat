@echo off
:: ==============================================================================
:: File: Compile_Project_DragDrop.bat
:: Version: 1.1
:: Made and Checked By DELTA SYNTH And Gemini Claude and ChatGPT
:: Original By DELTA SYNTH (Delta)
:: ==============================================================================
:: หมายเหตุ (Notes):
:: - [การป้องกันอักขระพิเศษ]: เปิดใช้งาน DelayedExpansion ป้องกัน Path พัง
:: - [การตรวจสอบระบบ]: ตรวจสอบ .csproj/.sln และ dotnet SDK ก่อนสั่งรันเสมอ
:: - [ปรับปรุงประสิทธิภาพ]: ลดการพิมพ์ Error ซ้ำซ้อน และแสดงสถานะชัดเจนขึ้น
:: ==============================================================================

chcp 65001 >nul
setlocal enabledelayedexpansion
title DELTA SYNTH - OpenUtau Auto Compiler V1.1
color 0B

echo =======================================================
echo      DELTA SYNTH - OpenUtau Project Auto Compiler
echo =======================================================
echo.

:: ตรวจสอบการ Drag & Drop ไฟล์/โฟลเดอร์
set "TARGET_DIR=%~1"
if "!TARGET_DIR!"=="" (
    echo [ERROR] กรุณาลากโฟลเดอร์โปรเจกต์มาวางทับที่ไฟล์ .bat นี้ครับ
    pause
    exit /b 1
)

:: เข้าสู่โฟลเดอร์เป้าหมายอย่างปลอดภัย
cd /d "!TARGET_DIR!" 2>nul
if errorlevel 1 (
    echo [ERROR] ไม่สามารถเข้าถึงโฟลเดอร์ได้ กรุณาตรวจสอบ Path อีกครั้งครับ
    pause
    exit /b 1
)

echo [INFO] Working Directory: !CD!
echo [INFO] กำลังตรวจสอบไฟล์โปรเจกต์...

:: ค้นหาไฟล์โครงสร้างหลัก
if not exist "*.csproj" (
    if not exist "*.sln" (
        echo [ERROR] ไม่พบไฟล์ .csproj หรือ .sln ในโฟลเดอร์นี้ครับ
        pause
        exit /b 1
    )
)

:: ตรวจสอบ Dependency ของระบบ
where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] ไม่พบคำสั่ง "dotnet" ในระบบ กรุณาติดตั้ง .NET SDK ก่อนครับ
    pause
    exit /b 1
)

echo [INFO] เริ่มต้นกระบวนการคอมไพล์ในโหมด Release...
echo.

dotnet build --configuration Release

:: ประเมินผลลัพธ์
if !ERRORLEVEL! EQU 0 (
    echo.
    echo [SUCCESS] การคอมไพล์เสร็จสมบูรณ์! ระบบมีเสถียรภาพและพร้อมใช้งานครับ
) else (
    echo.
    echo [FAILED] พบข้อผิดพลาดในการคอมไพล์ กรุณาตรวจสอบ Log ด้านบนเพื่อแก้ไขครับ
)

echo.
echo =======================================================
pause
exit /b 0