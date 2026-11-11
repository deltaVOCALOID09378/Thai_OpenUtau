#pragma warning disable CS0618, CS0649, CS8632, CS0108
#nullable enable
// ============================================================================
// Date         : 10/11/2026 (B.E. 2569 / A.D. 2026)
// File Name    : ThaiCpVPhonemizer.cs
// Version      : 4.2
// Author Credit: Made And Checked By DELTA SYNTH & Gemini AI [ Original by Patiphat Wongyai (Delta) ]
// File Location: OpenUtau.Plugin.Builtin/ThaiCpVPhonemizer.cs
// Description  : ปรับปรุงการแบ่งสัดส่วนเวลาตามหลักทศนิยม ยืดหยุ่นตามความยาวโน้ต: พยัญชนะต้น C+V ไม่เกิน 5-7%
//                และตัวสะกดท้าย [v c] ขยายกว้างขึ้นเป็น 30-40% ปลดล็อกค่าคงที่ ticks ให้สอดรับกับทุกโน้ต
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Thai C+V Phonemizer", "TH C+V Delta", "DELTA SYNTH", language: "TH")]
    public class ThaiCpVPhonemizer : Phonemizer {

        static readonly string[] vowels = new string[] { "a", "i", "u", "e", "o", "@", "Q", "3", "6", "1", "ia", "ua", "I", "8", "am", "aw", "ay" };
        static readonly string[] diphthongs = new string[] { "r", "l", "w" };
        static readonly string[] consonants = new string[] { "b", "ch", "d", "f", "g", "h", "j", "k", "kh", "l", "m", "n", "p", "ph", "r", "s", "t", "th", "w", "y" };
        static readonly string[] endingConsonants = new string[] { "b", "ch", "d", "f", "g", "h", "j", "k", "kh", "l", "m", "n", "p", "ph", "r", "s", "t", "th", "w", "y" };

        private static readonly List<(string Key, string Value)> VowelMapping = new List<(string Key, string Value)> {
            ("เcือะ", "6"), ("เcือx", "6"), ("แcะ", "@"), ("แcx", "@"), ("เcอะ", "3"), ("เcิร์x", "3"), ("เcอx", "3"), ("เcอ", "3"), 
            ("เcย", "3"), ("ไc", "I"), ("ใc", "I"), ("ไcx", "I"), ("ใcx", "I"),
            ("เcาะ", "Q"), ("cอx", "Q"), ("แccx", "@"),
            ("cืx", "1"), ("cึx", "1"), ("cือ", "1"), ("cะ", "a"), ("cัx", "a"), ("cาx", "a"), ("cรรx", "a"), 
            ("เcา", "8"), ("เcาx", "8"), 
            ("เcะ", "e"), ("เcx", "e"), ("cิx", "i"), ("cีx", "i"),
            ("เcียะ", "ia"), ("เcียx", "ia"), ("โcะ", "o"), ("โcx", "o"), ("cุx", "u"), ("cูx", "u"), 
            ("cัวะ", "ua"), ("cัว", "ua"), ("cวx", "ua"), ("เcิx", "3"), ("เcิ", "3"),
            ("cำ", "am"), ("cำx", "am")
        };

        private static readonly Dictionary<char, string> CMapping = new Dictionary<char, string> {
            {'ก', "k"}, {'ข', "kh"}, {'ค', "kh"}, {'ฆ', "kh"}, {'ฅ', "kh"}, {'ฃ', "kh"}, {'จ', "j"}, {'ฉ', "ch"}, {'ช', "ch"}, {'ฌ', "ch"},
            {'ฎ', "d"}, {'ด', "d"}, {'ต', "t"}, {'ฏ', "t"}, {'ถ', "th"}, {'ฐ', "th"}, {'ฑ', "th"}, {'ฒ', "th"}, {'ธ', "th"}, {'ท', "th"},
            {'บ', "b"}, {'ป', "p"}, {'พ', "ph"}, {'ผ', "ph"}, {'ภ', "ph"}, {'ฟ', "f"}, {'ฝ', "f"}, {'ห', "h"}, {'ฮ', "h"},
            {'ม', "m"}, {'น', "n"}, {'ณ', "n"}, {'ร', "r"}, {'ล', "l"}, {'ฤ', "r"}, {'ส', "s"}, {'ศ', "s"}, {'ษ', "s"}, {'ซ', "s"},
            {'ง', "g"}, {'ย', "y"}, {'ญ', "y"}, {'ว', "w"}, {'ฬ', "r"}
        };

        private static readonly Dictionary<char, string> XMapping = new Dictionary<char, string> {
            {'บ', "b"}, {'ป', "b"}, {'พ', "b"}, {'ฟ', "b"}, {'ภ', "b"},
            {'ด', "d"}, {'จ', "d"}, {'ช', "d"}, {'ซ', "d"}, {'ฎ', "d"}, {'ฏ', "d"}, {'ฐ', "d"}, {'ฑ', "d"}, {'ฒ', "d"}, {'ต', "d"}, {'ถ', "d"}, {'ท', "d"}, {'ธ', "d"}, {'ศ', "d"}, {'ษ', "d"}, {'ส', "d"},
            {'ก', "k"}, {'ข', "k"}, {'ค', "k"}, {'ฆ', "k"}, {'ว', "w"}, {'ย', "y"}, {'ง', "g"}, {'ม', "m"},
            {'น', "n"}, {'ญ', "n"}, {'ณ', "n"}, {'ร', "n"}, {'ล', "n"}, {'ฬ', "n"}
        };

        private static readonly HashSet<string> TrueClusters = new HashSet<string> {
            "กร", "กล", "กว", "ขร", "ขล", "ขว", "คร", "คล", "คว", "ปร", "ปล", "พร", "พล", "ตร", "ผล", "บร", "บล", "ฟร", "ฟล", "ดร", "ทร", "หง", "หญ", "หน", "หม", "หย", "หร", "หล", "หว", "อย"
        };

        private static readonly Regex ToneRegex = new Regex("[่้๊๋็]", RegexOptions.Compiled);
        private static readonly Regex KaranRegex1 = new Regex("[ก-ฮ][ิุ]?์", RegexOptions.Compiled);
        private static readonly Regex KaranRegex2 = new Regex("[ก-ฮ]์", RegexOptions.Compiled);
        private static readonly Regex ThaiCharRegex = new Regex("[ก-ฮ]", RegexOptions.Compiled);
        private static readonly Regex ValidVowelRegex = new Regex("[ะาิีึืุูเแโไใโั]", RegexOptions.Compiled);

        private static readonly List<(string Key, Regex Pattern, string Replacement)> CompiledVowelMappings = new List<(string, Regex, string)>();

        static ThaiCpVPhonemizer() {
            foreach (var mapping in VowelMapping) {
                string pattern = "^" + mapping.Key.Replace("c", "([ก-ฮ][ลรว]?|อ[ย]?|ห[ก-ฮ]?)").Replace("x", "([ก-ฮ]{0,2})");
                CompiledVowelMappings.Add((mapping.Key, new Regex(pattern, RegexOptions.Compiled), mapping.Value));
            }
        }

        private Dictionary<string, string> CustomDictionary = new Dictionary<string, string>(StringComparer.Ordinal);
        private USinger? singer;
        private bool isDictLoaded = false;
        private int loadedDictVersion = -1;
        
        public override void SetSinger(USinger singer) {
            this.singer = singer;
            isDictLoaded = false;
            LoadCustomDictionary();
        }

        // แปลงฟอนีม Diffsinger ให้เป็นสระสไตล์ VCCV/C+V (Convert Diffsinger phonemes)
        private string ConvertDiffsingerToSyllables(string dsPhonemes) {
            var phonemes = dsPhonemes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> syllables = new List<string>();
            string currentSyllable = "";
            var dictVowels = new HashSet<string> { "a", "i", "u", "e", "o", "@", "Q", "3", "6", "1", "ia", "ua", "am", "aw", "ay", "I", "8" };
            var diphthongs = new HashSet<string> { "r", "l", "w" };
            var endingConsonants = new HashSet<string> { "b", "ch", "d", "f", "g", "h", "j", "k", "kh", "l", "m", "n", "p", "ph", "r", "s", "t", "th", "w", "y" };

            for (int i = 0; i < phonemes.Length; i++) {
                string p = phonemes[i];
                if (p == "K") p = "k";
                else if (p == "D") p = "d";
                else if (p == "B") p = "b";
                else if (p == "N") p = "n";
                else if (p == "M") p = "m";
                else if (p == "Y") p = "y";
                else if (p == "W") p = "w";
                
                if (p == "A") p = "@";
                else if (p == "E") p = "3";
                else if (p == "O") p = "Q";
                else if (p == "U") p = "1";
                else if (p == "Ua") p = "6";
                else if (p == "au") p = "aw";
                
                if (p == "I") {
                    currentSyllable += "ay";
                    syllables.Add(currentSyllable);
                    currentSyllable = "";
                    continue;
                }
                if (p == "aw") {
                    currentSyllable += "aw";
                    syllables.Add(currentSyllable);
                    currentSyllable = "";
                    continue;
                }

                currentSyllable += p;
                
                if (dictVowels.Contains(p)) {
                    bool isCoda = false;
                    if (i + 1 < phonemes.Length) {
                        string nextP = phonemes[i + 1];
                        if (nextP == "K") nextP = "k";
                        else if (nextP == "D") nextP = "d";
                        else if (nextP == "B") nextP = "b";
                        else if (nextP == "N") nextP = "n";
                        else if (nextP == "M") nextP = "m";
                        else if (nextP == "Y") nextP = "y";
                        else if (nextP == "W") nextP = "w";

                        if (endingConsonants.Contains(nextP)) {
                            if (i + 2 >= phonemes.Length) {
                                isCoda = true;
                            } else {
                                string nextNextP = phonemes[i + 2];
                                if (dictVowels.Contains(nextNextP) || nextNextP == "I" || nextNextP == "aw") {
                                    isCoda = false;
                                } else if (diphthongs.Contains(nextNextP) && i + 3 < phonemes.Length && (dictVowels.Contains(phonemes[i + 3]) || phonemes[i + 3] == "I" || phonemes[i + 3] == "aw")) {
                                    isCoda = false;
                                } else {
                                    isCoda = true;
                                }
                            }
                        }
                    }
                    
                    if (isCoda) {
                        string nextP = phonemes[i + 1];
                        if (nextP == "K") nextP = "k";
                        else if (nextP == "D") nextP = "d";
                        else if (nextP == "B") nextP = "b";
                        else if (nextP == "N") nextP = "n";
                        else if (nextP == "M") nextP = "m";
                        else if (nextP == "Y") nextP = "y";
                        else if (nextP == "W") nextP = "w";
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
            int currentVer = ThaiDictionaryManager.EnsureUpToDate();
            if (isDictLoaded && loadedDictVersion == currentVer) return;
            try {
                // โหลดพจนานุกรมภาษาไทยส่วนกลาง (Load custom Thai dictionary)
                ThaiDictionaryManager.ApplyToDictionary(CustomDictionary, phonemes => ConvertDiffsingerToSyllables(phonemes));
                
                if (singer != null && !string.IsNullOrEmpty(singer.Location)) {
                    string[] singerDicts = { "dsdict-th.txt", "th_cpv_custom_dict.txt", "th_custom_dict.txt" };
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
                loadedDictVersion = ThaiDictionaryManager.CurrentVersion;
            } catch (Exception ex) { Log.Error(ex, "Failed to load custom dictionary."); }
            isDictLoaded = true;
        }

        private bool checkOtoUntilHit(string[] input, Note note, out UOto oto) {
            oto = default;
            if (singer == null) return false;
            var attr = note.phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            foreach (string test in input) {
                if (singer.TryGetMappedOto(test, note.tone + attr.toneShift, attr.voiceColor, out var otoCandidacy)) {
                    oto = otoCandidacy;
                    return true;
                }
            }
            return false;
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            var firstNote = notes[0];
            var lyric = string.IsNullOrEmpty(firstNote.phoneticHint) ? firstNote.lyric : firstNote.phoneticHint;
            var currentLyric = (lyric ?? "a").Normalize();
            var syllables = WordToPhonemes(currentLyric);
            if (syllables.Length == 0) syllables = new[] { currentLyric };

            var phonemes = new List<Phoneme>();
            
            bool isStart = prevNeighbour == null
                || string.IsNullOrWhiteSpace(prevNeighbour.Value.lyric)
                || prevNeighbour.Value.lyric == "-"
                || prevNeighbour.Value.lyric.ToLower() == "r";

            for (int sIdx = 0; sIdx < syllables.Length; sIdx++) {
                string syllable = syllables[sIdx];
                List<string> aliases = new List<string>();

                // คำนวณช่วงเวลาของพยางค์บนตัวโน้ตอย่างเที่ยงตรง ป้องกันการล้นขอบเขตโน้ต (Safe Sub-Interval Division)
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

                bool forceClose = false;
                bool isEnd = false;
                if (sIdx == syllables.Length - 1) {
                    isEnd = nextNeighbour == null || nextNeighbour.Value.lyric == "-" || nextNeighbour.Value.lyric.ToLower() == "r";
                    if (syllable.EndsWith("-") && syllable.Length > 1) {
                        forceClose = true;
                        isEnd = true;
                        syllable = syllable.Substring(0, syllable.Length - 1);
                    }
                }

                if (syllable == "-") {
                    aliases.Add("-");
                } else {
                    var (C, Dip, V, X) = ParseInput(syllable);

                    if (!string.IsNullOrEmpty(C)) {
                        aliases.Add((sIdx == 0 && isStart) ? $"- {C}" : C);
                        if (!string.IsNullOrEmpty(Dip)) aliases.Add(Dip);
                        if (!string.IsNullOrEmpty(V)) aliases.Add(V);
                    } else if (!string.IsNullOrEmpty(V)) {
                        aliases.Add((sIdx == 0 && isStart) ? $"- {V}" : V);
                    } else {
                        aliases.Add(syllable);
                    }

                    if (!string.IsNullOrEmpty(X)) aliases.Add(X);
                    if (forceClose || (sIdx == syllables.Length - 1 && (nextNeighbour == null || nextNeighbour.Value.lyric == "-"))) aliases.Add("-");
                }

                if (aliases.Count == 0) aliases.Add(string.IsNullOrEmpty(syllable) ? "a" : syllable);

                for (int i = 0; i < aliases.Count; i++) {
                    string alias = aliases[i];
                    if (!checkOtoUntilHit(new[] { alias }, currentNote, out var oto) && alias.StartsWith("- ")) {
                        alias = alias.Substring(2);
                    }

                    int position = 0;
                    double consRatio = Math.Clamp(0.05 + 0.02 * Math.Min(1.0, (double)noteDuration / 480.0), 0.05, 0.07);
                    double vcRatio = Math.Clamp(0.33 + 0.05 * Math.Min(1.0, (double)noteDuration / 480.0), 0.30, 0.40);

                    if (i == 0) {
                        position = 0;
                    } else if (alias == "-") {
                        position = (int)Math.Round(noteDuration * 0.95);
                    } else if (i == 1 && !vowels.Any(v => alias.Contains(v))) {
                        // พยัญชนะควบตัวที่สอง (CCV แบ่งพื้นที่ในกลุ่มพยัญชนะต้น 5-7%)
                        position = (int)Math.Round(noteDuration * (consRatio * 0.5));
                    } else if ((i == 1 || i == 2) && vowels.Any(v => alias.Contains(v))) {
                        // สระหลัก เริ่มต้นเมื่อพยัญชนะต้นสิ้นสุด (พยัญชนะต้น C+V รวมไม่เกิน 5-7% เสมอ)
                        position = (int)Math.Round(noteDuration * consRatio);
                    } else if (i == aliases.Count - 1 || (aliases.Count > 1 && i == aliases.Count - 2 && aliases[aliases.Count - 1] == "-")) {
                        // ตัวสะกดท้าย [v c] ขยายพื้นที่กว้างขึ้นเป็น 30-40% แบบยืดหยุ่นตามความยาวโน้ต
                        position = (int)Math.Round(noteDuration * (1.0 - vcRatio));
                    } else {
                        double stepRatio = 1.0 - (vcRatio * (1.0 + 0.1 * (aliases.Count - 1 - i)));
                        position = (int)Math.Round(noteDuration * Math.Clamp(stepRatio, consRatio + 0.05, 0.90));
                    }

                    // ป้องกันเสียงผีสิง หรือตำแหน่งล้นขอบเขต (Monotonic Timing Safety)
                    int absolutePosition = position + sylStartPos;
                    int lastPos = phonemes.Count > 0 ? phonemes.Last().position : -120;
                    int safePos = Math.Max(absolutePosition, lastPos + 10);
                    if (sIdx > 0 && safePos < sylStartPos) {
                        safePos = Math.Max(sylStartPos, lastPos + 10);
                    }
                    if (safePos >= sylEndPos && sylEndPos - 10 > lastPos) {
                        safePos = Math.Max(lastPos + 10, sylEndPos - 10);
                    }
                    phonemes.Add(new Phoneme { phoneme = alias, position = safePos });
                }
            }

            int gap = prevNeighbour == null ? 9999 : firstNote.position - (prevNeighbour.Value.position + prevNeighbour.Value.duration);
            if (gap >= 120 && singer != null) {
                bool hasOpening = phonemes.Count > 0 && (phonemes[0].phoneme.StartsWith("- ") || phonemes[0].phoneme.StartsWith("-"));
                if (!hasOpening) {
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

            foreach (var con in consonants) {
                if (input.StartsWith(con) && (consonant == null || consonant.Length < con.Length)) consonant = con;
            }
            int startIdx = consonant?.Length ?? 0;
            foreach (var dip in diphthongs) {
                if (input.Substring(startIdx).StartsWith(dip) && (diphthong == null || diphthong.Length < dip.Length)) diphthong = dip;
            }
            startIdx += diphthong?.Length ?? 0;
            foreach (var vow in vowels) {
                if (input.Substring(startIdx).StartsWith(vow) && (vowel == null || vowel.Length < vow.Length)) vowel = vow;
            }

            int vowelEndIdx = startIdx + (vowel?.Length ?? 0);
            if (vowelEndIdx < input.Length) {
                string remainder = input.Substring(vowelEndIdx);
                foreach (var con in endingConsonants) {
                    if (remainder.EndsWith(con) && (endingConsonant == null || endingConsonant.Length < con.Length)) endingConsonant = con;
                }
            }

            return (consonant, diphthong, vowel, endingConsonant);
        }

        public string[] WordToPhonemes(string input) {
            if (string.IsNullOrEmpty(input)) return new string[0];
            LoadCustomDictionary();
            input = input.Normalize(NormalizationForm.FormC);
            
            if (CustomDictionary.ContainsKey(input)) {
                return CustomDictionary[input].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }

            input = input.Replace(" ", "");
            if (string.IsNullOrEmpty(input)) return new string[0];

            input = input.Replace("ก็", "ก้อ").Replace("บ่", "บ่อ").Replace("ฤทธิ์", "ริด").Replace("อังกฤษ", "อังกิด").Replace("\u0E40\u0E40", "\u0E41");
            input = KaranRegex1.Replace(input, "");
            input = KaranRegex2.Replace(input, "");
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
                        else if (mapping.Key.EndsWith("x") && string.IsNullOrEmpty(x) && !mapping.Key.StartsWith("c")) {
                            if (!TrueClusters.Contains(c) && c.Length > 1) {
                                x = c.Substring(c.Length - 1);
                                c = c.Substring(0, c.Length - 1);
                            }
                        }

                        if (x.Length > 1) x = x.Substring(0, 1);
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
                    if (!ValidVowelRegex.IsMatch(input)) {
                        if (input.Contains("ว") && input.Length >= 3) {
                            int wIdx = input.IndexOf('ว');
                            string cStr = input.Substring(0, wIdx);
                            string xStr = input.Substring(wIdx + 1);
                            if (xStr.Length > 1) xStr = xStr.Substring(0, 1);
                            syllables.Add(ConvertC(cStr) + "ua" + ConvertX(xStr));
                            input = "";
                        } else if (input.Length >= 2) {
                            string cStr = input[0].ToString();
                            string xStr = input.Substring(1);
                            if (input.Length >= 3 && TrueClusters.Contains(input.Substring(0, 2))) {
                                cStr = input.Substring(0, 2);
                                xStr = input.Substring(2);
                            }
                            if (xStr.Length > 1) xStr = xStr.Substring(0, 1);
                            syllables.Add(ConvertC(cStr) + "o" + ConvertX(xStr));
                            input = "";
                        } else {
                            syllables.Add(input);
                            input = "";
                        }
                    } else {
                        syllables.Add(input);
                        input = "";
                    }
                }
            }

            return syllables.ToArray();
        }

        private string ConvertC(string input) {
            if (string.IsNullOrEmpty(input) || input == "อ") return "";
            if (input == "ทร" || input == "สร" || input == "ศร" || input == "ซร") return "s";
            if (input == "จร") return "j";
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
            char firstChar = input[0];
            return XMapping.ContainsKey(firstChar) ? XMapping[firstChar] : input;
        }
    }
}
