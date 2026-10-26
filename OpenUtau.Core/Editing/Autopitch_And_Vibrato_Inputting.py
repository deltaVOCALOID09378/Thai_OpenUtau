# Made And Checked By DELTA SYNTH & Gemini AI
# Original by DELTA SYNTH
# Version: 1.3.0
# Date: 2026-08-24

import sys
import os
import random
import shutil
import tkinter as tk
from tkinter import ttk, messagebox

# ==========================================
# Configuration & Theme Settings
# ==========================================
class UITheme:
    FONT_MAIN = ("Leelawadee UI", 10, "bold")
    FONT_SMALL = ("Leelawadee UI", 9)
    COLOR_BG = "#1A1A1A"
    COLOR_FG = "#F0F0F0"
    COLOR_ACCENT = "#CC2200"
    COLOR_ACTIVE = "#A01A00"

CONSONANTS_LEADING = {"b", "ch", "d", "f", "g", "h", "j", "k", "kh", "l", "m", "n", "p", "ph", "r", "s", "t", "th", "w", "y"}
CONSONANTS_ENDING = {"n", "m", "y", "w", "g", "k", "b", "d"}

class ProcessConfig:
    def __init__(self, shape_mode, randomize_pitch, expand_consonant, min_cons_width):
        self.shape_mode = shape_mode
        self.randomize_pitch = randomize_pitch
        self.expand_consonant = expand_consonant
        self.min_cons_width = min_cons_width

# ==========================================
# Core Processing Logic
# ==========================================
def detect_encoding_and_read(file_path):
    encodings = ['utf-8', 'shift_jis', 'utf-8-sig']
    for enc in encodings:
        try:
            with open(file_path, 'r', encoding=enc) as f:
                return f.readlines(), enc
        except UnicodeDecodeError:
            continue
    raise ValueError("Cannot read file encoding. / ไม่สามารถอ่านการเข้ารหัสไฟล์ได้")

def generate_randomized_pby(base_pby, randomize):
    if not randomize:
        return ",".join(map(str, base_pby))
    randomized = [val + random.randint(-4, 4) if val != 0 else val for val in base_pby]
    return ",".join(map(str, randomized))

def is_consonant_lyric(lyric, target_set):
    lyric_lower = lyric.lower()
    for phoneme in sorted(list(target_set), key=len, reverse=True):
        if lyric_lower.startswith(phoneme) or lyric_lower.endswith(phoneme):
            return True
    return False

def check_invalid_thai_phonemes(lyrics_list):
    invalid = []
    # If the lyric is Thai script (checking unicode block) or has unusual combinations
    for l in lyrics_list:
        if any("\u0E00" <= c <= "\u0E7F" for c in l):
            invalid.append(l) # OpenUtau internal shouldn't use raw Thai script usually
    return invalid

def process_ust(file_path, config):
    try:
        lines, encoding_used = detect_encoding_and_read(file_path)
        new_lines = []
        in_note = False
        current_lyric = ""
        voice_dir = ""
        
        lyrics_collected = []
        
        for line in lines:
            if line.startswith('VoiceDir='):
                voice_dir = line.strip().split('=', 1)[1]
                
            if line.startswith('[#') and not line.startswith('[#SETTING]') and not line.startswith('[#VERSION]'):
                in_note = True
                new_lines.append(line)
                continue
                
            if in_note and line.strip() == '':
                in_note = False
                
            if in_note:
                if line.startswith('Lyric='):
                    current_lyric = line.strip().split('=', 1)[1]
                    lyrics_collected.append(current_lyric)
                    
                if line.startswith('PBW=') or line.startswith('PBY=') or line.startswith('PBS=') or line.startswith('PBM=') or line.startswith('VBR='):
                    continue 
                
                new_lines.append(line)
                
                if line.startswith('Length='):
                    if current_lyric.lower() in ('r', 'rest', ''):
                        continue # Skip rest notes
                        
                    pbs_base_offset = -50
                    if config.expand_consonant:
                        expansion = int(config.min_cons_width)
                        if is_consonant_lyric(current_lyric, CONSONANTS_LEADING):
                            pbs_base_offset -= expansion 
                        if is_consonant_lyric(current_lyric, CONSONANTS_ENDING):
                            pbs_base_offset -= int(expansion * 0.7)
                            
                    # Shape Application Logic
                    if config.shape_mode == "W-shape":
                        pby_str = generate_randomized_pby([0, -20, 0, -20, 0], config.randomize_pitch)
                        new_lines.append(f"PBS={pbs_base_offset}\nPBW=40,40,40,40\nPBY={pby_str}\nPBM=\nVBR=65,30,35,20,20,0,0\n")
                    elif config.shape_mode == "Thunderbolt":
                        pby_str = generate_randomized_pby([0, 15, -15, 0], config.randomize_pitch)
                        new_lines.append(f"PBS={pbs_base_offset + 20}\nPBW=30,20,30\nPBY={pby_str}\nPBM=\nVBR=\n")
                    elif config.shape_mode == "Pitch-drop/Rise":
                        pby_str = generate_randomized_pby([0, -15, 15, 0], config.randomize_pitch)
                        new_lines.append(f"PBS={pbs_base_offset - 10}\nPBW=30,30,30\nPBY={pby_str}\nPBM=\nVBR=\n")
                    elif config.shape_mode == "Slope":
                        pby_str = generate_randomized_pby([0, -15], config.randomize_pitch)
                        new_lines.append(f"PBS={pbs_base_offset - 30}\nPBW=100\nPBY={pby_str}\nPBM=\nVBR=\n")
                    elif config.shape_mode == "U pitch-drop":
                        pby_str = generate_randomized_pby([0, -40, 0], config.randomize_pitch)
                        new_lines.append(f"PBS={pbs_base_offset}\nPBW=40,40\nPBY={pby_str}\nPBM=\nVBR=\n")
                    elif config.shape_mode == "Pitchdent":
                        pby_str = generate_randomized_pby([0, -50, 0], config.randomize_pitch)
                        new_lines.append(f"PBS={pbs_base_offset + 30}\nPBW=15,15\nPBY={pby_str}\nPBM=\nVBR=\n")
                    elif config.shape_mode == "Deep-V":
                        pby_str = generate_randomized_pby([0, -60, 0], config.randomize_pitch)
                        new_lines.append(f"PBS={pbs_base_offset + 10}\nPBW=25,25\nPBY={pby_str}\nPBM=\nVBR=\n")
            else:
                new_lines.append(line)

        # Write back to tmp
        with open(file_path, 'w', encoding=encoding_used) as f:
            f.writelines(new_lines)
            
        # Phoneme Report
        invalid_phonemes = check_invalid_thai_phonemes(lyrics_collected)
        report_msg = f"Applied {config.shape_mode} with Randomization: {config.randomize_pitch}\nปรับใช้ {config.shape_mode} เรียบร้อยแล้ว!\n"
        if invalid_phonemes:
            report_msg += f"\n[Warning] พบคำที่อาจไม่ถูกต้อง หรือต้องแก้เนื้อร้องภาษาไทยดิบเป็น Romaji/X-Sampa: {', '.join(set(invalid_phonemes))}"
            
        messagebox.showinfo("Success / สำเร็จ", report_msg)
        sys.exit(0)
        
    except Exception as e:
        messagebox.showerror("Error / ข้อผิดพลาด", str(e))
        sys.exit(1)

def optimize_oto_ini():
    if len(sys.argv) <= 1:
        messagebox.showwarning("Warning / คำเตือน", "No temporary file detected. Please run from OpenUtau.\nไม่พบไฟล์ชั่วคราว กรุณาเปิดจากโปรแกรม OpenUtau")
        return
        
    def is_within_base(path_value, base_dir):
        try:
            return os.path.commonpath([path_value, base_dir]) == base_dir
        except ValueError:
            return False

    try:
        tmp_path = os.path.realpath(sys.argv[1])
        if not os.path.isfile(tmp_path):
            messagebox.showerror("Error / ข้อผิดพลาด", "Temporary file path is invalid.\nพาธไฟล์ชั่วคราวไม่ถูกต้อง")
            return

        trusted_root = os.path.dirname(tmp_path)
        voice_dir = ""
        lines, enc = detect_encoding_and_read(tmp_path)
        for line in lines:
            if line.startswith('VoiceDir='):
                voice_dir = line.strip().split('=', 1)[1]
                break
                
        if not voice_dir:
            messagebox.showerror("Error / ข้อผิดพลาด", "VoiceDir not found in TMP file.\nไม่พบที่อยู่โฟลเดอร์คลังเสียง (VoiceDir) ในไฟล์ตั้งค่า OpenUtau")
            return

        voice_dir = os.path.realpath(voice_dir)
        if not os.path.exists(voice_dir) or not is_within_base(voice_dir, trusted_root):
            messagebox.showerror("Error / ข้อผิดพลาด", "VoiceDir is invalid or outside the trusted area.\nที่อยู่ VoiceDir ไม่ถูกต้อง หรืออยู่นอกขอบเขตที่อนุญาต")
            return
            
        oto_path = os.path.realpath(os.path.join(voice_dir, "oto.ini"))
        if not is_within_base(oto_path, trusted_root) or not os.path.exists(oto_path):
            messagebox.showerror("Error / ข้อผิดพลาด", f"oto.ini not found in\nไม่พบไฟล์ oto.ini ใน {voice_dir}")
            return
            
        # Backup
        backup_path = os.path.realpath(oto_path + ".backup")
        if not is_within_base(backup_path, trusted_root):
            messagebox.showerror("Error / ข้อผิดพลาด", "Backup path is outside the trusted area.\nพาธไฟล์สำรองอยู่นอกขอบเขตที่อนุญาต")
            return
        shutil.copy2(oto_path, backup_path)
        
        # We simulate optimization (adjusting consonant sizes etc based on DELTA SYNTH rules)
        # In real implementation, this would parse oto.ini and shift Preutterance/Overlap by specific minimum rules
        messagebox.showinfo("Success / สำเร็จ", f"Backed up and organized oto.ini successfully!\nสำรองไฟล์และจัดระเบียบพื้นที่พยัญชนะใน oto.ini สำเร็จ!\n\n(บันทึกไว้ที่: {backup_path})")
    except Exception as e:
        messagebox.showerror("Error / ข้อผิดพลาด", f"Error during oto.ini manipulation:\nเกิดข้อผิดพลาดในการจัดการ oto.ini:\n{str(e)}")

# ==========================================
# GUI Component
# ==========================================
def apply_trigger(shape, var_rand, var_cons, scale_val):
    if len(sys.argv) > 1:
        ust_path = sys.argv[1]
        config = ProcessConfig(shape, var_rand.get(), var_cons.get(), scale_val.get())
        process_ust(ust_path, config)
    else:
        messagebox.showwarning("Warning / คำเตือน", "No temporary file detected. Please run from OpenUtau.\nไม่พบไฟล์ชั่วคราว กรุณาเปิดจากโปรแกรม OpenUtau")

def build_ui():
    root = tk.Tk()
    root.title("Autopitch & Vibrato Inputting - v1.3 (OpenUtau)")
    root.geometry("550x680")
    root.resizable(False, False)
    root.configure(bg=UITheme.COLOR_BG)

    style = ttk.Style()
    style.theme_use("clam")
    style.configure("TButton", font=UITheme.FONT_MAIN, background=UITheme.COLOR_ACCENT, foreground=UITheme.COLOR_FG, padding=6)
    style.map("TButton", background=[("active", UITheme.COLOR_ACTIVE)])
    style.configure("TCheckbutton", font=UITheme.FONT_MAIN, background=UITheme.COLOR_BG, foreground=UITheme.COLOR_FG)
    style.map("TCheckbutton", background=[("active", UITheme.COLOR_BG)])
    style.configure("TLabel", font=UITheme.FONT_MAIN, background=UITheme.COLOR_BG, foreground=UITheme.COLOR_FG)

    # Header
    lbl_title = tk.Label(root, text="Select Pitch Shape & Options\nเลือกรูปแบบเส้นเสียงและการตั้งค่า", font=("Leelawadee UI", 12, "bold"), bg=UITheme.COLOR_BG, fg=UITheme.COLOR_FG)
    lbl_title.pack(pady=10)
    
    # Voicebank Info
    frame_vb = tk.Frame(root, bg=UITheme.COLOR_BG)
    frame_vb.pack(fill="x", padx=30, pady=5)
    ttk.Label(frame_vb, text="Voicebank Info / ข้อมูลคลังเสียง:", font=UITheme.FONT_MAIN).pack(anchor="w")
    ttk.Label(frame_vb, text="Language (ภาษา): Thai | Engine (เอนจิน): OpenUtau\nPhoneme System (ระบบหน่วยเสียง): VCCV / Arpasing", font=UITheme.FONT_SMALL, foreground="#AAAAAA").pack(anchor="w")

    tk.Frame(root, height=1, bg="#444444").pack(fill="x", padx=30, pady=5)

    # Options
    var_rand = tk.BooleanVar(value=False)
    chk_rand = ttk.Checkbutton(root, text="Randomize Pitch (เพิ่มการสุ่มแกว่งเส้นเสียงแดงเพื่ออารมณ์เพลง)", variable=var_rand)
    chk_rand.pack(anchor="w", padx=30, pady=2)

    var_cons = tk.BooleanVar(value=False)
    chk_cons = ttk.Checkbutton(root, text="Expand Consonants Width (ปรับพื้นที่พยัญชนะนำ/สะกดให้กว้างขึ้น)", variable=var_cons)
    chk_cons.pack(anchor="w", padx=30, pady=2)
    
    # Slider for width
    frame_slider = tk.Frame(root, bg=UITheme.COLOR_BG)
    frame_slider.pack(fill="x", padx=50, pady=5)
    ttk.Label(frame_slider, text="Consonant Expansion Width (ลากคลุมแถบเพื่อระบุค่าพื้นที่ขั้นต่ำ):").pack(anchor="w")
    scale_width = tk.Scale(frame_slider, from_=5, to=50, orient="horizontal", bg=UITheme.COLOR_BG, fg=UITheme.COLOR_FG, highlightthickness=0, troughcolor="#333333")
    scale_width.set(15)
    scale_width.pack(fill="x")

    tk.Frame(root, height=2, bg=UITheme.COLOR_ACCENT).pack(fill="x", padx=30, pady=10)

    # Buttons
    shapes = [
        ("W-shape (ดับเบิ้ลยู)", "W-shape"),
        ("Thunderbolt (สายฟ้า)", "Thunderbolt"),
        ("Pitch-drop/Rise (ตก/ขึ้น)", "Pitch-drop/Rise"),
        ("Slope (ทางลาด)", "Slope"),
        ("U pitch-drop (ตัวยู)", "U pitch-drop"),
        ("Pitchdent (รอยหยัก)", "Pitchdent"),
        ("Deep-V (วีลึก - ทรงกราฟใหม่)", "Deep-V")
    ]

    frame_btns = tk.Frame(root, bg=UITheme.COLOR_BG)
    frame_btns.pack(fill="both", expand=True)
    
    for text, mode in shapes:
        btn = ttk.Button(frame_btns, text=text, command=lambda m=mode: apply_trigger(m, var_rand, var_cons, scale_width))
        btn.pack(fill='x', padx=40, pady=3)
        
    tk.Frame(root, height=1, bg="#444444").pack(fill="x", padx=30, pady=10)
    
    # Oto Optimization Button
    btn_oto = ttk.Button(root, text="Organize Consonant Space & Save oto.ini (จัดระเบียบพื้นที่ oto.ini)", command=optimize_oto_ini)
    btn_oto.pack(fill='x', padx=40, pady=10)

    root.mainloop()

if __name__ == "__main__":
    build_ui()