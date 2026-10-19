# Project Context (Onboarding สำหรับ AI รุ่นต่อไป)

> **ข้อมูลสรุปโปรเจกต์:** นี่คือเอกสารสำหรับ Onboarding Agent ตัวใหม่ที่จะเข้ามาสานงานต่อ 

## 1. ความคืบหน้าปัจจุบัน (Progress)
- โครงสร้างไฟล์ในโฟลเดอร์ได้รับการจัดระเบียบใหม่ทั้งหมดเรียบร้อยแล้ว
- เอกสารอ้างอิงและ R&D ถูกรวมเป็น 2 ไฟล์หลัก:
  1. `Project_Overview_And_Reference.md` (การใช้งานและ Build)
  2. `R&D_Report.md` (ทฤษฎีและ Phonemizer)
- สคริปต์เครื่องมือ Agent ย้ายไปอยู่ที่ `All Editing File For Agent/`

## 2. โครงสร้างระบบไฟล์ (Structure)
- กรุณาอ้างอิงแผนผังแบบเต็มจากไฟล์ `All File Mapping.md`
- ⚠️ **คำเตือน (Systems):** ห้ามเคลื่อนย้าย เปลี่ยนชื่อ หรือลบไฟล์สคริปต์ Build ที่อยู่หน้า Root เด็ดขาด (ตรวจสอบรายชื่อไฟล์ที่ห้ามแตะได้ใน `The Source Code of System File's Name Mapping.md`)

## 3. ระบบหลักและ Dependency
- UI หลัก: Avalonia UI (C# / .NET 8)
- Phonemizer (การเรียงเสียงภาษาไทย): พัฒนาแบบ VCCV (มีไฟล์ Dictionary `dsdict-th.txt` เป็นตัวควบคุมการอ่าน)
- โค้ดที่ดูแล Phonemizer อยู่ใน `OpenUtau.Plugin.Builtin`

## 4. งานที่ค้างอยู่ (Pending Tasks)
- ณ ปัจจุบัน โครงสร้างไฟล์อยู่ในสถานะสมบูรณ์ (Zero-Defect ในด้าน File Management)
- พร้อมรับคำสั่งพัฒนาโค้ดหรือการจูนเสียงในเฟสต่อไปตามที่นายท่านเดลต้าต้องการ

**กฎสำคัญ (Prime Directive):** "อย่ารื้อระบบที่ทำงานได้ เพียงเพราะสามารถเขียนใหม่ได้" - ต้องทดสอบก่อนรื้อเสมอ!
