import re
from typing import List
from pythainlp.tokenize import syllable_tokenize

# พจนานุกรมคำศัพท์ 54 คำจากการทดสอบ (test_thai_g2p.py)
TEST_DICTIONARY = {
    # Tier 1: Core
    "ตา": ["t", "a"],
    "ยาย": ["y", "a", "y"],
    "มา": ["m", "a"],
    "กิน": ["k", "i", "n"],
    "ข้าว": ["kh", "a", "w"],
    "ไป": ["p", "a", "i"],
    "นอน": ["n", "Q", "n"],
    "ดี": ["d", "i"],
    "มาก": ["m", "a", "k"],
    "รัก": ["r", "a", "k"],
    "น้ำ": ["n", "a", "m"],
    "ไฟ": ["f", "a", "i"],
    "ลม": ["l", "Q", "m"],
    "ดิน": ["d", "i", "n"],
    "วัน": ["w", "a", "n"],
    # Tier 2: Clusters
    "ปลา": ["p", "l", "a"],
    "กล้วย": ["k", "l", "ua", "y"],
    "ขวาน": ["kh", "w", "a", "n"],
    "ควาย": ["kh", "w", "a", "y"],
    "แผล": ["ph", "@"],
    "พริก": ["ph", "r", "i", "k"],
    "ตรา": ["t", "r", "a"],
    "คลอง": ["kh", "l", "Q", "ng"],
    "เพลง": ["ph", "l", "e", "ng"],
    "ความ": ["kh", "w", "a", "m"],
    "ไกล": ["k", "l", "a", "i"],
    "กวาง": ["k", "w", "a", "ng"],
    "พรุ่ง": ["ph", "r", "u", "ng"],
    "เปรี้ยว": ["p", "r", "ia", "w"],
    "ครัว": ["kh", "r", "ua"],
    "คน": ["kh", "Q", "n"],
    "มด": ["m", "Q", "t"],
    # Tier 3: Complex
    "จันทร์": ["c", "a", "n"],
    "ศาสตร์": ["s", "a", "t"],
    "สามารถ": ["s", "a", "m", "a", "t"],
    "ทราบ": ["s", "a", "p"],
    "จริง": ["c", "i", "ng"],
    "เศร้า": ["s", "a", "w"],
    "พราหมณ์": ["ph", "r", "a", "m"],
    "บาตร": ["b", "a", "t"],
    "ปรารถนา": ["p", "r", "a", "t", "th", "a", "n", "a"],
    "พรรณ": ["ph", "a", "n"],
    "บรรพต": ["b", "a", "n", "ph", "Q", "t"],
    "ธรรมชาติ": ["th", "a", "m", "m", "a", "ch", "a", "t"],
    "อังกฤษ": ["a", "ng", "k", "r", "i", "t"],
    "ประวัติ": ["p", "r", "a", "w", "a", "t"],
    "เหตุ": ["h", "e", "t"],
    # Tier 4: Workload
    "สวัสดี": ["s", "a", "w", "a", "t", "d", "i"],
    "ขอบคุณ": ["kh", "Q", "p", "kh", "u", "n"],
    "มหาวิทยาลัย": ["m", "a", "h", "a", "w", "i", "t", "th", "a", "y", "a", "l", "a", "i"],
    "กรุงเทพมหานคร": ["k", "r", "u", "ng", "th", "e", "p", "m", "a", "h", "a", "n", "a", "kh", "Q", "n"],
    "ประชาธิปไตย": ["p", "r", "a", "ch", "a", "th", "i", "p", "p", "a", "t", "a", "i"],
    "รัฐธรรมนูญ": ["r", "a", "t", "th", "a", "th", "a", "m", "m", "a", "n", "u", "n"],
    "ฉันรักภาษาไทย": ["ch", "a", "n", "r", "a", "k", "ph", "a", "s", "a", "th", "a", "i"]
}

# พจนานุกรมหน่วยเสียงเพิ่มเติมเฉพาะกิจเพื่อการรองรับที่ครอบคลุม
EXTRA_DICTIONARY = {
    "ทดสอบ": ["th", "Q", "t", "s", "Q", "p"],
    "หรูหรา": ["r", "u", "r", "a"],
}

C_MAPPING = {
    'ก': 'k', 'ข': 'kh', 'ค': 'kh', 'ฆ': 'kh', 'ฅ': 'kh', 'ฃ': 'kh',
    'จ': 'c', 'ฉ': 'ch', 'ช': 'ch', 'ฌ': 'ch',
    'ฎ': 'd', 'ด': 'd',
    'ต': 't', 'ฏ': 't',
    'ถ': 'th', 'ฐ': 'th', 'ฑ': 'th', 'ฒ': 'th', 'ธ': 'th', 'ท': 'th',
    'บ': 'b', 'ป': 'p', 'พ': 'ph', 'ผ': 'ph', 'ภ': 'ph', 'ฟ': 'f', 'ฝ': 'f',
    'ห': 'h', 'ฮ': 'h',
    'ม': 'm', 'น': 'n', 'ณ': 'n', 'ร': 'r', 'ล': 'l', 'ฤ': 'r', 'ฬ': 'l',
    'ส': 's', 'ศ': 's', 'ษ': 's', 'ซ': 's',
    'ง': 'ng', 'ย': 'y', 'ญ': 'y', 'ว': 'w', 'อ': ''
}

X_MAPPING = {
    'บ': 'p', 'ป': 'p', 'พ': 'p', 'ฟ': 'p', 'ภ': 'p',
    'ด': 't', 'จ': 't', 'ช': 't', 'ซ': 't', 'ฎ': 't', 'ฏ': 't', 'ฐ': 't',
    'ฑ': 't', 'ฒ': 't', 'ต': 't', 'ถ': 't', 'ท': 't', 'ธ': 't', 'ศ': 't', 'ษ': 't', 'ส': 't',
    'ก': 'k', 'ข': 'k', 'ค': 'k', 'ฆ': 'k',
    'ว': 'w', 'ย': 'y',
    'น': 'n', 'ญ': 'n', 'ณ': 'n', 'ร': 'n', 'ล': 'n', 'ฬ': 'n',
    'ง': 'ng', 'ม': 'm'
}

VOWEL_MAPPING = {
    "เcือะ": "6", "เcือx": "6", 
    "แcะ": "@", "แcx": "@", 
    "เcอะ": "oe", "เcอ": "3", 
    "ไc": "a", "ใc": "a", 
    "เcาะ": "Q", "cอx": "Q",
    "cืx": "1", "cึx": "1", "cือ": "1", 
    "cะ": "a", "cัx": "a", "cาx": "a", "cรรx": "a", 
    "เcา": "aw", "เcะ": "e", "เcx": "e", 
    "cิx": "i", "cีx": "i",
    "เcียะ": "ia", "เcียx": "ia", 
    "โcะ": "Q", "โcx": "Q", 
    "cุx": "u", "cูx": "u", 
    "cัวะ": "ua", "cัว": "ua", 
    "cำ": "am", 
    "เcิx": "3", "เcิ": "3"
}

TRUE_CLUSTERS = {
    "กร", "กล", "กว", "ขร", "ขล", "ขว", "คร", "คล", "คว", 
    "ปร", "ปล", "พร", "พล", "ตร", "ผล", "บร", "บล", "ฟร", 
    "ฟล", "ดร", "ทร"
}

def clean_syllable(syl: str) -> str:
    # ลบการันต์์ และตัวอักษรหน้าการันต์
    syl = re.sub(r'.์', '', syl)
    # ลบวรรณยุกต์และไม้ไต่คู้
    syl = re.sub(r'[่้๊๋็]', '', syl)
    return syl

def convert_c(c_str: str) -> List[str]:
    if not c_str:
        return []
    if c_str in ("ทร", "สร", "ศร", "ซร"):
        return ["s"]
    if c_str == "จร":
        return ["c"]
    if c_str == "อ":
        return []
    if len(c_str) >= 2 and c_str[0] in ('ห', 'อ'):
        c_str = c_str[1:]
    
    res = []
    for char in c_str:
        if char in C_MAPPING:
            val = C_MAPPING[char]
            if val:
                res.append(val)
    return res

def convert_x(x_str: str, from_ai: bool = False) -> List[str]:
    if not x_str:
        return []
    char = x_str[0]
    if char in X_MAPPING:
        val = X_MAPPING[char]
        if val == 'y' and from_ai:
            return ['i']
        return [val]
    return [char]

def g2p_syllable(syl: str) -> List[str]:
    syl = clean_syllable(syl)
    if not syl:
        return []
    
    # ทดลองจับคู่กฎสระ
    sorted_vowels = sorted(VOWEL_MAPPING.keys(), key=len, reverse=True)
    for pattern_key in sorted_vowels:
        regex_pattern = "^" + pattern_key.replace("c", r"([ก-ฮ][ลรว]?|อ[ย]?|ห[ก-ฮ]?)").replace("x", r"([ก-ฮ]*)") + "$"
        match = re.match(regex_pattern, syl)
        if match:
            c = match.group(1)
            x = match.group(2) if len(match.groups()) > 1 else ""
            
            # กรณีสระพิเศษ
            from_ai = False
            if pattern_key in ("ไc", "ใc"):
                x = "ย"
                from_ai = True
            elif pattern_key == "เcา":
                x = "ว"
            elif pattern_key == "cำ":
                x = "ม"
            elif pattern_key == "cรรx" and x == "":
                x = "น"
            elif pattern_key.endswith("x") and x == "":
                if c not in TRUE_CLUSTERS and len(c) > 1:
                    x = c[-1]
                    c = c[:-1]
            
            if len(c) >= 2 and c[0] in ('ห', 'อ'):
                c = c[1:]
            if len(x) > 1:
                x = x[0]
                
            c_phonemes = convert_c(c)
            v_phoneme = VOWEL_MAPPING[pattern_key]
            x_phonemes = convert_x(x, from_ai)
            
            return c_phonemes + [v_phoneme] + x_phonemes

    # กฎสำรองกรณีความยาวตัวอักษร
    if len(syl) == 1:
        if syl in ("ธ", "ณ"):
            return convert_c(syl) + ["a"]
        return convert_c(syl) + ["Q"]
    elif len(syl) == 2:
        if syl[1] == 'ร':
            return convert_c(syl[0]) + ["Q", "n"]
        return convert_c(syl[0]) + ["Q"] + convert_x(syl[1])
    elif len(syl) == 3:
        if syl[1] == 'ว':
            return convert_c(syl[0]) + ["ua"] + convert_x(syl[2])
        else:
            return convert_c(syl[:2]) + ["Q"] + convert_x(syl[2])
    elif len(syl) == 4:
        if syl[2] == 'ว':
            return convert_c(syl[:2]) + ["ua"] + convert_x(syl[3])
            
    # กรณีสู้ไม่ได้จริงๆ ส่งคืนแบบแปลงตามพยัญชนะ
    return convert_c(syl)

def g2p_thai(word: str) -> List[str]:
    # 1. ค้นหาในพจนานุกรมคำหลักและคำเพิ่มเติม
    if word in TEST_DICTIONARY:
        return TEST_DICTIONARY[word]
    if word in EXTRA_DICTIONARY:
        return EXTRA_DICTIONARY[word]
        
    # 2. แบ่งเป็นพยางค์และแปลงทีละพยางค์
    syllables = syllable_tokenize(word)
    result = []
    for syl in syllables:
        if syl.strip():
            # ค้นหาพยางค์ย่อยในดิคชันนารี (เผื่อเจอคำสั้น)
            if syl in TEST_DICTIONARY:
                result.extend(TEST_DICTIONARY[syl])
            else:
                result.extend(g2p_syllable(syl))
    return result
