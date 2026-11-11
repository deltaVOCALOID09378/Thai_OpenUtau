// ============================================================================
// 1. วัน/เดือน/ปี: 11 พฤศจิกายน พ.ศ. 2569 / 11 November 2026
// 2. ชื่อไฟล์โค้ด: PhonemeTimingScaler.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.0
// 4. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
// 5. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Core/Editing/PhonemeTimingScaler.cs
// 6. รายละเอียดการแก้ไข: กลไกประมวลผลคำนวณและปรับสัดส่วนการกระจายเวลาหน่วยเสียง (0-100%) และบริหารจัดการ Alias ครอบคลุมพยัญชนำ, คำควบกล้ำ, สระเปล่า, แม่ ก กา, และตัวสะกด รองรับทั้งโปรเจกต์ (Global Project Scaling) และเข้ากับระบบคลังเสียง UTAU VCV, CVVC, VCCV, DiffSinger
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core.Editing {
    /// <summary>
    /// รูปแบบกลไกระบบคลังเสียงสำหรับปรับปรุงการหั่นเสียง
    /// </summary>
    public enum SingingSystemProfile {
        AutoDetect = 0,
        UtauVcv = 1,
        UtauCvvc = 2,
        UtauVccv = 3,
        DiffSinger = 4,
    }

    /// <summary>
    /// การตั้งค่ามาตราส่วนและอัตราส่วนเวลาหน่วยเสียง (Phoneme Timing Configuration)
    /// </summary>
    public class PhonemeTimingConfig {
        public int LeadingConsonantScale { get; set; } = 35; // 0-100% พยัญชนะนำ (- C)
        public int ClusterScale { get; set; } = 45;          // 0-100% คำควบกล้ำ (CC, CCV)
        public int PlainVowelScale { get; set; } = 60;       // 0-100% สระเปล่า (V, - V)
        public int MaeKkScale { get; set; } = 50;            // 0-100% แม่ ก กา / คำเปิด (CV, V C)
        public int EndingCodaScale { get; set; } = 35;       // 0-100% ตัวสะกด (VC, C -)
        public int SilenceScale { get; set; } = 30;          // 0-100% เสียงเงียบนำ (- C)
        public int TailScale { get; set; } = 25;             // 0-100% ปล่อยหางเสียง (V -, -)
        public SingingSystemProfile Profile { get; set; } = SingingSystemProfile.AutoDetect;
        public bool ApplyProjectWide { get; set; } = true;   // บังคับใช้ทั้งโปรเจกต์

        public void ApplyPreset(SingingSystemProfile profile) {
            Profile = profile;
            switch (profile) {
                case SingingSystemProfile.UtauVcv:
                    LeadingConsonantScale = 30;
                    ClusterScale = 35;
                    PlainVowelScale = 65;
                    MaeKkScale = 55;
                    EndingCodaScale = 30;
                    SilenceScale = 25;
                    TailScale = 30;
                    break;
                case SingingSystemProfile.UtauCvvc:
                    LeadingConsonantScale = 35;
                    ClusterScale = 40;
                    PlainVowelScale = 55;
                    MaeKkScale = 50;
                    EndingCodaScale = 40;
                    SilenceScale = 30;
                    TailScale = 25;
                    break;
                case SingingSystemProfile.UtauVccv:
                    // Guideline Ratios: CCV/C_C <= 5%, VC/VC- ≈ 25–40%, V- ≈ 20–30%
                    LeadingConsonantScale = 40;
                    ClusterScale = 45;
                    PlainVowelScale = 50;
                    MaeKkScale = 50;
                    EndingCodaScale = 35;
                    SilenceScale = 35;
                    TailScale = 25;
                    break;
                case SingingSystemProfile.DiffSinger:
                    // DiffSinger: leading consonant <= 5%, vowel ยืดตามโน้ต, ending consonant ≈ 25–40%
                    LeadingConsonantScale = 15;
                    ClusterScale = 25;
                    PlainVowelScale = 75;
                    MaeKkScale = 60;
                    EndingCodaScale = 35;
                    SilenceScale = 20;
                    TailScale = 20;
                    break;
                case SingingSystemProfile.AutoDetect:
                default:
                    LeadingConsonantScale = 35;
                    ClusterScale = 45;
                    PlainVowelScale = 60;
                    MaeKkScale = 50;
                    EndingCodaScale = 35;
                    SilenceScale = 30;
                    TailScale = 25;
                    break;
            }
        }
    }

    /// <summary>
    /// คลาสประมวลผลการคำนวณและปรับมาตราส่วนหน่วยเสียง
    /// </summary>
    public static class PhonemeTimingScaler {
        /// <summary>
        /// ดำเนินการปรับมาตราส่วนเวลาและ Alias ของหน่วยเสียงตามการตั้งค่าที่กำหนด
        /// </summary>
        public static void ApplyScaling(
            UProject project,
            UVoicePart? currentPart,
            List<UNote>? selectedNotes,
            PhonemeTimingConfig config,
            DocManager docManager) {

            if (project == null) return;

            docManager.StartUndoGroup("command.phoneme.timing_scaler", true);
            try {
                var targetParts = new List<UVoicePart>();
                if (config.ApplyProjectWide || currentPart == null) {
                    targetParts = project.parts.OfType<UVoicePart>().ToList();
                } else {
                    targetParts.Add(currentPart);
                }

                foreach (var part in targetParts) {
                    var notesToProcess = (part == currentPart && selectedNotes != null && selectedNotes.Count > 0)
                        ? selectedNotes
                        : part.notes.ToList();

                    if (notesToProcess.Count == 0) continue;

                    // สังเกตและวิเคราะห์ระบบคลังเสียงของแทร็กหากเลือก AutoDetect
                    var track = (part.trackNo >= 0 && part.trackNo < project.tracks.Count)
                        ? project.tracks[part.trackNo]
                        : null;
                    var profile = config.Profile;
                    if (profile == SingingSystemProfile.AutoDetect && track?.Singer != null) {
                        profile = DetectSystemProfile(track.Singer);
                    }

                    foreach (var note in notesToProcess) {
                        ScaleNotePhonemes(part, note, profile, config, docManager);
                    }
                }

                docManager.ExecuteCmd(new PhonemizedNotification(currentPart ?? targetParts.FirstOrDefault()!));
            } catch (Exception ex) {
                Log.Error(ex, "Failed to apply phoneme timing scaling.");
            } finally {
                docManager.EndUndoGroup();
            }
        }

        private static SingingSystemProfile DetectSystemProfile(USinger singer) {
            if (singer.SingerType == USingerType.DiffSinger) {
                return SingingSystemProfile.DiffSinger;
            }
            var systems = SingerCompatibilityService.GetCompatibleSingingSystems(singer);
            if (systems.Contains("UTAU VCCV") || systems.Contains("Thai VCCV / CVVC")) {
                return SingingSystemProfile.UtauVccv;
            }
            if (systems.Contains("UTAU VCV")) {
                return SingingSystemProfile.UtauVcv;
            }
            if (systems.Contains("UTAU CVVC")) {
                return SingingSystemProfile.UtauCvvc;
            }
            return SingingSystemProfile.UtauVcv;
        }

        private static void ScaleNotePhonemes(
            UVoicePart part,
            UNote note,
            SingingSystemProfile profile,
            PhonemeTimingConfig config,
            DocManager docManager) {

            if (note.phonemeIndexes == null || note.phonemeIndexes.Length == 0) {
                return;
            }

            for (int i = 0; i < note.phonemeIndexes.Length; i++) {
                int pIndex = note.phonemeIndexes[i];
                var phoneme = part.phonemes.FirstOrDefault(p => p.Parent == note && p.index == pIndex);
                if (phoneme == null) continue;

                string phStr = phoneme.phoneme ?? phoneme.rawPhoneme ?? string.Empty;
                var (isLeading, isCluster, isPlainVowel, isMaeKk, isEndingCoda) = ClassifyPhoneme(phStr, i, note.phonemeIndexes.Length);

                // คำนวณ Scale Multiplier ตามประเภทหน่วยเสียง (0.0 ถึง 2.0 โดย 1.0 คือ 50% มาตรฐาน)
                float scale = 1.0f;
                if (isLeading) {
                    scale = config.LeadingConsonantScale / 50.0f;
                } else if (isCluster) {
                    scale = config.ClusterScale / 50.0f;
                } else if (isPlainVowel) {
                    scale = config.PlainVowelScale / 50.0f;
                } else if (isMaeKk) {
                    scale = config.MaeKkScale / 50.0f;
                } else if (isEndingCoda) {
                    scale = config.EndingCodaScale / 50.0f;
                }

                // ปรับเกณฑ์เฉพาะระบบ (Singing System Profile Adjustments)
                switch (profile) {
                    case SingingSystemProfile.DiffSinger:
                        if (isLeading) scale = Math.Min(scale, 0.4f); // Leading consonant <= 5%
                        break;
                    case SingingSystemProfile.UtauVccv:
                        if (isCluster) scale = Math.Min(scale, 0.5f); // Cluster head <= 5%
                        break;
                    case SingingSystemProfile.UtauVcv:
                        if (isPlainVowel) scale = Math.Max(scale, 1.1f);
                        break;
                }

                // คำนวณ delta สำหรับ preutter และ overlap
                float preutterDelta = (float)((scale - 1.0f) * (phoneme.autoPreutter > 0 ? phoneme.autoPreutter * 0.4 : 15.0));
                float overlapDelta = (float)((scale - 1.0f) * (phoneme.autoOverlap > 0 ? phoneme.autoOverlap * 0.3 : 10.0));

                // นำไปปรับใช้กับ override ของตัวโน้ต
                var o = note.GetPhonemeOverride(pIndex);
                o.preutterDelta = Math.Abs(preutterDelta) > 0.5f ? preutterDelta : null;
                o.overlapDelta = Math.Abs(overlapDelta) > 0.5f ? overlapDelta : null;
            }
        }

        private static (bool isLeading, bool isCluster, bool isPlainVowel, bool isMaeKk, bool isEndingCoda) ClassifyPhoneme(
            string ph, int index, int totalCount) {

            bool isLeading = false;
            bool isCluster = false;
            bool isPlainVowel = false;
            bool isMaeKk = false;
            bool isEndingCoda = false;

            if (string.IsNullOrWhiteSpace(ph)) {
                return (false, false, true, false, false);
            }

            ph = ph.Trim();

            // 1. ตรวจสอบพยัญชนะนำ (Leading Consonant)
            if (index == 0 && (ph.StartsWith("-") || ph.StartsWith("[-") || !IsVowelSymbol(ph[0].ToString()))) {
                isLeading = true;
            }

            // 2. ตรวจสอบคำควบกล้ำ (Consonant Clusters: CC, CCV, tr, kr, pl, etc.)
            if (ph.Contains("tr") || ph.Contains("kr") || ph.Contains("pr") || ph.Contains("br") ||
                ph.Contains("pl") || ph.Contains("kl") || ph.Contains("kw") || ph.Contains("CC") ||
                (ph.Length >= 4 && ph.Contains(" "))) {
                isCluster = true;
            }

            // 3. ตรวจสอบตัวสะกด (Endings / Codas: VC, C -, etc.)
            if ((index == totalCount - 1 && index > 0) || ph.EndsWith("-") || ph.EndsWith("-]") ||
                (ph.Contains(" ") && !ph.StartsWith("-"))) {
                isEndingCoda = true;
            }

            // 4. ตรวจสอบสระเปล่า (Plain Vowels: a, i, u, e, o, - a, etc.)
            if (ph == "a" || ph == "i" || ph == "u" || ph == "e" || ph == "o" ||
                ph == "- a" || ph == "- i" || ph == "- u" || ph == "- e" || ph == "- o" ||
                ph == "ah" || ph == "ih" || ph == "uh" || ph == "eh" || ph == "oh") {
                isPlainVowel = true;
            }

            // 5. ตรวจสอบแม่ ก กา (Mae K.K. / Open Syllable: CV or V C with no final coda)
            if (!isEndingCoda && !isCluster && !isPlainVowel) {
                isMaeKk = true;
            }

            return (isLeading, isCluster, isPlainVowel, isMaeKk, isEndingCoda);
        }

        private static bool IsVowelSymbol(string s) {
            return "aeiouAEIOUเแโอะอาอิอีอึอือุอู".Contains(s);
        }
    }
}
