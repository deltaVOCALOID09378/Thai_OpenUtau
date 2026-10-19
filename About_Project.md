<!-- Made And Checked By DELTA SYNTH & All Code Agentic AI Engine -->
<!-- Original by OpenUtau Authors -->

# About Project — Thai OpenUtau Master Tool by DELTA SYNTH
*Consolidated Master Architecture, Development Roadmap & Onboarding Specification*

---

## 1. Origin & Significance (ที่มาและความสำคัญของโปรเจกต์)
- โปรเจกต์ **Thai OpenUtau by DELTA SYNTH** ถือกำเนิดขึ้นจากการรวบรวมและหลอมรวมแกนกลางการทำงานของ 4 สาขาหลัก ได้แก่:
  1. **Original OpenUtau (stakira):** แกนระบบ Editor สมัยใหม่บนสถาปัตยกรรม Avalonia UI
  2. **Thai OpenUtau Legacy:** ฐานข้อมูลการรองรับภาษาไทยดั้งเดิม
  3. **Type5 G2P Branch:** ระบบแปลงตัวอักษรเป็นเสียงอ่านภาษาไทย (Grapheme-to-Phoneme) ขั้นสูง
  4. **Moresampler & Wavtool Suite:** เครื่องมือสังเคราะห์เสียงแบบรวดเร็วและเป็นธรรมชาติ
- **ความสำคัญของระบบ:** ทลายข้อจำกัดด้านความแข็งทื่อของเสียงร้อง (Robotic Vocals) ในระบบ UTAU ดั้งเดิม ด้วยการวิเคราะห์และแก้ไขสมการคลื่นเสียงในระดับ DSP ผสานกับการคำนวณรอยต่อพยัญชนะ-สระ (Phoneme Transition) เพื่อยกระดับเสียงร้องภาษาไทยและอังกฤษให้มีความเป็นธรรมชาติระดับมนุษย์ (Humanized Expressive Singing)

---

## 2. Working Objectives (วัตถุประสงค์ของการทำงาน)
- **Zero-Defect Standard:** รักษามาตรฐานซอฟต์แวร์ระดับสมบูรณ์แบบ (0 Errors, 0 Warnings, 100% Passed Tests) ในทุกแพลตฟอร์ม (Windows, macOS, Linux)
- **Bilingual & Thai G2P Integration:** ผสานระบบแปลสัทอักษรภาษาไทย VCCV, VCV, CV เข้ากับ Audio Engine อย่างแนบเนียน
- **Snappy Responsiveness:** ปรับปรุงความเร็วในการเปิดโปรแกรม (Startup) และการปิดโปรแกรม (Graceful Shutdown) ให้ตอบสนองทันที ไม่ค้าง ไม่หน่วง และไม่มี Ghost Process ตกค้างในระบบปฏิบัติการ
- **Safe State & Preferences Preservation:** บันทึกการตั้งค่า หน้าต่าง ค่าพิกัด และข้อมูลโครงการของผู้ใช้อย่างปลอดภัย ป้องกันการสูญหายหรือไฟล์ Config เสียหาย

---

## 3. Required Tooling (เครื่องมือที่จำเป็นต้องใช้ทั้งหมด)
- **Runtime & SDK:** .NET 8.0 / .NET 10.0 SDK สำหรับ C# Avalonia UI และ Core Library
- **UI Framework:** Avalonia UI 11 พร้อม ReactiveUI
- **Native Audio DSP:** C++ Worldline DSP Engine (`cpp/worldline/`) พร้อมอัลกอริทึม DIO / HARVEST F0 Estimation
- **Security & Dependencies:** `System.Security.Cryptography.Xml` (v10.0.13), `System.Security.Cryptography.Pkcs` (v10.0.13) ปลอดช่องโหว่ความปลอดภัย NU1903/NU1902
- **Audio Output & Synthesis:** DirectSound (NAudio), MiniAudio, Worldline Resampler, Moresampler, Classic Wavtool
- **Test Framework:** xUnit (ครอบคลุม 282 Unit Tests) สำหรับภาษา C#
- **Automation & CLI:** PowerShell, CMD Batch Scripts (`Auto Compiler.bat`, `Clear-Cache.bat`, `Package_Program.bat`)

---

## 4. Detailed File Mapping (แผนผังตำแหน่งไฟล์ระบบ)
```
Thai_OpenUtau/
├── OpenUtau/                     # Presentation & UI Layer (Avalonia)
│   ├── Program.cs                # จุดเข้าโปรแกรม (Single-Instance Mutex, Global Exit)
│   ├── App.axaml / App.axaml.cs  # จัดการ Theme, Culture และ Resource Dictionaries
│   ├── Views/
│   │   ├── SplashWindow.axaml.cs # หน้าต่างโหลดเริ่มต้น (Parallel Subsystem Initialization)
│   │   ├── MainWindow.axaml.cs   # หน้าต่างหลัก (Snappy Shutdown, Audio Halt, Timer Cleanup)
│   │   └── PianoRoll*.axaml.cs   # หน้าต่างแก้ไขโน้ตดนตรีและตารางพยางค์
│   └── ViewModels/               # ViewModels ควบคุม Reactive State
├── OpenUtau.Core/                # Audio Engine & Business Logic Layer
│   ├── DocManager.cs             # ควบคุมเอกสารโครงการ, Undo/Redo, และ Command Dispatch
│   ├── PlaybackManager.cs        # ควบคุม Audio Playback, Master Mixdown, และ Metronome
│   ├── SingerManager.cs          # สแกนและโหลดฐานข้อมูลเสียง (Voicebanks & oto.ini)
│   ├── Classic/
│   │   └── ToolsManager.cs       # จัดการ Resamplers และ Wavtools ภายนอก
│   ├── Render/
│   │   ├── RenderEngine.cs       # เอนจินเรนเดอร์เสียงล่วงหน้า (Pre-rendering & Cancellation)
│   │   ├── RenderPhrase.cs       # จัดกลุ่มวลีเสียง
│   │   └── Worldline.cs          # รอยต่อเสียงและ S-Curve Smoothing
│   └── Util/
│       ├── Preferences.cs        # บันทึกการตั้งค่าผู้ใช้แบบ JSON
│       └── PathManager.cs        # จัดการ Path ข้อมูล, แคช และ Log
├── OpenUtau.Plugin.Builtin/      # ปลั๊กอินแปลงสัทอักษร (Phonemizers)
│   ├── ThaiVCCVPhonemizer.cs     # ตัวแปลงเสียงไทย VCCV มาตรฐาน
│   └── ThaiVccvPhonemizer_Delta.cs # ตัวแปลงเสียงไทย VCCV ขั้นสูง (Glottal & Tone Tuned)
├── OpenUtau.Test/                # โครงการทดสอบ Unit Tests (282 Tests ผ่าน 100%)
├── cpp/worldline/                # C++ DSP Source Files สำหรับประมวลผลเสียงระดับต่ำ
├── Docs/ & History/              # บันทึกประวัติและรายงานการวิจัย (R&D Reports)
├── Auto Compiler.bat             # สคริปต์คอมไพล์โปรเจกต์ Release Mode
└── Clear-Cache.bat               # สคริปต์ล้างไฟล์ขยะและแคชเสียง
```

---

## 5. Primary Goals & Milestones (เป้าหมายหลักในการพัฒนา)
- **Milestone 1:** การรวมโค้ด 4 ระบบเข้าด้วยกันสำเร็จโดยไม่มีข้อผิดพลาด (เสร็จสิ้น)
- **Milestone 2:** การปรับแก้สมการ S-Curve (Cosine Smoothstep) สำหรับรอยต่อพยัญชนะและสระ (เสร็จสิ้น)
- **Milestone 3:** การอัปเกรดความปลอดภัย Cryptography สู่ Zero Vulnerabilities และ Unit Tests ผ่าน 282/282 เทสต์ (100% Pass) (เสร็จสิ้น)
- **Milestone 4 (v3.1):** การปรับปรุง Lifecycle ทั้งระบบ — Startup รวดเร็วด้วย Named Mutex และ Parallel Initialization + Shutdown ปิดโปรแกรมได้ทันที ตัดการทำงานของ Audio Buffers และ Timers ปราศจาก Zombie Processes ค้างในระบบ (เสร็จสิ้น)
- **Milestone 5:** การสร้างระบบ Automated Cross-Platform Publishing สำหรับแจกจ่ายตัวโปรแกรม (กำลังดำเนินการ)

---

## 6. Working Vision (วิสัยทัศน์ในการทำงาน)
- มุ่งสู่การเป็นศูนย์กลาง (Master Hub) สำหรับนักทำเพลงและศิลปินเสียงสังเคราะห์ชาวไทย ที่มอบความสะดวก รวดเร็ว และคุณภาพเสียงระดับมืออาชีพโดยไม่ต้องพึ่งพาโปรแกรมแปลงไฟล์ภายนอก
- ทำงานได้คล่องตัว เปิดปุ๊บติดปั๊บ ปิดปุ๊บหยุดทันที ตอบสนองต่อการคลิกของผู้ใช้ได้อย่างไร้รอยต่อ

---

## 7. Working Identity & DELTA SYNTH Rules (อัตลักษณ์และระเบียบปฏิบัติ)
- **Preserve Before Replace:** ห้ามรื้อระบบเดิมที่ทำงานถูกต้อง ให้ต่อยอดและแก้เฉพาะจุดที่เป็น Root Cause (Surgical Edits)
- **Zero-Defect Policy:** ผลลัพธ์สุดท้ายต้องมี Errors = 0 และ Warnings = 0 เสมอ
- **UI Design Guidelines:** 
  - โทนสี Cyber-Thai: สีแดงหลัก `#CC2200`, สีดำ `#1A1A1A`, สีขาว `#F0F0F0`
  - ฟอนต์หลัก: `Leelawadee UI` รองรับทั้งภาษาไทยและอังกฤษอย่างลงตัว
- **Credit Standard:** ระบุเครดิตส่วนหัวของไฟล์โค้ดตามมาตรฐาน DELTA SYNTH เสมอ

---

## 8. Project Uniqueness (เอกลักษณ์ของโปรเจกต์)
- **Humanized Smooth Envelope:** อัลกอริทึมคำนวณ Micro-fade (0.99ms) และ Cosine Crossfade ป้องกันเสียงแตกพร่า (Clicks/Pops) ในระดับตัวอย่างเสียง (Sample-level)
- **Snappy Lifecycle Engine:** ระบบตรวจสอบ Single Instance ผ่าน Named Mutex ระดับ Kernel ตัดปัญหา WMI/Process Enumeration ที่ช้า ให้ความเร็วในการเปิดโปรแกรมระดับมิลลิวินาที
- **Zero-Lingering Shutdown:** เมื่อปิดหน้าต่างหลัก ระบบจะยุติการเรนเดอร์เสียง สั่ง Stop Timer และปิดหน้าต่างบริวารทั้งหมด ส่งผลให้ Process คืน RAM และ CPU แก่ระบบปฏิบัติการทันที

---

## 9. Outcome Hypotheses (สมมติฐานของผลลัพธ์)
- ผู้ใช้งานจะเปิดโปรแกรมได้รวดเร็วขึ้นกว่าเดิมอย่างเห็นได้ชัด (Cold start ไวกว่าเดิมอย่างน้อย 3-5 เท่า)
- ผู้ใช้งานสามารถปิดโปรแกรมได้ทันทีโดยไม่มีการหน่วงค้าง และสามารถเปิดซ้ำใหม่ได้ทันทีโดยไม่เจอปัญหา `Process already open. Exiting.`
- ไฟล์โปรเจกต์และการตั้งค่าถูกบันทึกอย่างครบถ้วน ไม่มีความเสี่ยงที่ข้อมูล Preferences จะสูญหาย

---

## 10. Development Roadmap & Instructions for Future AI Models (คำแนะนำสำหรับ AI รุ่นถัดไป)
สำหรับ AI Agent หรือโมเดลอื่นที่จะเข้ามาสานต่องานในโปรเจกต์นี้ กรุณาปฏิบัติตามข้อกำหนดดังต่อไปนี้อย่างเคร่งครัด:
1. **การอ่านบริบทก่อนเริ่มงาน:** อ่าน `About_Project.md`, `AGENT.md`, `PROJECT_CONTEXT.md`, และ `R&D_Report.md` ให้เข้าใจโครงสร้างก่อนแตะต้องโค้ด
2. **ห้ามแตะต้องสคริปต์ Root:** ห้ามลบหรือเปลี่ยนชื่อ `Auto Compiler.bat`, `Clear-Cache.bat`, `OpenUtau.nsi`
3. **การแก้ไขโค้ด Lifecycle:**
   - ใน `Program.cs`: รักษา Named Mutex และ `Environment.Exit(0)` ไว้เสมอ เพื่อป้องกัน Process ค้าง
   - ใน `SplashWindow.axaml.cs`: รักษารูปแบบ `Task.Run()` แบบ Parallel Initialization ระหว่าง Tools, Singers, DocManager และ Audio Output
   - ใน `MainWindow.axaml.cs`: ตรวจสอบให้มั่นใจว่าเมื่อสั่งปิดหน้าต่าง จะต้องเรียก `PlaybackManager.Inst.StopPlayback()`, หยุด Timer และปิด `pianoRollWindow` เสมอ
4. **การตรวจสอบผลงาน:** รัน `dotnet build` และ `dotnet test` เพื่อยืนยันว่าไม่มี Error หรือ Warning ใหม่เกิดขึ้น

---

## 11. Correct Improvement Methodology (วิธีการปรับปรุงโปรเจกต์ที่ถูกต้อง)
- ค้นหา Root Cause จาก Log ในโฟลเดอร์ `OpenUtau\Logs` เสมอ
- แก้ไขแบบ Surgical Edits โดยเปลี่ยนเฉพาะบรรทัดที่เกี่ยวข้อง
- ห้ามเพิ่ม Abstraction หรือ Dependency ภายนอกโดยไม่จำเป็น
- รักษาสัญญาระบบ (Public API Contracts) เพื่อให้โปรเจกต์ไฟล์ `.ustx` ดั้งเดิมเปิดได้ 100%

---

## 12. Work History Log (บันทึกประวัติการพัฒนา)
- **Version 1.0 (2026-08):** Codebase Unification & Master Tool Initiation (By DELTA SYNTH)
- **Version 2.0 (2026-08):** Audio Core Enhancement (Cosine Envelope Smoothing & Priority Resamplers Setup)
- **Version 2.1 (2026-08):** Automated Packaging & Deployment Scripts Integrated
- **Version 3.0 (2026-10):** Total Zero-Defect Consolidation: Fixed Security Vulnerabilities (NU1903/NU1902 via Cryptography 10.0.13), Complete Test Suite Fix (282/282 tests passed, 0 failures, 0 warnings), Master Documentation Architecture
- **Version 3.1 (2026-10):** Snappy Startup & Graceful Exit Optimization: Instant single-instance named Mutex, parallel audio & core discovery in SplashWindow, zero-delay thread dispatch, graceful audio halt, timer deactivation, child window cleanup, and immediate process termination eliminating lingering background instances.
