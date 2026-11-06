#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Program.fix_wav_extensions.py
e:\Program Developing\Moresampler-main\Program.fix_wav_extensions.py
Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
Version 1.0
==============================================================================
Fix Duplicate File Extensions (.wav.wav -> .wav) and Update oto.ini References
==============================================================================
"""

import sys
import os
import shutil

if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass

ENCODINGS = ["shift_jis", "cp932", "utf-8", "cp874", "latin-1"]
DUPLICATE_PATTERNS = [".wav.wav", ".WAV.WAV", ".WAV.wav", ".wav.WAV"]


def read_file_safe(path):
    """Read file trying multiple encodings."""
    for enc in ENCODINGS:
        try:
            with open(path, "r", encoding=enc) as f:
                return f.read(), enc
        except (UnicodeDecodeError, UnicodeError):
            continue
    return None, None


def write_file_safe(path, content, encoding):
    """Write file with original encoding."""
    with open(path, "w", encoding=encoding, newline="") as f:
        f.write(content)


def find_duplicate_ext_files(base_dir):
    """Find all files with duplicate extensions like .wav.wav"""
    results = []
    for root, dirs, files in os.walk(base_dir):
        for fname in files:
            lower = fname.lower()
            if lower.endswith(".wav.wav"):
                old_path = os.path.join(root, fname)
                # Remove the last .wav to fix the duplicate
                new_name = fname[:-4]
                # Safety: handle triple+ .wav.wav.wav
                while new_name.lower().endswith(".wav.wav"):
                    new_name = new_name[:-4]
                new_path = os.path.join(root, new_name)
                if old_path != new_path:
                    results.append((root, fname, new_name))
    return results


def update_oto_references(oto_path, old_name, new_name):
    """Update references in oto.ini from old filename to new filename."""
    content, enc = read_file_safe(oto_path)
    if content is None:
        return False

    if old_name not in content:
        return False

    new_content = content.replace(old_name, new_name)
    write_file_safe(oto_path, new_content, enc)
    return True


def fix_directory(base_dir, dry_run=False):
    """Fix all duplicate extension files in directory."""
    print(f"\n{'='*70}")
    print(f"  Scanning: {base_dir}")
    print(f"  Mode: {'DRY RUN (preview only)' if dry_run else 'LIVE (will rename files)'}")
    print(f"{'='*70}\n")

    duplicates = find_duplicate_ext_files(base_dir)

    if not duplicates:
        print("  [OK] No duplicate extensions found. All filenames are clean.")
        return 0

    print(f"  Found {len(duplicates)} file(s) with duplicate extensions:\n")

    renamed_count = 0
    oto_updated_count = 0

    for directory, old_name, new_name in duplicates:
        old_path = os.path.join(directory, old_name)
        new_path = os.path.join(directory, new_name)

        print(f"  [FIX] {old_name}")
        print(f"     -> {new_name}")

        if dry_run:
            print(f"     (dry run - no changes made)")
            continue

        # Check if target already exists
        if os.path.exists(new_path):
            print(f"     [!] Target already exists, skipping rename")
            continue

        # Rename the file
        try:
            os.rename(old_path, new_path)
            renamed_count += 1
            print(f"     [OK] File renamed")
        except Exception as e:
            print(f"     [ERROR] Rename failed: {e}")
            continue

        # Update oto.ini references in the same directory and parent directories
        search_dirs = [directory]
        parent = os.path.dirname(directory)
        if parent != directory:
            search_dirs.append(parent)
        grandparent = os.path.dirname(parent)
        if grandparent != parent:
            search_dirs.append(grandparent)

        for search_dir in search_dirs:
            if not os.path.isdir(search_dir):
                continue
            for fname in os.listdir(search_dir):
                if fname.lower() == "oto.ini":
                    oto_path = os.path.join(search_dir, fname)
                    if update_oto_references(oto_path, old_name, new_name):
                        oto_updated_count += 1
                        print(f"     [OK] Updated oto.ini: {oto_path}")

    print(f"\n{'='*70}")
    print(f"  Summary:")
    print(f"    Files renamed:      {renamed_count}")
    print(f"    oto.ini updated:    {oto_updated_count}")
    print(f"{'='*70}\n")
    return renamed_count


def main():
    import argparse
    parser = argparse.ArgumentParser(description="Fix duplicate file extensions (.wav.wav -> .wav)")
    parser.add_argument("path", nargs="?",
                        default=r"B:\All Voicebank in DELTA SYNTH",
                        help="Path to scan (default: B:\\All Voicebank in DELTA SYNTH)")
    parser.add_argument("--dry-run", action="store_true",
                        help="Preview changes without actually renaming")
    args = parser.parse_args()

    target = os.path.abspath(args.path)
    if not os.path.isdir(target):
        print(f"[!] Invalid directory: {target}")
        return

    fix_directory(target, dry_run=args.dry_run)


if __name__ == "__main__":
    main()
