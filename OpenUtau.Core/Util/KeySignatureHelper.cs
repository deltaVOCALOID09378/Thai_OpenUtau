using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core {
    public readonly struct MusicalKey : IEquatable<MusicalKey> {
        public int Tonic { get; }
        public bool IsMajor { get; }

        public MusicalKey(int tonic, bool isMajor) {
            Tonic = ((tonic % 12) + 12) % 12;
            IsMajor = isMajor;
        }

        public static MusicalKey FromProject(UProject project) =>
            new MusicalKey(project.key, project.keyIsMajor);

        public bool Equals(MusicalKey other) => Tonic == other.Tonic && IsMajor == other.IsMajor;
        public override bool Equals(object? obj) => obj is MusicalKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Tonic, IsMajor);

        public static bool operator ==(MusicalKey left, MusicalKey right) => left.Equals(right);
        public static bool operator !=(MusicalKey left, MusicalKey right) => !left.Equals(right);
    }

    public enum HarmonyInterval {
        ThirdAbove,
        ThirdBelow,
        FifthAbove,
        FifthBelow,
    }

    public static class KeySignatureHelper {
        static readonly int[] MajorScale = { 0, 2, 4, 5, 7, 9, 11 };
        static readonly int[] MinorScale = { 0, 2, 3, 5, 7, 8, 10 };

        static readonly double[] MajorProfile = {
            6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88,
        };
        static readonly double[] MinorProfile = {
            6.33, 2.68, 3.52, 5.38, 2.60, 3.53, 2.54, 4.75, 3.98, 2.69, 3.34, 3.17,
        };

        public static IReadOnlyList<MusicalKey> AllKeys() {
            var keys = new List<MusicalKey>(24);
            for (int tonic = 0; tonic < 12; tonic++) {
                keys.Add(new MusicalKey(tonic, true));
                keys.Add(new MusicalKey(tonic, false));
            }
            return keys;
        }

        public static string FormatKey(int tonic, bool isMajor, bool shortName = false) {
            tonic = ((tonic % 12) + 12) % 12;
            string root = MusicMath.KeysInOctave[tonic].Item1;
            if (shortName) {
                return isMajor ? root : $"{root}m";
            }
            return isMajor ? $"{root} Major" : $"{root} Minor";
        }

        public static string FormatKey(MusicalKey key, bool shortName = false) =>
            FormatKey(key.Tonic, key.IsMajor, shortName);

        public static string FormatProjectKey(UProject project, bool shortName = false) =>
            FormatKey(project.key, project.keyIsMajor, shortName);

        public static int Encode(MusicalKey key) => key.Tonic + (key.IsMajor ? 0 : 12);

        public static MusicalKey Decode(int encoded) {
            encoded = ((encoded % 24) + 24) % 24;
            return new MusicalKey(encoded % 12, encoded < 12);
        }

        public static bool IsInScale(int tone, MusicalKey key) {
            if (tone < 0) {
                return false;
            }
            int pitchClass = ((tone % 12) - key.Tonic + 12) % 12;
            var scale = key.IsMajor ? MajorScale : MinorScale;
            return scale.Contains(pitchClass);
        }

        public static int TransposeHarmony(int tone, MusicalKey key, HarmonyInterval interval) {
            int steps = interval switch {
                HarmonyInterval.ThirdAbove => 2,
                HarmonyInterval.ThirdBelow => -2,
                HarmonyInterval.FifthAbove => 4,
                HarmonyInterval.FifthBelow => -4,
                _ => 0,
            };
            return TransposeByScaleSteps(tone, key, steps);
        }

        public static int TransposeByScaleSteps(int tone, MusicalKey key, int scaleSteps) {
            if (tone < 0 || scaleSteps == 0) {
                return tone;
            }
            if (!IsInScale(tone, key)) {
                return tone;
            }
            int step = scaleSteps > 0 ? 1 : -1;
            int remaining = Math.Abs(scaleSteps);
            int result = tone;
            while (remaining > 0) {
                int next = result;
                do {
                    next += step;
                    if (next < 0) {
                        return result;
                    }
                } while (!IsInScale(next, key));
                result = next;
                remaining--;
            }
            return result;
        }

        public static (int degree, int octave) GetScaleDegreeAndOctave(int tone, MusicalKey key) {
            int pitchClass = ((tone % 12) - key.Tonic + 12) % 12;
            var scale = key.IsMajor ? MajorScale : MinorScale;
            int degree = Array.IndexOf(scale, pitchClass);
            if (degree < 0) {
                degree = 0;
            }
            int octave = tone / 12;
            return (degree, octave);
        }

        public static IReadOnlyList<MusicalKey> DetectKeys(IEnumerable<UNote> notes, int topCount = 3) {
            var histogram = BuildPitchClassHistogram(notes);
            if (histogram.Sum() <= 0) {
                return new[] { new MusicalKey(0, true) };
            }
            var scored = new List<(MusicalKey key, double score)>(24);
            for (int tonic = 0; tonic < 12; tonic++) {
                scored.Add((new MusicalKey(tonic, true), Correlate(histogram, MajorProfile, tonic)));
                scored.Add((new MusicalKey(tonic, false), Correlate(histogram, MinorProfile, tonic)));
            }
            return scored
                .OrderByDescending(pair => pair.score)
                .Select(pair => pair.key)
                .Distinct()
                .Take(Math.Max(1, topCount))
                .ToList();
        }

        public static IReadOnlyList<MusicalKey> DetectKeysFromProject(UProject project, int topCount = 3) {
            var notes = project.parts
                .OfType<UVoicePart>()
                .SelectMany(part => part.notes);
            return DetectKeys(notes, topCount);
        }

        static double[] BuildPitchClassHistogram(IEnumerable<UNote> notes) {
            var histogram = new double[12];
            foreach (var note in notes) {
                if (note.tone < 0) {
                    continue;
                }
                double weight = Math.Max(1, note.duration);
                histogram[note.tone % 12] += weight;
            }
            return histogram;
        }

        static double Correlate(double[] histogram, double[] profile, int tonic) {
            double sumHist = 0;
            double sumProf = 0;
            double sumHistSq = 0;
            double sumProfSq = 0;
            double sumProduct = 0;
            for (int i = 0; i < 12; i++) {
                double h = histogram[i];
                double p = profile[(i - tonic + 12) % 12];
                sumHist += h;
                sumProf += p;
                sumHistSq += h * h;
                sumProfSq += p * p;
                sumProduct += h * p;
            }
            double denom = Math.Sqrt(sumHistSq * sumProfSq);
            if (denom <= 0) {
                return 0;
            }
            return sumProduct / denom;
        }

        public static string HarmonyIntervalLabel(HarmonyInterval interval) => interval switch {
            HarmonyInterval.ThirdAbove => "harmony.interval.thirdabove",
            HarmonyInterval.ThirdBelow => "harmony.interval.thirdbelow",
            HarmonyInterval.FifthAbove => "harmony.interval.fifthabove",
            HarmonyInterval.FifthBelow => "harmony.interval.fifthbelow",
            _ => string.Empty,
        };
    }
}
