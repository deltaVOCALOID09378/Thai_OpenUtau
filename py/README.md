# G2P model

## Training

Example: to train en_us arpabet model.

- Place `cmudict-0.7b` file in `en_us` folder.
- Run commands:
```
cd py
pip install -r requirements.txt
python g2p/train.py
```

To train a different model:

- Create a new folder next to `en_us`. Place your dictionary in it.
- Copies `cfg.yaml` into it and modifies graphemes and phonemes to match your language. The first 4 graphemes and phonemes must be `<unk>`, `<pad>`, `<bos>`, `<eos>`.
- Modify `train.py` to load your config and dictionary.
- You may need to reformat your dictionary so that `SphinxDataset` can load it. Or you can write your own dataset class.
- You will need to tweak batch size and epochs (and maybe other parameters) for best results.

## Packing

A g2p pack zip file contains:
```
dict.txt
g2p.onnx
phones.txt
```

- The `G2pPack` class uses entries from `dict.txt` first.
- If not found, it uses `g2p.onnx` to generate phonemes.
- `phones.txt` allows phonemizers to know which phonemes are vowels and which phonemes to stretch.
---
*For compiling the G2p models read [Compiling-G2p-Mdels-Wiki](https://github.com/stakira/OpenUtau/wiki/Compiling-G2p-Models)*

---

# 🚀 ประวัติการพัฒนาและฟีเจอร์ใหม่ (Thai OpenUtau by DELTA SYNTH)
*อัปเดตล่าสุด: สิงหาคม 2026*

บทความนี้สรุปฟีเจอร์และการปรับปรุงทั้งหมดที่ได้ทำกับโปรเจกต์ **Thai OpenUtau by DELTA SYNTH** ตั้งแต่อดีตจนถึงปัจจุบัน เพื่อยกระดับให้เป็นซอฟต์แวร์สังเคราะห์เสียงที่สมบูรณ์แบบ ทันสมัย และตอบโจทย์ผู้ใช้งานสูงสุด

## 1. 🔧 ความเสถียรและระบบการบิลด์ (Build System & Stability)
* **Zero-Defect Compilation:** แก้ไขข้อผิดพลาด (Errors) และคำเตือน (Warnings) ทั้งหมดในโค้ดเบส จนสามารถรัน `dotnet build` ได้ผ่าน 100% โดยไม่มี Error
* **Crash Prevention:** แก้ไขปัญหาโปรแกรมเด้ง (Crash) ตอนเปิดใช้งาน ที่เกิดจากการเข้าถึงโฟลเดอร์ระบบ โดยเพิ่มระบบจัดการ Exception และ `IgnoreInaccessible`
* **Standalone Release & Installers:** สร้างระบบการ Publish แบบ Self-Contained พร้อมสคริปต์สำหรับการติดตั้ง (`ติดตั้ง Thai OpenUtau.bat`) และการรันสำหรับนักพัฒนา ทำให้แจกจ่ายโปรแกรมได้ง่ายและไม่ต้องพึ่งพา .NET Runtime ภายนอก
* **API & Dependencies Update:** อัปเดตการเรียกใช้ API ในหลายส่วน เช่น `IPlugin.Run`, แก้ไขบั๊กของ `SharpCompress` และปรับปรุง Class อย่าง `UNote` และ `UExpression` ให้รองรับโครงสร้างใหม่

## 2. 🎤 ระบบ Phonemizer และการประมวลผลภาษา (Linguistics & Phonemizers)
* **ชุด Phonemizer ภาษาไทยครบถ้วน:** 
  * `ThaiVCCVPhonemizer` (รองรับอักษรนำ ห/อ และคำควบกล้ำ TrueClusters)
  * `ThaiArpasingPhonemizer` และ `ThaiCpVPhonemizer`
  * การรองรับ **DiffSinger** สำหรับภาษาไทย (`DiffSingerThaiPhonemizer`)
* **ปรับปรุงคุณภาพเสียง (Global Articulation):** ขยายระยะเวลา Transition ของพยัญชนะ (จาก 100 เป็น 130 ticks) ใน `SyllableBasedPhonemizer` เพื่อให้เสียงพยัญชนะชัดเจนขึ้น ซึ่งส่งผลดีต่อภาษาอื่นๆ ด้วย (เช่น เกาหลี, อังกฤษ, ฝรั่งเศส, สเปน)
* **ยกเลิก Auto-Melisma:** ถอดฟีเจอร์นี้ออกเพื่อป้องกันปัญหา "เสียงผี" (Ghost Voices) หรือพยางค์ที่ถูกสร้างขึ้นมาเกินความจำเป็น
* **External Dictionary & Fallback:** ดึง Dictionary ออกมาอยู่ภายนอกเพื่อให้แก้ไขได้ง่าย และเพิ่มระบบ Fallback ให้กับ Phonemizer ภาษาอังกฤษและญี่ปุ่น

## 3. 🎨 การปรับโฉม UI และธีม (DELTA SYNTH UI Standard)
* **ธีมสี DELTA SYNTH:** ปรับเปลี่ยน UI ทั้งหมดให้เป็นธีม แดง-ดำ-ขาว (#CC2200, #1A1A1A, #F0F0F0) ตามมาตรฐานของ DELTA SYNTH
* **ฟอนต์หลัก:** เปลี่ยนมาใช้ฟอนต์ **Leelawadee UI** ทั่วทั้งแอปพลิเคชัน
* **การรองรับหลายภาษา (Bilingual & ASEAN):** รองรับภาษาไทยและอังกฤษอย่างสมบูรณ์แบบ และเพิ่มการแปลเป็น 8 ภาษาในอาเซียนและเอเชีย (ลาว, เมียนมาร์, เวียดนาม, มาเลย์, อินโดนีเซีย, ฟิลิปปินส์, ฮินดี, ไต้หวัน)
* **การปรับแต่ง UX ย่อย:** ปรับ Track Header, GridSplitter, Toast Notification, และ Toolbar Icons (24x24) ให้ดูทันสมัยและเป็นระเบียบตามโครงสร้าง Avalonia (Type 5) รวมถึงการตั้งค่า Piano Roll Keys ให้โปร่งใส (Transparent)
* **Render Status Bar:** ย้ายแถบสถานะการเรนเดอร์ไปไว้ที่มุมล่างซ้าย พร้อมแถบสี #00BFFF เพื่อไม่ให้บดบังพื้นที่ทำงาน (Workspace)

## 4. ⚙️ การเพิ่มประสิทธิภาพการทำงาน (Core System Optimization)
* **RenderEngine & Audio Cache:** ลบดีเลย์ (Delay) 200ms ออกจาก RenderEngine ทำให้เริ่มเรนเดอร์เสียงได้ทันที (Instant Start) และแก้ไขบั๊กไฟล์ Audio Cache ถูกล็อค
* **Thread Safety:** เพิ่ม Cache Lock ใน ClassicRenderer เพื่อป้องกันปัญหา Thread ชนกันระหว่างการเรนเดอร์
* **InitDefaultTools:** ตั้งค่าเริ่มต้นให้ใช้ Moresampler เป็น default เอนจิน

## 5. 🛠️ เมนูแก้ไขอัตโนมัติ (Piano Roll Batch Edits)
* **AutoPitchRandomizerBatchEdit:** เพิ่มปลั๊กอินสำหรับสุ่ม Pitch 3 รูปแบบ (Wave, Drunk Walk, Drop)
* **AutoMixBatchEdit:** ปรับปรุงโค้ดให้สอดคล้องกับ API ใหม่ ทำให้บิลด์ผ่านและใช้งานได้โดยไม่พัง
* **สคริปต์ทำความสะอาดโน้ต:** เพิ่มเมนู `RemoveTailRestAndSuffixes` และ `CleanAllSuffixes` พร้อมเมนูที่แสดงผลทั้งภาษาไทยและอังกฤษ

**บทสรุป:** โปรเจกต์ได้ถูกพัฒนาจากโค้ดเบสตั้งต้น ขึ้นมาเป็นแอปพลิเคชันที่สมบูรณ์แบบ ทั้งในด้านความสวยงาม (UI/UX) ความเสถียร (Zero Bugs/Crashes) การประมวลผลเสียงภาษาไทย และระบบการแจกจ่ายที่พร้อมใช้งานจริงสำหรับทุกคน
