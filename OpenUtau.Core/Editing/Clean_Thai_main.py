# Made And Checked By DELTA SYNTH & Gemini AI
# Original by DELTA SYNTH
# Version: 1.1.0

import sys
import re
import codecs

def clean_lyric(lyric: str) -> str:
    """
    Cleans unwanted expression/pitch symbols (Whisper, Soft, Arrows, Breath markers)
    without corrupting consonants or rest notes (R/r).
    """
    if lyric.strip().lower() in ["r", "rest"]:
        return "R"

    # 1. ลบสัญลักษณ์พิเศษ (Whisper, Soft, Tone Arrows)
    unwanted_symbols = ["弱", "囁", "↑", "↓", "←", "→"]
    for token in unwanted_symbols:
        lyric = lyric.replace(token, "")

    # 2. ลบ Breath marks (br/BR) เฉพาะเมื่อเป็นคำแยกหรือต่อท้าย
    lyric = re.sub(r'(?i)\bbr\b|_br|br_', '', lyric)

    # 3. เก็บเฉพาะตัวอักษรเนื้อเพลงหลัก (ไทย, อังกฤษ, ญี่ปุ่น, สัทศาสตร์) และช่องว่าง
    match = re.search(r'([a-zA-Z\u0E00-\u0E7F\u3040-\u30FF\s_\-]+)', lyric)
    if match:
        return match.group(1).strip()
    return lyric.strip()

def main() -> None:
    if len(sys.argv) < 2:
        return

    filepath = sys.argv[1]
    
    # อ่านไฟล์ชั่วคราวที่ OpenUtau ส่งมาให้ (รองรับทั้ง UTF-8 และ Shift-JIS)
    try:
        with codecs.open(filepath, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        enc = 'utf-8'
    except Exception:
        with codecs.open(filepath, 'r', encoding='shift_jis', errors='ignore') as f:
            lines = f.readlines()
        enc = 'shift_jis'

    new_content = []
    for line in lines:
        if line.startswith("Lyric="):
            key, val = line.strip().split("=", 1)
            # คลีนเฉพาะส่วนเนื้อเพลง
            cleaned_val = clean_lyric(val)
            new_content.append(f"Lyric={cleaned_val}\r\n")
        else:
            # รักษาค่าอื่นๆ ในโน้ตไว้ (NoteNum, Length, etc.)
            new_content.append(line if line.endswith('\n') else line + '\r\n')

    # เขียนทับกลับไปที่ไฟล์เดิมเพื่อให้ OpenUtau ทำงานต่อ
    with codecs.open(filepath, 'w', encoding=enc) as f:
        f.writelines(new_content)

if __name__ == "__main__":
    main()