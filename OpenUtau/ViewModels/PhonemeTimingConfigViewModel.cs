// ============================================================================
// 1. วัน/เดือน/ปี: 11 พฤศจิกายน พ.ศ. 2569 / 11 November 2026
// 2. ชื่อไฟล์โค้ด: PhonemeTimingConfigViewModel.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.1
// 4. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
// 5. ตำแหน่งที่จัดวางไฟล์: OpenUtau/ViewModels/PhonemeTimingConfigViewModel.cs
// 6. รายละเอียดการแก้ไข: โค้ดตัวจัดการข้อมูลและตรรกะ (ViewModel) สำหรับ Phoneme Timing & Ratio Balancer สเกล 0-100% ครบทุกโครงสร้างคำตามมาตรฐาน DELTA SYNTH
// ============================================================================

using System;
using System.Collections.Generic;
using System.Reactive;
using OpenUtau.Core;
using OpenUtau.Core.Editing;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class PhonemeTimingConfigViewModel : ViewModelBase {
        // --- ค่าคำปกติ (Normal) สเกล 0 - 100% ---
        [Reactive] public int NormalStartSilence { get; set; } = 50; // - C (5/10)
        [Reactive] public int NormalStartCv { get; set; } = 70;      // CV (7/10)
        [Reactive] public int NormalStartVc { get; set; } = 30;      // VC (3/10)

        [Reactive] public int NormalMidOpenCv { get; set; } = 80;    // ก กา (8/10)
        [Reactive] public int NormalMidCodaCv { get; set; } = 70;    // มีตัวสะกด (7/10)

        [Reactive] public int NormalEndOpenTail { get; set; } = 20;  // ก กา หางเสียง (2/10)
        [Reactive] public int NormalEndCodaTail { get; set; } = 10;  // มีตัวสะกด หางเสียง (1/10)

        // --- ค่าคำควบกล้ำ (Clusters) สเกล 0 - 100% ---
        [Reactive] public int ClusterStartSilence { get; set; } = 50; // - C (5/10)
        [Reactive] public int ClusterStartLead { get; set; } = 10;    // CV รอยต่อแรก (1/10)
        [Reactive] public int ClusterStartMainCv { get; set; } = 70;  // CV หลัก (7/10)

        [Reactive] public int ClusterMidCcv { get; set; } = 10;       // CCV (1/10)
        [Reactive] public int ClusterMidMainCv { get; set; } = 60;    // CV หลัก (6/10)
        [Reactive] public int ClusterEndTail { get; set; } = 10;      // V - (1/10)

        // --- ค่าสระเปล่าและตัวสะกด (Plain Vowels & Codas) สเกล 0 - 100% ---
        [Reactive] public int PlainVowelOnset { get; set; } = 40;
        [Reactive] public int PlainVowelStretch { get; set; } = 65;
        [Reactive] public int EndingCodaScale { get; set; } = 30;

        // --- พรีเซ็ตระบบคลังเสียง ---
        [Reactive] public int SelectedSystemIndex { get; set; } = 0; // 0: Auto, 1: VCV, 2: CVVC, 3: VCCV, 4: DiffSinger

        // --- ขอบเขตการทำงาน ---
        [Reactive] public bool ApplyProjectWide { get; set; } = true;

        // --- คำสั่ง (Commands) ---
        public ReactiveCommand<Unit, Unit> ResetDefaultCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public Action? RequestClose { get; set; }

        private readonly UVoicePart? targetPart;
        private readonly List<UNote>? selectedNotes;

        public PhonemeTimingConfigViewModel() : this(null, null) { }

        public PhonemeTimingConfigViewModel(UVoicePart? part, List<UNote>? selected) {
            targetPart = part;
            selectedNotes = selected;

            ResetDefaultCommand = ReactiveCommand.Create(ResetToDefault);
            SaveCommand = ReactiveCommand.Create(SaveAndApply);
            CancelCommand = ReactiveCommand.Create(Cancel);

            this.WhenAnyValue(x => x.SelectedSystemIndex)
                .Subscribe(idx => OnSystemPresetChanged(idx));
        }

        private void OnSystemPresetChanged(int idx) {
            if (idx <= 0) return; // 0 คือ Auto
            var profile = (SingingSystemProfile)idx;
            var config = new PhonemeTimingConfig();
            config.ApplyPreset(profile);

            NormalStartSilence = config.SilenceScale;
            NormalStartCv = config.MaeKkScale;
            NormalStartVc = config.EndingCodaScale;
            NormalMidOpenCv = config.MaeKkScale;
            NormalMidCodaCv = config.EndingCodaScale;
            NormalEndOpenTail = config.TailScale;
            NormalEndCodaTail = Math.Max(10, config.TailScale - 5);

            ClusterStartSilence = config.SilenceScale;
            ClusterStartLead = Math.Max(10, config.ClusterScale - 20);
            ClusterStartMainCv = config.MaeKkScale;
            ClusterMidCcv = Math.Min(25, config.ClusterScale);
            ClusterMidMainCv = config.ClusterScale;
            ClusterEndTail = config.TailScale;

            PlainVowelOnset = Math.Max(20, config.PlainVowelScale - 20);
            PlainVowelStretch = config.PlainVowelScale;
            EndingCodaScale = config.EndingCodaScale;
        }

        private void ResetToDefault() {
            NormalStartSilence = 50;
            NormalStartCv = 70;
            NormalStartVc = 30;
            NormalMidOpenCv = 80;
            NormalMidCodaCv = 70;
            NormalEndOpenTail = 20;
            NormalEndCodaTail = 10;

            ClusterStartSilence = 50;
            ClusterStartLead = 10;
            ClusterStartMainCv = 70;
            ClusterMidCcv = 10;
            ClusterMidMainCv = 60;
            ClusterEndTail = 10;

            PlainVowelOnset = 40;
            PlainVowelStretch = 65;
            EndingCodaScale = 30;

            SelectedSystemIndex = 0;
            ApplyProjectWide = true;
        }

        private void SaveAndApply() {
            var config = new PhonemeTimingConfig {
                SilenceScale = NormalStartSilence,
                LeadingConsonantScale = NormalStartCv,
                MaeKkScale = NormalMidOpenCv,
                ClusterScale = ClusterMidMainCv,
                PlainVowelScale = PlainVowelStretch,
                EndingCodaScale = EndingCodaScale,
                TailScale = NormalEndOpenTail,
                Profile = (SingingSystemProfile)SelectedSystemIndex,
                ApplyProjectWide = ApplyProjectWide,
            };

            PhonemeTimingScaler.ApplyScaling(
                DocManager.Inst.Project,
                targetPart,
                selectedNotes,
                config,
                DocManager.Inst);

            RequestClose?.Invoke();
        }

        private void Cancel() {
            RequestClose?.Invoke();
        }
    }
}
