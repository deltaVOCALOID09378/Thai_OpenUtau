import os

p = r"d:\Program Developing\Thai_OpenUtau\About-Project.md"
with open(p, "r", encoding="utf-8") as f:
    c = f.read()

# Update header version
c = c.replace("Version      : 3.9.2 (Enterprise Zero-Defect Sustainable R&D Master Edition)",
              "Version      : 3.9.3 (Enterprise Screen Recording, Dynamic Data Relocation & Responsive Architecture)")
c = c.replace("3 พฤศจิกายน 2569 / 3 November 2026", "3 พฤศจิกายน 2569 / 3 November 2026")
c = c.replace("v3.9.2 (Assembly/FileVersion 39.2.0.0 Enterprise Zero-Defect Edition)",
              "v3.9.3 (Assembly/FileVersion 39.3.0.0 Enterprise Responsive Architecture Edition)")
c = c.replace("2 พฤศจิกายน 2569 / 2 November 2026", "3 พฤศจิกายน 2569 / 3 November 2026")

# Add Phase 6 under Phase 5
phase5_end = """- **A11y & Contrast:** ปรับปรุงความเปรียบต่างสีตัวเลขแทร็ก `TrackNoForeground` อิงค่า Luminance อัตโนมัติ (ผ่านเกณฑ์ WCAG 2.2 ระดับ AAA)"""
phase6 = """- **A11y & Contrast:** ปรับปรุงความเปรียบต่างสีตัวเลขแทร็ก `TrackNoForeground` อิงค่า Luminance อัตโนมัติ (ผ่านเกณฑ์ WCAG 2.2 ระดับ AAA)

### ระยะที่ 6: v3.9.3 (Screen Recording, Dynamic Data Relocation & Responsive Architecture)
- **High-Performance Screen Capture & Video Recorder:** บันทึกภาพ PNG และวิดีโอ MP4/AVI/MKV เจาะจงเฉพาะหน้าต่างโปรแกรม รองรับความละเอียด 240p - 4K พร้อมระบบตัดปุ่มควบคุมและตัวเลขเวลาออกจากคลิปวิดีโอที่ส่งออก 100% (`BeforeRenderFrame` / `AfterRenderFrame` Exclusion)
- **Real-Time Duration Timer on Piano Roll:** แสดงเวลาการบันทึกวิดีโอสดแบบ `MM:SS` บนแถบควบคุมของ Piano Roll ให้ผู้ใช้งานทราบสถานะได้ทันที
- **Dynamic User Data Relocation:** ระบบเลือกและย้ายที่อยู่ฐานข้อมูลผู้ใช้เดิมอย่างอิสระผ่าน Pointer File (`%LOCALAPPDATA%\\ThaiOpenUtau\\datapath.txt`) รองรับการกู้คืนข้อมูลหรือลงวินโด้วส์ใหม่
- **Smart Loading Popup with Close 'X':** หน้าต่างป๊อปอัพรายงานสถานะการโหลดข้อมูลพร้อมปุ่มกากบาท X ที่มุมขวาบน และข้อความปิดท้ายบังคับ "กรุณา รอสักครู่.........." พร้อมอันตรธานหายไปทันทีเมื่อโหลดเสร็จ
- **Transparent Adaptive Toast Notifications:** ปรับปรุง Toast แจ้งเตือนสองภาษา ขยายเวลาอ่านข้อผิดพลาดเป็น 5 วินาที พร้อมระบบ `ClarifyMessage` ถอดรหัสข้อความเทคนิคให้อ่านเข้าใจง่าย
- **Multi-Language VSCode Debug Suite:** ยกระดับ `.vscode/launch.json` v2.0, สร้าง `tasks.json` และปรับปรุง `settings.json` ครอบคลุม .NET, Python, C++ และ Tests ไร้คำเตือน"""

if phase5_end in c and "ระยะที่ 6: v3.9.3" not in c:
    c = c.replace(phase5_end, phase6, 1)

with open(p, "w", encoding="utf-8") as f:
    f.write(c)
print("Updated About-Project.md successfully")
