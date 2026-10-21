// ============================================================================
// Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : ThaiVCCVPhonemizer.cs
// Version      : 18.8
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by PRINTmov ]
// File Location: OpenUtau.Plugin.Builtin/ThaiVCCVPhonemizer.cs
// Description  : Thai VCCV Phonemizer with DiffSinger hint-to-syllables parsing,
//                Melisma (+) support, explicit fallback for 'อ', and raw phoneme bypass.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Thai VCCV Phonemizer", "TH VCCV Delta", "DELTA SYNTH", language: "TH")]
    public class ThaiVCCVPhonemizer : Phonemizer {

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

        private static readonly Dictionary<string, string> DsConsonantMap = new Dictionary<string, string> {
            {"K", "k"}, {"D", "d"}, {"B", "b"}, {"N", "n"}, {"M", "m"}, {"Y", "y"}, {"W", "w"}
        };

        private static readonly Dictionary<string, string> DsVowelMap = new Dictionary<string, string> {
            {"A", "@"}, {"E", "3"}, {"O", "Q"}, {"U", "1"}, {"Ua", "6"}, {"au", "aw"}
        };

        private static readonly Regex ToneRegex = new Regex("[่้๊๋็]", RegexOptions.Compiled);
        private static readonly Regex KaranRegex = new Regex(".์", RegexOptions.Compiled);
        private static readonly Regex ThaiCharRegex = new Regex("[ก-ฮ]", RegexOptions.Compiled);
        private static readonly List<(string Key, Regex Pattern, string Replacement)> CompiledVowelMappings = new List<(string, Regex, string)>();

        static ThaiVCCVPhonemizer() {
            foreach (var mapping in VowelMapping) {
                string pattern = "^" + mapping.Key.Replace("c", "([ก-ฮ][ลรว]?|อ[ย]?|ห[ก-ฮ]?)").Replace("x", "([ก-ฮ]{0,2})");
                CompiledVowelMappings.Add((mapping.Key, new Regex(pattern, RegexOptions.Compiled), mapping.Value));
            }
        }

        private Dictionary<string, string> CustomDictionary = new Dictionary<string, string>();
        private bool isDictLoaded = false;
        private readonly object dictLock = new object();
        private USinger? singer;

        public override void SetSinger(USinger singer) {
            this.singer = singer;
            LoadCustomDictionary();
        }

        private string ConvertDiffsingerToSyllables(string dsPhonemes) {
            var phonemes = dsPhonemes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> syllables = new List<string>();
            string currentSyllable = "";
            
            var dictVowels = new HashSet<string>(vowels);
            var dictDiphthongs = new HashSet<string>(diphthongs);
            var dictEndingConsonants = new HashSet<string>(endingConsonants);

            for (int i = 0; i < phonemes.Length; i++) {
                string p = phonemes[i];
                
                if (DsConsonantMap.TryGetValue(p, out string? mappedC)) p = mappedC;
                else if (DsVowelMap.TryGetValue(p, out string? mappedV)) p = mappedV;
                
                if (p == "I" || p == "aw") {
                    currentSyllable += p;
                    syllables.Add(currentSyllable);
                    currentSyllable = "";
                    continue;
                }

                currentSyllable += p;
                
                if (dictVowels.Contains(p)) {
                    bool isCoda = false;
                    if (i + 1 < phonemes.Length) {
                        string nextP = phonemes[i + 1];
                        if (DsConsonantMap.TryGetValue(nextP, out string? nextMappedC)) nextP = nextMappedC;

                        if (dictEndingConsonants.Contains(nextP)) {
                            if (i + 2 >= phonemes.Length) {
                                isCoda = true;
                            } else {
                                string nextNextP = phonemes[i + 2];
                                if (dictVowels.Contains(nextNextP) || nextNextP == "I" || nextNextP == "aw") {
                                    isCoda = false;
                                } else if (dictDiphthongs.Contains(nextNextP) && i + 3 < phonemes.Length && 
                                          (dictVowels.Contains(phonemes[i + 3]) || phonemes[i + 3] == "I" || phonemes[i + 3] == "aw")) {
                                    isCoda = false;
                                } else {
                                    isCoda = true;
                                }
                            }
                        }
                    }
                    
                    if (isCoda) {
                        string nextP = phonemes[i + 1];
                        if (DsConsonantMap.TryGetValue(nextP, out string? codaMappedC)) nextP = codaMappedC;
                        currentSyllable += nextP;
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
                
                try {
                    // โหลดพจนานุกรมภาษาไทยส่วนกลาง (Load unified Thai dictionary cache)
                    ThaiDictionaryManager.ApplyToDictionary(CustomDictionary, phonemes => ConvertDiffsingerToSyllables(phonemes));

                    if (singer != null && !string.IsNullOrEmpty(singer.Location)) {
                        string[] singerDicts = { "dsdict-th.txt", "th_vccv_custom_dict.txt", "th_custom_dict.txt" };
                        foreach (var dict in singerDicts) {
                            string path = Path.Combine(singer.Location, dict);
                            if (File.Exists(path)) {
                                var lines = File.ReadAllLines(path, Encoding.UTF8);
                                foreach (var line in lines) {
                                    if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;
                                    var parts = line.Split(new[] { '=', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length >= 2) {
                                        CustomDictionary[parts[0].Trim()] = ConvertDiffsingerToSyllables(parts[1].Trim());
                                    }
                                }
                            }
                        }
                    }
                } catch (Exception ex) { Log.Error(ex, "เกิดข้อผิดพลาดในการโหลดพจนานุกรมแบบกำหนดเอง (Failed to load custom dictionary)"); }
                isDictLoaded = true;
            }
        }

        private bool checkOtoUntilHit(string[] input, Note note, out UOto oto) {
            oto = default;
            if (singer == null) return false;
            var attr = note.phonemeAttributes?.FirstOrDefault(a => a.index == 0) ?? default;
            foreach (string test in input) {
                if (singer.TryGetMappedOto(test, note.tone + attr.toneShift, attr.voiceColor, out var otoCandidacy)) {
                    oto = otoCandidacy;
                    return true;
                }
            }
            return false;
        }

        private string GetLyricOrHint(Note note) {
            return string.IsNullOrEmpty(note.phoneticHint) ? note.lyric.Normalize() : note.phoneticHint.Normalize();
        }

        // [DELTA SYNTH] Preprocessor for exceptional reduced vowels and specific rules
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
            if (string.IsNullOrEmpty(input)) return Array.Empty<string>();
            
            if (CustomDictionary.ContainsKey(input)) {
                return CustomDictionary[input].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }

            // [DELTA SYNTH] Direct Phoneme Input Fallback / Phonetic Hint
            // หากผู้ใช้ป้อนคำอ่านแบบโรมาจิหรือ Hint แยกช่องไฟ (เช่น "j a n") ให้แปลงเป็นพยางค์ก่อนเข้า ParseInput
            if (!ThaiCharRegex.IsMatch(input) && !input.Contains("+")) {
                var rawTokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (rawTokens.Length > 1) {
                    bool allTokensAreValidSyllables = rawTokens.All(t => ParseInput(t).Vowel != null);
                    if (!allTokensAreValidSyllables) {
                        string converted = ConvertDiffsingerToSyllables(input);
                        return converted.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                }
                return rawTokens;
            }

            input = input.Replace(" ", "");
            if (string.IsNullOrEmpty(input)) return Array.Empty<string>();

            // Pre-process for exceptions before applying Regex
            input = PreProcessThaiWord(input);

            input = input.Replace("\u0E40\u0E40", "\u0E41");
            input = KaranRegex.Replace(input, "");
            
            // ไม้ไต่คู้ถูกเพิกเฉยพร้อมวรรณยุกต์
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
                            // [DELTA SYNTH] Fix Greedy Regex Eating Final Consonant for words like แล้ว (แลว)
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
                        syllables.Add(ConvertC(input));
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
                            // จัดการคำอย่าง ศร, พร, กร โดยอัตโนมัติ (c Q n)
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
            var phonemes = new List<Phoneme>();
            var firstNote = notes[0];
            var currentLyric = GetLyricOrHint(firstNote);

            // [DELTA SYNTH] Melisma and Multi-syllable Vowel Extension Handling (+)
            if (currentLyric.StartsWith("+") || currentLyric == "-") {
                List<string> tests = new List<string>();
                string prevTemp = prevNeighbour != null ? GetLyricOrHint(prevNeighbour.Value) : "";
                var prevThStrs = WordToPhonemes(prevTemp);
                var prevTh = ParseInput(prevThStrs.Length > 0 ? prevThStrs.Last() : "");

                if (prevNeighbour != null && prevTh.Vowel != null) {
                    string v = prevTh.Vowel;
                    if (checkOtoUntilHit(new[] { v + " -", v + "-", v }, firstNote, out var tempOto)) tests.Add(tempOto.Alias);
                }

                if (tests.Count == 0 && checkOtoUntilHit(new[] { "-" }, firstNote, out var fallbackOto)) tests.Add(fallbackOto.Alias);
                
                if (tests.Count > 0) {
                    phonemes.Add(new Phoneme { phoneme = tests[0], position = 0 });
                }
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

            int currentNoteIndex = 0;
            int currentTickPosition = 0;
            int lastNoteIndex = -1;

            for (int sIdx = 0; sIdx < syllables.Length; sIdx++) {
                string syllable = syllables[sIdx];
                var noteTh = ParseInput(syllable);

                Note currentNote = notes[currentNoteIndex];
                int noteStartPos = currentNote.position - firstNote.position;
                int noteEndPos = noteStartPos + currentNote.duration;

                if (sIdx > 0 && currentNoteIndex < notes.Length - 1) {
                    currentNoteIndex++;
                    currentNote = notes[currentNoteIndex];
                    noteStartPos = currentNote.position - firstNote.position;
                    noteEndPos = noteStartPos + currentNote.duration;
                }
                
                int noteDuration = currentNote.duration;
                int remainingSyllablesOnNote = 1;
                for (int nextSIdx = sIdx + 1; nextSIdx < syllables.Length; nextSIdx++) {
                    if (nextSIdx > 0 && currentNoteIndex < notes.Length - 1) break; 
                    remainingSyllablesOnNote++;
                }
                
                int sylDuration = noteDuration / remainingSyllablesOnNote;
                if (sIdx > 0 && currentNoteIndex == lastNoteIndex) {
                    noteStartPos = currentTickPosition;
                    noteEndPos = noteStartPos + sylDuration;
                    noteDuration = sylDuration;
                } else if (remainingSyllablesOnNote > 1) {
                    noteEndPos = noteStartPos + sylDuration;
                    noteDuration = sylDuration;
                }
                lastNoteIndex = currentNoteIndex;

                Note? nextSylNeighbour = null;
                if (sIdx == syllables.Length - 1) {
                    nextSylNeighbour = nextNeighbour;
                } else if (currentNoteIndex < notes.Length - 1) {
                    nextSylNeighbour = notes[currentNoteIndex + 1];
                }

                List<string> tests = new List<string>();

                if (noteTh.Consonant != null) {
                    if (noteTh.Dipthong == null && noteTh.Vowel != null) {
                        // [DELTA SYNTH] Fallback for "อ" (-) to plain vowel if "- V" is missing
                        var cvFallbacks = new List<string> { noteTh.Consonant + noteTh.Vowel, noteTh.Consonant + " " + noteTh.Vowel };
                        if (noteTh.Consonant == "-") cvFallbacks.Add(noteTh.Vowel);
                        
                        if (checkOtoUntilHit(cvFallbacks.ToArray(), currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                    } else if (noteTh.Dipthong != null && noteTh.Vowel != null) {
                        if (checkOtoUntilHit(new[] { noteTh.Consonant + noteTh.Dipthong + noteTh.Vowel }, currentNote, out var tempOto)) {
                            tests.Add(tempOto.Alias);
                        } else {
                            if (checkOtoUntilHit(new[] { noteTh.Consonant + " " + noteTh.Dipthong, noteTh.Consonant + noteTh.Dipthong }, currentNote, out tempOto)) tests.Add(tempOto.Alias);
                            else if (checkOtoUntilHit(new[] { noteTh.Consonant }, currentNote, out tempOto)) tests.Add(tempOto.Alias);
                            if (checkOtoUntilHit(new[] { noteTh.Dipthong + noteTh.Vowel }, currentNote, out tempOto)) tests.Add(tempOto.Alias);
                        }
                    }
                }

                if (noteTh.Consonant == null && noteTh.Vowel != null) {
                    if (sIdx == 0 && prevThLast.EndingConsonant != null) {
                        if (checkOtoUntilHit(new[] { prevThLast.EndingConsonant + " " + noteTh.Vowel, prevThLast.EndingConsonant + noteTh.Vowel }, currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                        else if (checkOtoUntilHit(new[] { noteTh.Vowel }, currentNote, out tempOto)) tests.Add(tempOto.Alias);
                    } else if (sIdx == 0 && prevThLast.Vowel != null) {
                        if (checkOtoUntilHit(new[] { noteTh.Vowel }, currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                    } else {
                        if (checkOtoUntilHit(new[] { noteTh.Vowel }, currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                    }
                }

                if (noteTh.EndingConsonant != null && noteTh.Vowel != null) {
                    if (checkOtoUntilHit(new[] { noteTh.Vowel + " " + noteTh.EndingConsonant, noteTh.Vowel + noteTh.EndingConsonant }, currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                    else if (checkOtoUntilHit(new[] { noteTh.EndingConsonant }, currentNote, out tempOto)) tests.Add(tempOto.Alias);
                } else if (nextSylNeighbour != null && noteTh.Vowel != null && noteTh.EndingConsonant == null) {
                    var nextSylStrs = WordToPhonemes(GetLyricOrHint(nextSylNeighbour.Value));
                    var nextTh = ParseInput(nextSylStrs.Length > 0 ? nextSylStrs[0] : "");
                    if (nextTh.Consonant != null) {
                        if (checkOtoUntilHit(new[] { noteTh.Vowel + " " + nextTh.Consonant, noteTh.Vowel + nextTh.Consonant }, currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                    }
                }

                if (sIdx == 0 && prevNeighbour == null && tests.Count >= 1) {
                    if (checkOtoUntilHit(new[] { "- " + tests[0], "-" + tests[0], "-" + tests[0].Replace(" ", "") }, currentNote, out var tempOto)) tests[0] = tempOto.Alias;
                    else if (noteTh.Consonant == null && noteTh.Vowel != null) {
                        if (checkOtoUntilHit(new[] { "- " + noteTh.Vowel, "-" + noteTh.Vowel }, currentNote, out tempOto)) tests[0] = tempOto.Alias;
                    }
                }

                if (forceClose && sIdx == syllables.Length - 1 && tests.Count >= 1) {
                    if (noteTh.EndingConsonant == null) {
                        if (checkOtoUntilHit(new[] { noteTh.Vowel + " -", noteTh.Vowel + "-" }, currentNote, out var tempOto)) tests.Add(tempOto.Alias);
                    }
                }

                // Raw Phoneme Injection
                if (tests.Count <= 0 && checkOtoUntilHit(new[] { syllable }, currentNote, out var fallbackOto)) tests.Add(syllable);

                var testsArray = tests.ToArray();
                if (checkOtoUntilHit(testsArray, currentNote, out var oto)) {

                    if (sIdx == 0 && currentLyric != "-" && !currentLyric.StartsWith("+") && prevNeighbour == null && checkOtoUntilHit(new[] { "breath" }, currentNote, out var breathOto)) {
                        int space = prev != null ? currentNote.position - (prev.Value.position + prev.Value.duration) : currentNote.position;
                        int breathPosition = space > 0 ? -Math.Min(space, 240) : -120;
                        if (breathPosition > -120) breathPosition = -120;
                        phonemes.Add(new Phoneme { phoneme = breathOto.Alias, position = breathPosition });
                    }

                    for (int i = 0; i < testsArray.Length; i++) {
                        int position = 0;
                        int vcPosition = noteDuration - 120;

                        if (nextSylNeighbour != null && testsArray[i].Contains(" ")) {
                            var nextSylStrs = WordToPhonemes(GetLyricOrHint(nextSylNeighbour.Value));
                            var nextTh = ParseInput(nextSylStrs.Length > 0 ? nextSylStrs[0] : "");
                            var nextCheck = nextTh.Vowel;
                            if (nextTh.Consonant != null) nextCheck = nextTh.Consonant + nextTh.Vowel;
                            if (nextTh.Dipthong != null) nextCheck = nextTh.Consonant + nextTh.Dipthong + nextTh.Vowel;

                            var nextAttr = nextSylNeighbour.Value.phonemeAttributes?.FirstOrDefault(a => a.index == 0) ?? default;
                            if (singer != null && singer.TryGetMappedOto(nextCheck, nextSylNeighbour.Value.tone + nextAttr.toneShift, nextAttr.voiceColor, out var nextOto) && oto.Overlap > 0) {
                                vcPosition = noteDuration - MsToTick(nextOto.Overlap) - MsToTick(nextOto.Preutter);
                            }
                        }

                        // [DELTA SYNTH] Consonant Expansion & Automated Prep Feature
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
                            // [DELTA SYNTH] Adaptive Timing Clamping for Ultra-Short Notes
                            if (noteDuration < 300) {
                                consLength = Math.Max(15, (int)(noteDuration * 0.4));
                            } else if (noteDuration < consLength * 1.5) {
                                consLength = (int)(noteDuration / 1.5);
                            }
                            position = noteDuration - consLength;
                        }

                        int absolutePosition = noteStartPos + position;
                        int lastPos = phonemes.Count > 0 ? phonemes.Last().position : -120;
                        int minGap = 10;
                        
                        // Strict Overlap Prevention
                        if (absolutePosition <= lastPos) {
                            absolutePosition = lastPos + minGap;
                        }
                        if (absolutePosition >= noteEndPos) {
                            absolutePosition = Math.Max(lastPos + minGap, noteEndPos - minGap);
                        }

                        phonemes.Add(new Phoneme { phoneme = testsArray[i], position = absolutePosition });
                        currentTickPosition = absolutePosition;
                    }
                }
            }

            int gap = prevNeighbour == null ? 9999 : firstNote.position - (prevNeighbour.Value.position + prevNeighbour.Value.duration);
            if (gap >= 120) {
                bool hasOpening = phonemes.Count > 0 && (phonemes[0].phoneme.StartsWith("- ") || phonemes[0].phoneme.StartsWith("-"));
                if (!hasOpening && singer != null) {
                    var attr = firstNote.phonemeAttributes?.FirstOrDefault(a => a.index == 0) ?? default;
                    if (singer.TryGetMappedOto("Breath", firstNote.tone + attr.toneShift, attr.voiceColor, out var breathOto)) {
                        int insertPos = -60;
                        if (phonemes.Count > 0 && insertPos >= phonemes[0].position) {
                            insertPos = phonemes[0].position - 10;
                        }
                        phonemes.Insert(0, new Phoneme { phoneme = breathOto.Alias, position = insertPos });
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
            if (input == "อ") return "-"; // [DELTA SYNTH] Explicit Glottal Stop Fix
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
            if (input == "อ") return ""; // [DELTA SYNTH] Ignore pseudo-ending 'อ' for vowels like เ-ือ
            char firstChar = input[0];
            return XMapping.ContainsKey(firstChar) ? XMapping[firstChar] : input;
        }
    }
}