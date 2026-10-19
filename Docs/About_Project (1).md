# About Project — Thai OpenUtau Master Tool by DELTA SYNTH

## 1. Origin & Significance (ที่มาและความสำคัญของโปรเจกต์)
- โปรเจกต์นี้ถือกำเนิดขึ้นจากการรวบรวมแกนกลางการทำงานของ 4 สาขาหลัก (Original OpenUtau, ตัวเดิม, ตัวสำรอง Type5 G2P, และ Moresampler Suite) เพื่อยุบรวมให้กลายเป็น **Master Tool** เพียงหนึ่งเดียว 
- ความสำคัญอยู่ที่การทลายข้อจำกัดด้านความแข็งทื่อของเสียงร้อง (Robotic Vocals) ด้วยการวิเคราะห์และแก้ไขสมการคลื่นเสียงแบบเจาะลึก เพื่อยกระดับความสามารถในการสังเคราะห์เสียงภาษาไทยให้กลมกลืนและเป็นธรรมชาติสูงสุดระดับมนุษย์

## 2. Working Objectives (วัตถุประสงค์ของการทำงาน)
- เพื่อสร้างซอฟต์แวร์ UTAU Editor ขั้นสูงที่ทำงานได้เสถียร (Zero-Defect) ในทุกระบบปฏิบัติการ
- เพื่อรวมศูนย์ฟีเจอร์ G2P (Grapheme-to-Phoneme) สำหรับภาษาไทยเข้ากับ Audio Engine ที่อัปเกรดแล้ว
- ทำให้ระบบเลือกใช้ค่า Moresampler และ Wavetool-Simple เป็นฐานมาตรฐานหลักเพื่อรีดประสิทธิภาพความลื่นไหลของพยัญชนะและสระ

## 3. Required Tooling (เครื่องมือที่จำเป็นต้องใช้ทั้งหมด)
- **Runtime & SDK:** .NET 8.0 / .NET 10.0 SDK สำหรับ C# Avalonia UI
- **Dependencies (Python):** Python 3.14, `pytest`, `pyreadline3` สำหรับทดสอบ G2P อัตโนมัติ
- **Audio Engines:** Moresampler, Worldline, Classic Wavtool
- **CLI Tools:** PowerShell, CMD สำหรับ Batch Scripting และ Automation
- **AI Integration:** DELTA SYNTH Architecture & Gemini AI สำหรับการเขียนและ Audit โค้ด

## 4. File Mapping (แผนผังตำแหน่งไฟล์)
- `OpenUtau/` : ระบบกราฟิกและ UI (Avalonia)
- `OpenUtau.Core/` : แกนกลางหลักของระบบเสียง, ตัวจัดการการข้ามเสียง (ResamplerItem.cs), ตัวระบุเครื่องมือ (ToolsManager.cs)
- `OpenUtau.Test/` : Unit Test สำหรับภาษา C#
- `tests/` : โฟลเดอร์เก็บสคริปต์ทดสอบภาษา Python (PyTest) สำหรับระบบ G2P
- `Tools/` : สคริปต์เพิ่มประสิทธิภาพ Voicebank (Moresampler & DiffSinger Optimizer)
- `History/` : บันทึกประวัติการพัฒนาและ Audit Report ในแต่ละเวอร์ชัน
- `Export File/` : พื้นที่จัดเก็บไฟล์แพ็คเกจสำหรับแจกจ่ายข้ามแพลตฟอร์ม

## 5. Primary Goals (เป้าหมายหลักในการทำงาน)
- **Milestone 1:** การรวมโค้ด 4 ระบบเข้าด้วยกันสำเร็จโดยไม่มีข้อผิดพลาดระหว่าง Compile (เสร็จสิ้น)
- **Milestone 2:** การปรับแก้สมการ S-Curve (Cosine Smoothstep) สำหรับรอยต่อพยัญชนะและสระ (เสร็จสิ้น)
- **Milestone 3:** การสร้างระบบสร้างแพ็คเกจ (Automated Packaging Script) สำหรับ Windows, Linux, macOS (กำลังดำเนินการ)
- **Deliverables:** ไฟล์ Executable ของทุกแพลตฟอร์มที่จัดเรียงเรียบร้อยในโฟลเดอร์ `Export File`

## 6. Working Vision (วิสัยทัศน์ในการทำงาน)
- มุ่งสู่การเป็นศูนย์กลาง (Hub) สำหรับนักทำเพลงและนักปรับแต่งเสียงโวคอลลอยด์ชาวไทย ที่ไม่ต้องเสียเวลาพึ่งพาเครื่องมือภายนอก ทุกอย่างถูกรวมไว้จบในที่เดียว ทำงานง่าย ไว และได้คุณภาพเสียงระดับท็อป

## 7. Working Identity (อัตลักษณ์ของการทำงาน)
- ยึดหลักการทำงาน **Preserve Before Replace** (ไม่รื้อของเดิมที่ทำได้ดี แต่ต่อยอดให้ดีกว่า)
- ยืนหยัดในผลลัพธ์แบบ **Zero-Defect** (ไม่มี Error เมื่อส่งมอบงาน)
- ปฏิบัติตาม **UI Standard:** ธีม Cyber-Thai สีขาว/ดำ/แดง พร้อมใช้ฟอนต์ Leelawadee UI ตามข้อบังคับ

## 8. Project Uniqueness (เอกลักษณ์ของโปรเจกต์)
- เป็นโปรเจกต์ OpenUtau เดียวที่มีการเขียนอัลกอริทึม **Humanized Smooth Envelope** แฝงลงลึกไปถึงระดับ Native Code 
- ผสานการทดสอบระบบแบบ Dual-Language (C# xUnit และ Python PyTest) เพื่อการันตีความถูกต้องของภาษาไทย

## 9. Outcome Hypotheses (สมมติฐานของผลลัพธ์)
- ผู้ใช้งานจะได้รับความพึงพอใจในคุณภาพเสียงร้องที่กระตุกหรือสะดุดน้อยลงมากกว่า 80%
- การทดสอบและแพ็คเกจที่ครอบคลุมหลาย OS จะช่วยเพิ่มจำนวนผู้ใช้หน้าใหม่บน macOS และ Linux ได้อย่างน้อย 2 เท่าตัว

## 10. Full Plan Details (รายละเอียดแผนงานทั้งหมด)
1. **Analyze & Merge:** ดึงทรัพยากรเก่าทั้งหมดเข้าสู่โครงสร้างหลัก
2. **Engine Modification:** ปรับปรุง `ResamplerItem.cs` และ `ToolsManager.cs`
3. **Audit & Validation:** ยิงคำสั่ง `pytest` และ `dotnet build`
4. **Project Packaging:** เขียน Batch Script พิมพ์คำสั่ง `.NET Publish` แบบ Multi-Target และทำโครงสร้าง `Export File\No.X`
5. **Final Review:** ทดสอบสคริปต์ให้มั่นใจว่าไฟล์ปลายทางสามารถทำงานได้อย่างแท้จริง

## 11. Correct Improvement Methodology (วิธีการปรับปรุงโปรเจกต์ที่ถูกต้อง)
- หากต้องอัปเกรดระบบในอนาคต จะต้องใช้วิธี **Surgical Edits** เท่านั้น (หาบรรทัดเป้าหมายและแก้เฉพาะจุด)
- หลีกเลี่ยงการสร้าง Abstraction ที่ซับซ้อนเกินจำเป็น
- บังคับใช้ Type Checking ก่อนนำเข้าเสมอ เพื่อป้องกัน Regression

## 12. Work History Log (บันทึกผลการทำงาน)
- **Version 1.0 (2026-08):** Codebase Unification & Master Tool Initiation (By DELTA SYNTH)
- **Version 2.0 (2026-08):** Audio Core Enhancement (Cosine Envelope Smoothing & Priority Resamplers Setup)
- **Version 2.1 (2026-08):** Automated Packaging & Deployment Scripts Integrated
