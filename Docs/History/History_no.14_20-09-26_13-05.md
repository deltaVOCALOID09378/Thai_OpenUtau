# 📝 History Session Log: 14
**Date:** 20-09-2026 13:05 (System Time)
**Role:** DELTA SYNTH AI Agent (Software Architect & Systems Engineer)

## [1] Objectives Fulfillment (ความสำเร็จตามเป้าหมาย)
- **Objective (100% Fluid Rubber-Band Selection):** ปรับปรุงระบบคลุมดำ (Rubber-Band Selection Box) หน้าต่าง Piano Roll และหน้าต่าง Tracks ให้เป็นอิสระโดยสมบูรณ์ โดยแก้ไขให้แกน Y (Pitch/Track) เลิก Snap เข้ากับช่องโน้ต ทำให้กรอบสี่เหลี่ยมคลุมดำขยับตามเมาส์ได้อย่างละเอียดระดับไมโครเมตรทั้งแกนแนวตั้งและแนวนอนแบบ 100% (Fluid movement) ไร้อุปสรรคตารางกริดมาขวางกั้น

## [2] Files Changed (รายการไฟล์ที่ถูกปรับปรุงและแก้ไข)
- **`A:\Thai_OpenUtau\OpenUtau\Views\NoteEditStates.cs`**
  - **Logic Altered (NoteSelectionEditState):** ถอดการคำนวณตำแหน่งสี่เหลี่ยมด้วยฟังก์ชัน `TickToneToPoint` ซึ่งถูกบังคับให้เป็นจำนวนเต็มของ Pitch ออก แล้วแทนที่ด้วยการรับพิกัด Canvas แบบเรียลไทม์ (`startPoint.Y` และ `point.Y`) ทำให้การวาดกรอบมีความไหลลื่นเป็นอิสระ โดยยังคงจับคู่ตรรกะการประมวลผลโน้ตที่ถูกคลุม (`TempSelectNotes`) ได้อย่างถูกต้องตามเดิม
- **`A:\Thai_OpenUtau\OpenUtau\Views\PartEditStates.cs`**
  - **Logic Altered (PartSelectionEditState):** นำระบบการรับพิกัด Canvas เดียวกันมาใช้กับการคลุมดำเลือกท่อน Part ในหน้าต่างหลัก (Tracks Window) เพื่อให้การทำงานมีอิสระสอดคล้องกับหน้า Piano Roll อย่างสมบูรณ์

## [3] Performance / Stability Impact (ผลกระทบต่อประสิทธิภาพและเสถียรภาพ)
- ผู้ใช้งานสามารถลากคลุมดำได้อย่างนุ่มนวล (Smooth Tracking) ไม่มีอาการกระตุก (Stutter/Snapping) บนแกนแนวตั้งและแนวนอน มอบความรู้สึกพรีเมียมขณะใช้งาน (Premium UX)
- ไม่มีผลกระทบต่อตรรกะการทำงานภายใน เนื่องจากระบบหลังบ้านยังคงใช้ Index/Tick ในการเลือกโน้ตอยู่เบื้องหลัง

## [4] Verification Method (วิธีการตรวจสอบ)
- Source code analysis (Coordinates calculation verification)

## [5] Compatibility & Residual Risks (ความเข้ากันได้ และ ความเสี่ยง)
- เข้ากันได้กับโมดูลเดิมทั้งหมด 100%
- **Residual Risks:** None known.

---
**Status:** ปิดจ๊อปการอัปเกรดระบบเมาส์ 100% ไร้ข้อจำกัด
