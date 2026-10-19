# DELTA SYNTH — รายงานสรุปผลการปฏิรูประบบ AutoPitch และปลดล็อคการเข้าถึงปลั๊กอินแบบไร้ขีดจำกัด (Unrestricted Tuning & Plugins)
**Document ID:** `History_no.07_12-09-26_19-45.md`  
**Date & Time:** 12 กันยายน 2026 | 19:45 น.  
**Project:** Thai OpenUtau (DELTA SYNTH Edition)  
**Solution:** `Thai OpenUtau.sln`  
**Author/Architect:** Antigravity AI Systems Engineer (Pair Programming with นายท่านเดลต้า)

---

### 1. ภาพรวมการดำเนินงาน (Executive Summary)
ดำเนินการแก้ไขและยกระดับระบบตามข้อสั่งการของนายท่านเดลต้า:
1. **ปฏิรูประบบ AutoPitch และ Macro การจูนเสียงใหม่ยกชุด:**
   - แก้ไขปัญหา Root Cause ของ AutoPitch ที่จูนเพี้ยนมั่วซั่วจากการคำนวณ Tick และสเกลความสูงต่ำของคีย์ (PitchPoint.Y) ผิดสัดส่วน (เดิมใช้สเกล 40-50 กลายเป็นกระโดดข้าม 4-5 ครึ่งเสียง ทำให้ร้องเพี้ยนรุนแรง)
   - ปรับการคำนวณทั้งหมดให้เป็นมิลลิวินาทีจริง (`project.timeAxis.MsBetweenTickPos`) และสเกล Y ตามมาตรฐาน OpenUtau (1.0 = 10 cents) เพื่อให้เส้น Pitch นุ่มนวล หวานละมุน เป็นธรรมชาติ สไตล์นักร้องมืออาชีพ
   - กำจัดบั๊ก Double Portamento ชนกันระหว่างการดัดโค้งด้วยตนเองกับระบบ `snapFirst = true` ของ OpenUtau
   - เพิ่มระบบ Phrase-start Attack Scoop (18 cents) และ Phrase-end Release Fall (20-25 cents) อย่างสมบูรณ์
2. **ปรับปรุงการเข้าถึงและใช้งานปลั๊กอิน / Batch Edits ให้ตอบสนองทันทีและไร้ขีดจำกัด:**
   - เพิ่มปุ่มลัด **Vocal Tuning Toolbar Chip** ด้านบนของ Piano Roll เพื่อให้กดเรียกใช้งานปลั๊กอินปรับแต่งเสียงร้องได้ทันทีในคลิกเดียว
   - เพิ่มเมนูย่อย **Vocal Tuning** ลงในเมนูหลัก **Batch** และ **Context Menu (คลิกขวา)** บน Canvas
   - ปลดล็อคข้อจำกัดการเลือก (Unrestricted Selection): หากผู้ใช้ไม่ได้เลือกโน้ตไว้ล่วงหน้า ระบบจะตรวจจับและ Fallback ไปประมวลผลโน้ตทั้งหมดในพาร์ทที่เปิดอยู่ทันที ไม่มีการนิ่งเฉยหรือไม่ตอบสนอง
   - ปลดล็อคตัวกรอง Renderer ใน `AutoPitchVibrato` ทำให้สามารถใช้งานได้กับ Voicebank ทุกประเภท (UTAU Classic, World, DiffSinger, Enunu ฯลฯ)
   - คลิกขวาบนพื้นที่ว่างของ Piano Roll สามารถเปิดเมนูบริบทหลักเพื่อเรียกใช้งาน Batch Edits และการจูนเสียงได้ทันที

---

### 2. รายละเอียดการปรับปรุงเชิงลึก (Detailed System Changes)

#### 2.1 ปฏิรูประบบ AutoPitch & การดัดโค้ง Pitch (`AutoPitchContextualEngine.cs`, `NoteBatchEdits.cs`, `ThaiVocalBatchEdits.cs`)
- **AutoPitchContextualEngine.cs (Revision 1.2):**
  - เปลี่ยนการคำนวณระยะเวลาจาก Tick เป็นมิลลิวินาทีจริง ป้องกันความผิดเพี้ยนเมื่อโปรเจกต์มี Tempo หรือ Time Signature เปลี่ยนแปลง
  - ปรับปรุงการคำนวณ Pitch Point Shapes: Thunderbolt, PitchDropRise, Slope, WShape, UPitchDrop, PitchDent ให้อยู่ในย่าน Micro-intonation (10-35 cents)
  - เพิ่ม Legato Adaptiveness: หากโน้ตติดกันจะเชื่อม Portamento ธรรมชาติ และไม่สร้างจุดดัดชนกับ `snapFirst`
- **AutoPitchRandomizerBatchEdit.cs (Revision 1.2):**
  - เพิ่ม Parameterless Constructor รองรับการ Discovery ผ่าน Reflection
  - ปรับ Oscillation ให้เป็น Subtle Intonation (Default 15 cents = 1.5 semitones) ไร้เสียงเพี้ยนสั่นกระตุก
- **VocalTuningMacro.cs (Revision 1.3) & ThaiVocalBatchEdits.cs (Revision 1.2):**
  - ขจัดปัญหา Double Portamento Collision โดยเว้นจุดสิ้นสุดให้ `snapFirst` ของโน้ตถัดไปทำงานร่วมกันอย่างกลมกลืน
  - เสริม Dynamic Swell (DYN Curve) และ Vibrato Ease-in อย่างเป็นธรรมชาติ
- **AutoMixBatchEdit.cs (Revision 1.2):**
  - เพิ่ม Empty-selection Fallback ให้ทำงานกับโน้ตทั้งพาร์ทอัตโนมัติ

#### 2.2 ปลดล็อค UI และการเข้าถึงปลั๊กอินบน Piano Roll (`PianoRoll.axaml`, `PianoRoll.axaml.cs`, `PianoRollViewModel.cs`)
- **PianoRoll.axaml:**
  - เพิ่ม MenuItem `pianoroll.menu.vocaltuning` ลงในเมนู `Batch`
  - เพิ่มปุ่ม Toolbar Chip พร้อม ContextMenu สำหรับ Vocal Tuning ข้างปุ่ม Snap Division
- **PianoRoll.axaml.cs (Revision 1.2):**
  - ปรับปรุง `noteBatchEditCommand` ให้ตรวจสอบ `notesVm.Selection.Count == 0` หากไม่มีการเลือก ให้ดึงโน้ตทั้งหมดจาก `part.notes` มาดำเนินการทันที
  - เพิ่มฟังก์ชัน `OnVocalTuningMenuButton`, `OpenAutoPitchwriterDialog`, และ `OpenAutoPitchRandomizerDialog`
  - อัปเดต ContextMenu เมื่อคลิกขวาบน Notes Canvas ให้เปิดเมนู Vocal Tuning และ Batch Edits ได้เสมอแม้คลิกบนพื้นที่ว่าง
- **Strings.axaml & Strings.th-TH.axaml:**
  - เพิ่มคำแปลภาษาไทยและภาษาอังกฤษสมบูรณ์: `pianoroll.menu.vocaltuning`, `pianoroll.menu.notes.vocaltuning`, `contextualautopitch`, `autopitchallin1`, `autopitchrandomizer`, `autopitchvibrato`, `alignconsonants`, `drawvocalmoodcurve`, `addvocalruns`, `automix`, `openautopitchwriter`, `openautopitchrandomizer`

---

### 3. ผลการตรวจสอบและยืนยัน (Verification & Quality Gates)

| การตรวจสอบ (Test / Audit) | คำสั่ง / วิธียืนยัน | ผลลัพธ์ (Result) |
| :--- | :--- | :--- |
| **OpenUtau.Core Build** | `dotnet build OpenUtau.Core/OpenUtau.Core.csproj` | ✅ ผ่านฉลุย (**0 Warnings, 0 Errors**) |
| **OpenUtau (App) Build** | `dotnet build OpenUtau/OpenUtau.csproj` | ✅ ผ่านฉลุย (**0 Warnings, 0 Errors**) |
| **AutoPitch Unit Tests** | `dotnet test --filter "FullyQualifiedName~AutoPitch"` | ✅ ผ่าน 100% (**9 Passed, 0 Failed, 0 Skipped**) |
| **Core & Editing Unit Tests**| `dotnet test --filter "FullyQualifiedName~Editing\|FullyQualifiedName~Core"` | ✅ ผ่าน 100% (**90 Passed, 0 Failed, 0 Skipped**) |
| **Diagnostic Log Audit** | ตรวจสอบ `C:\Users\patip\OneDrive\Documents\OpenUtau\Logs\log20260912.txt` | ✅ ผ่านข้อกำหนด Rule 32: ล้างแคชสมบูรณ์ ออกจากโปรแกรมสะอาด ไร้ Unhandled Exceptions |

---

### 4. สรุปรายการไฟล์ที่ได้รับการแก้ไข (Files Changed)

1. `OpenUtau.Core/Editing/AutoPitchContextualEngine.cs` (Revision 1.2)
2. `OpenUtau.Core/Editing/AutoPitchRandomizerBatchEdit.cs` (Revision 1.2)
3. `OpenUtau.Core/Editing/VocalTuningMacro.cs` (Revision 1.3)
4. `OpenUtau.Core/Editing/NoteBatchEdits.cs` (Revision 1.2)
5. `OpenUtau.Core/Editing/ThaiVocalBatchEdits.cs` (Revision 1.2)
6. `OpenUtau.Core/Editing/AutoMixBatchEdit.cs` (Revision 1.2)
7. `OpenUtau/ViewModels/PianoRollViewModel.cs`
8. `OpenUtau/Controls/PianoRoll.axaml`
9. `OpenUtau/Controls/PianoRoll.axaml.cs` (Revision 1.2)
10. `OpenUtau/Strings/Strings.axaml`
11. `OpenUtau/Strings/Strings.th-TH.axaml`
12. `History/History_no.07_12-09-26_19-45.md` (รายงานประวัติฉบับนี้)

---

*Made And Checked By DELTA SYNTH & Gemini AI*
