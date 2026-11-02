#nullable enable
// ============================================================================
// Date         : 2026-10-25
// File Name    : LanguageAutopitchService.cs
// Author Credit: Made And Checked By DELTA SYNTH & Gemini AI
// Description  : Linguistic Grammar, Accent & Native Autopitch F0 Generation Engine.
//                Synthesizes phonetic tone rules, sandhi, micro-pitch consonant
//                perturbations, and portamento for authentic multi-language singing.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    public enum LanguageFamily {
        AutoDetect,
        Thai,
        Mandarin,
        Vietnamese,
        Japanese,
        EnglishGeneral
    }

    public enum ToneContourType {
        Flat,
        Low,
        Falling,
        High,
        Rising,
        Dipping,
        HighGlottal,
        LowConstricted
    }

    /// <summary>
    /// DELTA SYNTH Standard: Master Linguistic Autopitch Service.
    /// Drives native speech-level intonations and singing pitch curves.
    /// </summary>
    public static class LanguageAutopitchService {
        private static readonly Regex ThaiCharRegex = new Regex("[\u0E00-\u0E7F]", RegexOptions.Compiled);
        private static readonly Regex ChineseCharRegex = new Regex("[\u4E00-\u9FFF]", RegexOptions.Compiled);
        private static readonly Regex JapaneseCharRegex = new Regex("[\u3040-\u309F\u30A0-\u30FF]", RegexOptions.Compiled);
        private static readonly Regex VietnameseCharRegex = new Regex("[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Thai Consonant Classes for Tone Determination
        private static readonly HashSet<char> ThaiHighConsonants = new HashSet<char> { 'ข', 'ฃ', 'ฉ', 'ฐ', 'ถ', 'ผ', 'ฝ', 'ศ', 'ษ', 'ส', 'ห' };
        private static readonly HashSet<char> ThaiMidConsonants = new HashSet<char> { 'ก', 'จ', 'ด', 'ต', 'บ', 'ป', 'อ', 'ฎ', 'ฏ' };
        private static readonly HashSet<char> ThaiLowConsonants = new HashSet<char> { 'ค', 'ฅ', 'ฆ', 'ง', 'ช', 'ซ', 'ฌ', 'ญ', 'ฑ', 'ฒ', 'ณ', 'ท', 'ธ', 'น', 'พ', 'ฟ', 'ภ', 'ม', 'ย', 'ร', 'ล', 'ว', 'ฬ', 'ฮ' };

        // Thai Dead Ending Consonant Sounds
        private static readonly HashSet<char> ThaiDeadCodas = new HashSet<char> { 'ก', 'ข', 'ค', 'ฆ', 'ด', 'จ', 'ช', 'ซ', 'ฎ', 'ฏ', 'ฐ', 'ฑ', 'ฒ', 'ต', 'ถ', 'ท', 'ธ', 'ศ', 'ษ', 'ส', 'บ', 'ป', 'พ', 'ฟ', 'ภ' };
        private static readonly HashSet<char> ThaiShortVowels = new HashSet<char> { 'ะ', 'ั', 'ิ', 'ึ', 'ุ', '็' };

        /// <summary>
        /// Detects language from track phonemizer or note lyric
        /// </summary>
        public static LanguageFamily DetectLanguage(string? phonemizerTag, string lyric) {
            if (!string.IsNullOrEmpty(phonemizerTag)) {
                string tag = phonemizerTag.ToUpperInvariant();
                if (tag.Contains("TH") || tag.Contains("THAI")) return LanguageFamily.Thai;
                if (tag.Contains("ZH") || tag.Contains("CH") || tag.Contains("MAN")) return LanguageFamily.Mandarin;
                if (tag.Contains("VI") || tag.Contains("VN")) return LanguageFamily.Vietnamese;
                if (tag.Contains("JA") || tag.Contains("JP")) return LanguageFamily.Japanese;
                if (tag.Contains("EN") || tag.Contains("ARPA")) return LanguageFamily.EnglishGeneral;
            }

            if (ThaiCharRegex.IsMatch(lyric)) return LanguageFamily.Thai;
            if (ChineseCharRegex.IsMatch(lyric)) return LanguageFamily.Mandarin;
            if (VietnameseCharRegex.IsMatch(lyric)) return LanguageFamily.Vietnamese;
            if (JapaneseCharRegex.IsMatch(lyric)) return LanguageFamily.Japanese;

            return LanguageFamily.EnglishGeneral;
        }

        /// <summary>
        /// Analyzes native tone of a note
        /// </summary>
        public static ToneContourType AnalyzeTone(string lyric, LanguageFamily lang, string? nextLyric = null) {
            if (string.IsNullOrWhiteSpace(lyric)) return ToneContourType.Flat;

            switch (lang) {
                case LanguageFamily.Thai:
                    return AnalyzeThaiTone(lyric);
                case LanguageFamily.Mandarin:
                    return AnalyzeMandarinTone(lyric, nextLyric);
                case LanguageFamily.Vietnamese:
                    return AnalyzeVietnameseTone(lyric);
                case LanguageFamily.Japanese:
                    return AnalyzeJapaneseMora(lyric);
                default:
                    return ToneContourType.Flat;
            }
        }

        private static ToneContourType AnalyzeThaiTone(string word) {
            // 1. Explicit tone mark check
            if (word.Contains('่')) return ToneContourType.Low;
            if (word.Contains('้')) return ToneContourType.Falling;
            if (word.Contains('๊')) return ToneContourType.High;
            if (word.Contains('๋')) return ToneContourType.Rising;

            // 2. Initial consonant class analysis
            char initialC = 'ก';
            for (int i = 0; i < word.Length; i++) {
                char c = word[i];
                if (c >= '\u0E01' && c <= '\u0E2E') {
                    // Check leading ห or อ
                    if ((c == 'ห' || c == 'อ') && i + 1 < word.Length && (word[i + 1] >= '\u0E01' && word[i + 1] <= '\u0E2E')) {
                        initialC = word[i + 1];
                    } else {
                        initialC = c;
                    }
                    break;
                }
            }

            bool isDead = false;
            // Check coda
            for (int i = word.Length - 1; i >= 1; i--) {
                char c = word[i];
                if (ThaiDeadCodas.Contains(c)) {
                    isDead = true;
                    break;
                }
            }
            if (!isDead && word.Any(c => ThaiShortVowels.Contains(c))) {
                isDead = true;
            }

            // Standard Royal Institute Thai Grammar Rules
            if (ThaiHighConsonants.Contains(initialC)) {
                return isDead ? ToneContourType.Low : ToneContourType.Rising;
            } else if (ThaiMidConsonants.Contains(initialC)) {
                return isDead ? ToneContourType.Low : ToneContourType.Flat;
            } else { // Low consonant
                if (isDead) {
                    bool isShort = word.Any(c => ThaiShortVowels.Contains(c));
                    return isShort ? ToneContourType.High : ToneContourType.Falling;
                }
                return ToneContourType.Flat;
            }
        }

        private static ToneContourType AnalyzeMandarinTone(string word, string? nextWord) {
            // Check numeric tone indicator e.g. "ni3", "hao3"
            int toneNum = 0;
            for (int i = word.Length - 1; i >= 0; i--) {
                if (char.IsDigit(word[i])) {
                    toneNum = word[i] - '0';
                    break;
                }
            }

            // Diacritic tone checks
            if (toneNum == 0) {
                if (Regex.IsMatch(word, "[āēīōūǖ]", RegexOptions.IgnoreCase)) toneNum = 1;
                else if (Regex.IsMatch(word, "[áéíóúǘ]", RegexOptions.IgnoreCase)) toneNum = 2;
                else if (Regex.IsMatch(word, "[ǎěǐǒǔǚ]", RegexOptions.IgnoreCase)) toneNum = 3;
                else if (Regex.IsMatch(word, "[àèìòùǜ]", RegexOptions.IgnoreCase)) toneNum = 4;
            }

            // Tone Sandhi: 3 + 3 -> 2 + 3
            if (toneNum == 3 && !string.IsNullOrEmpty(nextWord)) {
                int nextTone = 0;
                for (int i = nextWord.Length - 1; i >= 0; i--) {
                    if (char.IsDigit(nextWord[i])) {
                        nextTone = nextWord[i] - '0';
                        break;
                    }
                }
                if (nextTone == 0 && Regex.IsMatch(nextWord, "[ǎěǐǒǔǚ]", RegexOptions.IgnoreCase)) nextTone = 3;
                if (nextTone == 3) {
                    toneNum = 2; // Sandhi change
                }
            }

            switch (toneNum) {
                case 1: return ToneContourType.High;
                case 2: return ToneContourType.Rising;
                case 3: return ToneContourType.Dipping;
                case 4: return ToneContourType.Falling;
                default: return ToneContourType.Flat;
            }
        }

        private static ToneContourType AnalyzeVietnameseTone(string word) {
            if (Regex.IsMatch(word, "[àèìòùỳầềồờừ]", RegexOptions.IgnoreCase)) return ToneContourType.Low; // Huyền
            if (Regex.IsMatch(word, "[áéíóúýắấếốớứ]", RegexOptions.IgnoreCase)) return ToneContourType.Rising; // Sắc
            if (Regex.IsMatch(word, "[ảẻỉỏủỷẳẩểổởử]", RegexOptions.IgnoreCase)) return ToneContourType.Dipping; // Hỏi
            if (Regex.IsMatch(word, "[ãẽĩõũỹẵẫễỗỡữ]", RegexOptions.IgnoreCase)) return ToneContourType.HighGlottal; // Ngã
            if (Regex.IsMatch(word, "[ạẹịọụỵặậệộợự]", RegexOptions.IgnoreCase)) return ToneContourType.LowConstricted; // Nặng
            return ToneContourType.Flat; // Ngang
        }

        private static ToneContourType AnalyzeJapaneseMora(string word) {
            // Natural Japanese vocal styling: slight lift at onset, flat core, gentle decay
            return ToneContourType.Flat;
        }

        /// <summary>
        /// Generates authentic PitchPoints for a note adhering to native linguistic grammar
        /// </summary>
        public static List<PitchPoint> GenerateNativePitchPoints(
            UNote note,
            UNote? prevNote,
            UNote? nextNote,
            double durationMs,
            LanguageFamily language,
            double intensity = 0.65) {

            var points = new List<PitchPoint>();
            if (durationMs <= 0) return points;

            float intensityFactor = (float)Math.Clamp(intensity, 0.1, 1.0);
            string lyric = note.lyric ?? "";
            string? nextLyric = nextNote?.lyric;

            ToneContourType tone = AnalyzeTone(lyric, language, nextLyric);

            // 1. Portamento calculation from prevNote
            float portamentoOffset = 0f;
            if (prevNote != null && (note.position - prevNote.End) <= 240) {
                int semitoneDiff = prevNote.tone - note.tone;
                // Bound portamento to avoid dissonance
                portamentoOffset = (float)Math.Clamp(semitoneDiff * 7f, -18f, 18f) * intensityFactor;
            }

            // Portamento Lead-in point
            if (Math.Abs(portamentoOffset) > 0.5f) {
                points.Add(new PitchPoint(-40f, portamentoOffset, PitchPointShape.io));
                points.Add(new PitchPoint(0f, portamentoOffset * 0.25f, PitchPointShape.io));
            } else {
                points.Add(new PitchPoint(-20f, 0f, PitchPointShape.io));
                points.Add(new PitchPoint(0f, 0f, PitchPointShape.io));
            }

            // 2. Micro-pitch perturbation based on initial consonant
            float microPitch = 0f;
            char firstC = lyric.FirstOrDefault();
            if ("kptscfhkx".Contains(char.ToLowerInvariant(firstC))) {
                microPitch = 2.0f * intensityFactor; // Voiceless stop/fricative overshoot
            } else if ("bdgmjnlrwvz".Contains(char.ToLowerInvariant(firstC))) {
                microPitch = -1.5f * intensityFactor; // Voiced consonant dip
            }

            if (Math.Abs(microPitch) > 0.2f && durationMs > 100) {
                points.Add(new PitchPoint(Math.Min(40f, (float)(durationMs * 0.12)), microPitch, PitchPointShape.io));
            }

            // 3. Body Linguistic Tone Trajectory
            float midX = (float)(durationMs * 0.45);
            float lateX = (float)(durationMs * 0.85);

            switch (tone) {
                case ToneContourType.Flat: // สามัญ / Ngang / Tone 1
                    points.Add(new PitchPoint(midX, 0f, PitchPointShape.io));
                    points.Add(new PitchPoint(lateX, -0.4f * intensityFactor, PitchPointShape.io));
                    break;

                case ToneContourType.Low: // เอก / Huyền
                    points.Add(new PitchPoint((float)(durationMs * 0.35), -2.6f * intensityFactor, PitchPointShape.io));
                    points.Add(new PitchPoint(lateX, -1.0f * intensityFactor, PitchPointShape.io));
                    break;

                case ToneContourType.Falling: // โท / 去声
                    points.Add(new PitchPoint((float)(durationMs * 0.25), 3.2f * intensityFactor, PitchPointShape.io));
                    points.Add(new PitchPoint((float)(durationMs * 0.65), -2.0f * intensityFactor, PitchPointShape.io));
                    points.Add(new PitchPoint(lateX, -4.2f * intensityFactor, PitchPointShape.io));
                    break;

                case ToneContourType.High: // ตรี / 阴平
                    points.Add(new PitchPoint((float)(durationMs * 0.40), 2.8f * intensityFactor, PitchPointShape.io));
                    points.Add(new PitchPoint(lateX, 3.8f * intensityFactor, PitchPointShape.io));
                    break;

                case ToneContourType.Rising: // จัตวา / 阳平 / Sắc
                    points.Add(new PitchPoint((float)(durationMs * 0.25), -2.8f * intensityFactor, PitchPointShape.io));
                    points.Add(new PitchPoint((float)(durationMs * 0.65), 1.2f * intensityFactor, PitchPointShape.io));
                    points.Add(new PitchPoint(lateX, 3.8f * intensityFactor, PitchPointShape.io));
                    break;

                case ToneContourType.Dipping: // 上声 (Tone 3) / Hỏi
                    points.Add(new PitchPoint((float)(durationMs * 0.35), -3.4f * intensityFactor, PitchPointShape.io));
                    points.Add(new PitchPoint(lateX, 1.8f * intensityFactor, PitchPointShape.io));
                    break;

                case ToneContourType.HighGlottal: // Ngã (Vietnamese glottal rising)
                    points.Add(new PitchPoint((float)(durationMs * 0.30), 1.0f * intensityFactor, PitchPointShape.io));
                    points.Add(new PitchPoint((float)(durationMs * 0.50), -1.5f * intensityFactor, PitchPointShape.i)); // glottal dip
                    points.Add(new PitchPoint(lateX, 3.5f * intensityFactor, PitchPointShape.io));
                    break;

                case ToneContourType.LowConstricted: // Nặng (Vietnamese glottal drop)
                    points.Add(new PitchPoint((float)(durationMs * 0.30), -1.2f * intensityFactor, PitchPointShape.io));
                    points.Add(new PitchPoint(lateX, -4.0f * intensityFactor, PitchPointShape.o));
                    break;
            }

            // 4. Note Tail & Phrase End Release
            float tailY = 0f;
            if (nextNote != null && (nextNote.position - note.End) <= 120) {
                // Glide toward next note
                int nextDiff = nextNote.tone - note.tone;
                tailY = (float)Math.Clamp(nextDiff * 4f, -10f, 10f) * intensityFactor;
            } else {
                // Natural release taper
                tailY = -1.2f * intensityFactor;
            }
            points.Add(new PitchPoint((float)durationMs, tailY, PitchPointShape.o));

            // Clean duplicate or out-of-order X positions
            points.Sort();
            for (int i = 1; i < points.Count; i++) {
                if (points[i].X <= points[i - 1].X) {
                    points[i].X = points[i - 1].X + 5f;
                }
            }

            return points;
        }

        /// <summary>
        /// Computes natural vibrato for long sustained notes
        /// </summary>
        public static UVibrato? GenerateNaturalVibrato(double durationMs) {
            if (durationMs < 420.0) return null; // No vibrato on short notes

            float lengthPercent = (float)Math.Clamp(((durationMs - 220.0) / durationMs) * 100.0, 30.0, 70.0);
            return new UVibrato {
                length = lengthPercent,
                period = 175f, // ~5.7 Hz natural human vibrato rate
                depth = 35f,   // subtle and sweet 35 cents depth
                @in = 25f,
                @out = 15f,
                shift = 0f,
                drift = 0f
            };
        }
    }
}
