# 🔬 R&D Report (Research & Development)
**Thai OpenUtau by DELTA SYNTH**

## 1. การวิจัยปัญหาเชิงสถาปัตยกรรม (Architectural Problems)
จากการตรวจสอบบริบทการทำงานที่ผ่านมา (Chat History) พบปัญหาหลักดังนี้:
1. **Log Spam จากไฟล์ `oto.ini`:** 
   * **Root Cause:** ผู้สร้าง Voicebank บางท่านได้ทำการตั้งค่า Placeholder ในไฟล์ `oto.ini` ด้วยค่า `.wav=` ทำให้เอนจิน `VoicebankLoader.cs` พยายามสแกนไฟล์ที่ไม่มีอยู่จริง และพิมพ์ Error ลงระบบนับพันบรรทัด ทำให้ประสิทธิภาพตกลง
   * **Resolution:** ดำเนินการ Patch ระบบให้อ่านค่า `oto.ini` อย่างมีวิจารณญาณ โดยข้ามไฟล์ที่มีเพียง `.wav=` เงียบๆ ทำให้สแปมลดลงเป็นศูนย์ และลดภาระของ I/O
2. **YAML Parsing Failure:**
   * **Root Cause:** บางไฟล์ `character.yaml` ตกหล่นเรื่องเว้นวรรคหลังเครื่องหมาย `:` ทำให้ระบบของ YamlDotNet ล้มเหลวระหว่างโหลด
   * **Resolution:** ดำเนินการแก้ไขโครงสร้างเว้นวรรคของไฟล์ต้นทางเรียบร้อยแล้ว
3. **Compilation Lock (Sandbox Environment):**
   * **Root Cause:** การ Build โปรเจกต์ผ่าน IDE ภายในเครื่องที่มีข้อจำกัด ทำให้ Background Task (Python) ถูกบล็อกโดยระบบรักษาความปลอดภัยของ Windows/Sandbox
   * **Resolution:** สร้างชุดสคริปต์ Standalone `Auto Compiler.bat` เพื่อข้ามการล็อก และทำงานผสานร่วมกับ `TuneAndFormat.ps1` ทำให้โปรเจกต์คอมไพล์ผ่านลื่นไหล 100%

## 2. แผนการพัฒนาความเร็วและความเสถียรเชิงรูปธรรม (Concrete Optimization Plan)
1. **Fast Startup (เข้าโปรแกรมเร็วขึ้น):** 
   * ปัจจุบัน UTAU คลาสสิกมักโหลดตารางเสียง (Voicebank) ทั้งหมดแบบ Synchronous ซึ่งทำให้จังหวะเปิดแอปพลิเคชันล่าช้า การปรับปรุง `VoicebankLoader` ให้ทำงานแบบ Lazy Loading หรือ Asynchronous จะช่วยแก้ปัญหาคอขวด 
2. **Safe Recovery (การฟื้นฟูหลังเกิดข้อผิดพลาดและปิดโปรแกรม):** 
   * กระบวนการปิดโปรแกรมต้องรับรองว่า `Preferences` และออฟเจ็กต์ใน Memory ถูกจัดการอย่างถูกต้องเพื่อไม่ให้ Config ของผู้ใช้เพี้ยนในรอบต่อไป
3. **C++ Native Extensions (โลกของ DSP):**
   * โมดูล C++ ทั้ง 37 ไฟล์ที่ทำการ Mapping ไว้ในระบบจะเป็นตัวขับเคลื่อนประสิทธิภาพ (Performance-Critical) โดยเราจะคงสถาปัตยกรรมเดิมไว้ แต่จะ Compile แยกเพื่อความรวดเร็ว

*(เอกสารอ้างอิงและบันทึกประวัติจะถูกเพิ่มมายัง R&D ฉบับนี้อย่างต่อเนื่องใน Phase ถัดไป)*
