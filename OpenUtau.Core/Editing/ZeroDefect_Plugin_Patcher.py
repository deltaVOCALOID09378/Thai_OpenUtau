# Made And Checked By DELTA SYNTH & Gemini AI
# Original by DELTA SYNTH
# Version: 1.0.0

import os
import re
import sys
from pathlib import Path

# ==========================================
# Configuration & Theme Definitions
# ==========================================
class LogTheme:
    BG_BLACK = "\033[48;2;26;26;26m"  # #1A1A1A
    FG_WHITE = "\033[38;2;240;240;240m" # #F0F0F0
    FG_RED = "\033[38;2;204;34;0m"    # #CC2200
    RESET = "\033[0m"

def print_log(message: str, is_error: bool = False) -> None:
    color = LogTheme.FG_RED if is_error else LogTheme.FG_WHITE
    print(f"{LogTheme.BG_BLACK}{color}[DELTA SYNTH] {message}{LogTheme.RESET}")

# ==========================================
# Core Refactoring Logic
# ==========================================
def patch_access_modifiers(root_dir: Path) -> int:
    """Refactors GetLangCode access modifier in all .cs files to prevent TypeLoadException."""
    files_changed = 0
    cs_files = list(root_dir.rglob("*.cs"))
    
    # Regex to match protected/internal override string GetLangCode
    pattern = re.compile(r'(protected|internal|private)\s+override\s+string\s+GetLangCode')
    
    for file_path in cs_files:
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                
            if pattern.search(content):
                new_content = pattern.sub('public override string GetLangCode', content)
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                print_log(f"Patched access modifier in: {file_path.name}")
                files_changed += 1
        except Exception as e:
            print_log(f"Failed to process {file_path.name}: {e}", is_error=True)
            
    return files_changed

def patch_target_framework(root_dir: Path) -> int:
    """Downgrades .NET 10 to .NET Standard 2.1 in all .csproj files to prevent FileNotFoundException."""
    files_changed = 0
    csproj_files = list(root_dir.rglob("*.csproj"))
    
    pattern = re.compile(r'<TargetFramework>net10\.0</TargetFramework>', re.IGNORECASE)
    
    for file_path in csproj_files:
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                
            if pattern.search(content):
                new_content = pattern.sub('<TargetFramework>netstandard2.1</TargetFramework>', content)
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                print_log(f"Downgraded TargetFramework in: {file_path.name}")
                files_changed += 1
        except Exception as e:
            print_log(f"Failed to process {file_path.name}: {e}", is_error=True)
            
    return files_changed

def main() -> None:
    print_log("Initializing Zero-Defect Plugin Patcher...")
    target_directory = Path(os.getcwd())
    
    print_log("Scanning for Access Modifier issues (TypeLoadException)...")
    cs_patched = patch_access_modifiers(target_directory)
    
    print_log("Scanning for Framework Compatibility issues (FileNotFoundException)...")
    csproj_patched = patch_target_framework(target_directory)
    
    print_log(f"Operation Complete. CS files patched: {cs_patched}, CSPROJ files patched: {csproj_patched}")

if __name__ == "__main__":
    main()