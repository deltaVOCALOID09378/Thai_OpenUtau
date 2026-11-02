// ============================================================================
// Date         : 29/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : MachineLearningPhonemizer.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau.Core/MachineLearningPhonemizer.cs
// Description  : ML Phonemizer base class. Refactored logic to reduce allocations.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core {
    public abstract class MachineLearningPhonemizer : Phonemizer {
        protected readonly Dictionary<int, List<Tuple<string, int>>> partResult = new();
        protected readonly Dictionary<int, string> unrecognizedLyrics = new();
        private readonly Dictionary<int, Exception> partExceptions = new();

        public override void SetUp(Note[][] groups, UProject project, UTrack track) {
            SetUpException = null;
            partResult.Clear();
            unrecognizedLyrics.Clear();
            partExceptions.Clear();
            if (groups.Length == 0) return;

            var lyrics = groups.Select(group => group[0].lyric).ToArray();
            var romanizedLyrics = Romanize(lyrics).ToArray();
            if (romanizedLyrics.Length != groups.Length) {
                Log.Error($"Romanize() returned {romanizedLyrics.Length} results for {groups.Length} lyrics; falling back to original (non-romanized) lyrics for this pass.");
                romanizedLyrics = lyrics;
            }
            for (int i = 0; i < groups.Length; ++i) {
                groups[i] = ChangeLyric(groups[i], romanizedLyrics[i]);
            }
            
            var phrase = new List<Note[]> { groups[0] };
            for (int i = 1; i < groups.Length; ++i) {
                if (groups[i - 1][^1].position + groups[i - 1][^1].duration == groups[i][0].position) {
                    phrase.Add(groups[i]);
                } else {
                    ProcessPhrase(phrase);
                    phrase.Clear();
                    phrase.Add(groups[i]);
                }
            }
            if (phrase.Count > 0) {
                ProcessPhrase(phrase);
            }
        }

        private void ProcessPhrase(List<Note[]> phrase) {
            try {
                ProcessPart(phrase.ToArray());
            } catch (Exception e) {
                foreach (var group in phrase) {
                    partExceptions[group[0].position] = e;
                }
                var sentenceLyrics = string.Join(" ", phrase.Select(group => group[0].lyric));
                Log.Error($"Failed to phonemize sentence {sentenceLyrics}: {e.Message}");
            }
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
            if (unrecognizedLyrics.TryGetValue(notes[0].position, out var lyric)) {
                if (string.IsNullOrEmpty(lyric)) throw new Exception("Phoneme not found for this note");
                throw new Exception($"Unrecognized phoneme \"{lyric}\"");
            }
            if (!partResult.TryGetValue(notes[0].position, out var phonemes)) {
                Exception? cause = partExceptions.TryGetValue(notes[0].position, out var partEx) ? partEx : SetUpException;
                if (cause != null) throw new Exception("Phonemizer failed to process.", cause);
                throw new Exception("Part result not found");
            }
            return new Result {
                phonemes = phonemes.Select(tu => new Phoneme { phoneme = tu.Item1, position = tu.Item2 }).ToArray(),
            };
        }

        public override void CleanUp() {
            partResult.Clear();
            unrecognizedLyrics.Clear();
            partExceptions.Clear();
        }

        protected abstract void ProcessPart(Note[][] phrase);
        protected virtual string[] Romanize(IEnumerable<string> lyrics) => lyrics.ToArray();

        protected static Note[] ChangeLyric(Note[] group, string lyric) {
            var oldNote = group[0];
            group[0] = new Note {
                lyric = lyric,
                phoneticHint = oldNote.phoneticHint,
                tone = oldNote.tone,
                position = oldNote.position,
                duration = oldNote.duration,
                phonemeAttributes = oldNote.phonemeAttributes,
            };
            return group;
        }
    }
}