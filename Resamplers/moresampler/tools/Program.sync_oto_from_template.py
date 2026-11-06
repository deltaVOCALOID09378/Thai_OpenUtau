#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Program.sync_oto_from_template.py
e:\Program Developing\Moresampler-main\Program.sync_oto_from_template.py
Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
Version 1.0
==============================================================================
Sync oto.ini from Template Voicebank to Target Voicebanks by Language
==============================================================================
"""

import sys
import os
import shutil
from datetime import datetime

if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass

# === CONFIG ===
TEMPLATE_BASE = (
    r"B:\All Voicebank in DELTA SYNTH\In The Process"
    r"\A Full TH-EN-JP\A Singer VB [ Copy This ] Full TH-EN-JP v.1.0"
)
TARGET_BASE = r"B:\All Voicebank in DELTA SYNTH\In The Process"

LANG_FOLDERS = ["EN", "TH", "JP"]

# Mapping: language folder name -> list of target subdirectory keywords
LANG_TARGET_MAP = {
    "EN": ["English Voicebank", "ENG Arpasing", "ENG VCCV"],
    "TH": ["Thai Voicebank", "THAI VCCV", "Thai VCCV"],
    "JP": ["Japanese Voicebank", "JPN VCV", "JPN CV", "JP VCV"],
}


def find_oto_files_by_lang(target_base, lang_keywords):
    """Find all oto.ini files under directories matching language keywords."""
    results = []
    for root, dirs, files in os.walk(target_base):
        rel = os.path.relpath(root, target_base)
        # Skip the template directory itself
        if "A Singer VB [ Copy This ]" in rel:
            continue
        for f in files:
            if f.lower() == "oto.ini":
                path_upper = root.upper()
                for kw in lang_keywords:
                    if kw.upper() in path_upper:
                        results.append(os.path.join(root, f))
                        break
    return results


def backup_file(path):
    """Create timestamped backup."""
    if os.path.exists(path):
        ts = datetime.now().strftime("%Y%m%d_%H%M%S")
        bak = path + f".sync_bak_{ts}"
        shutil.copy2(path, bak)
        return bak
    return None


def sync_oto(template_oto, target_oto, dry_run=False):
    """Copy template oto.ini over target, with backup."""
    if dry_run:
        print(f"  [DRY] Would copy: {os.path.basename(template_oto)}")
        print(f"        -> {target_oto}")
        return True

    bak = backup_file(target_oto)
    if bak:
        print(f"  [BAK] Backup: {os.path.basename(bak)}")

    try:
        shutil.copy2(template_oto, target_oto)
        print(f"  [OK]  Updated: {target_oto}")
        return True
    except Exception as e:
        print(f"  [ERR] Failed: {e}")
        return False


def main():
    import argparse
    parser = argparse.ArgumentParser(
        description="Sync oto.ini from template voicebank to all matching targets"
    )
    parser.add_argument("--dry-run", action="store_true",
                        help="Preview only, no changes")
    parser.add_argument("--template", default=TEMPLATE_BASE,
                        help="Path to template voicebank")
    parser.add_argument("--target", default=TARGET_BASE,
                        help="Path to target voicebank root")
    args = parser.parse_args()

    template_base = os.path.abspath(args.template)
    target_base = os.path.abspath(args.target)

    print(f"\n{'='*70}")
    print(f"  OTO.INI Sync Tool - DELTA SYNTH")
    print(f"  Template: {template_base}")
    print(f"  Target:   {target_base}")
    print(f"  Mode:     {'DRY RUN' if args.dry_run else 'LIVE'}")
    print(f"{'='*70}\n")

    total_synced = 0

    for lang in LANG_FOLDERS:
        template_dir = os.path.join(template_base, lang)
        template_oto = os.path.join(template_dir, "oto.ini")

        print(f"\n--- Language: {lang} ---")

        if not os.path.isfile(template_oto):
            print(f"  [SKIP] Template oto.ini not found: {template_oto}")
            continue

        keywords = LANG_TARGET_MAP.get(lang, [])
        targets = find_oto_files_by_lang(target_base, keywords)

        if not targets:
            print(f"  [SKIP] No target oto.ini found for {lang}")
            continue

        print(f"  Found {len(targets)} target oto.ini file(s):\n")

        for t in targets:
            rel = os.path.relpath(t, target_base)
            print(f"  Target: {rel}")
            if sync_oto(template_oto, t, dry_run=args.dry_run):
                total_synced += 1

    print(f"\n{'='*70}")
    print(f"  Total oto.ini synced: {total_synced}")
    print(f"{'='*70}\n")


if __name__ == "__main__":
    main()
