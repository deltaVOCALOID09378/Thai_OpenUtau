# ประวัติการพัฒนาและอัปเดต - Thai OpenUtau By DELTA SYNTH

## 2026-07-31
- สร้างสคริปต์ `Build_All_Installers.bat` สำหรับการคอมไพล์โปรเจกต์ .NET และสร้างตัวติดตั้ง 3 รูปแบบ (ZIP, MSI, และ WinRAR SFX EXE)
- นำเข้าและใช้งานไฟล์ทรัพยากรสำหรับการบิลด์จากต้นแบบ `A:\Thai OpenUtau by DELTA SYNTH`:
  - `Package.wxs` สำหรับ WiX Installer
  - `PrepareDirectML.ps1` สำหรับจัดการ `DirectML.dll` dependency
  - `BumpVersion.ps1` สำหรับจัดการรุ่นของโปรเจกต์
  - `version.txt`
- ยอมรับ Open Source Maintenance Fee (OSMF) EULA สำหรับ WiX Toolset v7 เพื่อแก้ปัญหาการบิลด์ MSI ไม่สำเร็จ
- ทดสอบระบบคอมไพล์ทั้งหมด สามารถสร้างไฟล์สำหรับ Export (ZIP, MSI, EXE) ได้สำเร็จ

*Made And Checked By DELTA SYNTH & Gemini AI*
