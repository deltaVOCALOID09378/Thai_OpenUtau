import re
from typing import List, Union
from pythainlp.tokenize import syllable_tokenize

# ย้าย Imports ทั้งหมดมารวมไว้ด้านบนเพื่อความเป็นระเบียบและลดปัญหา Circular Import
# สมมติว่ามีโมดูลเหล่านี้อยู่ในโปรเจกต์ของคุณ
from .vccv import generate_vccv_transitions
from .thai_g2p import g2p_thai as get_raw_phonemes

def clean_phonemes(phoneme_data: Union[str, List[str]]) -> List[str]:
    """
    Clean and split the phoneme string into individual phonemes.
    รองรับทั้ง Input ที่เป็น String และ List
    """
    # หาก Input มาเป็น List อยู่แล้ว ให้รวมเป็น String คั่นด้วยช่องว่างก่อนประมวลผล
    if isinstance(phoneme_data, list):
        phoneme_str = " ".join(phoneme_data)
    else:
        phoneme_str = str(phoneme_data)

    # Remove tone marks (1-5, IPA tones) and length marks (: , .)
    cleaned = re.sub(r'[0-9\.\:\-\u0300-\u036f˩˨˧˦˥]', '', phoneme_str)
    
    # Split by spaces and filter out empty strings
    phonemes = [p.strip() for p in cleaned.split() if p.strip()]
    return phonemes

def g2p_thai(text: str) -> List[str]:
    """
    Convert Thai text to a list of VCCV transitions.
    """
    # 1. ดักจับกรณี Text ว่างเปล่า (Edge Case Prevention)
    if not text or not text.strip():
        return []

    syllables = syllable_tokenize(text)
    all_phonemes = []

    for syl in syllables:
        syl = syl.strip()
        if not syl:
            continue
        
        # 2. ดึงค่า Raw Phonemes ออกมาจากพยางค์
        raw_ph = get_raw_phonemes(syl)
        
        if raw_ph:
            # 3. *** จุดที่แก้ไข ***: นำ raw_ph มาผ่านกระบวนการทำความสะอาดก่อนเสมอ
            cleaned_ph_list = clean_phonemes(raw_ph)
            
            # 4. หากทำความสะอาดแล้วยังมีข้อมูลอยู่ จึงนำไปเพิ่มในรายการ
            if cleaned_ph_list:
                all_phonemes.append(cleaned_ph_list)

    # 5. Generate VCCV transitions จาก List ของพยางค์ที่คลีนแล้ว 100%
    if not all_phonemes:
        return []
        
    transitions = generate_vccv_transitions(all_phonemes)
    return transitions
