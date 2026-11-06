import os

base_dir = r'B:\All Voicebank in DELTA SYNTH'
GAP = 30.0
modified_files = 0

for root, dirs, files in os.walk(base_dir):
    for file in files:
        if file.lower() == 'oto.ini':
            path = os.path.join(root, file)
            try:
                with open(path, 'r', encoding='shift-jis', errors='ignore') as f:
                    lines = f.readlines()
                
                new_lines = []
                changed = False
                for line in lines:
                    line = line.strip()
                    if not line or '=' not in line:
                        new_lines.append(line)
                        continue
                        
                    wav_part, params = line.split('=', 1)
                    parts = params.split(',')
                    
                    if len(parts) >= 6:
                        alias = parts[0]
                        offset = parts[1]
                        cons = parts[2]
                        cut = parts[3]
                        pre = parts[4]
                        ovl = parts[5]
                        
                        try:
                            cons_val = float(cons) if cons else 0.0
                            new_cut = -round(cons_val + GAP, 2)
                            
                            # Format cleanly, avoiding .0 if it's an integer
                            new_cut_str = str(new_cut)
                            if new_cut_str.endswith('.0'):
                                new_cut_str = new_cut_str[:-2]
                                
                            if parts[3] != new_cut_str:
                                parts[3] = new_cut_str
                                changed = True
                        except ValueError:
                            pass
                            
                    new_lines.append(wav_part + '=' + ','.join(parts))
                
                if changed:
                    with open(path, 'w', encoding='shift-jis') as f:
                        f.write('\n'.join(new_lines) + '\n')
                    modified_files += 1
            except Exception as e:
                print(f"Error processing {path}: {e}")

print(f"\n[OK] Successfully tightened Cutoffs for {modified_files} oto.ini files!")
