// ============================================================================
// Date         : 29/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : DefaultPhonemizer.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau.Core/DefaultPhonemizer.cs
// Description  : The simplest Phonemizer possible. Optimized with C# 12 collection expressions.
// ============================================================================
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using System.Linq;

namespace OpenUtau.Core {
    [Phonemizer("Default UTAU Phonemizer", "Default UTAU", language: "UTAU")]
    public class DefaultPhonemizer : Phonemizer {
        private USinger singer;
        public override void SetSinger(USinger singer) => this.singer = singer;
        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
            string alias = notes[0].lyric;
            var attr0 = notes[0].phonemeAttributes?.FirstOrDefault(attr => attr.index == 0) ?? default;
            if (singer.TryGetMappedOto(notes[0].lyric, notes[0].tone + attr0.toneShift, attr0.voiceColor, out var oto)) {
                alias = oto.Alias;
            }
            return new Result {
                phonemes = [new Phoneme { phoneme = alias }]
            };
        }
    }
}