# 📘 Project Overview And Reference (Thai OpenUtau)

*Consolidated Reference Document based on Claude's Planning Standards*

## 1. Executive Summary
โปรเจกต์ **Thai OpenUtau by DELTA SYNTH** คือระบบสืบทอดโปรแกรม OpenUtau เพื่อสร้าง Synthesizer สำหรับอุตสาหกรรมดนตรีที่รองรับ **สองภาษา (Bilingual: Thai / English)** และปรับแต่งเอนจินเฉพาะตัว (Custom C++ Worldline DSP) รวมไปถึงระบบการแปลคำ (Phonemizers) 

เป้าหมายสูงสุด: **Zero-Known Defects (ปราศจากบัค)**, มีความเสถียรสูงสุด (Stable), และกินทรัพยากรต่ำ (Resource-Aware) พร้อมรองรับ RVC Model Integrations ได้ในอนาคต

## 2. Core Architecture
ระบบถูกแบบแบ่งตามการทำงาน:
- **UI & Presentation Layer:** Avalonia UI (ข้ามแพลตฟอร์ม) / `OpenUtau`
- **Business Logic & Audio Management:** C# Core Engine (`OpenUtau.Core`)
- **Digital Signal Processing (DSP):** Native C++ (`cpp/worldline`) 
- **Compilation & Deployment:** Automation scripts (`Auto Compiler.bat`, NSIS installer)

## 3. Development Guidelines & System Boundaries
ผู้พัฒนาและ AI Agents จะต้องยึดถือเอกสารต่อไปนี้อย่างเคร่งครัด:
- `AGENT.md`: กฎกติกาและข้อบังคับสูงสุด (Preserve -> Strengthen -> Optimize -> Verify)
- `The Source Code of System File's Name Mapping.md`: คำอธิบายไฟล์ระดับวิกฤตของระบบ
- `All File Mapping.md`: โครงสร้างไฟล์ทั้งหมด

*ข้อจำกัดสูงสุด:* 
- ห้ามลบหรือเคลื่อนย้ายสคริปต์สำหรับการบิลด์ (`Auto Compiler.bat`, `Compile_Project_DragDrop.bat`, `OpenUtau.nsi`)
- ทุกครั้งที่ปรับปรุงโค้ด จะต้องไม่กระทบการทำงานกับโปรเจกต์ไฟล์ `.ustx` ดั้งเดิม

## 4. Known Issues & Optimizations (Pending)
จากการตรวจสอบใน R&D:
- การใช้ Memory พุ่งสูงเมื่อโหลด Voicebank จำนวนมาก ต้องใช้ระบบ Asynchronous / Lazy Loading เข้ามาช่วยจัดการ
- ประเด็น `oto.ini` และ `character.yaml` ที่ไม่ได้มาตรฐาน (รับมือไว้แล้วใน VoicebankLoader)
- การปิดโปรแกรม (Shutdown) ต้องมั่นใจว่าบันทึก Configurations ทุกตัวเพื่อป้องกัน Preferences เสียหาย
