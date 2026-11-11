# ══════════════════════════════════════

# 📋 About-Project — OpenUtau.Plugin.Builtin by DELTA SYNTH

# ══════════════════════════════════════

> ### ⚠️ คำสั่งบังคับและกฎเหล็กประจำโปรเจกต์ (Mandatory Enforcements)
>
> 1. **Preserve Before Replace:** ห้ามลบหรือ Rewrite โค้ดเดิมที่พิสูจน์แล้วว่าทำงานได้ดี ให้ใช้แนวทาง Surgical Edits แก้ที่ Root Cause ตรงจุดเท่านั้น
>
> 2. **Enterprise Zero-Defect:** โค้ดทุกไฟล์และทุกการคอมไพล์ต้องปราศจากข้อผิดพลาดและคำเตือน (0 Errors, 0 Warnings, 100% Passed Builds)
>
> 3. **Phoneme Timing Safety:** รักษาสัดส่วนหน่วยเสียงมาตรฐาน (CV ≥ 60%, VC 33–35%, CCV/C_C ≤ 5–7%, V- 20–30%) และระยะห่างความปลอดภัยของเวลา (`safePos = Math.Max(originalPos, prevPos + 10)`) เสมอ
>
> 4. **No Type-Ignore & No Exception Swallowing:** ห้ามใช้ `# type: ignore` หรือกลืน Exception เปล่า ต้องดักจับและส่งต่อบริบท error ที่ชัดเจน
>
> 5. **Cross-Lingual & Dictionary Integrity:** พจนานุกรมคำอ่านภาษาไทยและภาษาอื่น (`English-ARP.txt`) ต้องโหลดครบ 100% คำว่า `"a"` ต้องร้องเป็นหน่วยเสียง `aa` ตามมาตรฐาน และไม่ทำให้โปรแกรมแครชเมื่อพบคำศัพท์ที่ไม่มีในระบบ
>
> 6. **Clean Archive Separation:** ไฟล์ทดสอบ, สคริปต์ และไฟล์ที่เลิกใช้แล้วต้องเก็บใน `AI_Agent_Commands_And_Archive/` และถูก Exclude ออกจากการคอมไพล์โดยสมบูรณ์
>
> 7. **DELTA SYNTH UI Standard:** สำหรับคอมโพเนนต์หรือข้อความ UI ใช้โทนสี `#CC2200` (แดง), `#1A1A1A` (ดำ), `#F0F0F0` (ขาว) พร้อมฟอนต์ Leelawadee UI

## §0 — สถานะเบื้องต้น (Initial Status)

| ข้อมูลหลัก | ค่าที่กำหนด | 
| ----- | ----- | 
| **ชื่อโปรเจกต์** | OpenUtau.Plugin.Builtin — Thai & Multilingual Voice Synthesis Phonemizers | 
| **เวอร์ชันปัจจุบัน** | 3.7.1 (Enterprise Production Quality) | 
| **วันที่อัปเดตล่าสุด** | 10 พฤศจิกายน 2026 (10-11-2026) | 
| **แพลตฟอร์มเป้าหมาย** | Cross-Platform (Windows x64 Primary, macOS, Linux) | 
| **Target Framework** | .NET 8.0 (`net8.0`), C# 12 | 
| **ประเภทเอาต์พุต** | Class Library (`OpenUtau.Plugin.Builtin.dll`) | 
| **ตำแหน่งอ้างอิง Core** | `..\OpenUtau.Core\OpenUtau.Core.csproj` | 
| **สถานะการคอมไพล์** | 🟢 0 Warning(s), 0 Error(s) — Clean Build Verified | 

## §1 — ประเภทของโปรแกรม (Program Type)

โปรเจกต์นี้เป็น **Core Voice Synthesis Phonemizer Library (ปลั๊กอินแปลงโน้ตและเนื้อร้องเป็นหน่วยเสียง)** ของระบบ **Thai OpenUtau** ทำหน้าที่วิเคราะห์คำร้อง (Lyrics), สัญลักษณ์เสียง (G2P), ตัวโน้ตดนตรี (Notes & Pitches), และแปลงเป็นลำดับหน่วยเสียง (Phonemes) พร้อมกำหนดค่าออฟเซ็ตเวลา (Timing Offset), อัตราส่วนความยาวเสียงพยัญชนะ-สระ (Consonant/Vowel Ratios) ส่งต่อให้เอนจินเรนเดอร์เสียงสังเคราะห์ (Classic Resamplers, Worldline, Enunu, DiffSinger) ทำงานได้อย่างแม่นยำ ลื่นไหล และเป็นธรรมชาติที่สุด

## §2 — ที่มาและความสำคัญ (Origin)

1. **การเติมเต็มระบบเสียงภาษาไทยและสากลใน OpenUtau:** พัฒนาขึ้นโดย **DELTA SYNTH** เพื่อเป็นมาตรฐานกลางสำหรับเสียงไทยและสากล

2. **รองรับสถาปัตยกรรมเสียงหลากหลาย:** ครอบคลุมทั้ง Thai VCCV, Thai VCV, Thai CpV, Thai Arpasing, English Arpasing และระบบ Cross-lingual เช่น DeltaENtoJA

3. **การรักษาความปลอดภัยของเวลา (Timing Safety Algorithm):** ป้องกันปัญหาเสียงทับซ้อน (Phoneme Collision), เสียงกระตุก (Timing Glitch), และบั๊กการคำนวณตำแหน่งติดลบที่เคยเกิดขึ้นในระบบเดิม

## §3 — ส่วนประกอบและภาษา (Components)

| ภาษา / เทคโนโลยี | หน้าที่ในระบบ | 
| ----- | ----- | 
| **C# 12 (.NET 8.0)** | Logic หลักของ Phonemizers ทั้งหมด, การจัดการเวลา, และคำนวณสัดส่วนเสียง | 
| **YAML (YamlDotNet)** | คอนฟิกการแมปปิ้งหน่วยเสียง, กฎเกณฑ์การจัดเรียงเสียง (Phoneme Rules) | 
| **Python 3.10+** | สคริปต์บิลด์, สคริปต์สแกนคำเตือน, และตัวสร้างพจนานุกรม | 
| **PowerShell** | ไปป์ไลน์ตรวจสอบ, แพ็กเกจ และการรันคอมไพล์อัตโนมัติ | 

## §4 — ไฟล์วิกฤต (Critical Files)

| ไฟล์วิกฤต | เหตุผลที่ห้ามแตะต้องสุ่มสี่สุ่มห้า / ข้อควรระวัง | 
| ----- | ----- | 
| `SyllableBasedPhonemizer.cs` | บังคับใช้กฎเหล็กสัดส่วนเวลา (CV ≥ 60%, VC ≤ 33%, CC/C- ≤ 6%) และแก้การแปลงคำ `"a"` -> `aa`/`a` | 
| `ArpasingPhonemizer.cs` | ล็อกคำว่า `"a"` ให้ร้องเป็น `aa` เสมอ โหลดพจนานุกรม `English-ARP.txt` ครบ 100% | 
| `LatinDiphonePhonemizer.cs` | คลาสแม่แบบ Diphone พร้อมการตรวจสอบ Null-safety สำหรับ Singer และ Oto | 
| `ThaiBasePhonemizer_v1.2.0.cs` | หัวใจของระบบ Timing ภาษาไทย การเปลี่ยนตัวคูณอาจทำให้เสียงเพี้ยนทั้งระบบ | 
| `ThaiVCCVPhonemizer.cs` | กฎการประกอบเสียงสระ-พยัญชนะไทย ต้องคงความเข้ากันได้กับ VCCV Voicebank เดิม | 

## §5 — ประวัติ (Work History)

| เวอร์ชัน | วันที่ | สรุปการปรับปรุงและส่งมอบ | ผู้ดำเนินการ | 
| ----- | ----- | ----- | ----- | 
| **v1.0.0** | 28-06-2026 | สร้างโครงสร้างปลั๊กอิน Phonemizer ภาษาไทยและพจนานุกรมชุดแรก | DELTA SYNTH | 
| **v3.6.0** | 28-10-2026 | ซิงก์ Core Render Engine Cancellation Fix, กำจัด Duplicate Class, ยืนยัน 0 Errors 0 Warnings | DELTA SYNTH & AI Engine | 
| **v3.7.1** | 10-11-2026 | แก้ไขคำว่า `"a"` ในภาษาอังกฤษให้ร้อง `aa` ตามปกติใน `ArpasingPhonemizer.cs` (v2.3) และ `SyllableBasedPhonemizer.cs` (v2.4) | DELTA SYNTH & AI Engine | 

*เอกสารรับรองมาตรฐานโดย: DELTA SYNTH & All Code Agentic AI Engine (10 พฤศจิกายน 2026)*