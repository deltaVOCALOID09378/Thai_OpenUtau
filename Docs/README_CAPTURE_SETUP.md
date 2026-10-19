# การเชื่อมต่อระบบแคปภาพและบันทึกวิดีโอ (Screen Capture & Video Recorder Integration)
**Thai OpenUtau Project**
**Made And Checked By DELTA SYNTH [Patiphat Wongyai] & All Code Agentic AI Engine**
**[ Original by OpenUtau Authors ]**
**วันที่: 29 กันยายน 2569 (29/09/2026)**

---

### 1. ไฟล์หลักที่เพิ่มเข้ามาในโปรเจกต์ (New Engine File)
* **ตำแหน่ง:** 
* **หน้าที่:** จัดการเอนจินแคปภาพหน้าจอ (Lossless PNG) และการบันทึกวิดีโอ (60 FPS H.264 MP4) แบบ Non-blocking Background Queue ทำงานลื่นไหล ไม่กระตุก ไม่รบกวน Audio Engine
* **ความละเอียดที่รองรับ:** , , , , , 

---

### 2. โค้ดที่ต้องเชื่อมต่อใน 
ในไฟล์  (บริเวณ  ที่  หลังปุ่ม Fullscreen Toggle):



---

### 3. โค้ดที่ต้องเพิ่มใน 
ในไฟล์ :



---

### 4. การตั้งค่าใน Preferences
* หน้าต่าง **Preferences** (ผ่าน  หรือ ) สามารถตั้งค่า:
  1. : โฟลเดอร์ปลายทางที่ผู้ใช้ต้องการวางไฟล์
  2. : 240, 480, 720, 1080, 1440 (2K), 2160 (4K)
  3. : mp4, avi, gif, mkv
