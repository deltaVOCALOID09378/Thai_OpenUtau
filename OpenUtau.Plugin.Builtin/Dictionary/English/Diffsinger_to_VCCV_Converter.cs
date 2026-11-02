// ============================================================================
// 1. Date: 29 ตุลาคม พ.ศ. 2569 (29 October 2026)
// 2. File Name: Diffsinger_to_VCCV_Converter.py
// 3. Version: v1.8 (+0.2 from v1.6)
// 4. Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH & Gemini AI ]
// 5. Location: OpenUtau/Core/DiffSinger/Diffsinger_to_VCCV_Converter.py
// 6. Description: Upgrade Arpasing phoneme map to strictly use UPPERCASE matching phones.txt standards and fix G2P case-sensitivity fallback issues.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.DiffSinger
{
    [Phonemizer("Thai DiffSinger Advanced Phonemizer", "TH DiffSinger Adv", language: "TH", author: "DELTA SYNTH & Gemini AI")]
    public class ThaiDiffSingerPhonemizerExtended : DiffSingerBasePhonemizer
    {
        protected override string GetDictionaryName() => "dict.txt";
        protected override string GetLangCode() => "th";

        // ตารางแปลงหน่วยเสียงขั้นสูงจาก DiffSinger สู่มาตรฐาน Thai VCCV
        private static readonly Dictionary<string, string> AdvancedPhonemeMap = new(StringComparer.Ordinal)
        {
            { "ng", "g" },    
            { "kk", "k" },    
            { "k", "kh" },    
            { "pp", "p" },    
            { "p", "ph" },    
            { "tt", "t" },    
            { "t", "th" },    
            { "ch", "ch" },   
            { "j", "j" },     
            { "b", "b" },     
            { "d", "d" },     
            { "f", "f" },     
            { "h", "h" },     
            { "s", "s" },     
            { "r", "r" },     
            { "l", "l" },     
            { "m", "m" },     
            { "n", "n" },     
            { "w", "w" },     
            { "y", "y" },     
            { "A", "@" },     
            { "O", "Q" },     
            { "E", "3" },     
            { "Ua", "6" },    
            { "U", "1" },     
            { "I", "ay" },    
            { "au", "aw" },   
            { "ia", "ia" },   
            { "ua", "ua" },   
            { "oa", "oa" },   
            { "a", "a" },     
            { "e", "e" },     
            { "i", "i" },     
            { "o", "o" },     
            { "u", "u" }      
        };

        // ตารางแปลงหน่วยเสียงสู่มาตรฐาน CMU Arpasing (บังคับตัวพิมพ์ใหญ่ตาม phones.txt)
        private static readonly Dictionary<string, string> ArpasingPhonemeMap = new(StringComparer.Ordinal)
        {
            { "ng", "NG" },
            { "kk", "K" },
            { "k", "K" },
            { "pp", "P" },
            { "p", "P" },
            { "tt", "T" },
            { "t", "T" },
            { "ch", "CH" },
            { "j", "JH" },
            { "b", "B" },
            { "d", "D" },
            { "f", "F" },
            { "h", "HH" },
            { "s", "S" },
            { "r", "R" },
            { "l", "L" },
            { "m", "M" },
            { "n", "N" },
            { "w", "W" },
            { "y", "Y" },
            { "A", "AE" },
            { "O", "AO" },
            { "E", "ER" },
            { "Ua", "UH" },
            { "U", "UH" },
            { "I", "AY" },
            { "au", "AW" },
            { "ia", "IY" },
            { "ua", "UW" },
            { "oa", "UW" },
            { "a", "AA" },
            { "e", "EH" },
            { "i", "IY" },
            { "o", "OW" },
            { "u", "UW" },
            { "K", "K" },
            { "B", "P" },
            { "D", "T" },
            { "W", "W" },
            { "Y", "Y" }
        };

        // ตารางบังคับเสียงตัวสะกดตามมาตราตัวสะกดภาษาไทย
        private static readonly Dictionary<string, string> StrictEndingConsonants = new(StringComparer.Ordinal)
        {
            { "ng", "g" }, { "m", "m" }, { "n", "n" }, { "w", "w" }, { "y", "y" },
            { "k", "k" }, { "K", "k" }, { "p", "b" }, { "B", "b" }, { "t", "d" }, { "D", "d" },
            { "W", "w" }, { "Y", "y" }, { "M", "m" }, { "N", "n" }, { "G", "g" }
        };

        // ชุดสระในระบบ DiffSinger เพื่อใช้ระบุขอบเขตพยางค์ (Syllable Boundary Detection)
        private static readonly HashSet<string> DiffSingerVowels = new(StringComparer.Ordinal)
        {
            "A", "E", "I", "O", "U", "Ua", "a", "au", "e", "i", "ia", "o", "u", "ua", "oa",
            "@", "Q", "3", "6", "1", "aw", "ay"
        };

        // ตารางแปลงพยัญชนะไทยเป็นหน่วยเสียงพยัญชนะต้น DiffSinger สำหรับอักษรย่อ
        private static readonly Dictionary<char, string> ThaiConsonantToDiffSingerOnset = new()
        {
            { 'ก', "kk" },
            { 'ข', "k" }, { 'ฃ', "k" }, { 'ค', "k" }, { 'ฅ', "k" }, { 'ฆ', "k" },
            { 'ง', "ng" },
            { 'จ', "j" },
            { 'ฉ', "ch" }, { 'ช', "ch" }, { 'ฌ', "ch" },
            { 'ซ', "s" }, { 'ศ', "s" }, { 'ษ', "s" }, { 'ส', "s" },
            { 'ญ', "y" }, { 'ย', "y" },
            { 'ฎ', "d" }, { 'ด', "d" },
            { 'ฏ', "tt" }, { 'ต', "tt" },
            { 'ฐ', "t" }, { 'ฑ', "t" }, { 'ฒ', "t" }, { 'ถ', "t" }, { 'ท', "t" }, { 'ธ', "t" },
            { 'ณ', "n" }, { 'น', "n" },
            { 'บ', "b" },
            { 'ป', "pp" },
            { 'ผ', "p" }, { 'พ', "p" }, { 'ภ', "p" },
            { 'ฝ', "f" }, { 'ฟ', "f" },
            { 'ม', "m" },
            { 'ร', "r" },
            { 'ล', "l" }, { 'ฬ', "l" },
            { 'ว', "w" },
            { 'ห', "h" }, { 'ฮ', "h" },
            { 'อ', "" } 
        };

        // พจนานุกรมแก้ไขคำอ่านตามมาตรฐานราชบัณฑิตยสถาน
        private static readonly Dictionary<string, string[]> RoyalInstituteWordOverrides = new(StringComparer.Ordinal)
        {
            { "กค", new[] { "kk", "O", "k", "O" } },
            { "กทม", new[] { "kk", "O", "t", "O", "m", "O" } },
            { "ดร", new[] { "d", "O", "r", "O" } },
            { "ผช", new[] { "p", "O", "ch", "O" } },
            { "ผญ", new[] { "p", "O", "y", "O" } },
            { "ยธ", new[] { "y", "O", "t", "O" } },
            { "รพ", new[] { "r", "O", "p", "O" } },
            { "สว", new[] { "s", "O", "w", "O" } },
            { "สส", new[] { "s", "O", "s", "O" } },
            { "สสส", new[] { "s", "O", "s", "O", "s", "O" } },
            { "กอปร", new[] { "kk", "O", "B" } },
            { "กาญจน์", new[] { "kk", "a", "n" } },
            { "ความศรัทธา", new[] { "k", "w", "a", "m", "s", "a", "D", "t", "a" } },
            { "ความเจริญรุ่งเรือง", new[] { "k", "w", "a", "m", "j", "a", "r", "E", "n", "r", "u", "ng", "r", "Ua", "ng" } },
            { "เจริญรุ่งเรือง", new[] { "j", "a", "r", "E", "n", "r", "u", "ng", "r", "Ua", "ng" } },
            { "ขนาดเล็ก", new[] { "k", "a", "n", "a", "D", "l", "e", "K" } },
            { "คนเข้าเมือง", new[] { "k", "o", "n", "k", "au", "m", "Ua", "ng" } },
            { "คอสเพลย์", new[] { "k", "O", "D", "p", "l", "e" } },
            { "กำแหง", new[] { "kk", "a", "m", "h", "A", "ng" } },
            { "การเลี้ยงชีพ", new[] { "kk", "a", "n", "l", "ia", "ng", "ch", "i", "B" } },
            { "ค่าเลี้ยงชีพ", new[] { "k", "a", "l", "ia", "ng", "ch", "i", "B" } },
            { "หาเลี้ยงชีพ", new[] { "h", "a", "l", "ia", "ng", "ch", "i", "B" } },
            { "เลี้ยงชีพ", new[] { "l", "ia", "ng", "ch", "i", "B" } },
            { "กราย", new[] { "kk", "r", "a", "Y" } },
            { "กวีกระวาด", new[] { "kk", "r", "a", "w", "i", "kk", "r", "a", "w", "a", "D" } },
            { "กุฏิ", new[] { "kk", "u", "D", "tt", "i" } },
            { "ก้นกุฏิ", new[] { "kk", "o", "n", "kk", "u", "D", "tt", "i" } },
            { "ชาติภูมิ", new[] { "ch", "a", "D", "tt", "i", "p", "u", "m" } },
            { "ปิตุภูมิ", new[] { "pp", "i", "tt", "u", "p", "u", "m" } },
            { "มาตุภูมิ", new[] { "m", "a", "tt", "u", "p", "u", "m" } },
            { "สมรภูมิ", new[] { "s", "a", "m", "O", "r", "a", "p", "u", "m" } },
            { "ภูมิใจ", new[] { "p", "u", "m", "j", "I" } },
            { "ภาคภูมิใจ", new[] { "p", "a", "K", "p", "u", "m", "j", "I" } },
            { "ความภาคภูมิใจ", new[] { "k", "w", "a", "m", "p", "a", "K", "p", "u", "m", "j", "I" } },
            { "ประวัติการณ์", new[] { "pp", "r", "a", "w", "a", "D", "tt", "i", "kk", "a", "n" } },
            { "ประวัติกาล", new[] { "pp", "r", "a", "w", "a", "D", "tt", "i", "kk", "a", "n" } },
            { "อุบัติเหตุ", new[] { "u", "b", "a", "D", "tt", "i", "h", "e", "D" } },
            { "เกียรติประวัติ", new[] { "kk", "ia", "D", "tt", "i", "pp", "r", "a", "w", "a", "D" } },
            { "รสชาติ", new[] { "r", "o", "D", "ch", "a", "D" } },
            { "ธรรมชาติ", new[] { "t", "a", "m", "m", "a", "ch", "a", "D" } },
            { "พยาธิ", new[] { "p", "a", "y", "a", "D" } },
            { "พยาธิวิทยา", new[] { "p", "a", "y", "a", "t", "i", "w", "i", "D", "t", "a", "y", "a" } },
            { "สมาธิ", new[] { "s", "a", "m", "a", "t", "i" } },
            { "ขัดสมาธิ", new[] { "k", "a", "D", "s", "a", "m", "a", "D" } },
            { "นั่งขัดสมาธิ", new[] { "n", "a", "ng", "k", "a", "D", "s", "a", "m", "a", "D" } },
            { "ปรารถนา", new[] { "pp", "r", "a", "D", "t", "a", "n", "a" } },
            { "สามารถ", new[] { "s", "a", "m", "a", "D" } },
            { "จักจั่น", new[] { "j", "a", "K", "j", "a", "n" } },
            { "จักจี้", new[] { "j", "a", "K", "j", "i" } },
            { "จั๊กจี้", new[] { "j", "a", "K", "j", "i" } },
            { "ความจั๊กจี้", new[] { "k", "w", "a", "m", "j", "a", "K", "j", "i" } },
            { "สรรเสริญ", new[] { "s", "a", "n", "s", "E", "n" } },
            { "ทรุดโทรม", new[] { "s", "u", "D", "s", "o", "m" } },
            { "พุทรา", new[] { "p", "u", "D", "s", "a" } },
            { "ฉะเชิงเทรา", new[] { "ch", "a", "ch", "E", "ng", "s", "au" } },
            { "เทิดทูน", new[] { "t", "E", "D", "t", "u", "n" } },
            { "มุกดาหาร", new[] { "m", "u", "K", "d", "a", "h", "a", "n" } },
            { "ตากอากาศ", new[] { "tt", "a", "K", "a", "kk", "a", "D" } },
            { "ออกอากาศ", new[] { "O", "K", "a", "kk", "a", "D" } },
            { "ฮัก", new[] { "h", "a", "K" } },
            { "อ้าย", new[] { "a", "Y" } },
            { "บ่", new[] { "b", "O" } },
            { "แม่น", new[] { "m", "A", "n" } },
            { "อีหยัง", new[] { "i", "y", "a", "ng" } },
            { "จั่งใด๋", new[] { "j", "a", "ng", "d", "I" } },
            { "คึดฮอด", new[] { "k", "U", "D", "h", "O", "D" } },
            { "ซำบายดี", new[] { "s", "a", "m", "b", "a", "Y", "d", "i" } },
            { "ม่วนซื่น", new[] { "m", "ua", "n", "s", "U", "n" } },
            { "ละอ่อน", new[] { "l", "a", "O", "n" } },
            { "กึ๊ดเติง", new[] { "kk", "U", "D", "tt", "E", "ng" } },
            { "ข้าเจ้า", new[] { "k", "a", "j", "au" } },
            { "อู้กำเมือง", new[] { "u", "kk", "a", "m", "m", "Ua", "ng" } },
            { "หรอยแรง", new[] { "r", "O", "Y", "r", "A", "ng" } },
            { "แหลงใต้", new[] { "l", "A", "ng", "tt", "a", "Y" } },
            { "พันพรือ", new[] { "p", "a", "n", "p", "r", "U" } }
        };

        public static bool IsAbbreviationWithDot(string? lyric)
        {
            if (string.IsNullOrWhiteSpace(lyric))
            {
                return false;
            }
            string trimmed = lyric.Trim();
            return trimmed.EndsWith(".", StringComparison.Ordinal) && trimmed.Any(c => c >= '\u0E01' && c <= '\u0E2E');
        }

        public static string[] ConvertAbbreviationToStrictOrVowel(string lyric, bool useVccv = true)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(lyric))
            {
                return result.ToArray();
            }

            string vowelSymbol = useVccv ? "Q" : "O";

            foreach (char c in lyric)
            {
                if (ThaiConsonantToDiffSingerOnset.TryGetValue(c, out string? onset))
                {
                    if (!string.IsNullOrEmpty(onset))
                    {
                        if (useVccv && AdvancedPhonemeMap.TryGetValue(onset, out string? vccvOnset))
                        {
                            result.Add(vccvOnset);
                        }
                        else
                        {
                            result.Add(onset);
                        }
                    }
                    result.Add(vowelSymbol);
                }
            }

            return result.ToArray();
        }

        public static string[] ConvertDiffSingerToArpasing(string? word, string[] symbols)
        {
            if (!string.IsNullOrWhiteSpace(word) && IsAbbreviationWithDot(word))
            {
                var abbrResult = new List<string>();
                foreach (char c in word)
                {
                    if (ThaiConsonantToDiffSingerOnset.TryGetValue(c, out string? onset))
                    {
                        if (!string.IsNullOrEmpty(onset) && ArpasingPhonemeMap.TryGetValue(onset, out string? arpaOnset))
                        {
                            abbrResult.Add(arpaOnset);
                        }
                        abbrResult.Add("AO");
                    }
                }
                return abbrResult.ToArray();
            }

            string[] workingSymbols = symbols;
            
            // Normalize: ตรวจสอบพจนานุกรมด้วยการปรับเป็นตัวพิมพ์เล็กเสมอ
            string lookupWord = word?.Trim().ToLowerInvariant() ?? "";

            if (!string.IsNullOrWhiteSpace(lookupWord) && RoyalInstituteWordOverrides.TryGetValue(lookupWord, out string[]? overrideSymbols))
            {
                workingSymbols = overrideSymbols;
            }

            var arpasingTokens = new List<string>(workingSymbols.Length);
            foreach (string token in workingSymbols)
            {
                if (ArpasingPhonemeMap.TryGetValue(token, out string? arpaVal))
                {
                    arpasingTokens.Add(arpaVal);
                }
                else
                {
                    // Fallback: บังคับคายค่าเป็น Upper Case เพื่อรองรับ phones.txt ของ CMU Arpasing
                    arpasingTokens.Add(token.ToUpperInvariant());
                }
            }
            return arpasingTokens.ToArray();
        }

        protected override string[] PostProcessWordSymbols(Note[][] phrase, int wordIndex, string[] symbols)
        {
            string? currentWord = null;
            if (phrase != null && wordIndex >= 0 && wordIndex < phrase.Length && phrase[wordIndex] != null && phrase[wordIndex].Length > 0)
            {
                currentWord = phrase[wordIndex][0].lyric?.Trim();
            }

            if (!string.IsNullOrEmpty(currentWord) && IsAbbreviationWithDot(currentWord))
            {
                return ConvertAbbreviationToStrictOrVowel(currentWord, useVccv: true);
            }

            if (!string.IsNullOrEmpty(currentWord))
            {
                string lookup = currentWord.ToLowerInvariant();
                if (RoyalInstituteWordOverrides.TryGetValue(lookup, out string[]? correctedSymbols))
                {
                    symbols = correctedSymbols;
                }
            }

            if (symbols == null || symbols.Length == 0)
            {
                return symbols ?? Array.Empty<string>();
            }

            var optimizedTokens = new List<string>(symbols.Length);
            int totalTokens = symbols.Length;

            for (int i = 0; i < totalTokens; i++)
            {
                string token = symbols[i];
                bool isLastPosition = (i == totalTokens - 1);

                bool isExplicitCoda = token.Length == 1 && char.IsUpper(token[0]) && StrictEndingConsonants.ContainsKey(token);
                bool prevIsVowel = (i > 0) && DiffSingerVowels.Contains(symbols[i - 1]);
                bool nextIsVowel = (i + 1 < totalTokens) && DiffSingerVowels.Contains(symbols[i + 1]);
                bool isSyllableEndingConsonant = isLastPosition || isExplicitCoda || (prevIsVowel && !DiffSingerVowels.Contains(token) && !nextIsVowel);

                if (isSyllableEndingConsonant && StrictEndingConsonants.TryGetValue(token, out string? endingVal))
                {
                    optimizedTokens.Add(endingVal);
                }
                else if (AdvancedPhonemeMap.TryGetValue(token, out string? mappedTokenVal))
                {
                    optimizedTokens.Add(mappedTokenVal);
                }
                else
                {
                    optimizedTokens.Add(token);
                }
            }

            return optimizedTokens.ToArray();
        }
    }
}