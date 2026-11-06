// ============================================================================
// Date         : 06/11/2026 (B.E. 2569 / A.D. 2026)
// File Name    : SingerPhonemeFilteringTest.cs
// Version      : 1.1
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
// File Location: OpenUtau.Test/Plugins/SingerPhonemeFilteringTest.cs
// Description  : ทดสอบการกรองหน่วยเสียงและการเลือกตัวแปลงหน่วยเสียงตามที่คลังเสียงรองรับจริง (SupportsPhoneme/SupportsLanguage/EnumerateForSinger)
// ============================================================================
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using OpenUtau.Plugin.Builtin;
using Xunit;

namespace OpenUtau.Plugins {
    public class SingerPhonemeFilteringTest {
        private ClassicSinger CreateThaiSinger() {
            var vb = new Voicebank {
                BasePath = "null",
                File = "null",
                Name = "Thai Voicebank Test",
            };
            var set = new OtoSet { File = "null", Name = "main" };
            set.Otos.Add(new Oto { Alias = "k a", Wav = "ka.wav", Phonetic = "k a", IsValid = true });
            set.Otos.Add(new Oto { Alias = "s a", Wav = "sa.wav", Phonetic = "s a", IsValid = true });
            set.Otos.Add(new Oto { Alias = "tho", Wav = "tho.wav", Phonetic = "tho", IsValid = true });
            set.Otos.Add(new Oto { Alias = "od", Wav = "od.wav", Phonetic = "od", IsValid = true });
            set.Otos.Add(new Oto { Alias = "ก", Wav = "kor.wav", Phonetic = "ก", IsValid = true });
            vb.OtoSets.Add(set);
            var singer = new ClassicSinger(vb);
            return singer;
        }

        private ClassicSinger CreateJapaneseSinger() {
            var vb = new Voicebank {
                BasePath = "null",
                File = "null",
                Name = "Japanese CV Test",
            };
            var set = new OtoSet { File = "null", Name = "main" };
            set.Otos.Add(new Oto { Alias = "あ", Wav = "a.wav", Phonetic = "あ", IsValid = true });
            set.Otos.Add(new Oto { Alias = "い", Wav = "i.wav", Phonetic = "い", IsValid = true });
            set.Otos.Add(new Oto { Alias = "ka", Wav = "ka.wav", Phonetic = "ka", IsValid = true });
            set.Otos.Add(new Oto { Alias = "sa", Wav = "sa.wav", Phonetic = "sa", IsValid = true });
            vb.OtoSets.Add(set);
            var singer = new ClassicSinger(vb);
            return singer;
        }

        [Fact]
        public void SupportsPhoneme_ThaiSinger_AccurateMatching() {
            var singer = CreateThaiSinger();
            Assert.True(singer.SupportsPhoneme("k a"));
            Assert.True(singer.SupportsPhoneme("s a"));
            Assert.True(singer.SupportsPhoneme("[k a]"));
            Assert.True(singer.SupportsPhoneme("ก"));

            // Unsupported phonemes should return false
            Assert.False(singer.SupportsPhoneme("unknown_phoneme_xyz"));
            Assert.False(singer.SupportsPhoneme(""));
            Assert.False(singer.SupportsPhoneme("   "));
        }

        [Fact]
        public void SupportsLanguage_ThaiSinger_DetectsThai() {
            var singer = CreateThaiSinger();
            Assert.True(singer.SupportsLanguage("TH"));
            Assert.True(singer.SupportsLanguage("UTAU"));
            // Japanese CV specific phonemes are not in this Thai voicebank
            Assert.False(singer.SupportsLanguage("JA"));
        }

        [Fact]
        public void SupportsLanguage_JapaneseSinger_DetectsJapanese() {
            var singer = CreateJapaneseSinger();
            Assert.True(singer.SupportsLanguage("JA"));
            Assert.True(singer.SupportsLanguage("UTAU"));
            // Thai specific phonemes are not in this Japanese voicebank
            Assert.False(singer.SupportsLanguage("TH"));
        }

        [Fact]
        public void EnumerateForSinger_FiltersUnsupportedLanguages() {
            // Register test factories into PhonemizerFactory
            PhonemizerFactory.Get(typeof(OpenUtau.Core.DefaultPhonemizer));
            PhonemizerFactory.Get(typeof(ThaiVCCVPhonemizer));
            PhonemizerFactory.Get(typeof(RussianCVCPhonemizer));
            PhonemizerFactory.Get(typeof(SpanishVCCVPhonemizer));
            PhonemizerFactory.BuildList();

            var thaiSinger = CreateThaiSinger();
            var factories = PhonemizerFactory.EnumerateForSinger(thaiSinger).ToList();

            // Default UTAU phonemizer is always present
            Assert.Contains(factories, f => f.language == "UTAU");

            // Thai VCCV phonemizer is supported
            Assert.Contains(factories, f => f.type == typeof(ThaiVCCVPhonemizer));

            // Unsupported language phonemizers (e.g. Russian CVC, Spanish VCCV) should NOT be in the supported list
            Assert.DoesNotContain(factories, f => f.type == typeof(RussianCVCPhonemizer));
            Assert.DoesNotContain(factories, f => f.type == typeof(SpanishVCCVPhonemizer));
        }
    }
}
