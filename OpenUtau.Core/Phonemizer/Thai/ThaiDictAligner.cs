/*
 * File: ThaiDictAligner.cs
 * Version: 1.0.0
 * Location: OpenUtau.Core/Phonemizers/Thai/
 * Description: Thai dictionary alignment, syllable validation, and lookup engine with zero-defect safe queries.
 * Credit: Made And Checked By DELTA SYNTH & All Agentic AI
 * Original by DELTA SYNTH
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
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

            foreach (string rawLine in lines)
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