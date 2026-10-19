# 🔬 R&D Report (Research & Development)
**Thai OpenUtau by DELTA SYNTH**

## 1. การวิจัยปัญหาเชิงสถาปัตยกรรม (Architectural Problems)
จากการตรวจสอบบริบทการทำงานและ Log รันไทม์จริง พบปัญหาหลักดังนี้:
1. **Startup Stalling จาก Process Enumeration:**
   * **Root Cause:** ใน `Program.cs` โค้ดเดิมใช้ `Process.GetProcessesByName(processName)` ซึ่งบนระบบปฏิบัติการ Windows คำสั่งนี้จำเป็นต้องวนลูปตรวจสอบทุก Process ในระดับระบบปฏิบัติการ ส่งผลให้เกิดความล่าช้าหลายวินาทีในจังหวะเปิดโปรแกรม
   * **Resolution:** เปลี่ยนมาใช้ **Kernel-level Named Mutex** (`ThaiOpenUtau_SingleInstance_{User}`) ซึ่งใช้เวลาเพียงระดับไมโครวินาทีในการตรวจจับอินสแตนซ์ที่ซ้ำซ้อน รวดเร็ว แม่นยำ และปลอดภัยสูงสุด
2. **Ghost Background Processes & Exit Hanging:**
   * **Root Cause:** เมื่อผู้ใช้สั่งปิดหน้าต่างหลัก `MainWindow` หากมีหน้าต่างแยก (`pianoRollWindow`) หรือหน้าต่าง Dialog เปิดค้างอยู่ ตัวโปรแกรมจะไม่ยอมสิ้นสุดการทำงาน นอกจากนี้ Thread สังเคราะห์เสียงและ DispatcherTimer ยังคงทำงานต่อ ทำให้ Process ค้างในหน่วยความจำ และเมื่อผู้ใช้พยายามเปิดโปรแกรมใหม่จะขึ้นข้อความ `Process already open. Exiting.`
   * **Resolution:** เสริมขั้นตอน Graceful Shutdown ใน `WindowClosing`: สั่งหยุด `PlaybackManager.Inst.StopPlayback()` คืน Audio Hardware ทันที, หยุด `timer` และ `autosaveTimer`, สั่งปิดหน้าต่างบริวารทุกบาน (`pianoRollWindow.Close()`), บันทึก Preferences อย่างปลอดภัย และสั่ง `IControlledApplicationLifetime.Shutdown(0)` พร้อม `Environment.Exit(0)` ป้องกันเธรดตกค้าง 100%
3. **SplashWindow Serial Audio Initialization:**
   * **Root Cause:** `InitAudio()` ทำงานต่อท้ายหลังจาก Tools และ Plugins ทำงานเสร็จ ทำให้ต้องรอ Audio Driver Query (DirectSound COM) เพิ่มเติม
   * **Resolution:** แตกงาน `InitAudio()` ไปรันแบบขนาน (`tAudio`) ร่วมกับ `ToolsManager` และ `DocManager` พร้อมตัด `Thread.Sleep(100)` ที่หน่วงโดยไม่จำเป็นออก ทำให้เข้าสู่หน้าต่างหลักได้อย่างรวดเร็วทันใจ

## 2. แผนการพัฒนาความเร็วและความเสถียรเชิงรูปธรรม (Concrete Optimization Plan)
1. **Snappy Startup (เข้าโปรแกรมเร็วที่สุด):** 
   * ใช้ Named Mutex สำหรับ Single-instance Check
   * ผสาน Asynchronous & Parallel Task Scheduling ใน `SplashWindow` (Tools + Singers + DocManager + Audio)
2. **Graceful & Safe Shutdown (ปิดโปรแกรมทันใจ ปลอดภัยทุกการบันทึก):** 
   * เมื่อผู้ใช้สั่งปิด มีการเตือนบันทึกงานหากยังไม่บันทึก (`AskIfSaveAndContinue`)
   * บันทึกขนาดและพิกัดหน้าต่างลง `Preferences.Default.MainWindowSize` ก่อนปิด
   * ยกเลิก Audio Buffer และ Background Timers ทั้งหมด
   * รับประกันว่า Process คืนทรัพยากร RAM/CPU แก่ OS ทันทีที่หน้าต่างปิด
3. **C++ Native Extensions (โลกของ DSP):**
   * โมดูล C++ ทั้ง 37 ไฟล์ที่ทำการ Mapping ไว้ในระบบจะเป็นตัวขับเคลื่อนประสิทธิภาพ (Performance-Critical) โดยคงสถาปัตยกรรมเดิมไว้ และผสานการทำงานกับ C# Audio Engine ได้อย่างลงตัว
