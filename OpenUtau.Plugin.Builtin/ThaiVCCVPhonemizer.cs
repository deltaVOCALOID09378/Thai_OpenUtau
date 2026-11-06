// ============================================================================
// 1. วัน/เดือน/ปี: 6 พฤศจิกายน พ.ศ. 2569 / 6 November 2026
// 2. ชื่อไฟล์โค้ด: ThaiVCCVPhonemizer.cs
// 3. ลำดับเวอร์ชันของไฟล์: 19.6
// 4. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by PRINTmov & Patiphat Wongyai ]
// 5. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Plugin.Builtin/ThaiVCCVPhonemizer.cs
// 6. รายละเอียดการแก้ไข: แยกหน่วยเสียงและพจนานุกรมของ UTAU (VCCV) กับ DiffSinger ออกจากกันอย่างเด็ดขาด 100%
//                       ห้ามนำไฟล์หรือหน่วยเสียงข้ามระบบมาปะปนกัน และคืนค่าโครงสร้างหลักของ ThaiVCCVPhonemizer ให้ครบถ้วนสมบูรณ์
// ============================================================================

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {

    /// <summary>
    /// ระบบแปลงหน่วยเสียงภาษาไทยเฉพาะ UTAU VCCV (Pure UTAU VCCV Phonemizer)
    /// </summary>
    [Phonemizer("Thai VCCV Phonemizer", "TH VCCV Delta", "DELTA SYNTH", language: "TH")]
    public class ThaiVCCVPhonemizer : Phonemizer {

        // หน่วยเสียงมาตรฐานของ UTAU Thai VCCV เท่านั้น (ไม่มีหน่วยเสียงของ DiffSinger ปะปน)
        static readonly string[] vowels = new string[] { "a", "i", "u", "e", "o", "@", "Q", "3", "6", "1", "ia", "ua", "I", "aw", "am" };
        static readonly string[] diphthongs = new string[] { "r", "l", "w" };
        static readonly string[] consonants = new string[] { "b", "ch", "d", "f", "g", "h", "j", "k", "kh", "l", "m", "n", "p", "ph", "r", "s", "t", "th", "w", "y", "-" };
        static readonly string[] endingConsonants = new string[] { "b", "ch", "d", "f", "g", "h", "j", "k", "kh", "l", "m", "n", "p", "ph", "r", "s", "t", "th", "w", "y" };

        private static readonly List<(string Key, string Value)> VowelMapping = new List<(string Key, string Value)> {
            ("เcือะ", "6"), ("เcือx", "6"), ("แcะ", "@"), ("แcx", "@"), ("เcอะ", "3"), ("เcิร์x", "3"), ("เcอx", "3"), ("เcอ", "3"), 
            ("เcย", "3"), ("ไc", "I"), ("ใc", "I"), ("ไcx", "I"), ("ใcx", "I"),
            ("เcาะ", "Q"), ("cอx", "Q"), ("แccx", "@"), 
            ("cืx", "1"), ("cึx", "1"), ("cือ", "1"), ("cะ", "a"), ("cัx", "a"), ("cาx", "a"), ("cรรx", "a"), 
            ("เcา", "aw"), ("เcาx", "aw"), 
            ("เcะ", "e"), ("เcx", "e"), ("cิx", "i"), ("cีx", "i"),
            ("เcียะ", "ia"), ("เcียx", "ia"), ("โcะ", "o"), ("โcx", "o"), ("cุx", "u"), ("cูx", "u"), 
            ("cัวะ", "ua"), ("cัว", "ua"), ("cวx", "ua"), ("เcิx", "3"), ("เcิ", "3"),
            ("cำ", "am"), ("cำx", "am")
        };

        private static readonly Dictionary<char, string> CMapping = new Dictionary<char, string> {
            {'ก', "k"}, {'ข', "kh"}, {'ค', "kh"}, {'ฆ', "kh"}, {'ฅ', "kh"}, {'ฃ', "kh"},
            {'จ', "j"}, {'ฉ', "ch"}, {'ช', "ch"}, {'ฌ', "ch"}, {'ฎ', "d"}, {'ด', "d"}, {'ต', "t"}, {'ฏ', "t"},
            {'ถ', "th"}, {'ฐ', "th"}, {'ฑ', "th"}, {'ฒ', "th"}, {'ธ', "th"}, {'ท', "th"},
            {'บ', "b"}, {'ป', "p"}, {'พ', "ph"}, {'ผ', "ph"}, {'ภ', "ph"}, {'ฟ', "f"}, {'ฝ', "f"},
            {'ห', "h"}, {'ฮ', "h"}, {'ม', "m"}, {'น', "n"}, {'ณ', "n"}, {'ร', "r"}, {'ล', "l"}, {'ฤ', "r"},
            {'ส', "s"}, {'ศ', "s"}, {'ษ', "s"}, {'ซ', "s"}, {'ง', "g"}, {'ย', "y"}, {'ญ', "y"}, {'ว', "w"}, {'ฬ', "r"},
            {'อ', "-"} 
        };

        private static readonly Dictionary<char, string> XMapping = new Dictionary<char, string> {
            {'บ', "b"}, {'ป', "b"}, {'พ', "b"}, {'ฟ', "b"}, {'ภ', "b"},
            {'ด', "d"}, {'จ', "d"}, {'ช', "d"}, {'ซ', "d"}, {'ฎ', "d"}, {'ฏ', "d"}, {'ฐ', "d"}, {'ฑ', "d"}, {'ฒ', "d"}, {'ต', "d"}, {'ถ', "d"}, {'ท', "d"}, {'ธ', "d"}, {'ศ', "d"}, {'ษ', "d"}, {'ส', "d"},
            {'ก', "k"}, {'ข', "k"}, {'ค', "k"}, {'ฆ', "k"}, {'ว', "w"}, {'ย', "y"}, {'น', "n"}, {'ญ', "n"}, {'ณ', "n"}, {'ร', "n"}, {'ล', "n"}, {'ฬ', "n"}, {'ง', "g"}, {'ม', "m"}
        };

        private static readonly HashSet<string> TrueClusters = new HashSet<string> {
            "กร", "กล", "กว", "ขร", "ขล", "ขว", "คร", "คล", "คว", "ปร", "ปล", "พร", "พล", "ตร", "ผล", "บร", "บล", "ฟร", "ฟล", "ดร", "ทร", "หง", "หญ", "หน", "หม", "หย", "หร", "หล", "หว", "อย", "สร", "ศร", "ซร", "จร"
        };

        private static readonly Regex ToneRegex = new Regex("[่้๊๋็]", RegexOptions.Compiled);
        private static readonly Regex KaranRegex = new Regex(".์", RegexOptions.Compiled);
        private static readonly Regex ThaiCharRegex = new Regex("[ก-ฮ]", RegexOptions.Compiled);
        private static readonly Regex PunctuationCleanupRegex = new Regex(@"[.\u2026,~_!?""'()\[\]{}]+", RegexOptions.Compiled);
        private static readonly List<(string Key, Regex Pattern, string Replacement)> CompiledVowelMappings = new List<(string, Regex, string)>();

        static ThaiVCCVPhonemizer() {
            foreach (var mapping in VowelMapping) {
                string pattern = "^" + mapping.Key.Replace("c", "([ก-ฮ][ลรว]?|อ[ย]?|ห[ก-ฮ]?)").Replace("x", "([ก-ฮ]{0,2})");
                CompiledVowelMappings.Add((mapping.Key, new Regex(pattern, RegexOptions.Compiled), mapping.Value));
            }
        }

        private readonly Dictionary<string, string> CustomDictionary = new Dictionary<string, string>(StringComparer.Ordinal);
        private bool isDictLoaded = false;
        private readonly object dictLock = new object();
        private USinger? singer;

        public override void SetSinger(USinger singer) {
            this.singer = singer;
            isDictLoaded = false;
            LoadCustomDictionary();
        }

        /// <summary>
        /// รวมหน่วยเสียงที่คั่นด้วยช่องว่างในไฟล์ TH-VCCV_Dict.txt (เช่น "k o k" หรือ "m ay") ให้เป็นพยางค์ของ UTAU VCCV ("kok", "mI")
        /// ใช้เฉพาะสัญลักษณ์ของระบบ UTAU VCCV เท่านั้น
        /// </summary>
        private string CombineVccvTokensToSyllables(string vccvSpacedPhonemes) {
            var tokens = vccvSpacedPhonemes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> syllables = new List<string>();
            string currentSyllable = "";

            var dictVowels = new HashSet<string>(vowels);
            var dictDiphthongs = new HashSet<string>(diphthongs);
            var dictEndingConsonants = new HashSet<string>(endingConsonants);

            for (int i = 0; i < tokens.Length; i++) {
                string p = tokens[i];
                // ในไฟล์ TH-VCCV_Dict.txt สระไอ/ใอ ใช้สัญลักษณ์ "ay" ซึ่งตรงกับหน่วยเสียง "I" ในโครงสร้างหลักของ ThaiVCCVPhonemizer
                if (p == "ay") p = "I";

                if (p == "I" || p == "aw" || p == "am") {
                    currentSyllable += p;
                    syllables.Add(currentSyllable);
                    currentSyllable = "";
                    continue;
                }

                currentSyllable += p;

                if (dictVowels.Contains(p)) {
                    bool isCoda = false;
                    if (i + 1 < tokens.Length) {
                        string nextP = tokens[i + 1];
                        if (nextP == "ay") nextP = "I";

                        if (dictEndingConsonants.Contains(nextP)) {
                            if (i + 2 >= tokens.Length) {
                                isCoda = true;
                            } else {
                                string nextNextP = tokens[i + 2];
                                if (nextNextP == "ay") nextNextP = "I";

                                if (dictVowels.Contains(nextNextP) || nextNextP == "I" || nextNextP == "aw" || nextNextP == "am") {
                                    isCoda = false;
                                } else if (dictDiphthongs.Contains(nextNextP) && i + 3 < tokens.Length) {
                                    string thirdP = tokens[i + 3] == "ay" ? "I" : tokens[i + 3];
                                    if (dictVowels.Contains(thirdP) || thirdP == "I" || thirdP == "aw" || thirdP == "am") {
                                        isCoda = false;
                                    } else {
                                        isCoda = true;
                                    }
                                } else {
                                    isCoda = true;
                                }
                            }
                        }
                    }

                    if (isCoda) {
                        currentSyllable += tokens[i + 1];
                        i++;
                    }

                    syllables.Add(currentSyllable);
                    currentSyllable = "";
                }
            }

            if (!string.IsNullOrEmpty(currentSyllable)) {
                syllables.Add(currentSyllable);
            }

            return string.Join(" ", syllables);
        }

        private void LoadCustomDictionary() {
            if (isDictLoaded) return;

            lock (dictLock) {
                if (isDictLoaded) return;
                CustomDictionary.Clear();

                try {
                    // โหลดเฉพาะพจนานุกรมของระบบ UTAU VCCV เท่านั้น ("vccv")
                    ThaiDictionaryManager.ApplyToDictionary(CustomDictionary, phonemes => {
                        return phonemes.Contains(" ") ? CombineVccvTokensToSyllables(phonemes) : phonemes;
                    }, "vccv");

                    if (singer != null && !string.IsNullOrEmpty(singer.Location)) {
                        // สร้างและอ่านเฉพาะไฟล์ของ UTAU VCCV เท่านั้น ไม่โหลด dsdict-th ของ DiffSinger มาปะปน
                        string yamlPath = ThaiDictionaryManager.EnsureSingerVccvYaml(singer.Location);
                        if (!string.IsNullOrEmpty(yamlPath) && File.Exists(yamlPath)) {
                            var tempYamlDict = new Dictionary<string, string>(StringComparer.Ordinal);
                            int count = 0;
                            ThaiDictionaryManager.ParseVccvYamlDictionary(yamlPath, tempYamlDict, ref count);
                            foreach (var kvp in tempYamlDict) {
                                CustomDictionary[kvp.Key] = kvp.Value.Contains(" ") ? CombineVccvTokensToSyllables(kvp.Value) : kvp.Value;
                            }
                        }

                        string[] singerVccvDicts = { "TH-VCCV_Dict.txt", "TH_VCCV_Dict.txt", "th_vccv_custom_dict.txt" };
                        foreach (var dict in singerVccvDicts) {
                            string path = Path.Combine(singer.Location, dict);
                            if (File.Exists(path)) {
                                var lines = File.ReadAllLines(path, Encoding.UTF8);
                                foreach (var line in lines) {
                                    if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;
                                    var parts = line.Split(new[] { '=', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length >= 2) {
                                        string key = parts[0].Trim().Normalize(NormalizationForm.FormC);
                                        string val = parts[1].Trim().Normalize(NormalizationForm.FormC);
                                        CustomDictionary[key] = val.Contains(" ") ? CombineVccvTokensToSyllables(val) : val;
                                    }
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    Log.Error(ex, "เกิดข้อผิดพลาดในการโหลดพจนานุกรม UTAU VCCV (Failed to load UTAU VCCV custom dictionary)");
                }
                isDictLoaded = true;
            }
        }

        private bool checkOtoUntilHit(string[] input, Note note, out UOto oto) {
            oto = default;
            if (singer == null) return false;
            var attr = note.phonemeAttributes?.FirstOrDefault(a => a.index == 0) ?? default;
            foreach (string test in input) {
                if (string.IsNullOrEmpty(test)) continue;
                if (singer.TryGetMappedOto(test, note.tone + attr.toneShift, attr.voiceColor, out var otoCandidacy)) {
                    oto = otoCandidacy;
                    return true;
                }
            }
            return false;
        }

        private string GetLyricOrHint(Note note) {
            return string.IsNullOrEmpty(note.phoneticHint) ? note.lyric.Normalize(NormalizationForm.FormC) : note.phoneticHint.Normalize(NormalizationForm.FormC);
        }

        private string PreProcessThaiWord(string input) {
            switch (input) {
                case "บวร": return "บะวอน";
                case "สมร": return "สะหมอน";
                case "โสน": return "สะโหน";
                case "ธ": return "ทะ";
                case "ณ": return "นะ";
                case "ศร": return "สอน";
                case "พร": return "พอน";
                case "จร": return "จอน";
                case "กรรม": return "กำ";
                case "ธรรม": return "ทำ";
                case "พจน์": return "พด";
            }
            return input.Replace("ก็", "เกาะ")
                        .Replace("ฤทธิ์", "ริด")
                        .Replace("พฤกษ์", "เพลิก")
                        .Replace("ฤกษ์", "เลิก")
                        .Replace("ฤๅ", "รือ")
                        .Replace("ฤ", "รึ");
        }

        public string[] WordToPhonemes(string input) {
            if (!isDictLoaded) LoadCustomDictionary();
            if (string.IsNullOrWhiteSpace(input)) return Array.Empty<string>();

            input = input.Trim().Normalize(NormalizationForm.FormC);
            if (input != "-" && !input.StartsWith("+")) {
                input = PunctuationCleanupRegex.Replace(input, "").Trim();
            }
            if (string.IsNullOrEmpty(input)) return Array.Empty<string>();

            if (CustomDictionary.TryGetValue(input, out string? dictPhonemes)) {
                return dictPhonemes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (!ThaiCharRegex.IsMatch(input) && !input.Contains("+")) {
                var rawTokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (rawTokens.Length > 1) {
                    bool allTokensAreValidSyllables = rawTokens.All(t => ParseInput(t).Vowel != null);
                    if (!allTokensAreValidSyllables) {
                        string converted = CombineVccvTokensToSyllables(input);
                        return converted.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                }
                return rawTokens;
            }

            input = input.Replace(" ", "");
            if (string.IsNullOrEmpty(input)) return Array.Empty<string>();

            if (CustomDictionary.TryGetValue(input, out string? cleanedDictPhonemes)) {
                return cleanedDictPhonemes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }

            input = PreProcessThaiWord(input);
            input = input.Replace("\u0E40\u0E40", "\u0E41");
            input = KaranRegex.Replace(input, "");
            input = ToneRegex.Replace(input, "");

            if (!ThaiCharRegex.IsMatch(input)) return new[] { input };

            List<string> syllables = new List<string>();
            int safetyCounter = 0;
            while (input.Length > 0 && safetyCounter < 10) {
                safetyCounter++;
                bool matched = false;

                foreach (var mapping in CompiledVowelMappings) {
                    var match = mapping.Pattern.Match(input);
                    if (match.Success) {
                        string c = match.Groups[1].Value;
                        string x = match.Groups.Count > 2 ? match.Groups[2].Value : string.Empty;

                        if (mapping.Key == "cรรx" && string.IsNullOrEmpty(x)) x = "น";
                        else if (mapping.Key.EndsWith("x") && string.IsNullOrEmpty(x)) {
                            if (!TrueClusters.Contains(c) && c.Length > 1) {
                                x = c.Substring(c.Length - 1);
                                c = c.Substring(0, c.Length - 1);
                            }
                        }

                        if (c.Length >= 2 && (c.StartsWith("ห") || c.StartsWith("อ"))) c = c.Substring(1);
                        string cConverted = ConvertC(c);
                        string xConverted = ConvertX(x);

                        string phoneme;
                        if (mapping.Replacement == "a" && input.Contains("ั") && x == "ว") phoneme = cConverted + "ua";
                        else if (mapping.Key == "เcย") phoneme = cConverted + "3y";
                        else if (mapping.Replacement == "e" && x == "ย") phoneme = cConverted + "3" + xConverted;
                        else phoneme = cConverted + mapping.Replacement + xConverted;

                        syllables.Add(phoneme);
                        input = input.Substring(match.Length);
                        matched = true;
                        break;
                    }
                }

                if (!matched) {
                    if (input.Length == 1) {
                        string singleC = ConvertC(input);
                        syllables.Add(singleC == "-" ? "a" : singleC + "a");
                        input = "";
                    } else if (input.Length >= 2) {
                        if (input.Length == 3 && input[1] == 'ว') {
                            syllables.Add(ConvertC(input[0].ToString()) + "ua" + ConvertX(input[2].ToString()));
                            input = input.Substring(3);
                        } else if (input.Length == 4 && input[2] == 'ว') {
                            syllables.Add(ConvertC(input.Substring(0, 2)) + "ua" + ConvertX(input[3].ToString()));
                            input = input.Substring(4);
                        } else if (input.Length >= 3 && TrueClusters.Contains(input.Substring(0, 2))) {
                            string cStr = input.Substring(0, 2);
                            string xStr = input.Substring(2, 1);
                            syllables.Add(ConvertC(cStr) + "o" + ConvertX(xStr));
                            input = input.Substring(3);
                        } else if (input.Length == 2 && input[1] == 'ร') {
                            syllables.Add(ConvertC(input[0].ToString()) + "Qn");
                            input = "";
                        } else {
                            syllables.Add(ConvertC(input[0].ToString()) + "o" + ConvertX(input[1].ToString()));
                            input = input.Substring(2);
                        }
                    } else {
                        syllables.Add(input);
                        input = "";
                    }
                }
            }

            return syllables.ToArray();
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            if (!isDictLoaded) LoadCustomDictionary();

            var phonemes = new List<Phoneme>();
            var firstNote = notes[0];
            var currentLyric = GetLyricOrHint(firstNote);

            if (currentLyric.StartsWith("+") || currentLyric == "-") {
                List<string> tests = new List<string>();
                string prevTemp = prevNeighbour != null ? GetLyricOrHint(prevNeighbour.Value) : "";
                var prevThStrs = WordToPhonemes(prevTemp);
                var prevTh = ParseInput(prevThStrs.Length > 0 ? prevThStrs.Last() : "");

                if (prevNeighbour != null && prevTh.Vowel != null) {
                    string v = prevTh.Vowel;
                    if (checkOtoUntilHit(new[] { v + " -", v + "-", v }, firstNote, out var tempOto)) tests.Add(tempOto.Alias);
                    else tests.Add(v);
                }

                if (tests.Count == 0) {
                    if (checkOtoUntilHit(new[] { "-" }, firstNote, out var fallbackOto)) tests.Add(fallbackOto.Alias);
                    else tests.Add("-");
                }

                phonemes.Add(new Phoneme { phoneme = tests[0], position = 0 });
                return new Result { phonemes = phonemes.ToArray() };
            }

            bool forceClose = false;
            if (currentLyric.EndsWith("-") && currentLyric.Length > 1) {
                forceClose = true;
                currentLyric = currentLyric.Substring(0, currentLyric.Length - 1);
            }

            var syllables = WordToPhonemes(currentLyric);
            if (syllables.Length == 0) return new Result { phonemes = phonemes.ToArray() };

            string prevTempNote = prevNeighbour != null ? GetLyricOrHint(prevNeighbour.Value) : "";
            var prevSyllables = WordToPhonemes(prevTempNote);
            var prevThLast = ParseInput(prevSyllables.Length > 0 ? prevSyllables.Last() : "");

            for (int sIdx = 0; sIdx < syllables.Length; sIdx++) {
                string syllable = syllables[sIdx];
                var noteTh = ParseInput(syllable);

                if (noteTh.Vowel == null && noteTh.Consonant != null) {
                    noteTh = (noteTh.Consonant, noteTh.Dipthong, "a", noteTh.EndingConsonant);
                }

                int targetNoteIdx;
                int sylStartPos;
                int sylEndPos;
                Note currentNote;

                if (syllables.Length <= notes.Length) {
                    targetNoteIdx = sIdx;
                    currentNote = notes[targetNoteIdx];
                    sylStartPos = currentNote.position - firstNote.position;
                    sylEndPos = sylStartPos + currentNote.duration;
                } else {
                    if (sIdx < notes.Length - 1) {
                        targetNoteIdx = sIdx;
                        currentNote = notes[targetNoteIdx];
                        sylStartPos = currentNote.position - firstNote.position;
                        sylEndPos = sylStartPos + currentNote.duration;
                    } else {
                        targetNoteIdx = notes.Length - 1;
                        currentNote = notes[targetNoteIdx];
                        int sharedCount = syllables.Length - (notes.Length - 1);
                        int subIdx = sIdx - (notes.Length - 1);
                        int baseStart = currentNote.position - firstNote.position;
                        int dur = currentNote.duration / sharedCount;
                        sylStartPos = baseStart + subIdx * dur;
                        sylEndPos = (subIdx == sharedCount - 1) ? (baseStart + currentNote.duration) : (sylStartPos + dur);
                    }
                }
                int noteDuration = Math.Max(10, sylEndPos - sylStartPos);

                Note? nextSylNeighbour = null;
                if (sIdx == syllables.Length - 1) {
                    nextSylNeighbour = nextNeighbour;
                } else if (targetNoteIdx < notes.Length - 1) {
                    nextSylNeighbour = notes[targetNoteIdx + 1];
                }

                List<string> tests = new List<string>();

                if (noteTh.Consonant != null) {
                    if (noteTh.Dipthong == null && noteTh.Vowel != null) {
                        var cvFallbacks = new List<string> { noteTh.Consonant + noteTh.Vowel, noteTh.Consonant + " " + noteTh.Vowel };
                        if (noteTh.Consonant == "-") cvFallbacks.Add(noteTh.Vowel);

                        if (checkOtoUntilHit(cvFallbacks.ToArray(), currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                        else tests.Add(noteTh.Consonant == "-" ? noteTh.Vowel : noteTh.Consonant + noteTh.Vowel);
                    } else if (noteTh.Dipthong != null && noteTh.Vowel != null) {
                        if (checkOtoUntilHit(new[] { noteTh.Consonant + noteTh.Dipthong + noteTh.Vowel }, currentNote, out var tempOto)) {
                            tests.Add(tempOto.Alias);
                        } else {
                            if (checkOtoUntilHit(new[] { noteTh.Consonant + " " + noteTh.Dipthong, noteTh.Consonant + noteTh.Dipthong }, currentNote, out tempOto)) tests.Add(tempOto.Alias);
                            else if (checkOtoUntilHit(new[] { noteTh.Consonant }, currentNote, out tempOto)) tests.Add(tempOto.Alias);
                            else tests.Add(noteTh.Consonant);

                            if (checkOtoUntilHit(new[] { noteTh.Dipthong + noteTh.Vowel }, currentNote, out tempOto)) tests.Add(tempOto.Alias);
                            else tests.Add(noteTh.Dipthong + noteTh.Vowel);
                        }
                    }
                }

                if (noteTh.Consonant == null && noteTh.Vowel != null) {
                    if (sIdx == 0 && prevThLast.EndingConsonant != null) {
                        if (checkOtoUntilHit(new[] { prevThLast.EndingConsonant + " " + noteTh.Vowel, prevThLast.EndingConsonant + noteTh.Vowel }, currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                        else if (checkOtoUntilHit(new[] { "-" + noteTh.Vowel, "- " + noteTh.Vowel, noteTh.Vowel }, currentNote, out tempOto)) tests.Add(tempOto.Alias);
                        else tests.Add(noteTh.Vowel);
                    } else if (sIdx == 0 && prevThLast.Vowel != null) {
                        if (checkOtoUntilHit(new[] { noteTh.Vowel, "-" + noteTh.Vowel, "- " + noteTh.Vowel }, currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                        else tests.Add(noteTh.Vowel);
                    } else {
                        if (checkOtoUntilHit(new[] { "-" + noteTh.Vowel, "- " + noteTh.Vowel, noteTh.Vowel }, currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                        else tests.Add(noteTh.Vowel);
                    }
                }

                if (noteTh.EndingConsonant != null && noteTh.Vowel != null) {
                    if (checkOtoUntilHit(new[] { noteTh.Vowel + " " + noteTh.EndingConsonant, noteTh.Vowel + noteTh.EndingConsonant }, currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                    else if (checkOtoUntilHit(new[] { noteTh.EndingConsonant }, currentNote, out tempOto)) tests.Add(tempOto.Alias);
                    else tests.Add(noteTh.Vowel + noteTh.EndingConsonant);
                } else if (nextSylNeighbour != null && noteTh.Vowel != null && noteTh.EndingConsonant == null) {
                    var nextSylStrs = WordToPhonemes(GetLyricOrHint(nextSylNeighbour.Value));
                    var nextTh = ParseInput(nextSylStrs.Length > 0 ? nextSylStrs[0] : "");
                    if (nextTh.Consonant != null) {
                        if (checkOtoUntilHit(new[] { noteTh.Vowel + " " + nextTh.Consonant, noteTh.Vowel + nextTh.Consonant }, currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                    }
                }

                if (sIdx == 0 && prevNeighbour == null && tests.Count >= 1) {
                    string firstPh = tests[0];
                    if (checkOtoUntilHit(new[] { "- " + firstPh, "-" + firstPh, "-" + firstPh.Replace(" ", "") }, currentNote, out var tempOto)) tests[0] = tempOto.Alias;
                    else if (noteTh.Consonant == null && noteTh.Vowel != null) {
                        if (checkOtoUntilHit(new[] { "- " + noteTh.Vowel, "-" + noteTh.Vowel }, currentNote, out tempOto)) tests[0] = tempOto.Alias;
                    }
                }

                if (forceClose && sIdx == syllables.Length - 1 && tests.Count >= 1) {
                    if (noteTh.EndingConsonant == null && noteTh.Vowel != null) {
                        if (checkOtoUntilHit(new[] { noteTh.Vowel + " -", noteTh.Vowel + "-" }, currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                    }
                }

                if (tests.Count == 0) {
                    if (checkOtoUntilHit(new[] { syllable }, currentNote, out var fallbackOto)) tests.Add(fallbackOto.Alias);
                    else tests.Add(string.IsNullOrEmpty(syllable) ? "a" : syllable);
                }

                var testsArray = tests.ToArray();
                if (testsArray.Length > 0) {
                    if (sIdx == 0 && currentLyric != "-" && !currentLyric.StartsWith("+") && prevNeighbour == null && checkOtoUntilHit(new[] { "breath", "Breath" }, currentNote, out var breathOto)) {
                        int space = prev != null ? currentNote.position - (prev.Value.position + prev.Value.duration) : currentNote.position;
                        int breathPosition = space > 0 ? -Math.Min(space, 240) : -120;
                        if (breathPosition > -120) breathPosition = -120;
                        phonemes.Add(new Phoneme { phoneme = breathOto.Alias, position = breathPosition });
                    }

                    for (int i = 0; i < testsArray.Length; i++) {
                        int position = 0;
                        int vcPosition = Math.Max(0, noteDuration - 120);

                        if (nextSylNeighbour != null && testsArray[i].Contains(" ")) {
                            var nextSylStrs = WordToPhonemes(GetLyricOrHint(nextSylNeighbour.Value));
                            var nextTh = ParseInput(nextSylStrs.Length > 0 ? nextSylStrs[0] : "");
                            var nextCheck = nextTh.Vowel;
                            if (nextTh.Consonant != null) nextCheck = nextTh.Consonant + nextTh.Vowel;
                            if (nextTh.Dipthong != null) nextCheck = nextTh.Consonant + nextTh.Dipthong + nextTh.Vowel;

                            var nextAttr = nextSylNeighbour.Value.phonemeAttributes?.FirstOrDefault(a => a.index == 0) ?? default;
                            if (singer != null && !string.IsNullOrEmpty(nextCheck) && singer.TryGetMappedOto(nextCheck, nextSylNeighbour.Value.tone + nextAttr.toneShift, nextAttr.voiceColor, out var nextOto) && nextOto.Overlap > 0) {
                                vcPosition = Math.Max(0, noteDuration - MsToTick(nextOto.Overlap) - MsToTick(nextOto.Preutter));
                            }
                        }

                        if (noteTh.Dipthong == null || testsArray.Length <= 2) {
                            if (i == 1) {
                                position = Math.Max((int)(noteDuration * 0.65), vcPosition);
                            }
                        } else {
                            if (i == 1) {
                                position = Math.Min((int)(noteDuration * 0.16), 120);
                            } else if (i == 2) {
                                position = Math.Max((int)(noteDuration * 0.65), vcPosition);
                            }
                        }

                        if (testsArray[i].EndsWith("-") && testsArray.Length > 1 && i == testsArray.Length - 1) {
                            position = Math.Max((int)(noteDuration * 0.90), noteDuration - 18);
                        } else if (noteTh.EndingConsonant != null && i == testsArray.Length - 1) {
                            int consLength = 150;
                            if (noteDuration < 300) {
                                consLength = Math.Max(15, (int)(noteDuration * 0.4));
                            } else if (noteDuration < consLength * 1.5) {
                                consLength = (int)(noteDuration / 1.5);
                            }
                            position = Math.Max(0, noteDuration - consLength);
                        }

                        int absolutePosition = sylStartPos + position;
                        int lastPos = phonemes.Count > 0 ? phonemes.Last().position : -120;
                        int minGap = 10;

                        int safePos = Math.Max(absolutePosition, lastPos + minGap);
                        if (safePos >= sylEndPos) {
                            safePos = Math.Max(lastPos + minGap, sylEndPos - minGap);
                        }

                        phonemes.Add(new Phoneme { phoneme = testsArray[i], position = safePos });
                    }
                }
            }

            return new Result { phonemes = phonemes.ToArray() };
        }

        (string? Consonant, string? Dipthong, string? Vowel, string? EndingConsonant) ParseInput(string input) {
            if (string.IsNullOrEmpty(input)) return (null, null, null, null);
            string? consonant = null, diphthong = null, vowel = null, endingConsonant = null;

            foreach (var con in consonants) if (input.StartsWith(con) && (consonant == null || consonant.Length < con.Length)) consonant = con;
            int startIdx = consonant?.Length ?? 0;

            foreach (var dip in diphthongs) if (input.Substring(startIdx).StartsWith(dip) && (diphthong == null || diphthong.Length < dip.Length)) diphthong = dip;
            startIdx += diphthong?.Length ?? 0;

            foreach (var vow in vowels) if (input.Substring(startIdx).StartsWith(vow) && (vowel == null || vowel.Length < vow.Length)) vowel = vow;

            int vowelEndIdx = startIdx + (vowel?.Length ?? 0);
            if (vowelEndIdx < input.Length) {
                string remainder = input.Substring(vowelEndIdx);
                foreach (var con in endingConsonants) if (remainder.EndsWith(con) && (endingConsonant == null || endingConsonant.Length < con.Length)) endingConsonant = con;
            }

            return (consonant, diphthong, vowel, endingConsonant);
        }

        private string ConvertC(string input) {
            if (string.IsNullOrEmpty(input)) return "";
            if (input == "ทร" || input == "สร" || input == "ศร" || input == "ซร") return "s";
            if (input == "จร") return "j";
            if (input == "อ") return "-";
            if (input.Length >= 2 && (input.StartsWith("ห") || input.StartsWith("อ"))) input = input.Substring(1);

            char firstChar = input[0];
            char? secondChar = input.Length > 1 ? input[1] : (char?)null;
            if (CMapping.ContainsKey(firstChar)) {
                string firstCharConverted = CMapping[firstChar];
                if (secondChar != null && CMapping.ContainsKey((char)secondChar)) return firstCharConverted + CMapping[(char)secondChar];
                return firstCharConverted;
            }
            return input;
        }

        private string ConvertX(string input) {
            if (string.IsNullOrEmpty(input)) return "";
            if (input == "อ") return "";
            char firstChar = input[0];
            return XMapping.ContainsKey(firstChar) ? XMapping[firstChar] : input;
        }
    }
}