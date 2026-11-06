import os
import shutil

base_dir = r'B:\All Voicebank in DELTA SYNTH'
restored_count = 0
for root, dirs, files in os.walk(base_dir):
    for file in files:
        if file.lower() == 'oto.ini.bak':
            bak_path = os.path.join(root, file)
            ini_path = os.path.join(root, 'oto.ini')
            try:
                # Copy bak over ini
                shutil.copy2(bak_path, ini_path)
                restored_count += 1
                # Optional: print(f"Restored {ini_path}")
            except Exception as e:
                print(f"Failed to restore {ini_path}: {e}")

print(f"\n[OK] Successfully restored {restored_count} oto.ini files from backup!")
