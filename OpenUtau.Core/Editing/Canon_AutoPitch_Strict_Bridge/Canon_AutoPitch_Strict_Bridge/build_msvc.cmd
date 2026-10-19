@echo off
setlocal
where cl >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Visual C++ compiler was not found.
  echo Run this file from Developer Command Prompt for Visual Studio.
  exit /b 1
)
cl /nologo /std:c++17 /O2 /EHsc /DUNICODE /D_UNICODE CanonAutoPitchBridge.cpp shell32.lib user32.lib /link /SUBSYSTEM:WINDOWS /OUT:CanonAutoPitchBridge.exe
if errorlevel 1 exit /b %errorlevel%
echo Built CanonAutoPitchBridge.exe successfully.
