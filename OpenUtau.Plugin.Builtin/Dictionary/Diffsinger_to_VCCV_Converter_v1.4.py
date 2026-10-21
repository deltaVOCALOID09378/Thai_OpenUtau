using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.DiffSinger
{
    [Phonemizer("Thai DiffSinger Advanced Phonemizer", "TH DiffSinger Adv", language: "TH", author: "DELTA SYNTH & Gemini AI")]
    public class ThaiDiffSingerPhonemizerExtended : DiffSingerBasePhonemizer
    {
        protected override string GetDictionaryName() => "dsdict-th.txt";
        protected override string GetLangCode() => "th";

        private static readonly Dictionary<string, string> AdvancedPhonemeMap = new()
        {
            { "ng", "g" },    // ง
            { "kk", "k" },    // ก (เสียงกัก)
            { "k", "kh" },    // ค
            { "pp", "p" },    // ป (เสียงกัก)
            { "p", "ph" },    // พ
            { "tt", "t" },    // ต (เสียงกัก)
            { "t", "th" },    // ท
            { "ch", "ch" },   // ช
            { "j", "j" },     // จ
            { "f", "f" },     // ฟ
            { "h", "h" },     // ฮ
            { "s", "s" },     // ซ
            { "r", "r" },     // ร
            { "l", "l" },     // ล
            { "w", "w" },     // ว
            { "y", "y" },     // ย
            { "A", "@" },     // แอ
            { "O", "Q" },     // ออ
            { "E", "3" },     // เออ
            { "Ua", "6" },    // เอือ
            { "U", "1" },     // อือ
            { "au", "aw" },   // สระประสม
            { "ia", "ia" },   // สระเอีย
            { "oa", "oa" }    // สระอัว
        };

        private static readonly Dictionary<string, string> StrictEndingConsonants = new()
        {
            { "ng", "g" }, { "m", "m" }, { "n", "n" }, { "w", "w" }, { "y", "y" },
            { "k", "k" }, { "K", "k" }, { "p", "b" }, { "B", "b" }, { "t", "d" }, { "D", "d" }
        };

        protected override string[] PostProcessWordSymbols(Note[][] phrase, int wordIndex, string[] symbols)
        {
            if (symbols == null || symbols.Length == 0)
            {
                return symbols;
            }

            var optimizedTokens = new List<string>();
            int totalTokens = symbols.Length;

            for (int i = 0; i < totalTokens; i++)
            {
                string token = symbols[i];
                bool isLastPosition = (i == totalTokens - 1);

                if (isLastPosition)
                {
                    // ตำแหน่งตัวสะกด: บังคับใช้เสียงสะกดมาตรฐานภาษาไทยเพื่อความชัดเจน
                    if (StrictEndingConsonants.TryGetValue(token, out string? endingVal))
                    {
                        optimizedTokens.Add(endingVal);
                    }
                    else if (AdvancedPhonemeMap.TryGetValue(token, out string? mappedEnding))
                    {
                        optimizedTokens.Add(mappedEnding);
                    }
                    else
                    {
                        optimizedTokens.Add(token);
                    }
                }
                else
                {
                    // พยัญชนะต้นและสระ: แปลงตามพจนานุกรมขั้นสูง พร้อมรองรับคำควบกล้ำและสระลดรูป
                    if (AdvancedPhonemeMap.TryGetValue(token, out string? mappedTokenVal))
                    {
                        optimizedTokens.Add(mappedTokenVal);
                    }
                    else
                    {
                        optimizedTokens.Add(token);
                    }
                }
            }

            return optimizedTokens.ToArray();
        }
    }
}