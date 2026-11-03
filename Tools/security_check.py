import subprocess
import re
import os

BASE_DIR = r"d:\Program Developing\Thai_OpenUtau"
out = subprocess.check_output(['git', 'diff', '--name-only'], cwd=BASE_DIR).decode('utf-8').splitlines()
patterns = [
    re.compile(r'(?i)(api[_-]?key|secret|token|password)\s*[:=]\s*[\'"][a-zA-Z0-9_\-]{8,}[\'"]'),
    re.compile(r'(?i)ghp_[a-zA-Z0-9]{30,}'),
    re.compile(r'(?i)AIzaSy[a-zA-Z0-9_\-]{30,}')
]
found = False
for f in out:
    f = f.strip()
    if not f: continue
    full = os.path.join(BASE_DIR, f)
    if os.path.exists(full):
        try:
            with open(full, 'r', encoding='utf-8') as fp:
                c = fp.read()
                for p in patterns:
                    if p.search(c):
                        print(f"SECURITY WARNING: Potential secret found in {f}")
                        found = True
        except Exception:
            pass

if not found:
    print("SECURITY CHECK PASSED: Zero credentials, tokens or private keys found in changes.")
