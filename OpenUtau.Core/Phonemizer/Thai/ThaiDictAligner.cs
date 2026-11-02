/*
 * ============================================================================
 * Date         : 29/10/2026 (B.E. 2569 / A.D. 2026)
 * File Name    : ThaiDictAligner.cs
 * Version      : 1.2.0
 * Location     : OpenUtau.Core/Phonemizers/Thai/
 * Description  : Thai dictionary alignment, syllable validation, and lookup engine.
 *                Optimized with Lazy Loading (File.ReadLines) to reduce RAM usage.
 * Credit       : Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
 * ============================================================================
 */

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenUtau.Core.Phonemizers.Thai
{
    public class ThaiDictAligner
    {
        private readonly Dictionary<string, string> _dictTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public int LoadedEntriesCount => _dictTable.Count;

        public bool LoadDictionary(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            _dictTable.Clear();

            // [DELTA SYNTH] Optimized: Using File.ReadLines (Lazy Evaluation) 
            // instead of File.ReadAllLines to prevent massive RAM allocation for huge dictionaries.
            foreach (string rawLine in File.ReadLines(filePath, Encoding.UTF8))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                int separatorIndex = line.IndexOf('\t');
                if (separatorIndex == -1)
                {
                    separatorIndex = line.IndexOf(' ');
                }

                if (separatorIndex > 0)
                {
                    string word = line.Substring(0, separatorIndex).Trim();
                    string phonemes = line.Substring(separatorIndex + 1).Trim();

                    if (!string.IsNullOrEmpty(word) && !string.IsNullOrEmpty(phonemes))
                    {
                        _dictTable[word] = phonemes;
                    }
                }
            }

            return _dictTable.Count > 0;
        }

        public string ResolvePhonemes(string lyric)
        {
            if (string.IsNullOrWhiteSpace(lyric))
            {
                return "- a";
            }

            string cleanLyric = lyric.Trim();
            if (_dictTable.TryGetValue(cleanLyric, out string? matched))
            {
                return matched;
            }

            return cleanLyric;
        }
    }
}
#nullable restore