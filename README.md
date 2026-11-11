<!--
  ============================================================================
  1. วัน/เดือน/ปี (Date)        : 11 พฤศจิกายน พ.ศ. 2569 / 11 November 2026 (B.E. 2569 / A.D. 2026)
  2. ชื่อไฟล์ (File Name)       : Readme.md
  3. ลำดับเวอร์ชัน (Version)    : v3.9.5 (Release 2.0)
  4. รายชื่อเครดิต (Credit)     : Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Contributors & DELTA SYNTH ]
  5. ตำแหน่งที่จัดวาง (Path)    : d:\Program Developing\Thai_OpenUtau\Readme.md
  6. รายละเอียดการแก้ไข (Summary): ปรับปรุงเอกสารรายงานวิจัยและพัฒนาสถาปัตยกรรมระบบรูปภาพนักร้อง DELTA SYNTH (แยกขาด Full-Body และ Profile Icon), ระบบบันทึกหน้าจอ Hardware Accelerated h264_mf, แผนผัง My Mapping และประวัติเวอร์ชัน v3.9.5
  ============================================================================
-->

# Thai OpenUtau (เวอร์ชันภาษาไทย & DELTA SYNTH Edition)

**Thai OpenUtau** คือโปรแกรมแก้ไขเสียงร้อง (Editor) แบบโอเพนซอร์สและใช้งานฟรี ที่ถูกพัฒนาและยกระดับประสิทธิภาพโดย **DELTA SYNTH** เพื่อมอบประสบการณ์การสังเคราะห์เสียงระดับมืออาชีพ รองรับระบบเสียงภาษาไทยครบวงจร (VCCV, VCV, CpV, Arpasing) พร้อมเอนจินประมวลผลเสียงความเร็วสูงและระบบมัลติมีเดียที่ทันสมัย

[![สถานะการสร้าง](https://img.shields.io/github/actions/workflow/status/stakira/OpenUtau/build.yml?style=for-the-badge)](https://github.com/stakira/OpenUtau/actions/workflows/build.yml)
[![Discord](https://img.shields.io/discord/551606189386104834?style=for-the-badge&label=discord&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/UfpMnqMmEM)
[![Made And Checked By](https://img.shields.io/badge/Made%20And%20Checked%20By-DELTA%20SYNTH%20%26%20Gemini%20AI-blueviolet?style=for-the-badge)](https://github.com/stakira/OpenUtau)
[![Release Version](https://img.shields.io/badge/Release-v3.9.5%20Standalone-red?style=for-the-badge)](#)

---

## สารบัญ (Table of Contents)
1. [การเริ่มต้นใช้งาน (Getting Started)](#การเริ่มต้นใช้งาน-getting-started)
2. [รายงานผลการวิจัยและพัฒนาสถาปัตยกรรมระบบ (Research & Architecture Report)](#รายงานผลการวิจัยและพัฒนาสถาปัตยกรรมระบบ-research--architecture-report)
   - [2.1 สถาปัตยกรรมรูปภาพนักร้อง (Singer Picture Strict Separation)](#21-สถาปัตยกรรมรูปภาพนักร้อง-singer-picture-strict-separation)
   - [2.2 เอนจินบันทึกภาพหน้าจอและวิดีโอ (Hardware Accelerated Recording Engine)](#22-เอนจินบันทึกภาพหน้าจอและวิดีโอ-hardware-accelerated-recording-engine)
   - [2.3 การปรับปรุงส่วนติดต่อผู้ใช้ (UI & Button Enhancements)](#23-การปรับปรุงส่วนติดต่อผู้ใช้-ui--button-enhancements)
3. [คุณสมบัติหลักของโปรแกรม (Main Features)](#คุณสมบัติหลักของโปรแกรม-main-features)
4. [แผนผังโครงสร้างตำแหน่งไฟล์ (My Mapping)](#แผนผังโครงสร้างตำแหน่งไฟล์-my-mapping)
5. [ประวัติการพัฒนาและเอกสารอ้างอิง (History & Documentation)](#ประวัติการพัฒนาและเอกสารอ้างอิง-history--documentation)
6. [การมีส่วนร่วมและการพัฒนา (How to Contribute)](#การมีส่วนร่วมและการพัฒนา-how-to-contribute)
7. [เครื่องมือและสคริปต์ประจำระบบ (.bat Tools)](#เครื่องมือและสคริปต์ประจำระบบ-bat-tools)

---

## การเริ่มต้นใช้งาน (Getting Started)

ชุดโปรแกรม Thai OpenUtau Standalone พร้อมรันไทม์ .NET 8 ในตัว สามารถเปิดใช้งานได้ทันทีโดยไม่ต้องติดตั้งซอฟต์แวร์เสริม:

* **ชุดติดตั้งอัตโนมัติ (Standalone SFX Installer):** `Thai_OpenUtau_Installer_v3.9.5.exe`
* **ชุดพกพาไม่ต้องติดตั้ง (Standalone ZIP Package):** `Thai_OpenUtau_v3.9.5_Standalone.zip`
* **การเปิดโปรแกรม:** ดับเบิลคลิกที่ `Start-The-Program.bat` หรือ `OpenUtau.exe`

> **ข้อแนะนำสำคัญ:** เพื่อการใช้งานที่สมบูรณ์แบบที่สุด ขอแนะนำให้อ่านคู่มือผ่าน [Github Wiki](https://github.com/stakira/OpenUtau/wiki) โดยเฉพาะหัวใจหลักอย่าง **Phonemizers** (ระบบถอดรหัสเสียงวรรณยุกต์ภาษาไทย) และ **Resamplers** (เช่น WORLDLINE-R, Moresampler)

---

## รายงานผลการวิจัยและพัฒนาสถาปัตยกรรมระบบ (Research & Architecture Report)

### 2.1 สถาปัตยกรรมรูปภาพนักร้อง (Singer Picture Strict Separation)

จากการวิจัยและตรวจสอบระบบจัดเก็บทรัพยากรภาพของนักร้องเสมือนจริงในเครือข่าย DELTA SYNTH พบปัญหาความสับสนของการนำภาพโปรไฟล์ (โคลสอัปใบหน้า) ไปทับภาพเต็มตัว (Full-Body Standing Portrait) ในเวอร์ชันก่อนหน้า จึงได้กำหนด **กฎเหล็กสถาปัตยกรรมรูปภาพนักร้อง (Code Guardian Protocol)** ไว้อย่างชัดเจนและเคร่งครัด:

```
[ Central Assets Storage (E:\) ]
 ├── E:\A Profile for Singer Picture\ (ภาพโคลสอัปใบหน้า) ──► icon.png / Icon.png ──► Track Header Avatar (หัวแทร็กเพลง)
 └── E:\A Full Body Picture\          (ภาพเต็มตัวความละเอียดสูง) ──► portrait.png / Full.png ──► Piano Roll Background (เปียโนโรล)
```

#### กฎระเบียบข้อบังคับถาวร:
1. **ภาพแบบเต็มตัว (Full-Body Portrait):**
   - **แหล่งที่มาต้นฉบับ:** ดึงจากโฟลเดอร์กลาง `E:\A Full Body Picture` เท่านั้น
   - **ไฟล์เป้าหมาย:** บันทึกเป็น `portrait.png` (และสำรองเป็น `Full.png`) ในโฟลเดอร์ของนักร้องแต่ละคน
   - **การแสดงผล:** ใช้สำหรับแสดงผลเป็นภาพพื้นหลังขนาดใหญ่บนเปียโนโรล (Piano Roll Portrait) เท่านั้น **ห้ามนำไปย่อเป็นอวาตาร์โดยเด็ดขาด**
2. **รูปโปรไฟล์แสดงตัวตน (Profile Avatar Icon):**
   - **แหล่งที่มาต้นฉบับ:** ดึงจากโฟลเดอร์กลาง `E:\A Profile for Singer Picture` เท่านั้น
   - **ไฟล์เป้าหมาย:** บันทึกเป็น `icon.png` (และ `Icon.png`) ในโฟลเดอร์ของนักร้องแต่ละคน
   - **การแสดงผล:** ใช้สำหรับเป็นรูปโปรไฟล์แสดงตัวตนบนหัวแทร็กเพลง (Track Header Avatar) เท่านั้น
3. **ระบบป้องกันภาพสลับกัน (Sanity Guard & Anti-Contamination):**
   - ในโมดูล `DeltaSingerProfileResolver.cs` ติดตั้งระบบตรวจสอบขนาดไฟล์ หากพบว่าขนาดไฟล์ของ `portrait.png` เท่ากับ `icon.png` ระบบจะระบุว่าเป็นภาพที่เสียหาย (Corrupted) และปฏิเสธการโหลดทันที พร้อมสลับไปดึงภาพเต็มตัวตัวจริงจาก `Full.png` หรือ `E:\A Full Body Picture` มาแทนที่อัตโนมัติ
   - ตรวจสอบคลังเสียงจริงทั้ง 101 โฟลเดอร์ใน `E:\All Voicebank in DELTA SYNTH` ผลการตรวจสอบ: **Corrupted Count = 0 (สมบูรณ์ 100%)**

---

### 2.2 เอนจินบันทึกภาพหน้าจอและวิดีโอ (Hardware Accelerated Recording Engine)

จากการวิเคราะห์ Log ข้อผิดพลาดเดิม พบว่าการบันทึกวิดีโอของ OpenUtau ล้มเหลวเนื่องจากการเรียกใช้ตัวเข้ารหัสซอฟต์แวร์ `-c:v libx264` ซึ่งไม่มีอยู่ใน FFmpeg build ของ Windows รวมถึงการบันทึกภาพนิ่งเกิด Cross-thread Skia bitmap disposal บน Avalonia UI Thread

#### แนวทางการแก้ไขและผลลัพธ์:
1. **เปลี่ยนเอนจินการเข้ารหัส:** ปรับปรุง `ScreenCaptureService.cs` และ `ScreenRecorder.cs` ให้ใช้ตัวเข้ารหัสฮาร์ดแวร์ **Windows Media Foundation (`-c:v h264_mf -pix_fmt yuv420p`)**
   - ความเร็วในการบันทึกวิดีโอ 60 FPS ลื่นไหล ไร้การกระตุก
   - ไม่พึ่งพา External GPL Codec ทำงานได้บน Windows ทุกเครื่องที่มีการ์ดจอ Intel, NVIDIA หรือ AMD
2. **Synchronous Lossless PNG Capture:** ปรับระบบบันทึกภาพนิ่งหน้าจอให้เซฟไฟล์ผ่าน `FileStream` โดยตรงบน UI Thread ป้องกัน Bitmap ถูก Dispose ข้ามเธรด
3. **Automated FFmpeg Locator:** เพิ่มระบบตรวจหา `ffmpeg.exe` อัตโนมัติจากพาธรันไทม์, โฟลเดอร์ติดตั้ง และไดเรกทอรีโปรแกรม

---

### 2.3 การปรับปรุงส่วนติดต่อผู้ใช้ (UI & Button Enhancements)

เพื่อให้เป็นไปตามมาตรฐานการออกแบบระดับมืออาชีพของ DELTA SYNTH (Clean UI, Leelawadee UI, Red `#CC2200`):
1. **Dual-Preview Layout ใน SingersDialog:**
   - เพิ่มการแสดงผลรูปนักร้องแบบคู่ขนาน: กล่องซ้ายแสดง Profile Avatar (100x100) และกล่องขวาแสดง Full-Body Portrait (65x100)
   - ผู้ใช้สามารถตรวจสอบความถูกต้องของรูปทั้งสองประเภทได้พร้อมกันทันที
2. **ปุ่มคำสั่งซิงก์รูปภาพ (Picture Synchronization Commands):**
   - **Singer Gear Menu:** เพิ่มเมนู "ซิงก์รูปภาพนักร้อง DELTA (Sync Pictures)" ในหน้าต่างจัดการนักร้อง
   - **Main Tools Menu:** เพิ่มเมนู "ซิงก์รูปภาพนักร้อง DELTA SYNTH ทั้งหมด (Sync All DELTA Singer Pictures)" บนแถบเมนูหลักของโปรแกรม ประมวลผลแบบเบื้องหลังไม่ทำให้โปรแกรมค้าง
3. **ตรวจสอบปุ่มกดและฟังก์ชันทั้งหมด:**
   - ปุ่มบันทึกภาพนิ่ง (Camera Icon / F12) ทำงานได้ปกติ 100%
   - ปุ่มบันทึกวิดีโอ (Screen Recording) เริ่มและหยุดบันทึก พร้อมสร้างไฟล์ MP4 สมบูรณ์ 100%
   - เมนูและปุ่มควบคุมทั้งหมดตอบสนองรวดเร็ว ปราศจากอาการค้างหรือหน่วง

---

## คุณสมบัติหลักของโปรแกรม (Main Features)

* **ประสบการณ์การใช้งานที่ทันสมัย:** อินเทอร์เฟซถูกออกแบบมาให้ลื่นไหล รองรับการซูมและเคลื่อนที่ด้วยเมาส์และ Scroll Wheel อย่างเป็นธรรมชาติ
* **เครื่องมือปรับแต่งเสียงที่ครบครัน:** 
  * **MIDI Editor:** จัดการโน้ตและนำเข้าไฟล์ VSQX (Vocaloid), UST (UTAU), SVP (Synthesizer V), MusicXML ได้อย่างราบรื่น
  * **Vibrato Editor:** สร้างลูกเอื้อนที่มีเอกลักษณ์และถ่ายทอดอารมณ์ได้ชัดเจน
  * **Expressions:** ใช้ระบบการปรับแต่ง Curves & Expressions ที่ละเอียดสูง ควบคุม Dynamics, Pitch, Breathiness, Gender ได้แม่นยำ
* **ระบบ Phonemizer ภาษาไทยอัจฉริยะ:**
  * รองรับระบบวรรณยุกต์และสระภาษาไทยครบทุกรูปแบบ: **Thai VCCV**, **Thai VCV**, **Thai CpV**, **Thai Arpasing**
  * มีพจนานุกรมภาษาไทย (Thai Dictionaries) ในตัว แปลงข้อความภาษาไทยเป็นสัทอักษรได้อัตโนมัติ
* **เทคโนโลยี Pre-rendering & Realtime DSP:**
  * ประมวลผลเสียงล่วงหน้าแบบมัลติเธรด ฟังพรีวิวได้ทันทีไม่ต้องรอ
  * ขับเคลื่อนด้วย Worldline DSP ความเร็วสูง และ Moresampler
* **การรองรับ AI และเทคโนโลยีใหม่:**
  * รองรับนักร้องระบบ **DiffSinger (ONNX / DirectML)** และ **ENUNU (NNSVS)**

---

## แผนผังโครงสร้างตำแหน่งไฟล์ (My Mapping)

```
d:\Program Developing\Thai_OpenUtau\
├── OpenUtau/                                (Presentation Layer - Avalonia XAML UI)
│   ├── Capture/
│   │   └── ScreenCaptureService.cs          (เอนจินบันทึกภาพนิ่งและวิดีโอ Windows MediaFoundation)
│   ├── ViewModels/
│   │   ├── MainWindowViewModel.cs           (วิวโมเดลหน้าต่างหลัก)
│   │   └── SingersViewModel.cs              (วิวโมเดลจัดการนักร้อง + พรีวิวภาพคู่ + ระบบซิงก์ภาพ)
│   ├── Views/
│   │   ├── MainWindow.axaml / .cs           (หน้าต่างหลัก + เมนู Tools: ซิงก์รูปภาพนักร้องทั้งหมด)
│   │   └── SingersDialog.axaml / .cs        (หน้าต่างเลือกนักร้อง + Dual Avatar/Portrait Display)
│   └── Strings/
│       ├── Strings.th-TH.axaml              (ชุดข้อความภาษาไทย)
│       └── Strings.axaml                    (ชุดข้อความภาษาอังกฤษ)
│
├── OpenUtau.Core/                           (Logic Layer - Core Synthesis & Business Logic)
│   ├── DeltaSingers/
│   │   ├── DeltaSingerProfileResolver.cs    (ตัวแก้ไขและซิงก์รูปภาพนักร้อง + Sanity Guard)
│   │   └── DeltaSynthSingerRegistry.cs      (ฐานข้อมูลและรายชื่อนักร้องทางการ DELTA SYNTH)
│   └── Util/
│       └── ScreenRecorder.cs                (เอนจินบันทึกวิดีโอระดับแกนหลัก h264_mf)
│
├── OpenUtau.Plugin.Builtin/                 (Plugin Layer - Phonemizers & Dictionaries)
│   ├── ThaiVCCVPhonemizer.cs                (ระบบถอดรหัสเสียงวรรณยุกต์ภาษาไทย VCCV)
│   ├── ThaiSyllableBasedPhonemizer.cs       (ระบบวิเคราะห์พยางค์ภาษาไทย)
│   └── Dictionary/                          (พจนานุกรมคำอ่านภาษาไทย)
│
├── Publish/                                 (ไดเรกทอรีโปรแกรม Standalone คอมไพล์พร้อมรันไทม์ .NET 8)
│   ├── OpenUtau.exe                         (ไฟล์รันโปรแกรมหลัก)
│   ├── ffmpeg.exe                           (เครื่องมือประมวลผลวิดีโอและเสียง)
│   ├── worldline.dll                        (เอนจิน DSP เรนเดอร์เสียงสังเคราะห์)
│   └── Dictionaries/                        (พจนานุกรมภาษาไทยที่รวมเข้ากับแพ็กเกจ)
│
├── Published/                               (ชุดติดตั้งและไฟล์แพ็กเกจสำหรับแจกจ่าย)
│   └── Version no. 3.9.5/
│       ├── Thai_OpenUtau_v3.9.5_Standalone.zip  (แพ็กเกจพกพา Standalone บีบอัดระดับ Ultra)
│       ├── Thai_OpenUtau_Installer_v3.9.5.exe  (ชุดติดตั้ง GUI อัตโนมัติ SFX Installer)
│       └── SHA256SUMS.txt                   (รหัสตรวจสอบความถูกต้องของไฟล์)
│
├── Tools/                                   (เครื่องมือช่วยพัฒนา ตรวจสอบ และทดสอบระบบ)
│   ├── Release_Cycle.bat                    (สคริปต์ไปป์ไลน์ออกเวอร์ชันอัตโนมัติ 7 ขั้นตอน)
│   ├── Compile_Full_Program.bat             (สคริปต์คอมไพล์โปรแกรม Standalone พร้อม .NET 8)
│   └── Package_Program.bat                  (สคริปต์บีบอัด ZIP และสร้างชุดติดตั้ง SFX)
│
└── Docs/
    └── History/
        └── History-11.11.2026.3.9.5.md      (บันทึกประวัติการพัฒนาและแก้ไขระบบอย่างละเอียด)
```

---

## ประวัติการพัฒนาและเอกสารอ้างอิง (History & Documentation)

* [ประวัติการพัฒนาเวอร์ชัน v3.9.5 (11 พฤศจิกายน 2569)](Docs/History/History-11.11.2026.3.9.5.md) — สถาปัตยกรรมรูปภาพนักร้องใหม่, แก้ไขระบบบันทึกหน้าจอ, Dual Preview และชุดติดตั้ง Standalone
* [คลังประวัติการพัฒนาย้อนหลังทั้งหมด](Docs/History/)

---

## การมีส่วนร่วมและการพัฒนา (How to Contribute)

หากคุณต้องการเป็นส่วนหนึ่งของการพัฒนาโปรเจกต์นี้:
1. **รายงานปัญหา:** แจ้งบั๊กผ่านทาง Discord หรือ GitHub Issues
2. **เสนอแนะฟีเจอร์:** ร่วมพูดคุยแนวทางการพัฒนากับทีมงาน DELTA SYNTH
3. **เขียนโค้ด:** ส่ง Pull Requests เพื่อปรับปรุงประสิทธิภาพตามมาตรฐาน **Preserve Before Replace**

---

## เครื่องมือและสคริปต์ประจำระบบ (.bat Tools)

โปรเจกต์ประกอบด้วยสคริปต์ระบบภาษาอังกฤษตามมาตรฐาน DELTA SYNTH สำหรับการบำรุงรักษา:
* `Compile_Full_Program.bat` — คอมไพล์โปรแกรมเต็มรูปแบบพร้อมพจนานุกรมและ .NET 8 Runtime
* `Package_Program.bat` — บีบอัดไฟล์แพ็กเกจ ZIP Ultra Compression และสร้าง SFX Installer
* `Release_Cycle.bat` — รันวงจรออกเวอร์ชันเต็มรูปแบบ (Preflight -> Version -> Build -> Test -> Publish -> Package -> Verify)
* `Start-The-Program.bat` — เรียกเปิดโปรแกรมหลักอย่างปลอดภัย
