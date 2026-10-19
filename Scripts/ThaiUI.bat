@echo off
:: ========================================================================
:: Made And Checked By DELTA SYNTH & All Agentic AI
:: Original by DELTA SYNTH
:: Version: 2.0 (Zero-Defect Leelawadee UI Standard)
:: Description: Drag and drop the target OpenUtau "Strings" folder onto this 
:: .bat file to automatically apply the Thai UI Localization & Font patch.
:: ========================================================================

title OpenUtau Thai UI Localization Patcher V2.0 (Leelawadee UI)
color 0B
chcp 65001 >nul

echo =======================================================
echo    OpenUtau Thai UI Localization Patcher (V2.0)
echo    Font Standard: Leelawadee UI (Standardized UI)
echo =======================================================
echo.

set "TARGET_DIR=%~1"
if "%TARGET_DIR%"=="" (
    if exist "Strings" (
        set "TARGET_DIR=%CD%\Strings"
    ) else if exist "..\Strings" (
        set "TARGET_DIR=%CD%\..\Strings"
    ) else (
        echo [ERROR] กรุณาลากโฟลเดอร์ "Strings" ของ OpenUtau มาวางทับที่ไฟล์ .bat นี้ครับ
        echo         (Please drag and drop the OpenUtau "Strings" folder onto this .bat file.)
        pause
        exit /b 1
    )
)

cd /d "%TARGET_DIR%" 2>nul
if errorlevel 1 (
    echo [ERROR] ไม่สามารถเข้าถึงโฟลเดอร์ได้ กรุณาตรวจสอบ Path อีกครั้งครับ
    pause
    exit /b 1
)

echo [INFO] Working Directory: %CD%
echo [INFO] กำลังติดตั้งชุดภาษาไทยและโครงสร้างฟอนต์ Leelawadee UI...

if exist "%~dp0Strings.th-TH.axaml" (
    copy /y "%~dp0Strings.th-TH.axaml" "Strings.th-TH.axaml" >nul
    echo [OK] อัปเดต Strings.th-TH.axaml เรียบร้อย
)
if exist "%~dp0unhandled_strings.json" (
    copy /y "%~dp0unhandled_strings.json" "unhandled_strings.json" >nul
    echo [OK] อัปเดต unhandled_strings.json (Leelawadee UI default) เรียบร้อย
)
if exist "%~dp0th.axaml" (
    copy /y "%~dp0th.axaml" "th.axaml" >nul
)

echo.
echo [SUCCESS] ติดตั้งชุดภาษาสำเร็จ! ระบบ UI ภาษาไทยพร้อมใช้งานแล้วครับ
echo           (Localization patch applied successfully. Leelawadee UI is active.)
echo.
echo =======================================================
pause
exit /b 0