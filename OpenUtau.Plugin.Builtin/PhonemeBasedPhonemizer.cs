// ============================================================================
// 1. วัน/เดือน/ปี: 10 พฤศจิกายน พ.ศ. 2569 / 10 November 2026
// 2. ชื่อไฟล์โค้ด: PhonemeBasedPhonemizer.cs
// 3. ลำดับเวอร์ชันของไฟล์: 2.2
// 4. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Plugin.Builtin/PhonemeBasedPhonemizer.cs
// 5. หน้าที่: คลาสแม่สำหรับ Arpasing และ Diphone ทุกภาษา บังคับใช้กฎเหล็กสัดส่วนเสียง
//            ปรับความกว้าง [v c] แบบ Dynamic ทศนิยมยืดหยุ่นตามความยาวโน้ต (30-40%)
//            และพยัญชนะ/คลัสเตอร์คั่นกลางไม่เกิน 5-7% เสมอ
// 6. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Agentic AI | Original by OpenUtau Authors
// ============================================================================

#pragma warning disable CS0618, CS0649, CS8632, CS0108, CS8600, CS8601, CS8602, CS8603, CS8604, CS8618
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    public abstract class PhonemeBasedPhonemizer : Phonemizer {
        protected Dictionary<string, string[]> vowelFallback = new Dictionary<string, string[]>();
        protected USinger singer;
        protected IG2p g2p;
        protected bool isDictionaryLoading;

        // [(index of phoneme, tick position from the lyrical note in notes[], is manual)]
        protected readonly List<Tuple<int, int, bool>> alignments = new List<Tuple<int, int, bool>>();

        /// <summary>
        /// Base consonant length in ticks (default 30 ticks for crisp transitions).
        /// </summary>
        public int ConsonantLength { get; set; } = 30;

        public bool addTail { get; set; } = true;

        public PhonemeBasedPhonemizer() {
            try {
                Initialize();
            } catch (Exception e) {
                Log.Error(e, "Failed to initialize.");
            }
        }

        protected abstract IG2p LoadG2p();

        protected abstract Dictionary<string, string[]> LoadVowelFallbacks();

        protected void Initialize() {
            g2p = LoadG2p();
            vowelFallback = LoadVowelFallbacks() ?? new Dictionary<string, string[]>();
        }

        public override void SetSinger(USinger singer) {
            this.singer = singer;
            g2p = LoadG2p();
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            if (isDictionaryLoading || notes == null || notes.Length == 0) {
                return MakeSimpleResult("");
            }
            var note = notes[0];

            // Force alias using ? prefix
            if (!string.IsNullOrEmpty(note.lyric) && note.lyric[0] == '?') {
                return MakeSimpleResult(note.lyric.Substring(1));
            }

            // Get the symbols of previous note.
            var prevSymbols = prevNeighbour == null ? null : GetSymbols(prevNeighbour.Value);
            if ((prevSymbols == null || prevSymbols.Length == 0) && prevNeighbour != null && prevNeighbour.Value.lyric == "-" && prevNeighbours != null && prevNeighbours.Length > 0) {
                for (int pIdx = prevNeighbours.Length - 1; pIdx >= 0; pIdx--) {
                    if (prevNeighbours[pIdx].lyric != "-") {
                        prevSymbols = GetSymbols(prevNeighbours[pIdx]);
                        if (prevSymbols != null && prevSymbols.Length > 0) break;
                    }
                }
            }
            if (note.lyric == "-" && prevSymbols != null && prevSymbols.Length > 0) {
                var attr = note.phonemeAttributes?.FirstOrDefault() ?? default;
                string color = attr.voiceColor ?? string.Empty;
                string alias = $"{prevSymbols.Last()} -";
                if (singer != null && singer.TryGetMappedOto(alias, note.tone, color, out var oto)) {
                    return MakeSimpleResult(oto.Alias);
                }
                return MakeSimpleResult(alias);
            }

            // Get the symbols of current note.
            string[]? symbols = GetSymbols(note);
            if (addTail && nextNeighbour == null) {
                if (symbols != null && symbols.Length > 0) {
                    if (symbols.Last() != "-") {
                        symbols = symbols.Append("-").ToArray();
                    }
                } else {
                    symbols = new string[] { "-" };
                }
            }
            if (symbols == null || symbols.Length == 0 || g2p == null) {
                return MakeSimpleResult(note.lyric);
            }

            var isVowel = symbols.Select(s => g2p.IsVowel(s)).ToArray();
            var isGlide = symbols.Select(s => g2p.IsGlide(s)).ToArray();
            var phonemes = new Phoneme[symbols.Length];

            alignments.Clear();
            var nonExtensionNotes = notes.Where(n => !IsSyllableVowelExtensionNote(n)).ToArray();
            for (int i = 0; i < symbols.Length; i++) {
                if (isVowel[i] && alignments.Count < nonExtensionNotes.Length) {
                    if (i >= 2 && isGlide[i - 1] && !isVowel[i - 2]) {
                        alignments.Add(Tuple.Create(i - 1, nonExtensionNotes[alignments.Count].position - notes[0].position, false));
                    } else {
                        alignments.Add(Tuple.Create(i, nonExtensionNotes[alignments.Count].position - notes[0].position, false));
                    }
                }
            }
            int totalDuration = notes.Sum(n => n.duration);
            int position = notes[0].duration;
            for (int i = 1; i < notes.Length; ++i) {
                if (!string.IsNullOrEmpty(notes[i].lyric) && notes[i].lyric.Length > 1 && int.TryParse(notes[i].lyric.Substring(1), out var idx)) {
                    alignments.Add(Tuple.Create(idx - 1, position, true));
                }
                position += notes[i].duration;
            }
            alignments.Add(Tuple.Create(phonemes.Length, position, true));
            alignments.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            for (int i = 0; i < alignments.Count; ++i) {
                if (alignments[i].Item3) {
                    while (i > 0 && (alignments[i - 1].Item2 >= alignments[i].Item2 ||
                        alignments[i - 1].Item1 == alignments[i].Item1)) {
                        alignments.RemoveAt(i - 1);
                        i--;
                    }
                    while (i < alignments.Count - 1 && (alignments[i + 1].Item2 <= alignments[i].Item2 ||
                        alignments[i + 1].Item1 == alignments[i].Item1)) {
                        alignments.RemoveAt(i + 1);
                    }
                }
            }

            int startIndex = 0;
            int firstVowel = Math.Max(0, Array.IndexOf(isVowel, true));
            if (firstVowel < 0) firstVowel = 0;

            // [DELTA SYNTH IRON RULE]
            // - หากเป็นโน้ตเปิดวลี (ไม่มี prevNeighbour): กลุ่มเปิดเสียง [- C] ใช้พื้นที่ 10-20% ก่อนเข้าโน้ตหลัก
            // - หากมีโน้ตนำหน้า: กลุ่มเชื่อมคำ [V_C] / [C C] ยืมพื้นที่โน้ตนำหน้าไม่เกิน 5-7% เสมอ
            int prevDur = prevNeighbour?.duration ?? notes[0].duration;
            int preNoteLeadTicks = prevNeighbour == null
                ? Math.Max(15, (int)Math.Round(notes[0].duration * 0.15))
                : Math.Max(10, (int)Math.Round(prevDur * 0.06));

            int startTick = firstVowel > 0 ? -preNoteLeadTicks : 0;
            foreach (var alignment in alignments) {
                DistributeDuration(symbols, isVowel, phonemes, startIndex, alignment.Item1, startTick, alignment.Item2);
                startIndex = alignment.Item1;
                startTick = alignment.Item2;
            }
            alignments.Clear();

            // Select aliases.
            int noteIndex = 0;
            string prevSymbol = (prevSymbols == null || prevSymbols.Length == 0) ? "-" : prevSymbols.Last();
            for (int i = 0; i < symbols.Length; i++) {
                var attr = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == i) ?? default;
                string alt = attr.alternate?.ToString() ?? string.Empty;
                string color = attr.voiceColor ?? string.Empty;
                int toneShift = attr.toneShift;
                var phoneme = phonemes[i];
                while (noteIndex < notes.Length - 1 && notes[noteIndex].position - note.position < phoneme.position) {
                    noteIndex++;
                }
                int tone = (i == 0 && prevNeighbours != null && prevNeighbours.Length > 0)
                    ? prevNeighbours.Last().tone : notes[noteIndex].tone;
                phoneme.phoneme = GetPhonemeOrFallback(prevSymbol, symbols[i], tone + toneShift, color, alt);
                phonemes[i] = phoneme;
                prevSymbol = symbols[i];
            }

            EnforceDeltaSynthTimingRules(phonemes, isVowel, totalDuration, prevNeighbour);

            return new Result {
                phonemes = phonemes,
            };
        }

        /// <summary>
        /// [DELTA SYNTH IRON RULE] บังคับสัดส่วนพื้นที่เสียงตามหลักทศนิยมและยืดหยุ่นตามความยาวโน้ต:
        /// 1. [- V] / [CV] สระหลัก เริ่มที่ 0 (หรือ <= 5-7% กรณีพยัญชนะควบกล้ำ)
        /// 2. [VC] ตัวสะกดหลัก ได้พื้นที่กว้างขึ้นอย่างยืดหยุ่นตามความยาวของโน้ตรายตัว โดยอยู่ในช่วง 30%-40% เสมอ (ไม่ล็อกค่าคงที่ตายตัว)
        /// 3. [C C] / [C -] พยัญชนะท้ายและเสียงปิดคำ ห้ามกว้างเกิน 5-7% (อยู่ช่วงท้ายโน้ต 93%-95%)
        /// </summary>
        protected virtual void EnforceDeltaSynthTimingRules(Phoneme[] phonemes, bool[] isVowel, int noteDur, Note? prevNeighbour) {
            if (phonemes == null || phonemes.Length == 0) return;

            int firstVowelIdx = Array.IndexOf(isVowel, true);
            if (firstVowelIdx < 0) firstVowelIdx = 0;

            int prevDur = prevNeighbour?.duration ?? noteDur;
            int maxPreBorrow = prevNeighbour == null
                ? Math.Max(15, (int)Math.Round(noteDur * 0.15)) // [- C] 10-20%
                : Math.Max(10, (int)Math.Round(prevDur * 0.06)); // [V_C] / [C C] <= 5-7%

            // 1. จัดตำแหน่งกลุ่มหน้าสระตัวแรก (Leading Consonants: [- C], [V_C], [C C])
            if (firstVowelIdx > 0) {
                for (int i = 0; i < firstVowelIdx; i++) {
                    double ratio = (double)(firstVowelIdx - i) / firstVowelIdx;
                    phonemes[i].position = -(int)Math.Round(maxPreBorrow * ratio);
                }
            }

            // 2. ล็อกตำแหน่งสระหลัก [CV] / [- V] ให้เริ่มที่จุดเริ่มต้นโน้ต (0) เสมอ เพื่อไม่ให้ถูกบีบ
            if (firstVowelIdx < phonemes.Length) {
                phonemes[firstVowelIdx].position = 0;
            }

            // 3. จัดตำแหน่งกลุ่มหลังสระตัวสุดท้าย (Coda: [VC], [C C], [C -], [V -])
            int lastVowelIdx = Array.LastIndexOf(isVowel, true);
            if (lastVowelIdx >= 0 && lastVowelIdx < phonemes.Length - 1) {
                int codaCount = phonemes.Length - 1 - lastVowelIdx;

                // [DELTA SYNTH] คำนวณความกว้างของกลุ่ม [v c] แบบยืดหยุ่นตามความยาวโน้ตรายตัว (Dynamic Decimal Ratio)
                // ความกว้างของ [v c] อยู่ในช่วง 30% ถึง 40% (0.30 - 0.40) เสมอตามทศนิยม ไม่ล็อก ticks ตายตัว
                double vcRatio = Math.Clamp(0.33 + 0.05 * Math.Min(1.0, (double)noteDur / 480.0), 0.30, 0.40);
                int vcStartTick = (int)Math.Round(noteDur * (1.0 - vcRatio));
                int tailStartTick = Math.Max(vcStartTick + 10, (int)Math.Round(noteDur * 0.94)); // [C -] หรือ [V -] ท้ายโน้ต (ไม่เกิน 5-6%)

                if (codaCount == 1) {
                    // กรณีมีตัวปิดตัวเดียว เช่น [V -] หรือ [V C]
                    string tailAlias = phonemes[lastVowelIdx + 1].phoneme ?? "";
                    bool isPureBreathTail = tailAlias.EndsWith(" -") || tailAlias.EndsWith("-");
                    phonemes[lastVowelIdx + 1].position = isPureBreathTail
                        ? Math.Max((int)Math.Round(noteDur * 0.88), noteDur - 25)
                        : vcStartTick;
                } else {
                    // กรณีมี [V C] ตามด้วย [C C] และ/หรือ [C -]
                    // ตัวแรกหลังสระ คือ [V C] -> ได้พื้นที่ 30%-40%
                    phonemes[lastVowelIdx + 1].position = vcStartTick;

                    // ตัวที่เหลือ คือ [C C] และ [C -] -> บีบให้อยู่ในโซน 5-7% สุดท้ายของโน้ต (ไม่เกิน 6-7%)
                    int extraCodaCount = codaCount - 1;
                    int clusterZoneStart = Math.Max(vcStartTick + 10, (int)Math.Round(noteDur * (0.94 - 0.04 * extraCodaCount)));
                    int clusterZoneSpan = Math.Max(10, tailStartTick - clusterZoneStart);

                    for (int k = 1; k <= extraCodaCount; k++) {
                        int idx = lastVowelIdx + 1 + k;
                        if (k == extraCodaCount) {
                            phonemes[idx].position = tailStartTick;
                        } else {
                            phonemes[idx].position = clusterZoneStart + (clusterZoneSpan * (k - 1) / extraCodaCount);
                        }
                    }
                }
            }

            // 4. Safety Buffer ป้องกันหน่วยเสียงทับกันโดยไม่ดันสระหลักหนี
            int minGap = noteDur < 120 ? 5 : 10;
            for (int i = 1; i < phonemes.Length; i++) {
                if (phonemes[i].position <= phonemes[i - 1].position) {
                    phonemes[i].position = phonemes[i - 1].position + minGap;
                }
                if (phonemes[i].position > noteDur - minGap) {
                    phonemes[i].position = Math.Max(phonemes[i - 1].position + 2, noteDur - minGap);
                }
            }
        }

        protected bool IsSyllableVowelExtensionNote(Note note) {
            return !string.IsNullOrEmpty(note.lyric) && (note.lyric.StartsWith("+~") || note.lyric.StartsWith("+*"));
        }

        protected virtual string[] GetSymbols(Note note) {
            if (g2p == null) {
                return Array.Empty<string>();
            }
            if (string.IsNullOrEmpty(note.phoneticHint)) {
                string cleanLyric = (note.lyric ?? "").Trim().ToLowerInvariant();
                var result = g2p.Query(cleanLyric);
                if (result == null && cleanLyric.EndsWith("-") && cleanLyric.Length > 1) {
                    result = g2p.Query(cleanLyric.TrimEnd('-'));
                }
                return result ?? Array.Empty<string>();
            }
            return note.phoneticHint.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => g2p.IsValidSymbol(s))
                .ToArray();
        }

        protected abstract string GetPhonemeOrFallback(string prevSymbol, string symbol, int tone, string color, string alt);

        void DistributeDuration(string[] symbols, bool[] isVowel, Phoneme[] phonemes, int startIndex, int endIndex, int startTick, int endTick) {
            if (startIndex >= endIndex) {
                return;
            }
            int duration = Math.Max(10, endTick - startTick);
            int consonants = 0;
            int vowels = 0;
            for (int i = startIndex; i < endIndex; i++) {
                if (isVowel[i]) {
                    vowels++;
                } else {
                    consonants++;
                }
            }

            // [DELTA SYNTH IRON RULE] จำกัดความกว้างของพยัญชนะคั่นกลางไม่ให้เกิน 5-7% ของช่วงเวลาโน้ต
            int maxConsonantTotal = (int)Math.Round(duration * 0.06 * Math.Max(1, consonants));
            int consonantDuration = vowels > 0
                ? (consonants > 0 ? Math.Min(ConsonantLength, Math.Max(5, maxConsonantTotal / consonants)) : 0)
                : (consonants > 0 ? duration / consonants : 0);

            int vowelDuration = vowels > 0 ? Math.Max(10, (duration - consonantDuration * consonants) / vowels) : 0;
            int position = startTick;
            for (int i = startIndex; i < endIndex; i++) {
                if (isVowel[i]) {
                    phonemes[i].position = position;
                    position += vowelDuration;
                } else {
                    phonemes[i].position = position;
                    position += consonantDuration;
                }
            }
        }
    }
}
