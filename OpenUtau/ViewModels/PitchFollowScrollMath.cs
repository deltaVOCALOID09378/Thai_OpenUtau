using System;

namespace OpenUtau.App.ViewModels {
    /// <summary>Shared pitch-follow scroll math for path transitions and frame smoothing.</summary>
    static class PitchFollowScrollMath {
        public const double ReferenceStepMs = 15.0;
        public const double SmoothScrollTimeConstantMs = 80.0;
        public const int DefaultSemitoneThreshold = 2;
        public const double DefaultTransitionBeats = 1.0;
        public const double DefaultLeadTimeRatio = 1.0;
        public const double DefaultVerticalPosition = 0.5;
        public const double DefaultFrameSmoothing = 0.1;

        public static double ApplyTransitionEase(double t) {
            t = Math.Clamp(t, 0, 1);
            // Cosine ease-in-out: zero slope at t=0 and t=1, peak velocity at t=0.5.
            return 0.5 - 0.5 * Math.Cos(Math.PI * t);
        }

        /// <summary>Maps the frame-smoothing preference to a two-pass filter strength.</summary>
        public static double FrameSmoothingToAlpha(double frameSmoothing) {
            return Math.Clamp(frameSmoothing * 0.85, 0.02, 0.92);
        }

        /// <summary>Symmetric forward/backward exponential smoothing (zero phase lag).</summary>
        public static void TwoPassSmooth(ReadOnlySpan<double> input, Span<double> output, double alpha) {
            if (input.Length == 0) {
                return;
            }
            if (input.Length == 1) {
                output[0] = input[0];
                return;
            }

            var forward = new double[input.Length];
            forward[0] = input[0];
            for (int i = 1; i < input.Length; i++) {
                forward[i] = forward[i - 1] + alpha * (input[i] - forward[i - 1]);
            }

            output[^1] = forward[^1];
            for (int i = input.Length - 2; i >= 0; i--) {
                output[i] = output[i + 1] + alpha * (forward[i] - output[i + 1]);
            }
        }
    }
}
