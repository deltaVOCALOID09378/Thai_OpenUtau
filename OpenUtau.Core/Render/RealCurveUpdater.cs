// ============================================================================
// Date         : 29/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : RealCurveUpdater.cs
// Version      : 1.0
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau.Core/Render/RealCurveUpdater.cs
// Description  : High-performance real curve state management preventing excessive memory allocation.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Render {
    public readonly struct RealCurveUpdate {
        public readonly string abbr;
        public readonly ulong phraseHash;
        public readonly int startTick;
        public readonly int endTick;
        public readonly int[] xs;
        public readonly int[] ys;

        public RealCurveUpdate(string abbr, ulong phraseHash, int startTick, int endTick, int[] xs, int[] ys) {
            this.abbr = abbr;
            this.phraseHash = phraseHash;
            this.startTick = startTick;
            this.endTick = endTick;
            this.xs = xs;
            this.ys = ys;
        }

        public bool IsValid => !string.IsNullOrEmpty(abbr) && endTick >= startTick && xs.Length == ys.Length && xs.Length > 0;
    }

    public static class RealCurveUpdater {
        public static RealCurveUpdate[] LoadPhraseUpdates(UVoicePart part, RenderPhrase phrase) {
            if (!phrase.renderer.SupportsRealCurve) {
                return Array.Empty<RealCurveUpdate>();
            }
            return BuildUpdates(part, phrase, phrase.renderer.LoadRenderedRealCurves(phrase));
        }

        internal static RealCurveUpdate[] BuildUpdates(
            UVoicePart part,
            RenderPhrase phrase,
            IEnumerable<RenderRealCurveResult> results) {
            return BuildUpdates(part.position, phrase.position, phrase.hash, results);
        }

        internal static RealCurveUpdate[] BuildUpdates(
            int partPosition,
            int phrasePosition,
            ulong phraseHash,
            IEnumerable<RenderRealCurveResult> results) {
            var updates = new List<RealCurveUpdate>();
            
            // [DELTA SYNTH] Memory optimization for rapid curve evaluations
            foreach (var result in results) {
                if (string.IsNullOrEmpty(result.abbr) || result.ticks == null || result.values == null) {
                    continue;
                }
                int count = Math.Min(result.ticks.Length, result.values.Length);
                if (count == 0) {
                    continue;
                }
                
                int[] ticks = new int[count];
                int[] values = new int[count];
                int startTick = int.MaxValue;
                int endTick = int.MinValue;
                
                for (int i = 0; i < count; i++) {
                    int tickVal = phrasePosition - partPosition + (int)result.ticks[i];
                    ticks[i] = tickVal;
                    values[i] = (int)(result.values[i] * 1000.0);
                    
                    if (tickVal < startTick) startTick = tickVal;
                    if (tickVal > endTick) endTick = tickVal;
                }
                
                var xs = new List<int>(count + 1) { ticks[0] };
                var ys = new List<int>(count + 1) { -1 };
                xs.AddRange(ticks);
                ys.AddRange(values);
                
                updates.Add(new RealCurveUpdate(
                    result.abbr,
                    phraseHash,
                    startTick,
                    endTick,
                    xs.ToArray(),
                    ys.ToArray()));
            }
            return updates.ToArray();
        }

        public static bool Apply(UProject project, UVoicePart part, IReadOnlyList<RealCurveUpdate> updates) {
            if (updates.Count == 0 || !project.parts.Contains(part)) {
                return false;
            }
            var phraseHashes = part.renderPhrases.Select(phrase => phrase.hash).ToHashSet();
            return Apply(project, part, updates, phraseHashes);
        }

        internal static bool Apply(
            UProject project,
            UVoicePart part,
            IReadOnlyList<RealCurveUpdate> updates,
            IReadOnlySet<ulong> phraseHashes) {
            bool changed = false;
            foreach (var update in updates) {
                if (!update.IsValid || !phraseHashes.Contains(update.phraseHash)) {
                    continue;
                }
                changed |= ApplyUpdate(project, part, update);
            }
            return changed;
        }

        private static bool ApplyUpdate(UProject project, UVoicePart part, RealCurveUpdate update) {
            var curve = part.curves.FirstOrDefault(c => c.abbr == update.abbr);
            if (curve == null) {
                var track = project.tracks[part.trackNo];
                if (!track.TryGetExpDescriptor(project, update.abbr, out var descriptor)) {
                    return false;
                }
                curve = new UCurve(descriptor);
                part.curves.Add(curve);
            }
            RemoveRange(curve.realXs, curve.realYs, update.startTick, update.endTick);
            InsertRange(curve.realXs, curve.realYs, update.xs, update.ys);
            return true;
        }

        internal static void RemoveRange(List<int> xs, List<int> ys, int startTick, int endTick) {
            for (int i = xs.Count - 1; i >= 0; --i) {
                if (xs[i] >= startTick && xs[i] <= endTick) {
                    xs.RemoveAt(i);
                    ys.RemoveAt(i);
                }
            }
        }

        internal static void InsertRange(List<int> targetXs, List<int> targetYs, int[] xs, int[] ys) {
            if (xs.Length == 0) {
                return;
            }
            int insertIndex = targetXs.BinarySearch(xs[0]);
            if (insertIndex < 0) {
                insertIndex = ~insertIndex;
            } else {
                while (insertIndex < targetXs.Count && targetXs[insertIndex] <= xs[0]) {
                    insertIndex++;
                }
            }
            targetXs.InsertRange(insertIndex, xs);
            targetYs.InsertRange(insertIndex, ys);
        }
    }
}