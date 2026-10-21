# DELTA SYNTH - OpenUtau Full Audit & Upstream/Origin Synchronization Update

**Date:** 13.10.2026  
**Version:** 1.1  
**Author:** DELTA SYNTH & All Code Agentic AI Engine  

---

## 📖 ภาพรวมการอัปเดต (Overview)
การตรวจสอบ วิเคราะห์ ปรับปรุง และประสานโค้ดครั้งใหญ่นี้ มุ่งเน้นการหลอมรวมการปรับปรุงล่าสุดจากทั้งทางฝั่งส่วนกลาง (Official Upstream `openutau/OpenUtau`) และฝั่ง DELTA SYNTH (`deltaVOCALOID09378/Thai_OpenUtau`) โดยขจัดปัญหาข้อขัดข้อง คำเตือน และจุดบกพร่องทุกประการจนเป็นศูนย์ (Zero Known Defects: 0 Error, 0 Warning) พร้อมผ่านการทดสอบ Unit Test ครบถ้วน 100% (282/282 ผ่านทั้งหมด)

---

## 🛠️ รายละเอียดการแก้ไขและปรับสมดุลระบบ (Key Changes & Root Causes)

### 1. การแก้ไขปัญหาความเข้ากันได้ของ NuGet และแพ็กเกจ Avalonia UI
- **ไฟล์:** `OpenUtau\OpenUtau.csproj`
- **สาเหตุ:** การระบุเวอร์ชันแบบลอยตัวและการใช้ `AliasSystemReactive` ส่งผลให้ระบบ Type Forwarding ของ Avalonia ขัดแย้งกับ System.Reactive รุ่นใหม่ ก่อให้เกิดข้อผิดพลาดกว่า 1,000 จุด
- **การแก้ไข:** ปรับใช้แพ็กเกจที่เสถียร สอดคล้องตรงกันข้ามทั้งโซลูชัน (`Avalonia 11.2.4`, `ReactiveUI.Fody 19.5.41`, `Avalonia.ReactiveUI 11.2.4`, `Tmds.DBus.Protocol 0.94.2`) และตัด Alias ที่ซ้ำซ้อนออก

### 2. การสร้างและเชื่อมต่อระบบแจ้งเตือนข้อความบน Status Bar
- **ไฟล์:** `OpenUtau.Core\Commands\Notifications.cs`, `OpenUtau\ViewModels\MainWindowViewModel.cs`
- **สาเหตุ:** โมดูล ScreenCaptureService มีการเรียกใช้ `UserMessageNotification` ซึ่งยังไม่ได้รับการประกาศใน Core
- **การแก้ไข:** ประกาศคลาส `UserMessageNotification` ใน `Notifications.cs` และเพิ่มกลไกรับคำสั่งใน `MainWindowViewModel` พร้อมระบบนับเวลาล้างข้อความอัตโนมัติ

### 3. การเพิ่มการตั้งค่าสำหรับระบบบันทึกหน้าจอ (Screen Capture Preferences)
- **ไฟล์:** `OpenUtau.Core\Util\Preferences.cs`
- **การแก้ไข:** เพิ่มฟิลด์การบันทึกหน้าจอ (`CaptureExportLocation`, `CaptureFilenameFormat`, `CaptureVideoFormat`, `CaptureResolution`, `CaptureCountdown`) ลงใน `SerializablePreferences`

### 4. การจัดการหน่วยความจำของ ONNX Inference Session
- **ไฟล์:** `OpenUtau.Core\Util\Onnx.cs`
- **สาเหตุ:** Unmanaged Handle ของ `SessionOptions` อาจถูก Garbage Collector ของ .NET เก็บกวาดก่อนที่ตัวโมเดลจะโหลดเสร็จ
- **การแก้ไข:** ใช้ `using var options = getOnnxSessionOptions();` เพื่อควบคุมรอบอายุของ Handle ให้ถูกต้องตามมาตรฐาน C#

### 5. การแก้ไข Unit Test และการรองรับคลังเสียง (Classic & DiffSinger)
- **ไฟล์:** `OpenUtau.Core\Classic\Flags\UstFlagParser.cs`: รองรับสัญลักษณ์ `/` ในชุด Flag
- **ไฟล์:** `OpenUtau.Core\Classic\Presamp.cs`: ทำการโคลนคอลเลกชันค่าตั้งต้นเพื่อป้องกัน State Contamination ข้ามอินสแตนซ์ และรองรับเซกชัน `[CFLAGS]` ว่าง
- **ไฟล์:** `OpenUtau.Core\Classic\ClassicSinger.cs`: ปรับกระบวนการโหลด Collection ให้เป็นการสลับอ้างอิงระดับอะตอม (Atomic Reference Swap) ป้องกัน Race Condition ขณะอ่านแบบขนาน
- **ไฟล์:** `OpenUtau.Core\DeltaSingers\SingerHubClient.cs`: ตรวจจับ Credits ของเสียงรองรับทั้ง `DELTA SYNTH` และ `LUNAI`
- **ไฟล์:** `OpenUtau.Core\OpenUtau.Core.csproj`: ฝังทรัพยากร `DiffSinger\Data\**` (`lunai-dsunvoiced.yaml`)
- **ไฟล์:** `OpenUtau.Plugin.Builtin\BaseKoreanPhonemizer.cs`: ปรับแก้การเรียกใช้ Boolean ให้ถูกต้อง
- **ไฟล์:** `OpenUtau.Plugin.Builtin\EnXSampaPhonemizer.cs`: ลบ Label ที่ไม่ได้ถูกเรียกใช้งาน
- **ไฟล์:** `OpenUtau\ViewModels\PianoRollViewModel.cs`: เพิ่ม Property `PitchPointTool => ToolIndex == 3;`

### 6. การปรับปรุง Avalonia XAML Compatibility
- **ไฟล์:** `PianoRoll.axaml.cs`, `GenerateHarmonyDialog.axaml.cs`, `PianoRollDetachedWindow.axaml.cs`, `ThemeEditorWindow.axaml.cs`
- **การแก้ไข:** เพิ่ม Parameterless Constructor ป้องกันคำเตือน AVLN3001 จาก Avalonia XAML Compiler

---

## 🧪 ผลการทดสอบและการตรวจสอบคุณภาพ (Verification Results)
1. **Compilation (Release Mode):**
   - `OpenUtau.Core`: 0 Warning(s), 0 Error(s)
   - `OpenUtau.Plugin.Builtin`: 0 Warning(s), 0 Error(s)
   - `OpenUtau`: 0 Warning(s), 0 Error(s)
   - `OpenUtau.Test`: 0 Warning(s), 0 Error(s)
   - `Thai OpenUtau.sln`: 0 Warning(s), 0 Error(s)
2. **Unit Tests (OpenUtau.Test Suite):**
   - ผ่านการทดสอบทั้งหมด 282 / 282 รายการ (100% Pass, 0 Failed, 0 Skipped)
