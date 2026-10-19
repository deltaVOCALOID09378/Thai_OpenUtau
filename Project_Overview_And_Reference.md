# 📘 Project Overview And Reference (Thai OpenUtau)
*Consolidated Reference Document based on DELTA SYNTH Architectural Standards*

## 1. Executive Summary
โปรเจกต์ **Thai OpenUtau by DELTA SYNTH** คือระบบสืบทอดโปรแกรม OpenUtau เพื่อสร้าง Synthesizer สำหรับอุตสาหกรรมดนตรีที่รองรับ **สองภาษา (Bilingual: Thai / English)** และปรับแต่งเอนจินเฉพาะตัว (Custom C++ Worldline DSP) รวมไปถึงระบบการแปลคำ (Phonemizers) 

เป้าหมายสูงสุด: **Zero-Known Defects (ปราศจากบัค: 0 Errors, 0 Warnings, 100% Passed Tests)**, มีความเสถียรสูงสุด (Stable), และกินทรัพยากรต่ำ (Resource-Aware) พร้อมรองรับ RVC Model Integrations ได้ในอนาคต

## 2. Core Architecture
ระบบถูกแบบแบ่งตามการทำงาน:
- **UI & Presentation Layer:** Avalonia UI (ข้ามแพลตฟอร์ม) / `OpenUtau`
- **Business Logic & Audio Management:** C# Core Engine (`OpenUtau.Core`)
- **Digital Signal Processing (DSP):** Native C++ (`cpp/worldline`) 
- **Compilation & Deployment:** Automation scripts (`Auto Compiler.bat`, NSIS installer)

## 3. Development Guidelines & System Boundaries
ผู้พัฒนาและ AI Agents จะต้องยึดถือเอกสารต่อไปนี้อย่างเคร่งครัด:
- `AGENT.md`: กฎกติกาและข้อบังคับสูงสุด (Preserve -> Strengthen -> Optimize -> Verify)
- `About_Project.md` / `About-Project.md`: แผนแม่บทสถาปัตยกรรมและการส่งต่องาน
- `The Source Code of System File's Name Mapping.md`: คำอธิบายไฟล์ระดับวิกฤตของระบบ
- `All File Mapping.md`: โครงสร้างไฟล์ทั้งหมด

*ข้อจำกัดสูงสุด:* 
- ห้ามลบหรือเคลื่อนย้ายสคริปต์สำหรับการบิลด์ (`Auto Compiler.bat`, `Compile_Project_DragDrop.bat`, `OpenUtau.nsi`)
- ทุกครั้งที่ปรับปรุงโค้ด จะต้องไม่กระทบการทำงานกับโปรเจกต์ไฟล์ `.ustx` ดั้งเดิม

## 4. Known Issues & Optimizations (Resolved in v3.1)
จากการตรวจสอบและปรับสมดุลระบบ:
- **Snappy Startup (เปิดโปรแกรมได้ไว):** แทนที่การสแกนโปรเซสด้วย Kernel-level Named Mutex ตรวจสอบ Single Instance ภายในเสี้ยววินาที พร้อมรันการค้นหา Audio Output แบบ Parallel ใน `SplashWindow` ช่วยลดเวลา Cold-start ได้อย่างมาก
- **Graceful Shutdown (ปิดโปรแกรมได้ไวและบันทึกงานปลอดภัย):** ปิดหน้าต่างบริวารทั้งหมด (`pianoRollWindow`), หยุดการทำงานของ `timer` / `autosaveTimer`, ยุติการสังเคราะห์เสียงของ `PlaybackManager` คืน Audio Buffer ทันที พร้อมบันทึก `Preferences` อย่างครบถ้วน และสั่งจบโปรเซสอย่างสมบูรณ์แบบ ไร้ปัญหา Ghost Process ตกค้าง
- **Memory & Voicebank Loader:** จัดการคัดกรองข้ามแถว `.wav=` ที่เป็น Placeholder ใน `oto.ini` อย่างมีวิจารณญาณ ไม่สร้าง Log Spam
- **Zero-Defect Build:** การันตี 0 Errors, 0 Warnings, และผ่าน Unit Tests ทั้งหมด 282/282 เทสต์
