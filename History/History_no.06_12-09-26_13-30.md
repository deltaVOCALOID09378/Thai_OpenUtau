# DELTA SYNTH — รายงานสรุปผลการปรับปรุง UI และระบบการใช้งานเมาส์แบบไร้ข้อจำกัด (No Limits)
**Document ID:** `History_no.06_12-09-26_13-30.md`  
**Date & Time:** 12 กันยายน 2026 | 13:30 น.  
**Project:** Thai OpenUtau (DELTA SYNTH Edition)  
**Solution:** `Thai OpenUtau.sln`  
**Author/Architect:** Antigravity AI Systems Engineer (Pair Programming with นายท่านเดลต้า)

---

### 1. ภาพรวมการดำเนินงาน (Executive Summary)
ดำเนินการตามข้อสั่งการของนายท่านเดลต้า:
1. **ปรับปรุง UI ใหม่ยกชุดและหลอมรวมกับ GitHub ล่าสุด:** ผสานโค้ด UI Avalonia 11.2 ล่าสุดจาก Upstream เข้ากับ Thai OpenUtau เพื่อแก้ไขปัญหาการแสดงผลที่รุนแรงของชุดเดิม โดยยังคงรักษาอัตลักษณ์ DELTA SYNTH, การแปลภาษาไทย (`Strings.th-TH.axaml`), ฟอนต์ `Leelawadee UI`, สี Accent `#CC2200`, Batch Edits / Vocal Macros, และระบบเร่งความเร็ว DirectML ไว้อย่างครบถ้วน
2. **ปลดล็อคและปรับปรุงการใช้งานเมาส์อย่างสมบูรณ์แบบ ไร้ขีดจำกัดในทุกรูปแบบ:**
   - ปลดล็อคระยะลากโน้ตไปทางขวา (Unrestricted Rightward Drag): กำจัดตัวจำกัด `maxDeltaTick` เดิม ทำให้ผู้ใช้สามารถลากโน้ตข้ามขอบ Part ได้อย่างอิสระ โดยระบบจะคำนวณและขยายขนาด Part อัตโนมัติแบบไร้รอยต่อ
   - เพิ่มระบบทำซ้ำโน้ตด้วยการกด Alt ค้างแล้วลากเมาส์ (Alt + Drag Note Duplication)
   - เพิ่มระบบทำซ้ำพาร์ทด้วยการกด Alt ค้างแล้วลากเมาส์ (Alt + Drag Part Duplication)
   - เพิ่มการสลับเลือก (Toggle Select) ด้วย Ctrl + Click บน Part และ Note
   - เพิ่มการเลือกแบบกล่องเพิ่มเติม (Additive Box Selection) ด้วย Shift + Drag บน Notes Canvas และ Parts Canvas
   - ปรับแต่ง Deadband ความไวของเมาส์จาก 4px เหลือ 2px เพื่อการตอบสนองที่ฉับไว แม่นยำระดับพิกเซล
   - เพิ่ม Cursor Feedback เมื่อ Hover เหนือโน้ต/พาร์ทด้วย Alt (`cursorSizeAll`) และ Shift/Ctrl บน Canvas (`cursorCross`)

---

### 2. รายละเอียดการปรับปรุงเชิงลึก (Detailed System Changes)

#### 2.1 การจัดการความขัดแย้ง UI และโครงสร้างโค้ด
- **กำจัด Shadowed Files:** นำไฟล์เก่า `OpenUtau/Views/PianoRollWindow.axaml` และ `.cs` ออกจากระบบ เพื่อขจัดข้อขัดแย้งชนิดข้อมูลกำกวม `IValueTip` ที่ชนกับ `Controls/PianoRoll.axaml.cs`
- **แก้ไข Constructor & Signature:** ปรับปรุง `AutoPitchwriterDialog.axaml.cs` ให้รองรับ `AutoPitchwriterOptions` พร้อม Action callback ที่เป็น Nullable
- **แก้ไขการเรียกภาษาใน PhonemeUIRender:** เปลี่ยนจากการเรียกเมธอด `GetLangCode()` เป็นการอ่าน Property สาธารณะ `.LangCode`
- **ตรวจสอบ Preferences & Audio Capture:** คืนค่า Property ที่เกี่ยวข้องกับการบันทึกเสียงและพาธ Export ใน `PreferencesViewModel.cs` และจัดระเบียบ `Preferences.cs` ให้เรียบร้อย 0 Error 0 Warning

#### 2.2 ปลดล็อคระบบการใช้เมาส์ให้ไร้ข้อจำกัด (`NoteEditStates.cs` & `NoteCommands.cs`)
- **NoteMoveEditState:**
  - ยกเลิกการ Clamp `maxDeltaTick` ที่ผูกกับ `part.Duration` เดิม ทำให้การลากโน้ตไปทางขวาสามารถขยายความยาวพาร์ทได้อย่างอิสระไร้ขีดจำกัด
  - ลด Deadband การขยับเมาส์จาก 4px เหลือ 2px ทำให้การตอบสนองเมาส์คล่องแคล่ว ว่องไว
  - ผนวกฟังก์ชัน Alt + Drag สำหรับการ Clone/Duplicate โน้ต พร้อมเชื่อมต่อคำสั่ง `AddNoteCommand` และระบบ Undo แบบ `"command.note.duplicate"`
- **NoteCommands (AddNoteCommand, MoveNoteCommand, ResizeNoteCommand):**
  - ปรับปรุงการคำนวณ `minDurTick` ให้หาค่าสูงสุดของโน้ตทั้งหมดด้วย `notes.Max(n => n.End)` แทนที่จะพึ่งพาเพียง `notes.LastOrDefault()` ซึ่งอาจให้ผลผิดพลาดหากโน้ตในลิสต์ไม่ได้เรียงตามตำแหน่งเวลา

#### 2.3 ปรับปรุงการควบคุมเมาส์บน Tracks & Parts (`PartEditStates.cs` & `MainWindow.axaml.cs`)
- **PartMoveEditState:**
  - รองรับการคัดลอกพาร์ทด้วย Alt + Drag โดยอัตโนมัติ
  - ลด Deadband เหลือ 2px
- **MainWindow.axaml.cs:**
  - รองรับการกด `Ctrl + Click` เพื่อ Toggle การเลือกพาร์ท
  - รองรับการกด `Shift + Drag` หรือ `Ctrl + Drag` บนพื้นที่ว่างของแทร็กเพื่อสร้าง Selection Box
  - แสดง Cursor `cursorSizeAll` เมื่อกด Alt ชี้เหนือพาร์ท และ `cursorCross` เมื่อกด Shift/Ctrl บน Canvas

#### 2.4 การแปลภาษา (Localization)
- เพิ่มคำแปลสำหรับคำสั่งใหม่ใน `Strings.axaml` และ `Strings.th-TH.axaml`:
  - `command.note.duplicate`: "Duplicate note(s)" / "ทำซ้ำโน้ต"
  - `command.part.duplicate`: "Duplicate part(s)" / "ทำซ้ำพาร์ท"

---

### 3. ผลการตรวจสอบและยืนยัน (Verification & Audit)

| การทดสอบ (Test Case) | วิธีการ (Method) | ผลลัพธ์ (Result) |
| :--- | :--- | :--- |
| **Solution Full Build** | `dotnet build "Thai OpenUtau.sln"` | ✅ ผ่านฉลุย (**0 Warnings, 0 Errors**) |
| **Core & Classic Unit Tests** | `dotnet test --filter "Core\|Classic"` | ✅ ผ่าน 100% (**103 Passed, 0 Failed, 0 Skipped**) |
| **Diagnostic Log Audit** | ตรวจสอบไฟล์ใน `C:\Users\patip\OneDrive\Documents\OpenUtau\Logs` ตาม Rule 32 | ✅ ระบบคืน Cache และปิดตัวอย่างสมบูรณ์แบบ ไร้ Unhandled Exceptions |

---

*Made And Checked By DELTA SYNTH & Gemini AI*
