<!--
  Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
  Product: Thai OpenUtau Master Tool
  Workspace Hygiene & Archive Index
-->

# AI Agent Commands & Workspace Archive — Thai OpenUtau

> **DELTA SYNTH Architecture Standard**  
> **Status:** Active & Enforced  
> **Standard:** Preserve → Strengthen → Optimize → Verify  
> **Engine:** DELTA SYNTH & All Code Agentic AI Engine  

---

## 1. Overview (ภาพรวม)

* **TH:** แฟ้มจัดเก็บสารบบคำสั่ง สคริปต์เฉพาะกิจ ไฟล์สำรอง และบันทึกประวัติการรันคำสั่งของ Agent AI สำหรับโปรเจกต์ `Thai_OpenUtau` เพื่อรักษาไดเรกทอรีราก (Root Directory) ให้สะอาด ปลอดภัย และมีประสิทธิภาพสูงสุดตามหลักการ **Preserve Before Replace** และข้อกำหนดระเบียบ `agent-workspace-archive`
* **EN:** The designated archival repository for AI Agent command runners, utility scripts, auxiliary backups, and execution logs in `Thai_OpenUtau`. This enforces strict root workspace hygiene, ensuring only active production entry points and configuration files reside in the root directory while preserving historical records safely.

---

## 2. Directory Structure & File Index (โครงสร้างสารบบและดัชนีไฟล์)

```
AI_Agent_Commands_And_Archive/
├── Command_Runners/                                  # สคริปต์และตัวรันคำสั่งสำหรับ Agent และระบบ Automation
│   ├── Pull Request.bat                              # สคริปต์ Git เช็กเอาต์และดึง Main branch สำหรับ Release
│   ├── Pull Request-1.bat                            # สคริปต์ Git ดึง Head ของ Pull Requests ต่างๆ
│   ├── run.bat                                       # สคริปต์ตรวจสอบการคอมไพล์และโครงสร้างคำศัพท์ VCCV (audit_vccv_engine.bat)
│   ├── update_audio_output.py                        # สคริปต์ Agent ปรับปรุงโมดูล Audio Output
│   └── update_preferences_vm.py                      # สคริปต์ Agent ปรับปรุงโมดูล Preferences ViewModel
├── Unused_Files/                                     # บันทึกประวัติและไฟล์สำรองที่ไม่ถูกเรียกใช้งานหลัก
│   ├── About-Project.md                              # เอกสารสรุปโครงการร่างเดิม (ฉบับหลักปัจจุบันคือ About_Project.md)
│   ├── All File Mapping.md                           # บันทึกการจับคู่รายการไฟล์ระบบขนาดใหญ่
│   ├── All File of (Thai OpenUtau Type5).txt         # รายการไฟล์ Type5 เดิม
│   ├── All UI interface.md                           # บันทึกข้อกำหนดและรายการ UI Components เดิม
│   ├── audit.md.gdoc                                 # ชอร์ตคัตเอกสาร Google Docs การตรวจสอบระบบ
│   ├── DELTA_SYNTH_SYSTEM_TUNING_AND_INTEGRATION_GUIDE.md # คู่มือการปรับจูนระบบเดิม
│   ├── LunaiFiles.txt                                # รายการไฟล์ระบบเสียง Lunai
│   ├── msbuild.log                                   # บันทึกประวัติการ Build ในอดีต
│   ├── OpenUtau.slnx                                 # โครงสร้าง Solution แบบ XML เดิม (ใช้ Thai OpenUtau.sln เป็นหลัก)
│   ├── Optimize for this plan.md                     # แผนการและแนวทางการเพิ่มประสิทธิภาพระบบเดิม
│   ├── Strings.xaml                                  # ไฟล์สตริงภาษาไทยสำรอง
│   └── The Source Code of System File_s Name Mapping.md # เอกสาร Mindmap รายชื่อไฟล์ระบบเดิม
└── README.md                                         # เอกสารกำกับสารบบการจัดเก็บ (ฉบับนี้)
```

---

## 3. Maintenance Rules (กฎระเบียบการบำรุงรักษา)

1. **Root Directory Hygiene:**
   - ไดเรกทอรีราก (Root Directory) มีเฉพาะไฟล์คอนฟิกหลัก (`.editorconfig`, `.gitattributes`, `.gitignore`, `Directory.Build.props`, `global.json`, `protect.cedar`), ตัวเปิดระบบและตัวติดตั้งทางการ (`Start-The-Program.bat`, `Auto Compiler.bat`, `Build_All_Installers.bat`, `Compile_Full_Program.bat`, `Package_Program.bat`, `Release_Cycle.bat`, `Install-File.bat`), และเอกสารมาตรฐาน (`README.md`, `About_Project.md`, `AGENT.md`, `AGENTS.md`, `PROJECT_CONTEXT.md`, `Project_Overview_And_Reference.md`, `R&D_Report.md`, `SECURITY.md`, `version.txt`)
   - ไฟล์ติดตั้งที่คอมไพล์สำเร็จแล้วถูกจัดเก็บไว้อย่างเป็นระเบียบในแฟ้ม `Published/Version no. 3.3.0/`
2. **Command Runners Isolation:**
   - หาก Agent มีความจำเป็นต้องสร้างสคริปต์ทดสอบ วิเคราะห์ หรืออัปเดตระบบ ให้สร้างและทำงานภายใน `AI_Agent_Commands_And_Archive/Command_Runners/`
3. **Build & Execution Isolation:**
   - ไฟล์ที่อยู่ใน `Unused_Files/` จะไม่ถูกโหลดหรือคอมไพล์โดยตรงจาก `dotnet build` ที่ระดับ Root
   - รักษาสถานะ Zero Known Defects และรักษาความเข้ากันได้ของระบบ 100%
