# -*- coding: utf-8 -*-
# ============================================================================
# 1. Date: 29/10/2569 (2026-10-29)
# 2. File Name: Diffsinger_to_VCCV_Converter.py
# 3. Version: 2.0
# 4. Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH & Gemini AI ]
# 5. Location: Root/Tools/
# 6. Description: High-speed dictionary phoneme converter with dedicated output folder and STRICT UPPERCASE Arpasing output.
# ============================================================================

import sys
import os

# Advanced Phoneme Map (Thai to VCCV - รักษาสัดส่วนพิมพ์เล็ก/ใหญ่ ตามมาตรฐานเดิม)
MAP_VCCV = {
    "ng": "g", "kk": "k", "k": "kh", "pp": "p", "p": "ph", "tt": "t", "t": "th",
    "ch": "ch", "j": "j", "b": "b", "d": "d", "f": "f", "h": "h", "s": "s",
    "r": "r", "l": "l", "m": "m", "n": "n", "w": "w", "y": "y",
    "A": "@", "O": "Q", "E": "3", "Ua": "6", "U": "1", "I": "ay", "au": "aw",
    "ia": "ia", "ua": "ua", "oa": "oa", "a": "a", "e": "e", "i": "i", "o": "o", "u": "u",
    "K": "k", "B": "b", "D": "d", "W": "w", "Y": "y", "M": "m", "N": "n", "G": "g"
}

# Strict UPPERCASE Phoneme Map for Arpasing (บังคับพิมพ์ใหญ่ทั้งหมด เพื่อให้สอดคล้องกับ phones.txt)
MAP_ARP = {
    "ng": "NG", "kk": "K", "k": "KH", "pp": "P", "p": "PH", "tt": "T", "t": "TH",
    "ch": "CH", "j": "JH", "b": "B", "d": "D", "f": "F", "h": "HH", "s": "S",
    "r": "R", "l": "L", "m": "M", "n": "N", "w": "W", "y": "Y",
    "A": "AE", "O": "AO", "E": "ER", "Ua": "UEA", "U": "UE", "I": "AY", "au": "AW",
    "ia": "IY", "ua": "UW", "oa": "UW", "a": "AA", "e": "EH", "i": "IY", "o": "OW", "u": "UW",
    "K": "K", "B": "P", "D": "T", "W": "W", "Y": "Y", "M": "M", "N": "N", "G": "G"
}

def convert_dictionary(input_file):
    if not os.path.exists(input_file):
        print(f"[ERROR] File not found: {input_file}")
        sys.exit(1)
        
    dir_name = os.path.dirname(input_file)
    if not dir_name:
        dir_name = "."
        
    base_name = os.path.splitext(os.path.basename(input_file))[0]
    output_folder = os.path.join(dir_name, f"{base_name}_Converted")
    
    os.makedirs(output_folder, exist_ok=True)
        
    out_vccv_path = os.path.join(output_folder, "TH-VCCV_Dict.txt")
    out_arp_path = os.path.join(output_folder, "TH-ARP_Dict.txt")
    
    print(f"[PROCESSING] Reading words from: {input_file}")
    print(f"[INFO] Output will be saved to : {output_folder}")
    
    out_vccv_lines = []
    out_arp_lines = []
    
    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            lines = f.readlines()
            
        for line in lines:
            line = line.strip()
            if not line:
                continue
                
            parts = line.replace('\t', ' ').split()
            if len(parts) < 2:
                continue
                
            thai_word = parts[0]
            phonemes = parts[1:]
            
            vccv_phonemes = []
            arp_phonemes = []
            
            for token in phonemes:
                vccv_phonemes.append(MAP_VCCV.get(token, token))
                # Fallback ให้แปลงเป็นตัวพิมพ์ใหญ่ (.upper()) เสมอหากไม่พบใน Map เพื่อป้อนเข้าสู่ระบบ Arpasing
                arp_phonemes.append(MAP_ARP.get(token, token.upper()))
                
            str_vccv = " ".join(vccv_phonemes)
            str_arp = " ".join(arp_phonemes)
            
            out_vccv_lines.append(f"{thai_word}={str_vccv}\n")
            out_arp_lines.append(f"{thai_word}={str_arp}\n")
            
            print(f"   [WORD]: {thai_word:<15} => VCCV: [{str_vccv}] | ARP: [{str_arp}]")
            
        with open(out_vccv_path, 'w', encoding='utf-8') as f:
            f.writelines(out_vccv_lines)
            
        with open(out_arp_path, 'w', encoding='utf-8') as f:
            f.writelines(out_arp_lines)
            
        print(f"\n[SUCCESS] Exported: {out_vccv_path}")
        print(f"[SUCCESS] Exported: {out_arp_path}")
        
    except Exception as e:
        print(f"[ERROR] An unexpected exception occurred: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("[ERROR] No input dictionary file provided.")
        sys.exit(1)
        
    target_file = sys.argv[1]
    convert_dictionary(target_file)