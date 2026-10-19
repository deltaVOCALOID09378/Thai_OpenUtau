using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.App;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;

namespace OpenUtau.App.ViewModels {
    public class PlaybackPitchFollowSettingsChangedEvent { }
    public class PitchFollowPathPreviewChangedEvent { }

    public readonly struct PitchFollowPathSamplePoint {
        public readonly double Tick;
        public readonly double TrackOffset;

        public PitchFollowPathSamplePoint(double tick, double trackOffset) {
            Tick = tick;
            TrackOffset = trackOffset;
        }
    }

    /// <summary>
    /// Pre-computes a smooth vertical scroll path (TrackOffset over time) for piano-roll playback.
    /// Transitions are scheduled ahead of note entries so the view does not react only when the playhead arrives.
    /// </summary>
    internal sealed class PlaybackPitchFollowPath {
        sealed class Segment {
            public double StartTick;
            public double EndTick;
            public double FromOffset;
            public double ToOffset;

            public bool IsHold => Math.Abs(FromOffset - ToOffset) < 1e-6;

            public double Evaluate(double tick) {
                if (IsHold || EndTick <= StartTick) {
                    return FromOffset;
                }
                double t = Math.Clamp((tick - StartTick) / (EndTick - StartTick), 0, 1);
                t = PitchFollowScrollMath.ApplyTransitionEase(t);
                return FromOffset + (ToOffset - FromOffset) * t;
            }
        }

        readonly List<Segment> segments = new List<Segment>();
        double fallbackOffset;
        double pathEndTick;

        public bool IsBuilt => segments.Count > 0;
        public double PathEndTick => pathEndTick;

        public void Build(
            UVoicePart? part,
            double viewportTracks,
            double vScrollMax,
            int semitoneThreshold,
            double verticalPosition,
            int resolution) {
            segments.Clear();
            fallbackOffset = 0;
            pathEndTick = 0;
            if (part == null || part.notes.Count == 0 || viewportTracks <= 0) {
                return;
            }

            var zones = BuildPitchZones(part.notes, semitoneThreshold);
            if (zones.Count == 0) {
                return;
            }

            double baseTransitionTicks = Math.Max(resolution * 0.25, PitchFollowScrollMath.DefaultTransitionBeats * resolution);
            double leadTimeRatio = PitchFollowScrollMath.DefaultLeadTimeRatio;
            verticalPosition = Math.Clamp(verticalPosition, 0.15, 0.85);

            var zoneTargets = zones
                .Select(z => ToneToTrackOffset(z.CenterTone, viewportTracks, vScrollMax, verticalPosition))
                .ToArray();

            fallbackOffset = zoneTargets[0];
            double partEndTick = Math.Max(part.Duration, zones[^1].EndTick);

            // Hold before the first zone.
            AddHold(0, zones[0].StartTick, zoneTargets[0]);

            for (int i = 0; i < zones.Count - 1; i++) {
                var sourceZone = zones[i];
                var targetZone = zones[i + 1];
                double holdEnd = sourceZone.EndTick;
                double nextStart = targetZone.StartTick;
                double gap = nextStart - holdEnd;
                double from = zoneTargets[i];
                double to = zoneTargets[i + 1];

                if (Math.Abs(from - to) < 1e-6) {
                    AddHold(holdEnd, nextStart, from);
                    continue;
                }

                double transitionDuration = ComputeTransitionDuration(
                    sourceZone, targetZone, gap, baseTransitionTicks, leadTimeRatio, resolution);
                double transitionEnd = nextStart;
                double transitionStart = transitionEnd - transitionDuration;

                if (gap > 0) {
                    // Prefer finishing the move as the next zone begins; never start before the previous zone ends.
                    transitionStart = Math.Max(holdEnd, nextStart - transitionDuration);
                    transitionEnd = Math.Min(nextStart, transitionStart + transitionDuration);
                    if (transitionEnd <= transitionStart) {
                        transitionEnd = transitionStart + Math.Max(resolution * 0.125, 1);
                    }
                } else {
                    // Overlapping / legato: begin late in the source zone so the move stays quick.
                    double bleed = Math.Min(sourceZone.Duration * 0.35, transitionDuration * 1.5);
                    transitionStart = Math.Max(sourceZone.StartTick, holdEnd - bleed);
                    transitionEnd = Math.Min(nextStart + Math.Max(-gap, 0), transitionStart + transitionDuration);
                    if (transitionEnd <= transitionStart) {
                        transitionEnd = transitionStart + Math.Max(resolution * 0.125, 1);
                    }
                }

                if (transitionStart > holdEnd) {
                    AddHold(holdEnd, transitionStart, from);
                }
                AddTransition(transitionStart, transitionEnd, from, to);
                if (transitionEnd < nextStart) {
                    AddHold(transitionEnd, nextStart, to);
                }
            }

            AddHold(zones[^1].EndTick, partEndTick + resolution * 4, zoneTargets[^1]);
            CoalesceSegments();
            pathEndTick = segments.Count > 0 ? segments[^1].EndTick : 0;
        }

        public void SamplePoints(double stepTicks, List<PitchFollowPathSamplePoint> output) {
            output.Clear();
            if (!IsBuilt || stepTicks <= 0) {
                return;
            }
            double start = segments[0].StartTick;
            for (double tick = start; tick < pathEndTick; tick += stepTicks) {
                output.Add(new PitchFollowPathSamplePoint(tick, Evaluate(tick)));
            }
            output.Add(new PitchFollowPathSamplePoint(pathEndTick, Evaluate(pathEndTick)));
        }

        /// <summary>
        /// Builds the runtime/preview path: ideal curve + symmetric two-pass frame smoothing.
        /// </summary>
        public void SampleSmoothedPoints(
            double frameSmoothing,
            int resolution,
            List<PitchFollowPathSamplePoint> output) {
            output.Clear();
            if (!IsBuilt) {
                return;
            }

            double stepTicks = Math.Max(1, resolution / 64.0);
            int count = Math.Max(2, (int)Math.Ceiling(pathEndTick / stepTicks) + 1);
            var ticks = new double[count];
            var ideal = new double[count];
            for (int i = 0; i < count; i++) {
                double tick = Math.Min(i * stepTicks, pathEndTick);
                ticks[i] = tick;
                ideal[i] = Evaluate(tick);
            }
            ticks[^1] = pathEndTick;
            ideal[^1] = Evaluate(pathEndTick);

            double alpha = PitchFollowScrollMath.FrameSmoothingToAlpha(frameSmoothing);
            var smoothed = new double[count];
            PitchFollowScrollMath.TwoPassSmooth(ideal, smoothed, alpha);

            for (int i = 0; i < count; i++) {
                output.Add(new PitchFollowPathSamplePoint(ticks[i], smoothed[i]));
            }
        }

        public double EvaluateSmoothedAtTick(
            IReadOnlyList<PitchFollowPathSamplePoint> smoothedSamples,
            double localTick) {
            if (smoothedSamples.Count == 0) {
                return Evaluate(localTick);
            }
            if (localTick <= smoothedSamples[0].Tick) {
                return smoothedSamples[0].TrackOffset;
            }
            for (int i = 1; i < smoothedSamples.Count; i++) {
                var prev = smoothedSamples[i - 1];
                var cur = smoothedSamples[i];
                if (localTick <= cur.Tick) {
                    double span = cur.Tick - prev.Tick;
                    if (span <= 0) {
                        return cur.TrackOffset;
                    }
                    double t = Math.Clamp((localTick - prev.Tick) / span, 0, 1);
                    t = t * t * (3 - 2 * t);
                    return prev.TrackOffset + (cur.TrackOffset - prev.TrackOffset) * t;
                }
            }
            return smoothedSamples[^1].TrackOffset;
        }

        /// <summary>
        /// Transition length scales with the destination zone duration (long notes → slower pan)
        /// and compresses when the gap before the next zone is short (fast jumps between staccato notes).
        /// </summary>
        static double ComputeTransitionDuration(
            PitchZone source,
            PitchZone target,
            double gap,
            double baseTransitionTicks,
            double leadTimeRatio,
            int resolution) {
            double minTicks = resolution * 0.125;
            double maxTicks = baseTransitionTicks * 3;

            double targetTicks = target.Duration;
            double targetBeats = targetTicks / resolution;
            double baseBeats = baseTransitionTicks / resolution;
            double gapTicks = Math.Max(0, gap);
            double gapBeats = gapTicks / resolution;

            // Longer destination zones get a longer, gentler camera move.
            double lengthMultiplier = Math.Clamp(
                Math.Sqrt(targetBeats / Math.Max(baseBeats, 0.25)),
                0.3,
                3.5);
            double idealTicks = baseTransitionTicks * lengthMultiplier;

            // Short gaps or overlaps force a faster move.
            double urgency;
            if (gapTicks <= 0) {
                urgency = 0.22;
            } else if (gapBeats <= 0.25) {
                urgency = 0.32;
            } else if (gapBeats <= baseBeats) {
                urgency = 0.35 + 0.65 * (gapBeats / baseBeats);
            } else {
                urgency = 1.0;
            }

            // Quick succession of short notes at different pitches: boost urgency further.
            if (targetBeats <= baseBeats * 0.5 && gapBeats <= baseBeats * 0.5) {
                urgency *= 0.55;
            }

            double duration = idealTicks * urgency;

            if (gapTicks > 0) {
                double maxFromGap = gapTicks * Math.Clamp(leadTimeRatio, 0.25, 1.0);
                if (targetBeats >= baseBeats * 1.5 && gapBeats >= baseBeats * 0.75) {
                    // Long note with a comfortable gap: allow a slow pan across most of the rest.
                    maxFromGap = Math.Max(maxFromGap, gapTicks * 0.9);
                }
                duration = Math.Min(duration, maxFromGap);
            }

            if (targetBeats >= baseBeats * 2 && gapTicks > 0) {
                double minSlow = Math.Min(idealTicks * 0.65, gapTicks * 0.55);
                duration = Math.Max(duration, minSlow);
            }

            if (gapTicks <= 0) {
                duration = Math.Min(duration, Math.Max(minTicks, baseTransitionTicks * 0.35));
            }

            return Math.Clamp(duration, minTicks, gapTicks > 0 ? Math.Max(maxTicks, gapTicks) : maxTicks);
        }

        public double Evaluate(double localTick) {
            if (segments.Count == 0) {
                return fallbackOffset;
            }
            if (localTick <= segments[0].StartTick) {
                return segments[0].FromOffset;
            }
            for (int i = 0; i < segments.Count; i++) {
                var seg = segments[i];
                if (localTick < seg.EndTick || i == segments.Count - 1) {
                    return seg.Evaluate(localTick);
                }
            }
            return segments[^1].ToOffset;
        }

        static List<PitchZone> BuildPitchZones(IEnumerable<UNote> notes, int semitoneThreshold) {
            var sorted = notes
                .OrderBy(n => n.position)
                .ToList();
            var zones = new List<PitchZone>();
            if (sorted.Count == 0) {
                return zones;
            }

            int threshold = Math.Max(0, semitoneThreshold);
            int zoneStart = sorted[0].position;
            int zoneEnd = sorted[0].End;
            int toneSum = sorted[0].tone;
            int toneCount = 1;
            int anchorTone = sorted[0].tone;

            for (int i = 1; i < sorted.Count; i++) {
                var note = sorted[i];
                if (threshold == 0 || Math.Abs(note.tone - anchorTone) >= threshold) {
                    zones.Add(new PitchZone(zoneStart, zoneEnd, toneSum / (double)toneCount));
                    zoneStart = note.position;
                    zoneEnd = note.End;
                    toneSum = note.tone;
                    toneCount = 1;
                    anchorTone = note.tone;
                } else {
                    zoneEnd = Math.Max(zoneEnd, note.End);
                    toneSum += note.tone;
                    toneCount++;
                }
            }
            zones.Add(new PitchZone(zoneStart, zoneEnd, toneSum / (double)toneCount));
            return zones;
        }

        public static double ToneToTrackOffset(
            double tone,
            double viewportTracks,
            double vScrollMax,
            double verticalPosition) {
            // +0.5 aligns the follow center with note rows (legacy +2 sat ~1.5 semitones low).
            double offset = ViewConstants.MaxTone - tone + 0.5 - viewportTracks * verticalPosition;
            return Math.Clamp(offset, 0, vScrollMax);
        }

        void AddHold(double startTick, double endTick, double offset) {
            if (endTick <= startTick) {
                return;
            }
            if (segments.Count > 0) {
                var last = segments[^1];
                if (last.IsHold && Math.Abs(last.FromOffset - offset) < 1e-6) {
                    last.EndTick = endTick;
                    return;
                }
            }
            segments.Add(new Segment {
                StartTick = startTick,
                EndTick = endTick,
                FromOffset = offset,
                ToOffset = offset,
            });
        }

        void AddTransition(double startTick, double endTick, double from, double to) {
            if (endTick <= startTick) {
                return;
            }
            if (segments.Count > 0) {
                var last = segments[^1];
                if (last.IsHold && Math.Abs(last.FromOffset - from) < 1e-6 && Math.Abs(last.EndTick - startTick) < 1e-6) {
                    last.EndTick = startTick;
                    if (last.EndTick <= last.StartTick) {
                        segments.RemoveAt(segments.Count - 1);
                    }
                }
            }
            segments.Add(new Segment {
                StartTick = startTick,
                EndTick = endTick,
                FromOffset = from,
                ToOffset = to,
            });
        }

        void CoalesceSegments() {
            for (int i = segments.Count - 1; i > 0; i--) {
                var prev = segments[i - 1];
                var cur = segments[i];
                if (prev.IsHold && cur.IsHold && Math.Abs(prev.FromOffset - cur.FromOffset) < 1e-6) {
                    prev.EndTick = cur.EndTick;
                    segments.RemoveAt(i);
                }
            }
        }

        readonly struct PitchZone {
            public readonly double StartTick;
            public readonly double EndTick;
            public readonly double CenterTone;
            public double Duration => EndTick - StartTick;

            public PitchZone(double startTick, double endTick, double centerTone) {
                StartTick = startTick;
                EndTick = endTick;
                CenterTone = centerTone;
            }
        }
    }
}
