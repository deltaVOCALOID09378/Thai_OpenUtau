# AI Agent Commands & Workspace Archive — Thai OpenUtau

> **DELTA SYNTH Architecture Standard**  
> Status: Active & Managed  
> Engine: DELTA SYNTH & All Code Agentic AI Engine

---

## 1. Overview (ภาพรวม)
พื้นที่จัดเก็บสำหรับคำสั่ง อาร์ติแฟกต์ สคริปต์เสริม และไฟล์ที่ไม่ได้ใช้งานในกระบวนการทำงานหลัก เพื่อรักษา Root Directory ของโปรเจกต์ `Thai_OpenUtau` ให้สะอาด ปลอดภัย และมีประสิทธิภาพสูงสุดตามหลักการ **Preserve Before Replace**

## 2. Directory Structure (โครงสร้างโฟลเดอร์)
```
AI_Agent_Commands_And_Archive/
├── Command_Runners/          # สคริปต์และตัวรันคำสั่งสำหรับ Agent และระบบ Automation
├── Unused_Files/             # ไฟล์ที่ไม่ได้ใช้งานหลัก (เช่น Solution รอง, Log อดีต, Text Dump)
│   ├── OpenUtau.slnx         # Solution แบบ XML เดิม (สำรองไว้ ไม่ใช้ใน Root เพื่อให้ `dotnet build` รันได้ 100%)
│   ├── All File of (Thai OpenUtau Type5).txt
│   ├── LunaiFiles.txt
│   └── msbuild.log
└── README.md                 # เอกสารอธิบายการจัดเก็บ
```

## 3. Maintenance Rules (กฎการบำรุงรักษา)
- ไฟล์ที่อยู่ใน `Unused_Files/` จะไม่ถูกโหลดหรือคอมไพล์โดยตรงจาก `dotnet build` ที่ระดับ Root
- โค้ดและฟีเจอร์หลักทั้งหมดรวมอยู่ใน `Thai OpenUtau.sln` เพียงจุดเดียว
- ไฟล์ชั่วคราวหรือ Log อนาคตสามารถย้ายเข้ามาพักไว้ที่นี่เพื่อหลีกเลี่ยงการสะสมใน Root ของโครงการ
