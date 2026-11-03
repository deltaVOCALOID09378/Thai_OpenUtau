# บันทึกประวัติการพัฒนาและปรับปรุงระบบ Thai OpenUtau (History Log)
**วันที่:** 3 พฤศจิกายน พ.ศ. 2569 (2026-11-03)  
**เวอร์ชัน:** `v3.9.3` (Enterprise Screen Recording, Dynamic Data Relocation & Responsive Architecture Master Edition)  
**เครดิต:** Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]  
**ตำแหน่งไฟล์:** `History/History-03.11.2026.3.9.3.md`  

---

## 1. วัตถุประสงค์ของการดำเนินงาน (Objectives)
1. **ระบบบันทึกภาพหน้าจอและถ่ายคลิปวิดีโอระดับมืออาชีพ (Advanced Screen Capture & Video Recording Engine):**
   - รองรับการถ่ายภาพหน้าจอ (Screenshot Lossless PNG) และบันทึกวิดีโอ (Video Recording MP4 / AVI / MKV) เจาะจงเฉพาะหน้าต่างโปรแกรม/Piano Roll เท่านั้น
   - รองรับระดับความละเอียดครอบคลุมตั้งแต่ **240p, 480p, 720p, 1080p, 2K (1440p) จนถึง 4K (2160p)**
   - บนหน้า Piano Roll เพิ่มการแสดงระยะเวลาการถ่ายคลิปวิดีโอแบบเรียลไทม์ (`00:00`) บริเวณ Floating Bar
   - **กลไก Visual Exclusion ก่อนเรนเดอร์เฟรม (`BeforeRenderFrame` / `AfterRenderFrame`):** ป้องกันไม่ให้ปุ่มกล้อง ปุ่มอัดวิดีโอ และตัวเลขจับเวลาบันทึก ปรากฏเข้าไปในภาพหรือคลิปวิดีโอที่ส่งออกอย่างเด็ดขาด (ภาพและวิดีโอที่ได้จะสะอาดหมดจดเฉพาะเนื้อหาของโปรแกรม)
   - ปรับแต่งหน้าการตั้งค่า `PreferencesRecordPane.axaml` และ `PreferencesDialog.axaml` ให้มีหน้าต่างควบคุมเฉพาะทาง รองรับการเลือกไดเรกทอรีจัดเก็บไฟล์ภาพและวิดีโอได้อย่างอิสระ พร้อมปุ่ม Reset คืนค่าเริ่มต้น
2. **ระบบเลือกและย้ายที่อยู่ฐานข้อมูลผู้ใช้งานเดิมอย่างอิสระ (User Database & Data Path Relocation Architecture):**
   - พัฒนากลไก Pointer File (`datapath.txt`) ภายนอกไดเรกทอรีโปรแกรม ในโฟลเดอร์ LocalAppData เพื่อให้สามารถเก็บจำตำแหน่งฐานข้อมูลผู้ใช้ได้ข้ามการติดตั้งวินโด้วส์ใหม่หรือการกู้คืนข้อมูล
   - เพิ่ม UI ในแท็บ Paths ของ `PreferencesDialog.axaml`: แสดงที่อยู่ปัจจุบัน, ปุ่มเปิดโฟลเดอร์ (`Open`), ปุ่มเลือกตำแหน่งใหม่ (`Select`), และปุ่มคืนค่าเริ่มต้น (`Reset`)
   - ปรับปรุง `PathManager.cs` และ `PreferencesViewModel.cs` ให้รองรับการอ่านและกำหนดตำแหน่ง User Data Path โดยอัตโนมัติ
3. **การปรับปรุงการตอบสนองของปุ่มกดทุกจุดในโปรแกรม (< 1 วินาที):**
   - ปรับสถาปัตยกรรมการประมวลผลของปุ่มกด เมนู และฟังก์ชันคำสั่งทั้งหมดให้ทำงานแบบ Non-blocking Async ผ่าน `DispatcherPriority.Render` และ `Task.Run`
   - คัดแยกงาน Background I/O และ Video Encoding ออกจาก UI Thread 100% ทำให้ปุ่มกดตอบสนองทันทีที่คลิก ไร้อาการหน่วงหรือกระตุก
4. **หน้าต่างป๊อปอัพรายงานสถานะการโหลดข้อมูลพร้อมปุ่มกากบาท (X) (Smart Loading Popup Dialog):**
   - ยกระดับ `LoadingWindow.axaml` เพิ่มปุ่มกากบาท `X` ที่มุมขวาบนของขอบหน้าต่าง เพื่อให้ผู้ใช้สามารถปิดหน้าต่างหรือทราบว่าโปรแกรมไม่ได้ค้าง
   - เพิ่มระบบรายงานเหตุผลและวัตถุประสงค์ในการทำงาน เช่น:
     - `ระบบกำลังโหลดข้อมูลนักร้องและเสียงพากย์...`
     - `ระบบกำลังโหลดข้อมูลโปรเจกต์และแทร็กเสียง...`
     - `ระบบกำลังโหลดข้อมูลการตั้งค่า Oto.ini...`
     - `ระบบกำลังประมวลผลคำสั่งปลั๊กอิน...`
   - เพิ่มข้อความปิดท้ายบรรทัดล่างสุดตามข้อกำหนด: `กรุณา รอสักครู่..........` ทุกครั้ง
   - ตั้งค่าให้หน้าต่างป๊อปอัพอันตรธานหายไปทันทีที่การจัดเตรียมข้อมูลเสร็จสมบูรณ์
5. **ระบบการแจ้งเตือนสองภาษาแบบโปร่งใส ครอบคลุมทุกเหตุการณ์ (Transparent & Adaptive Toast Notification System):**
   - ขยายระยะเวลาแสดงผล Toast สำหรับข้อผิดพลาด (`Error` / `Fatal`) เป็น 5 วินาที เพื่อให้ผู้ใช้มีเวลาอ่านและเข้าใจสาเหตุได้อย่างถี่ถ้วน
   - พัฒนาระบบแปลความหมายเชิงเทคนิค (`ClarifyMessage`) ให้เป็นข้อความภาษาไทยที่สุภาพ ชัดเจน และมีคำแนะนำแนวทางแก้ไขถัดไป
   - เชื่อมต่อ `UserMessageNotification` เข้ากับ `ToastViewModel` ทำให้การกระทำสำคัญทุกอย่างแจ้งเตือนบนหน้าจอทันทีโดยไม่ต้องเปิดไฟล์ Log
6. **การกำหนดค่าดีบั๊กของโปรเจกต์ใน `.vscode` ครอบคลุมทุกสกุลไฟล์ (Full Multi-Language VSCode Debug Suite):**
   - ปรับปรุง `.vscode/launch.json` อัปเกรดเป็น v2.0 ครอบคลุม .NET 8 CoreCLR, Deep Debug, Release Verify, Unit Tests, Python (Current File, Thai G2P, Fixes Script), และ C++ Native Debug
   - สร้าง `.vscode/tasks.json` กำหนดคำสั่ง `build`, `clean`, `test`, `publish` อย่างเป็นแบบแผน ไร้คำเตือน
   - แก้ไข `.vscode/settings.json` ขจัดพาธตกค้าง `b:\` และกำหนดค่าโซลูชัน `Thai OpenUtau.sln` แบบ Portable ผ่าน `${workspaceFolder}`

---

## 2. รายการไฟล์ที่ได้รับการแก้ไขและสร้างใหม่ (Files Changed)
1. `OpenUtau.Core/Util/PathManager.cs` — เพิ่มระบบจัดการ `CustomDataPathFile`, `ReadCustomDataPath()`, `SetCustomDataPath()`
2. `OpenUtau/ViewModels/PreferencesViewModel.cs` — เพิ่ม Properties `UserDataPath`, `IsCustomUserDataPath`, และเมธอด `SetUserDataPath()`, `ResetUserDataPath()`
3. `OpenUtau/Views/PreferencesDialog.axaml` — เพิ่ม UI ส่วนจัดการตำแหน่ง User Data Path ในหน้าต่างการตั้งค่า
4. `OpenUtau/Views/PreferencesDialog.axaml.cs` — เพิ่มอีเวนต์แฮนด์เลอร์ `OpenUserDataFolder`, `ResetUserDataPath`, `SelectUserDataPath`
5. `OpenUtau/Views/PreferencesRecordPane.axaml` — ปรับปรุง UI ให้รองรับคุณภาพ 240p - 4K และการตั้งค่าโฟลเดอร์จัดเก็บ
6. `OpenUtau/Capture/ScreenCaptureService.cs` — เพิ่ม `BeforeRenderFrame`, `AfterRenderFrame`, `OnRecordingDurationTick`, และ `Stopwatch`
7. `OpenUtau/Controls/PianoRoll.axaml` — กำหนดชื่อ `CaptureToolbarBorder` และเพิ่ม `CaptureDurationText` แสดงระยะเวลาการอัดคลิป
8. `OpenUtau/Controls/PianoRoll.axaml.cs` — เชื่อมโยง Hooks ยกเว้นการวาด Toolbar ลงในภาพ/วิดีโอ และอัปเดตระยะเวลาแบบเรียลไทม์
9. `OpenUtau/Views/LoadingWindow.axaml` — เพิ่มปุ่มกากบาท (X) ปิดหน้าต่างป๊อปอัพ และปรับสไตล์ให้ทันสมัย
10. `OpenUtau/Views/LoadingWindow.axaml.cs` — เพิ่ม `OnCloseButtonClick`, `FormatReason`, `SetLoadingStatus` พร้อมข้อความปิดท้าย "กรุณา รอสักครู่.........."
11. `OpenUtau/Views/MainWindow.axaml.cs` — เชื่อมต่อการแจ้งเตือน `LoadingNotification` และการระบุเหตุผลการโหลด
12. `OpenUtau/Views/SingersDialog.axaml.cs` — ปรับแต่งการระบุเหตุผลการโหลดคลังเสียง
13. `OpenUtau/ViewModels/ToastViewModel.cs` — ปรับปรุงเวลาแสดงผล Toast และฟังก์ชัน `ClarifyMessage`
14. `OpenUtau/ViewModels/MainWindowViewModel.cs` — ส่งต่อ `UserMessageNotification` สู่ระบบ Toast
15. `OpenUtau/Strings/Strings.axaml` & `Strings.th-TH.axaml` — เพิ่มคำแปลตำแหน่งฐานข้อมูลผู้ใช้สองภาษา
16. `.vscode/launch.json` — คอนฟิกดีบั๊กครอบคลุมทุกภาษา (.NET, Python, C++, Tests) ไร้คำเตือน
17. `.vscode/tasks.json` — Tasks มาตรฐานสำหรับ Build, Rebuild, Test, Clean
18. `.vscode/settings.json` — แก้ไข Path ไดรฟ์ตกค้างให้เป็น `${workspaceFolder}`
19. `Tools/apply_all_user_requirements.py` — สคริปต์อัตโนมัติสำหรับการบูรณาการโค้ด
20. `History/History-03.11.2026.3.9.3.md` — บันทึกประวัติการทำงานฉบับเต็ม

---

## 3. สรุปผลการตรวจสอบ (Verification & Stability Report)
- [x] ยืนยันการทำงานของระบบถ่ายภาพและบันทึกวิดีโอ: ปุ่มกดและตัวเลขเวลาจะไม่ปรากฏในไฟล์วิดีโอ/ภาพที่ส่งออก 100%
- [x] ยืนยันระบบย้าย/กู้คืนฐานข้อมูลผู้ใช้: รองรับการเลือกโฟลเดอร์ใหม่ได้อย่างอิสระเมื่อลงวินโด้วส์ใหม่หรือย้ายไดรฟ์
- [x] ยืนยันหน้าต่างป๊อปอัพโหลด: แสดงเหตุผลชัดเจน มีปุ่มกากบาท X และข้อความ "กรุณา รอสักครู่.........." พร้อมปิดตัวทันทีเมื่อโหลดเสร็จ
- [x] ยืนยันระบบ Toast: แสดงผลชัดเจน โปร่งใส และขยายเวลาอ่านข้อความผิดพลาด
- [x] ยืนยันคอนฟิก `.vscode`: ครอบคลุมทุกสกุลไฟล์และปราศจากข้อผิดพลาด
