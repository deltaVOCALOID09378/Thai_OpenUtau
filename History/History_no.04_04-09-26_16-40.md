# DELTA SYNTH — รายงานสรุปผลการตรวจสอบและปรับปรุงระบบการเลือกและเครื่องมือเมาส์ใน Piano Roll
**Document ID:** `History_no.04_04-09-26_16-40.md`  
**Date & Time:** 4 กันยายน 2026 | 16:40 น.  
**Project:** Thai OpenUtau (DELTA SYNTH Edition)  
**Solution:** `Thai OpenUtau.sln`  
**Author/Architect:** Antigravity AI Systems Engineer (Pair Programming with นายท่านเดลต้า)

---

### 1. ภาพรวมการดำเนินงาน (Executive Summary)
ดำเนินการตรวจสอบโครงสร้างระบบการโต้ตอบด้วยเมาส์ (Mouse Selection, Pointer Capture, Pen Drawing, Eraser, Marquee Box Selection) ในส่วนของ **Piano Roll** และ Canvases ทั้งหมดของโปรแกรม เพื่อปลดล็อกข้อจำกัดในการลากเมาส์คลุมดำ (Marquee Selection) การวาดโน้ตด้วยปากกา (Pen Tool) และการลบ (Eraser Tool) ให้สามารถทำงานได้อย่างอิสระ ลื่นไหล ไร้รอยสะดุด และครอบคลุมทั่วทุกมิลลิเมตรของพื้นที่หน้าจอ

---

### 2. รายละเอียดการปรับปรุงเชิงลึก (Detailed System Changes)

#### 2.1 ปลดล็อกการลากคลุมดำอิสระแบบพิกเซล (`OpenUtau/Views/NoteEditStates.cs` - `NoteSelectionEditState`)
- **Pixel-Accurate Selection Box:** ปรับเปลี่ยนการคำนวณตำแหน่งและขนาดของกล่องคลุมดำ (`SelectionBox`) จากเดิมที่เคยล็อกกับเส้น Grid Snap Line ก้าวกระโดด ให้ใช้พิกัดจุดสัมผัสจริงของเคอร์เซอร์เมาส์ (`Math.Min(startPoint.X, point.X)` และ `Math.Abs(point.X - startPoint.X)`) ทำให้กล่องคลุมดำขยายตัวและหดตัวตามเมาส์ได้อย่างละเอียด นุ่มนวล และแม่นยำทุกมิลลิเมตร
- **Unconstrained Note Range Selection:** คำนวณช่วง Tick และ Tone ของโน้ตที่ถูกเลือก (`minTick`, `maxTick`, `y0`, `y1`) โดยตรงจากพิกัดตำแหน่งเมาส์ ปราศจากการหน่วงหรือกักขอบเขตที่ขอบล่าง/บน/ซ้าย/ขวา
- **Additive Selection Support:** รองรับการกดปุ่ม `Shift` เพื่อคลุมดำเลือกโน้ตเพิ่มต่อเนื่องโดยไม่ล้างรายการโน้ตเดิมที่เลือกไว้

#### 2.2 ปลดล็อกการวาดโน้ตด้วยปากกา (`OpenUtau/Controls/PianoRoll.axaml.cs` & `NoteDrawEditState`)
- **Direct Pen Drag-to-Draw:** เชื่อมต่อสถานะ `NoteDrawEditState` เข้ากับ `NotesCanvasLeftPointerPressed` เมื่อผู้ใช้เลือกเครื่องมือปากกา (`PenTool` หรือ `PenPlusTool`) ทำให้การคลิกแล้วลากเมาส์บนพื้นที่ว่างจะเป็นการสร้างโน้ตใหม่พร้อมยืดความยาวโน้ตตามการลากเมาส์ได้ทันที มีเสียงพรีวิวตัวโน้ต และกล่อง Tooltip บอกความยาวโน้ตแบบ Real-time
- **Smooth Eraser (`NoteEraseEditState`):** เพิ่มการตรวจสอบการลบ (`EraseAt`) ทั้งในขั้นตอน `Begin` (คลิกเดี่ยวลบโน้ตทันที) และขั้นตอน `Update` (ลากเมาส์ผ่านเพื่อลบโน้ตหลายตัวต่อเนื่อง)
- **Immediate Pitch Reset (`ResetPitchState`):** เพิ่มการรีเซ็ตพิตช์ทันทีเมื่อคลิกขวาในโหมดพิตช์ และลากเมาส์เพื่อรีเซ็ตพิตช์ตลอดเส้นทาง

#### 2.3 ปรับปรุงการตอบสนองของเคอร์เซอร์เมาส์ (Cursor Feedback)
- แสดงรูปเคอร์เซอร์ Crosshair (`cursorCross`) เมื่อชี้บนพื้นที่ว่างในโหมด Cursor Tool, Pen Tool หรือเมื่อกดปุ่ม `Ctrl`/`Cmd`
- แสดงรูปเคอร์เซอร์ห้าม (`cursorNo`) เมื่อเลือกโหมด Eraser
- แสดงเคอร์เซอร์ลูกศรสองทิศทาง (`cursorSizeWE`, `cursorSizeNS`) และมือจับ (`cursorHand`, `cursorSizeAll`) ได้อย่างถูกต้องตามบริบท

---

### 3. ผลการตรวจสอบและยืนยัน (Verification & Audit)

| การทดสอบ (Test Case) | วิธีการ (Method) | ผลลัพธ์ (Result) |
| :--- | :--- | :--- |
| **Solution Full Build** | `dotnet build "Thai OpenUtau.sln"` | ✅ ผ่าน (`0 Warning`, `0 Error`) |
| **Release Artifact Publish** | `dotnet publish "OpenUtau.csproj" -c Release -o "Published\Version no. 2.0.1"` | ✅ สำเร็จ 100% |
| **Asset & Dictionaries Sync** | ซิงค์แฟ้ม `Dictionaries` และ `runtimes` เข้าสู่ `Version no. 2.0.1` | ✅ พร้อมใช้งานทันที (Immediately Ready to Use) |
| **Runtime Log Verification** | ตรวจสอบไฟล์บันทึกการทำงานในระบบ | ✅ ไร้ข้อผิดพลาด ปราศจาก Crash |

---

*Made And Checked By DELTA SYNTH & Gemini AI*
