// ============================================================================
// 1. วัน/เดือน/ปี: 10 พฤศจิกายน พ.ศ. 2569 / 10 November 2026
// 2. ชื่อไฟล์โค้ด: MachineLearningPhonemizer.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.3
// 4. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// 5. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Core/MachineLearningPhonemizer.cs
// 6. รายละเอียดการแก้ไข: เพิ่ม Fallback Phoneme ที่ปลอดภัย ไม่ throw unhandled exception เมื่อประมวลผลคำไม่สำเร็จ
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
            if (partResult.TryGetValue(notes[0].position, out var phonemes) && phonemes != null && phonemes.Count > 0) {
                return new Result {
                    phonemes = phonemes.Select(tu => new Phoneme { phoneme = tu.Item1, position = tu.Item2 }).ToArray(),
                };
            }

            // DELTA SYNTH: Resilient fallback phoneme to avoid unhandled exceptions during playback/rendering
            string fallbackPhoneme = notes[0].lyric;
            if (unrecognizedLyrics.TryGetValue(notes[0].position, out var lyric) && !string.IsNullOrEmpty(lyric)) {
                fallbackPhoneme = lyric;
            }
            if (string.IsNullOrWhiteSpace(fallbackPhoneme)) {
                fallbackPhoneme = "SP";
            }
            Exception? cause = partExceptions.TryGetValue(notes[0].position, out var partEx) ? partEx : SetUpException;

            return new Result {
                phonemes = new[] {
                    new Phoneme {
                        phoneme = fallbackPhoneme,
                        position = 0,
                        error = cause
                    }
                }
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