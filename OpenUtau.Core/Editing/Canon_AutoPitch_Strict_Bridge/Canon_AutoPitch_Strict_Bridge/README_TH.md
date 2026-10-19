# Canon AutoPitch Strict Bridge

ชุดนี้แก้ปัญหาที่โค้ดเดิม “จำลอง Canon” ด้วยสูตร Pitch แบบตายตัว ซึ่งไม่สามารถเหมือนอัลกอริทึมภายในของ Canon ได้จริง

## สิ่งที่เปลี่ยน

- ใช้ `AutoPitchwriter.exe` ของ Canon เป็นเครื่องจูนเพียงตัวเดียว
- ไม่สร้าง PBS/PBW/PBY/PBM/VBR เอง
- ไม่มี fallback หรือการสุ่ม Pitch แบบ DELTA SYNTH
- ไม่ประมวลผลผลลัพธ์ซ้ำหลัง Canon ทำงานเสร็จ
- ไม่อ่าน คัดลอก หรือแก้ไข `oto.ini`
- ใช้ไฟล์ตั้งค่า `AutoPitchwriter` และหน้าต่างตั้งค่าของ Canon เดิม
- ส่ง exit code ของ Canon กลับไปยัง OpenUtau โดยตรง

ดังนั้นพฤติกรรมการจูนจะเป็นของ Canon จริง ไม่ใช่การประมาณจากเอกสาร ทั้งนี้ผลลัพธ์ยังขึ้นกับ UST ที่ส่งเข้าไป การเลือกโน้ต จังหวะ 4/4 ตำแหน่งห้องเพลง CVVC และค่าที่บันทึกไว้ใน `AutoPitchwriter`

## การ Build บน Windows

1. เปิด **Developer Command Prompt for Visual Studio**
2. รัน `build_msvc.cmd`
3. จะได้ `CanonAutoPitchBridge.exe`

หรือใช้ MinGW-w64:

```text
g++ -std=c++17 -O2 -municode -mwindows CanonAutoPitchBridge.cpp -o CanonAutoPitchBridge.exe -lshell32 -luser32
```

## การติดตั้ง

วางไฟล์เหล่านี้ไว้ในโฟลเดอร์เดียวกัน:

- `CanonAutoPitchBridge.exe`
- `AutoPitchwriter.exe` — ไฟล์ Canon ต้นฉบับ
- `AutoPitchwriter` — ไฟล์ตั้งค่า Canon ต้นฉบับ

จากนั้นกำหนดให้ OpenUtau เรียก `CanonAutoPitchBridge.exe` เป็นปลั๊กอิน โดยบริดจ์จะส่งไฟล์ UST ที่ OpenUtau ให้มาไปยัง Canon โดยไม่ดัดแปลง

## ข้อควรทราบ

- ถ้าไม่พบ Canon EXE บริดจ์จะหยุดทันที ไม่สร้าง Pitch ทดแทน
- ห้ามตั้งชื่อบริดจ์เป็น `AutoPitchwriter.exe` เพราะจะชนกับโปรแกรม Canon ต้นฉบับ
- สำรองโปรเจกต์/UST ก่อนใช้ปลั๊กอินตามคำเตือนในคู่มือ Canon
- ไม่ควรนำ `AutoPitchWriter_Wrapper.cpp` รุ่นเดิมกลับมาใช้ เพราะมี Native fallback และฟังก์ชัน Oto ที่ไม่ใช่พฤติกรรม Canon
