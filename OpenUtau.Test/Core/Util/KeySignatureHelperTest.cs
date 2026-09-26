using System.Collections.Generic;
using OpenUtau.Core;
using Xunit;

namespace OpenUtau.Test.Core.Util {
    public class KeySignatureHelperTest {
        static readonly MusicalKey CMajor = new MusicalKey(0, true);

        [Theory]
        [InlineData(59, HarmonyInterval.ThirdAbove, 62)]
        [InlineData(60, HarmonyInterval.ThirdAbove, 64)]
        [InlineData(59, HarmonyInterval.FifthAbove, 65)]
        [InlineData(60, HarmonyInterval.FifthBelow, 53)]
        [InlineData(71, HarmonyInterval.FifthAbove, 77)]
        [InlineData(72, HarmonyInterval.FifthBelow, 65)]
        [InlineData(23, HarmonyInterval.ThirdAbove, 26)]
        [InlineData(24, HarmonyInterval.ThirdAbove, 28)]
        [InlineData(35, HarmonyInterval.ThirdAbove, 38)]
        [InlineData(36, HarmonyInterval.ThirdBelow, 33)]
        public void TransposeHarmony_KnownCases(int tone, HarmonyInterval interval, int expected) {
            int actual = KeySignatureHelper.TransposeHarmony(tone, CMajor, interval);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TransposeHarmony_MatchesBruteForceNearC() {
            var failures = new List<string>();
            foreach (var key in KeySignatureHelper.AllKeys()) {
                for (int tone = 12; tone < 12 * 8; tone++) {
                    int pc = tone % 12;
                    if (pc != 11 && pc != 0 && pc != 1) {
                        continue;
                    }
                    if (!KeySignatureHelper.IsInScale(tone, key)) {
                        continue;
                    }
                    foreach (var interval in new[] {
                        HarmonyInterval.ThirdAbove,
                        HarmonyInterval.ThirdBelow,
                        HarmonyInterval.FifthAbove,
                        HarmonyInterval.FifthBelow,
                    }) {
                        int expected = BruteForceTranspose(tone, key, interval);
                        int actual = KeySignatureHelper.TransposeHarmony(tone, key, interval);
                        if (actual != expected) {
                            failures.Add($"{KeySignatureHelper.FormatKey(key)} {MusicMath.GetToneName(tone)} {interval}: expected {MusicMath.GetToneName(expected)}({expected}) got {MusicMath.GetToneName(actual)}({actual})");
                        }
                    }
                }
            }
            Assert.Empty(failures);
        }

        static int BruteForceTranspose(int tone, MusicalKey key, HarmonyInterval interval) {
            int steps = interval switch {
                HarmonyInterval.ThirdAbove => 2,
                HarmonyInterval.ThirdBelow => -2,
                HarmonyInterval.FifthAbove => 4,
                HarmonyInterval.FifthBelow => -4,
                _ => 0,
            };
            var scaleTones = new List<int>();
            for (int t = 0; t < 12 * 12; t++) {
                if (KeySignatureHelper.IsInScale(t, key)) {
                    scaleTones.Add(t);
                }
            }
            int index = scaleTones.IndexOf(tone);
            if (index < 0) {
                return tone;
            }
            int target = index + steps;
            if (target < 0 || target >= scaleTones.Count) {
                return tone;
            }
            return scaleTones[target];
        }
    }
}
