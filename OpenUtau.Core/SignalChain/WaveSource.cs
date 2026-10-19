using System;

namespace OpenUtau.Core.SignalChain {
    public class WaveSource : ISignalSource {
        public readonly double offsetMs;
        public readonly double estimatedLengthMs;
        public readonly int offset;
        public readonly int estimatedLength;
        public readonly int channels;

        public double EndMs => offsetMs + estimatedLengthMs;
        public bool HasSamples => data != null;

        private readonly object lockObj = new object();
        private float[] data;

        public WaveSource(double offsetMs, double estimatedLengthMs, double skipOverMs, int channels) {
            this.offsetMs = offsetMs;
            this.estimatedLengthMs = estimatedLengthMs;
            this.channels = channels;
            offset = (int)((offsetMs - skipOverMs) * 44100 / 1000) * channels;
            estimatedLength = (int)(estimatedLengthMs * 44100 / 1000) * channels;
        }

        public void SetSamples(float[] samples) {
            lock (lockObj) {
                // DELTA SYNTH Optimization: Universal Sub-millisecond Head/Tail Micro-Fade 
                // Applies to all rendering engines (DiffSinger, UTAU, ENUNU, etc.) to eliminate micro-clicks at buffer boundaries.
                // 44 samples = approx 0.99 millisecond at 44.1kHz.
                int microFadeSamples = 44 * channels;
                for (int i = 0; i < samples.Length; ++i) {
                    if (i < microFadeSamples) {
                        samples[i] *= (float)i / microFadeSamples;
                    } else if (i > samples.Length - 1 - microFadeSamples) {
                        samples[i] *= (float)(samples.Length - 1 - i) / microFadeSamples;
                    }
                }
                data = samples;
            }
        }

        public float[]? GetSamples() {
            lock (lockObj) {
                return data;
            }
        }

        public bool IsReady(int position, int count) {
            int copies = 2 / channels;
            return position + count <= offset * copies
                || offset * copies + estimatedLength * copies <= position
                || data != null;
        }

        public int Mix(int position, float[] buffer, int index, int count) {
            int copies = 2 / channels;
            if (data == null) {
                if (position + count <= offset * copies) {
                    return position + count;
                }
                return position;
            }
            int start = Math.Max(position, offset * copies);
            int end = Math.Min(position + count, offset * copies + data.Length * copies);
            for (int i = start; i < end; ++i) {
                buffer[index + i - position] += data[i / copies - offset];
            }
            return end;
        }
    }
}
