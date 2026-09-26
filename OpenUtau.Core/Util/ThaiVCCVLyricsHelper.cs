// ==========================================
// Made And Checked By DELTA SYNTH & Gemini AI
// Original by Patiphat Wongyai
// Version: v.17.3
// History/Summary: สร้างตัวช่วยแปลงเนื้อร้องสำหรับ VCCV ภาษาไทย 
// ==========================================
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace OpenUtau.Core.Util {
    public class ThaiVCCVLyricsHelper : ILyricsHelper {
        public string Source => "Thai VCCV";

        private static readonly List<(string Key, string Value)> VowelMapping = new List<(string Key, string Value)> {
            ("เcือะ", "6"), ("เcือx", "6"), ("แcะ", "@"), ("แcx", "@"), ("เcอะ", "3"), ("เcิร์x", "3"), ("เcอx", "3"), ("เcอ", "3"),
            ("เcย", "3"), ("ไc", "ay"), ("ใc", "ay"), ("ไcx", "ay"), ("ใcx", "ay"),
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
            {'ส', "s"}, {'ศ', "s"}, {'ษ', "s"}, {'ซ', "s"}, {'ง', "g"}, {'ย', "y"}, {'ญ', "y"}, {'ว', "w"}, {'ฬ', "r"}
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
        
        private static readonly List<(string Key, Regex Pattern, string Replacement)> CompiledVowelMappings = new List<(string, Regex, string)>();

        static ThaiVCCVLyricsHelper() {
            foreach (var mapping in VowelMapping) {
                string pattern = "^" + mapping.Key.Replace("c", "([ก-ฮ][ลรว]?|อ[ย]?|ห[ก-ฮ]?)").Replace("x", "([ก-ฮ]{0,2})");
                CompiledVowelMappings.Add((mapping.Key, new Regex(pattern, RegexOptions.Compiled), mapping.Value));
            }
        }

        public string Convert(string lyric) {
            var words = WordToPhonemes(lyric);
            if (words == null || words.Length == 0) return lyric;
            return string.Join(" ", words);
        }

        private string[] WordToPhonemes(string lyric) {
            lyric = ToneRegex.Replace(lyric, "");
            lyric = KaranRegex.Replace(lyric, "");
            var syllables = new List<string>();
            while (lyric.Length > 0) {
                bool matched = false;
                foreach (var mapping in CompiledVowelMappings) {
                    var match = mapping.Pattern.Match(lyric);
                    if (match.Success) {
                        string matchedText = match.Value;
                        string cPart = match.Groups[1].Value;
                        string xPart = match.Groups.Count > 2 ? match.Groups[2].Value : "";
                        string romanizedVowel = mapping.Replacement;
                        
                        string romanizedC = "";
                        if (cPart.Length > 0) {
                            if (cPart.Length == 2 && TrueClusters.Contains(cPart)) {
                                romanizedC = (CMapping.ContainsKey(cPart[0]) ? CMapping[cPart[0]] : "") + (CMapping.ContainsKey(cPart[1]) ? CMapping[cPart[1]] : "");
                            } else if (cPart.Length == 2 && cPart[0] == 'ห') {
                                romanizedC = CMapping.ContainsKey(cPart[1]) ? CMapping[cPart[1]] : "";
                            } else if (cPart.Length == 2 && cPart == "อย") {
                                romanizedC = "y";
                            } else {
                                romanizedC = CMapping.ContainsKey(cPart[0]) ? CMapping[cPart[0]] : "";
                            }
                        }
                        
                        string romanizedX = "";
                        if (xPart.Length > 0) {
                            if (xPart.Length == 2 && xPart.EndsWith("ร")) {
                                romanizedX = XMapping.ContainsKey(xPart[0]) ? XMapping[xPart[0]] : "";
                            } else {
                                romanizedX = XMapping.ContainsKey(xPart[0]) ? XMapping[xPart[0]] : "";
                            }
                        }
                        
                        if (string.IsNullOrEmpty(romanizedVowel)) {
                            syllables.Add(matchedText);
                        } else {
                            if (string.IsNullOrEmpty(romanizedC)) {
                                syllables.Add(romanizedVowel + (string.IsNullOrEmpty(romanizedX) ? "" : " " + romanizedX));
                            } else {
                                syllables.Add(romanizedC + " " + romanizedVowel + (string.IsNullOrEmpty(romanizedX) ? "" : " " + romanizedX));
                            }
                        }
                        lyric = lyric.Substring(match.Length);
                        matched = true;
                        break;
                    }
                }
                
                if (!matched) {
                    if (lyric.StartsWith("อ")) {
                        syllables.Add("Q");
                        lyric = lyric.Substring(1);
                        matched = true;
                    } else if (lyric.Length >= 2 && ThaiCharRegex.IsMatch(lyric.Substring(0, 1)) && ThaiCharRegex.IsMatch(lyric.Substring(1, 1))) {
                        string c = lyric.Substring(0, 1);
                        string x = lyric.Substring(1, 1);
                        string romC = CMapping.ContainsKey(c[0]) ? CMapping[c[0]] : "";
                        string romX = XMapping.ContainsKey(x[0]) ? XMapping[x[0]] : "";
                        syllables.Add(romC + " o " + romX);
                        lyric = lyric.Substring(2);
                        matched = true;
                    }
                }

                if (!matched) {
                    syllables.Add(lyric.Substring(0, 1));
                    lyric = lyric.Substring(1);
                }
            }
            return syllables.ToArray();
        }
    }
}
