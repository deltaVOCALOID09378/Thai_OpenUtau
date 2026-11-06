#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ============================================================================
# 1. วัน/เดือน/ปี: 6 พฤศจิกายน พ.ศ. 2569 / 6 November 2026
# 2. ชื่อไฟล์โค้ด: Program.moresampler_coordinator.py
# 3. ลำดับเวอร์ชันของไฟล์: 1.0
# 4. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
# 5. ตำแหน่งที่จัดวางไฟล์: Resamplers/moresampler/tools/Program.moresampler_coordinator.py
# 6. รายละเอียดการแก้ไข: ศูนย์กลางการประสานงานเครื่องมือวิเคราะห์ สังเคราะห์ และปรับแต่งคลังเสียงสำหรับ Moresampler
# ============================================================================

import os
import sys
import subprocess
import argparse
import datetime
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parent
RESAMPLER_DIR = TOOLS_DIR.parent
PROJECT_ROOT = RESAMPLER_DIR.parent.parent
LOGS_DIR = TOOLS_DIR / "logs"

LOGS_DIR.mkdir(parents=True, exist_ok=True)

class MoresamplerCoordinator:
    """Master Coordinator and Dispatcher for Moresampler source tools and voicebank optimization."""

    def __init__(self, target_dir=None):
        self.target_dir = Path(target_dir).resolve() if target_dir else None
        self.timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        self.log_file = LOGS_DIR / f"coordinator_{self.timestamp}.log"

    def log(self, message: str, level: str = "INFO"):
        entry = f"[{datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] [{level}] {message}"
        print(entry)
        try:
            with open(self.log_file, "a", encoding="utf-8") as f:
                f.write(entry + "\n")
        except Exception:
            pass

    def run_subtool(self, script_name: str, args: list = None) -> int:
        script_path = TOOLS_DIR / script_name
        if not script_path.exists():
            self.log(f"Required subtool script not found: {script_path}", level="ERROR")
            return 1

        cmd = [sys.executable, str(script_path)]
        if args:
            cmd.extend(args)

        self.log(f"Dispatching subtool: {script_name} with arguments: {args}")
        try:
            res = subprocess.run(cmd, cwd=str(TOOLS_DIR))
            if res.returncode == 0:
                self.log(f"Subtool {script_name} completed successfully.", level="OK")
            else:
                self.log(f"Subtool {script_name} exited with code {res.returncode}", level="WARNING")
            return res.returncode
        except Exception as ex:
            self.log(f"Failed to execute {script_name}: {ex}", level="ERROR")
            return 1

    def action_inspect_binary(self, exe_path: str = None):
        """Inspect moresampler.exe PE structure, dependencies, and Lua chunks."""
        self.log("--- Action: Inspect Moresampler Binary Architecture ---")
        target_exe = exe_path or str(RESAMPLER_DIR / "moresampler.exe")
        self.log(f"Target executable: {target_exe}")
        
        # 1. Run analyze_pe.py
        self.run_subtool("analyze_pe.py", [target_exe] if exe_path else [])
        # 2. Run deep_inspect.py
        self.run_subtool("deep_inspect.py", [target_exe] if exe_path else [])

    def action_audit_voicebank(self, vb_path: str):
        """Audit voicebank integrity and oto completeness."""
        self.log(f"--- Action: Audit Voicebank --- ({vb_path})")
        return self.run_subtool("delta_voicebank_master_suite.py", ["--path", vb_path])

    def action_optimize_thai(self, vb_path: str):
        """Optimize Thai VCCV and CVVC oto configurations."""
        self.log(f"--- Action: Optimize Thai Oto for Moresampler --- ({vb_path})")
        return self.run_subtool("thai_oto_optimizer.py", ["--path", vb_path])

    def action_balance_vcv(self, vb_path: str):
        """Balance VCV multi-pitch oto configurations."""
        self.log(f"--- Action: Balance VCV Oto --- ({vb_path})")
        return self.run_subtool("vcv_master_oto_balancer.py", ["--path", vb_path])

    def action_balance_arpasing(self, vb_path: str):
        """Balance Arpasing oto configurations."""
        self.log(f"--- Action: Balance Arpasing Oto --- ({vb_path})")
        return self.run_subtool("arpasing_oto_balancer.py", ["--path", vb_path])

    def action_optimize_diffsinger(self, ds_path: str):
        """Optimize DiffSinger dataset and transcripts."""
        self.log(f"--- Action: Optimize DiffSinger Dataset --- ({ds_path})")
        return self.run_subtool("diffsinger_suite_optimizer.py", ["--path", ds_path])

    def action_repair_wav(self, vb_path: str):
        """Repair and normalize WAV file extensions and headers."""
        self.log(f"--- Action: Repair WAV Files --- ({vb_path})")
        return self.run_subtool("Program.fix_wav_extensions.py", ["--path", vb_path])

    def action_sync_template(self, vb_path: str, template_path: str = None):
        """Synchronize Oto from template."""
        self.log(f"--- Action: Sync Oto Template --- ({vb_path})")
        args = ["--path", vb_path]
        if template_path:
            args.extend(["--template", template_path])
        return self.run_subtool("Program.sync_oto_from_template.py", args)

    def action_tighten_cutoffs(self, vb_path: str):
        """Tighten safe negative cutoffs."""
        self.log(f"--- Action: Tighten Cutoffs --- ({vb_path})")
        return self.run_subtool("tighten_cutoffs.py", ["--path", vb_path])

    def action_restore_oto(self, vb_path: str):
        """Restore Oto from backup."""
        self.log(f"--- Action: Restore Oto Backup --- ({vb_path})")
        return self.run_subtool("restore_oto.py", ["--path", vb_path])

    def action_pack_voicebanks(self, vb_path: str):
        """Pack voicebanks into standalone zip package."""
        self.log(f"--- Action: Pack Voicebanks --- ({vb_path})")
        return self.run_subtool("Program.pack_voicebanks.py", ["--path", vb_path])

    def action_pipeline_all(self, vb_path: str):
        """Execute full high-value pipeline: Repair -> Sync -> Optimize -> Tighten -> Audit -> Pack."""
        self.log(f"============================================================")
        self.log(f"Starting Unified High-Value Pipeline for: {vb_path}")
        self.log(f"============================================================")

        # 1. Repair WAVs
        self.action_repair_wav(vb_path)

        # 2. Template Sync
        self.action_sync_template(vb_path)

        # 3. Detection of Voicebank Style & Specialized Optimization
        is_thai = any((Path(vb_path) / name).exists() for name in ["th_vccv_phonemes.yaml", "TH-VCCV_Dict.txt", "thai_oto.ini"])
        is_arpasing = (Path(vb_path) / "arpasing.txt").exists() or any("arp" in f.name.lower() for f in Path(vb_path).glob("*.txt"))

        if is_thai:
            self.log("Detected Thai Vocal Synth Voicebank - running Thai Oto Optimizer...")
            self.action_optimize_thai(vb_path)
        elif is_arpasing:
            self.log("Detected Arpasing Voicebank - running Arpasing Balancer...")
            self.action_balance_arpasing(vb_path)
        else:
            self.log("Running VCV Master Balancer...")
            self.action_balance_vcv(vb_path)

        # 4. Tighten Cutoffs
        self.action_tighten_cutoffs(vb_path)

        # 5. Master Audit
        self.action_audit_voicebank(vb_path)

        # 6. Pack
        self.action_pack_voicebanks(vb_path)

        self.log(f"Unified Pipeline completed successfully for: {vb_path}", level="OK")

def interactive_menu():
    print("====================================================================")
    print("      DELTA SYNTH - Moresampler Master Tool Coordinator v1.0        ")
    print("   Orchestrator for Resamplers, Oto Optimizers & Voicebank Suite    ")
    print("====================================================================")
    print("  [1] Inspect Moresampler Binary Architecture (PE & Embedded Lua)")
    print("  [2] Run Full Voicebank Optimization Pipeline (End-to-End)")
    print("  [3] Optimize Thai Voicebank (VCCV / CVVC)")
    print("  [4] Balance VCV Multi-Pitch Voicebank")
    print("  [5] Balance Arpasing Voicebank")
    print("  [6] Repair WAV File Extensions & Audio Headers")
    print("  [7] Audit Voicebank Integrity & Parameter Consistency")
    print("  [8] Pack Voicebank to Distribution Archive")
    print("  [0] Exit")
    print("====================================================================")
    choice = input("Select an option (0-8): ").strip()
    
    coordinator = MoresamplerCoordinator()
    if choice == "1":
        coordinator.action_inspect_binary()
    elif choice in ["2", "3", "4", "5", "6", "7", "8"]:
        target = input("Enter voicebank folder path: ").strip().strip('"')
        if not target or not Path(target).exists():
            print(f"[ERROR] Target folder not found: {target}")
            return
        if choice == "2":
            coordinator.action_pipeline_all(target)
        elif choice == "3":
            coordinator.action_optimize_thai(target)
        elif choice == "4":
            coordinator.action_balance_vcv(target)
        elif choice == "5":
            coordinator.action_balance_arpasing(target)
        elif choice == "6":
            coordinator.action_repair_wav(target)
        elif choice == "7":
            coordinator.action_audit_voicebank(target)
        elif choice == "8":
            coordinator.action_pack_voicebanks(target)
    elif choice == "0":
        print("Exiting coordinator.")
    else:
        print("Invalid option.")

def main():
    parser = argparse.ArgumentParser(description="DELTA SYNTH - Moresampler Master Tool Coordinator")
    parser.add_argument("action", nargs="?", choices=[
        "inspect-binary", "pipeline-all", "audit", "optimize-thai", 
        "balance-vcv", "balance-arpasing", "optimize-diffsinger", 
        "repair-wav", "sync-template", "tighten", "restore", "pack"
    ], help="Action to execute")
    parser.add_argument("--path", "-p", help="Target voicebank or file path")
    parser.add_argument("--template", "-t", help="Template path for sync-template")
    parser.add_argument("--interactive", "-i", action="store_true", help="Launch interactive menu")

    args = parser.parse_args()

    if args.interactive or not args.action:
        interactive_menu()
        return

    coordinator = MoresamplerCoordinator(args.path)

    if args.action == "inspect-binary":
        coordinator.action_inspect_binary(args.path)
    elif args.action == "pipeline-all":
        if not args.path:
            print("[ERROR] --path is required for pipeline-all")
            sys.exit(1)
        coordinator.action_pipeline_all(args.path)
    elif args.action == "audit":
        coordinator.action_audit_voicebank(args.path)
    elif args.action == "optimize-thai":
        coordinator.action_optimize_thai(args.path)
    elif args.action == "balance-vcv":
        coordinator.action_balance_vcv(args.path)
    elif args.action == "balance-arpasing":
        coordinator.action_balance_arpasing(args.path)
    elif args.action == "optimize-diffsinger":
        coordinator.action_optimize_diffsinger(args.path)
    elif args.action == "repair-wav":
        coordinator.action_repair_wav(args.path)
    elif args.action == "sync-template":
        coordinator.action_sync_template(args.path, args.template)
    elif args.action == "tighten":
        coordinator.action_tighten_cutoffs(args.path)
    elif args.action == "restore":
        coordinator.action_restore_oto(args.path)
    elif args.action == "pack":
        coordinator.action_pack_voicebanks(args.path)

if __name__ == "__main__":
    main()
