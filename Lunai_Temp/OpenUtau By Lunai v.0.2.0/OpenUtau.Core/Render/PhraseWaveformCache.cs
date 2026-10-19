using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OpenUtau.Core.Render {
    /// <summary>Per-phrase rendered audio used for incremental piano-roll waveform display.</summary>
    public static class PhraseWaveformCache {
        public const double FadeDurationMs = 220;

        public readonly struct Entry {
            public readonly int TrackNo;
            public readonly double PosMs;
            public readonly float[] Samples;
            public readonly DateTime RenderTime;
            public readonly DateTime? FadeOutSince;

            internal Entry(CacheEntry entry) {
                TrackNo = entry.TrackNo;
                PosMs = entry.PosMs;
                Samples = entry.Samples;
                RenderTime = entry.RenderTime;
                FadeOutSince = entry.FadeOutSince;
            }
        }

        internal sealed class CacheEntry {
            public int TrackNo;
            public double PosMs;
            public float[] Samples = Array.Empty<float>();
            public DateTime RenderTime;
            public DateTime? FadeOutSince;
        }

        static readonly ConcurrentDictionary<string, CacheEntry> entries = new ConcurrentDictionary<string, CacheEntry>();

        public static event Action? Changed;

        public static void Clear() {
            entries.Clear();
            Changed?.Invoke();
        }

        public static bool TryGet(ulong phraseHash, out Entry entry) {
            if (entries.TryGetValue(phraseHash.ToString(), out var cached)) {
                entry = new Entry(cached);
                return true;
            }
            entry = default;
            return false;
        }

        /// <summary>Drop cached phrases on this track that are no longer in the current layout.</summary>
        public static void RemoveStaleForTrack(int trackNo, IEnumerable<ulong> keepHashes) {
            var keep = keepHashes.Select(hash => hash.ToString()).ToHashSet();
            bool anyChanged = false;
            foreach (var pair in entries) {
                if (pair.Value.TrackNo != trackNo || keep.Contains(pair.Key)) {
                    continue;
                }
                if (!pair.Value.FadeOutSince.HasValue) {
                    pair.Value.FadeOutSince = DateTime.Now;
                    anyChanged = true;
                }
            }
            if (PurgeCompletedFadeOuts()) {
                anyChanged = true;
            }
            if (anyChanged) {
                Changed?.Invoke();
            }
        }

        public static void Put(int trackNo, ulong phraseHash, double posMs, float[] samples) {
            var key = phraseHash.ToString();
            var isNew = !entries.ContainsKey(key);
            entries[key] = new CacheEntry {
                TrackNo = trackNo,
                PosMs = posMs,
                Samples = samples,
                RenderTime = isNew ? DateTime.Now : entries[key].RenderTime,
                FadeOutSince = null,
            };
            Changed?.Invoke();
        }

        public static IEnumerable<Entry> GetForTrack(int trackNo) {
            PurgeCompletedFadeOuts();
            return entries.Values
                .Where(entry => entry.TrackNo == trackNo)
                .Select(entry => new Entry(entry));
        }

        public static float GetVisualScale(in Entry entry, ref bool needsAnotherFrame) {
            if (entry.FadeOutSince.HasValue) {
                double fadeOutAge = (DateTime.Now - entry.FadeOutSince.Value).TotalMilliseconds;
                double fadeOutProgress = Math.Clamp(fadeOutAge / FadeDurationMs, 0.0, 1.0);
                if (fadeOutProgress < 1.0) {
                    needsAnotherFrame = true;
                }
                return 1.0f - EaseOutCubic((float)fadeOutProgress);
            }
            double fadeInAge = (DateTime.Now - entry.RenderTime).TotalMilliseconds;
            double fadeInProgress = Math.Clamp(fadeInAge / FadeDurationMs, 0.0, 1.0);
            if (fadeInProgress < 1.0) {
                needsAnotherFrame = true;
            }
            return EaseOutCubic((float)fadeInProgress);
        }

        static bool PurgeCompletedFadeOuts() {
            var removeKeys = entries
                .Where(pair => pair.Value.FadeOutSince.HasValue
                    && (DateTime.Now - pair.Value.FadeOutSince.Value).TotalMilliseconds >= FadeDurationMs)
                .Select(pair => pair.Key)
                .ToArray();
            if (removeKeys.Length == 0) {
                return false;
            }
            foreach (var key in removeKeys) {
                entries.TryRemove(key, out _);
            }
            return true;
        }

        static float EaseOutCubic(float t) {
            return 1.0f - (float)Math.Pow(1.0 - t, 3);
        }
    }
}
