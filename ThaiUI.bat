@echo off
:: ========================================================================
:: Made And Checked By DELTA SYNTH & All Agentic AI
:: Original by DELTA SYNTH
:: Version: 1.1
:: Description: Drag and drop the target OpenUtau "Strings" folder onto this 
:: .bat file to automatically apply the Thai UI Localization patch.
:: ========================================================================

title OpenUtau Thai UI Localization Patcher V1.1
color 0B
chcp 65001 >nul

echo =======================================================
echo    OpenUtau Thai UI Localization Patcher
echo =======================================================
echo.

if "%~1" == "" (
    echo [ERROR] กรุณาลากโฟลเดอร์ "Strings" ของ OpenUtau มาวางทับที่ไฟล์ .bat นี้ครับ
    echo         (Please drag and drop the OpenUtau "Strings" folder onto this .bat file.)
    pause
    exit /b 1
)

cd /d "%~1" 2>nul
if errorlevel 1 (
    echo [ERROR] ไม่สามารถเข้าถึงโฟลเดอร์ได้ กรุณาตรวจสอบ Path อีกครั้งครับ
    echo         (Could not access the dropped folder. Please verify the path.)
    pause
    exit /b 1
)

echo [INFO] Working Directory: %CD%
echo [INFO] กำลังติดตั้งแพทช์ th.axaml... (Applying th.axaml patch...)

copy /y "%~dp0th.axaml" "th.axaml" >nul

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [SUCCESS] ติดตั้งชุดภาษาสำเร็จ! ระบบ UI ภาษาไทยพร้อมใช้งานแล้วครับ
    echo           (Localization patch applied successfully. The Thai UI is now ready.)
) else (
    echo.
    echo [FAILED] ไม่สามารถคัดลอกไฟล์ได้ กรุณาตรวจสอบสิทธิ์การใช้งาน (Permissions)
    echo          (Could not apply the patch. Please check file permissions.)
)

echo.
echo =======================================================
pause
exit /b 0