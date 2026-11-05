// ============================================================================
// 1. วัน/เดือน/ปี: 1 พฤศจิกายน พ.ศ. 2569 / 01 November 2026
// 2. ชื่อไฟล์โค้ด: IRenderer.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.1
// 4. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Core/Render/IRenderer.cs
// 5. หน้าที่: อินเทอร์เฟซหลักสำหรับระบบเรนเดอร์เสียงและคลาส Exception ของ Resampler/Wavtool
// 6. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Agentic AI | Original by OpenUtau Authors
// ============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Render {
    public class NoResamplerException : Exception { }
    public class NoWavtoolException : Exception { }
    public class ResamplerFailedException : Exception {
        public ResamplerFailedException() { }
        public ResamplerFailedException(string message) : base(message) { }
        public ResamplerFailedException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class RenderResult {
        public float[] samples;
        public double leadingMs;
        public double positionMs;
        public double estimatedLengthMs;
    }

    public class RenderPitchResult {
        public float[] ticks;
        public float[] tones;
        public bool[]? retakeMask;
    }

    public class RenderRealCurveResult {
        public string abbr;
        public float[] ticks;
        public float[] values;
    }

    public class RenderPhraseEvents {
        readonly Action<IReadOnlyList<RenderRealCurveResult>>? realCurvesCallback;

        public RenderPhraseEvents(Action<IReadOnlyList<RenderRealCurveResult>>? realCurvesCallback = null) {
            this.realCurvesCallback = realCurvesCallback;
        }

        public void ReportRealCurves(IReadOnlyList<RenderRealCurveResult> realCurves) {
            if (realCurves.Count > 0) {
                realCurvesCallback?.Invoke(realCurves);
            }
        }
    }

    public interface IRenderer {
        USingerType SingerType { get; }
        bool SupportsRenderPitch { get; }
        bool SupportsRealCurve => false;

        bool SupportsExpression(UExpressionDescriptor descriptor);
        RenderResult Layout(RenderPhrase phrase);
        Task<RenderResult> Render(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender = false, RenderPhraseEvents? renderEvents = null);
        RenderPitchResult LoadRenderedPitch(RenderPhrase phrase);
        RenderPitchResult LoadRenderedPitch(RenderPhrase phrase, HashSet<int> selectedNotePositions) => LoadRenderedPitch(phrase);
        List<RenderRealCurveResult> LoadRenderedRealCurves(RenderPhrase phrase) => new List<RenderRealCurveResult>(0);
        void ScheduleRealCurveRefresh(UProject project, UVoicePart part, UCommand command) { }
        UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings renderSettings);
    }
}
