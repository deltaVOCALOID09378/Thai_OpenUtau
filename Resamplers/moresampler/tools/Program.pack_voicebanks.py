#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Program.pack_voicebanks.py
e:\Program Developing\Moresampler-main\Program.pack_voicebanks.py
Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
Version 1.0
==============================================================================
Pack Voicebanks into .rar Archives with Auto-Version Counting
Supports upload to Google Drive via rclone or copy to K: drive
==============================================================================
"""

import sys
import os
import re
import subprocess
import glob
import argparse

if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass

# === CONFIG ===
RAR_EXE = r"C:\Program Files\WinRAR\Rar.exe"
DEFAULT_SOURCE = r"B:\All Voicebank in DELTA SYNTH\For DELTA SYNTH"
DEFAULT_OUTPUT = r"B:\All Voicebank in DELTA SYNTH\UFRV1.0"
GDRIVE_FOLDER_ID = "1tboFHk0sj2Util_1CGBvqEPfV-qqCvMx"
K_DRIVE_PATH = r"K:\Voicebank Archives"


def find_existing_versions(output_dir, base_name):
    """
    Find existing .rar files matching the base name and return the highest version.
    Pattern: BaseName v1.0.rar, BaseName v2.0.rar, etc.
    Also matches: BaseName_v1.0.rar, BaseName 1.0.rar
    """
    highest = 0.0
    safe_base = re.escape(base_name.rstrip())

    # Search patterns for version numbers
    patterns = [
        re.compile(rf"^{safe_base}\s*v?\.?\s*(\d+(?:\.\d+)?)\s*\.rar$", re.IGNORECASE),
        re.compile(rf"^{safe_base}\s+v(\d+(?:\.\d+)?)\s*\.rar$", re.IGNORECASE),
        re.compile(rf"^{safe_base}_v(\d+(?:\.\d+)?)\s*\.rar$", re.IGNORECASE),
    ]

    if not os.path.isdir(output_dir):
        return highest

    for fname in os.listdir(output_dir):
        if not fname.lower().endswith(".rar"):
            continue
        for pat in patterns:
            m = pat.match(fname)
            if m:
                ver = float(m.group(1))
                if ver > highest:
                    highest = ver
                break

    return highest


def get_next_version(output_dir, base_name):
    """Get the next version number (existing + 1.0)."""
    existing = find_existing_versions(output_dir, base_name)
    if existing > 0:
        return existing + 1.0
    return 1.0


def pack_voicebank(source_dir, output_dir, dry_run=False):
    """Pack a single voicebank directory into .rar with versioned name."""
    base_name = os.path.basename(source_dir)

    # Skip empty directories or non-voicebank dirs
    has_content = False
    for root, dirs, files in os.walk(source_dir):
        if any(f.lower().endswith((".wav", ".ini", ".txt", ".yaml")) for f in files):
            has_content = True
            break

    if not has_content:
        return None, "No voicebank content found"

    # Determine version
    version = get_next_version(output_dir, base_name)
    version_str = f"v{version:.1f}"

    # Clean up base name - remove existing version if present
    clean_name = re.sub(r'\s*v\d+\.\d+\s*$', '', base_name, flags=re.IGNORECASE).strip()
    archive_name = f"{clean_name} {version_str}.rar"
    archive_path = os.path.join(output_dir, archive_name)

    print(f"  [{version_str}] {base_name}")
    print(f"       -> {archive_name}")

    if dry_run:
        print(f"       (dry run - no archive created)")
        return archive_path, "dry_run"

    # Create output directory if needed
    os.makedirs(output_dir, exist_ok=True)

    # Skip if archive already exists
    if os.path.exists(archive_path):
        print(f"       [SKIP] Archive already exists")
        return archive_path, "exists"

    # Run WinRAR
    if not os.path.isfile(RAR_EXE):
        print(f"       [ERROR] WinRAR not found at: {RAR_EXE}")
        return None, "rar_not_found"

    cmd = [
        RAR_EXE,
        "a",           # Add to archive
        "-r",          # Recurse subdirectories
        "-ep1",        # Exclude base directory from paths
        "-m3",         # Normal compression
        "-o+",         # Overwrite existing files
        "-y",          # Yes to all queries
        archive_path,
        os.path.join(source_dir, "*"),
    ]

    try:
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=600,
            encoding="utf-8",
            errors="replace"
        )
        if result.returncode == 0:
            size_mb = os.path.getsize(archive_path) / (1024 * 1024)
            print(f"       [OK] Created ({size_mb:.1f} MB)")
            return archive_path, "created"
        else:
            print(f"       [ERROR] RAR failed: {result.stderr[:200]}")
            return None, "rar_error"
    except subprocess.TimeoutExpired:
        print(f"       [ERROR] RAR timed out (10 min)")
        return None, "timeout"
    except Exception as e:
        print(f"       [ERROR] {e}")
        return None, str(e)


def upload_to_gdrive_rclone(archive_path, remote_name="gdrive"):
    """Upload archive to Google Drive using rclone."""
    if not archive_path or not os.path.isfile(archive_path):
        return False

    fname = os.path.basename(archive_path)
    dest = f"{remote_name}:Voicebank Archives/{fname}"

    print(f"       [UPLOAD] rclone -> {dest}")

    try:
        result = subprocess.run(
            ["rclone", "copy", archive_path, f"{remote_name}:Voicebank Archives/",
             "--progress", "--transfers", "1"],
            capture_output=True, text=True, timeout=3600,
            encoding="utf-8", errors="replace"
        )
        if result.returncode == 0:
            print(f"       [OK] Uploaded to Google Drive")
            return True
        else:
            print(f"       [ERROR] rclone: {result.stderr[:200]}")
            return False
    except FileNotFoundError:
        print(f"       [WARN] rclone not installed - skipping upload")
        return False
    except Exception as e:
        print(f"       [ERROR] Upload failed: {e}")
        return False


def upload_to_gdrive_browser(archive_path):
    """Open Google Drive upload page in browser as fallback."""
    folder_url = f"https://drive.google.com/drive/u/0/folders/{GDRIVE_FOLDER_ID}"
    print(f"       [INFO] Opening Google Drive folder in browser...")
    print(f"       [INFO] URL: {folder_url}")
    print(f"       [INFO] Please drag and drop the archive file manually:")
    print(f"       [INFO] File: {archive_path}")
    try:
        os.startfile(folder_url)
    except Exception:
        pass


def copy_to_k_drive(archive_path):
    """Copy archive to K: drive if available."""
    if not archive_path or not os.path.isfile(archive_path):
        return False

    if not os.path.exists("K:\\"):
        print(f"       [WARN] K: drive not accessible - skipping")
        return False

    os.makedirs(K_DRIVE_PATH, exist_ok=True)
    dest = os.path.join(K_DRIVE_PATH, os.path.basename(archive_path))

    print(f"       [COPY] -> {dest}")
    try:
        import shutil
        shutil.copy2(archive_path, dest)
        print(f"       [OK] Copied to K: drive")
        return True
    except Exception as e:
        print(f"       [ERROR] Copy failed: {e}")
        return False


def main():
    parser = argparse.ArgumentParser(
        description="Pack voicebanks into .rar archives with auto-versioning"
    )
    parser.add_argument("source", nargs="?", default=DEFAULT_SOURCE,
                        help="Source directory containing voicebank folders")
    parser.add_argument("--output", "-o", default=DEFAULT_OUTPUT,
                        help="Output directory for .rar archives")
    parser.add_argument("--dry-run", action="store_true",
                        help="Preview only, no archives created")
    parser.add_argument("--upload-gdrive", action="store_true",
                        help="Upload to Google Drive via rclone")
    parser.add_argument("--upload-browser", action="store_true",
                        help="Open Google Drive in browser for manual upload")
    parser.add_argument("--copy-k", action="store_true",
                        help="Copy archives to K: drive")
    parser.add_argument("--single", default=None,
                        help="Pack only a single voicebank folder name")
    args = parser.parse_args()

    source_dir = os.path.abspath(args.source)
    output_dir = os.path.abspath(args.output)

    print(f"\n{'='*70}")
    print(f"  DELTA SYNTH - Voicebank Packer")
    print(f"  Source:  {source_dir}")
    print(f"  Output:  {output_dir}")
    print(f"  Mode:    {'DRY RUN' if args.dry_run else 'LIVE'}")
    print(f"{'='*70}\n")

    if not os.path.isdir(source_dir):
        print(f"[!] Source directory not found: {source_dir}")
        return

    # Get list of voicebank folders
    if args.single:
        target_path = os.path.join(source_dir, args.single)
        if os.path.isdir(target_path):
            vb_dirs = [target_path]
        else:
            print(f"[!] Voicebank not found: {args.single}")
            return
    else:
        vb_dirs = [
            os.path.join(source_dir, d)
            for d in sorted(os.listdir(source_dir))
            if os.path.isdir(os.path.join(source_dir, d))
        ]

    if not vb_dirs:
        print("[!] No voicebank folders found.")
        return

    print(f"  Found {len(vb_dirs)} voicebank folder(s)\n")

    created_archives = []
    for vb_dir in vb_dirs:
        archive_path, status = pack_voicebank(vb_dir, output_dir, dry_run=args.dry_run)
        if archive_path and status == "created":
            created_archives.append(archive_path)

    print(f"\n{'='*70}")
    print(f"  Archives created: {len(created_archives)}")
    print(f"{'='*70}\n")

    # Upload / copy phase
    if created_archives and not args.dry_run:
        if args.upload_gdrive:
            print("\n  --- Uploading to Google Drive (rclone) ---\n")
            for arc in created_archives:
                upload_to_gdrive_rclone(arc)

        if args.upload_browser and created_archives:
            print("\n  --- Opening Google Drive for manual upload ---\n")
            upload_to_gdrive_browser(created_archives[0])

        if args.copy_k:
            print("\n  --- Copying to K: drive ---\n")
            for arc in created_archives:
                copy_to_k_drive(arc)


if __name__ == "__main__":
    main()
