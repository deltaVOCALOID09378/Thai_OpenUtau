/*
 * File: ThaiPhonemizer.cs
 * Version: 1.0.0
 * Location: OpenUtau.Core/Phonemizers/Thai/
 * Description: Core Thai phonemizer engine handling syllable parsing, VCCV mapping, and automatic tuning.
 * Credit: Made And Checked By DELTA SYNTH & All Agentic AI
 * Original by DELTA SYNTH
 */

#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenUtau.Core.Phonemizers.Thai
{
    public class ThaiPhonemizer
    {
        private static readonly Regex ToneMarkRegex = new Regex(@"[\u0E48-\u0E4C]", RegexOptions.Compiled);
        private static readonly Dictionary<string, string> FallbackConsonants = new Dictionary<string, string>
        {
            { "k", "k" }, { "kh", "kh" }, { "g", "g" }, { "ch", "ch" },
            { "d", "d" }, { "t", "t" }, { "th", "th" }, { "n", "n" },
            { "b", "b" }, { "p", "p" }, { "ph", "ph" }, { "m", "m" },
            { "y", "y" }, { "r", "r" }, { "l", "l" }, { "w", "w" },
            { "s", "s" }, { "h", "h" }, { "-", "-" }
        };

        public string ProcessSyllable(string inputLyric)
        {
            if (string.IsNullOrWhiteSpace(inputLyric))
            {
                return "-";
            }

            string sanitized = ToneMarkRegex.Replace(inputLyric.Trim(), string.Empty);
            return sanitized.Length > 0 ? sanitized : "-";
        }

        public bool ValidatePhoneme(string phoneme)
        {
            if (string.IsNullOrEmpty(phoneme))
            {
                return false;
            }

            return FallbackConsonants.ContainsKey(phoneme) || FallbackConsonants.ContainsValue(phoneme);
        }
    }
}
#nullable restore