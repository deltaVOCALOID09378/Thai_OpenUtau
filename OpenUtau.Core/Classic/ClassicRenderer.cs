// ============================================================================
// 1. วัน/เดือน/ปี: 31 ตุลาคม พ.ศ. 2569 / 31 October 2026
// 2. ชื่อไฟล์โค้ด: ClassicRenderer.cs
// 3. ลำดับเวอร์ชันของไฟล์: 3.1
// 4. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Core/Classic/ClassicRenderer.cs
// 5. หน้าที่: เรนเดอร์เสียงแบบ Classic UTAU พร้อมระบบ Atomic Cache Locking และ Silence Fallback
// 6. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Agentic AI | Original by OpenUtau Contributors
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using OpenUtau.Core;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Classic {
    public class ClassicRenderer : IRenderer {
        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Ustx.DYN,
            Ustx.PITD,
            Ustx.CLR,
            Ustx.CLRY,
            Ustx.XSY,
            Ustx.ENG,
            Ustx.VEL,
            Ustx.VOL,
            Ustx.ATK,
            Ustx.DEC,
            Ustx.MOD,
            Ustx.MODP,
            Ustx.ALT,
            Ustx.DIR,
            Ustx.SHFT,
            Ustx.GENC,
            Ustx.TENC,
            Ustx.BREC,
            Ustx.VOIC
        };

        public USingerType SingerType => USingerType.Classic;

        public bool SupportsRenderPitch => false;

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return descriptor.isFlag
                || !string.IsNullOrEmpty(descriptor.flag)
                || supportedExp.Contains(descriptor.abbr);
        }

        public RenderResult Layout(RenderPhrase phrase) {
            return new RenderResult() {
                leadingMs = phrase.leadingMs,
                positionMs = phrase.positionMs,
                estimatedLengthMs = phrase.durationMs + phrase.leadingMs,
            };
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender, RenderPhraseEvents? renderEvents = null) {
            if (phrase.wavtool == SharpWavtool.nameConvergence || phrase.wavtool == SharpWavtool.nameSimple) {
                return RenderInternal(phrase, progress, trackNo, cancellation, isPreRender);
            } else {
                return RenderExternal(phrase, progress, trackNo, cancellation, isPreRender);
            }
        }

        public Task<RenderResult> RenderInternal(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender) {
            var resamplerItems = new List<ResamplerItem>();
            foreach (var phone in phrase.phones) {
                resamplerItems.Add(new ResamplerItem(phrase, phone));
            }
            var task = Task.Run(() => {
                Parallel.ForEach(source: resamplerItems, parallelOptions: new ParallelOptions() {
                    MaxDegreeOfParallelism = Math.Max(1, Preferences.Default.NumRenderThreads)
                }, body: item => {
                    if (!cancellation.IsCancellationRequested) {
                        lock (Renderers.GetCacheLock(item.outputFile)) {
                            bool needsResample = !File.Exists(item.outputFile) || new FileInfo(item.outputFile).Length < 44;
                            if (needsResample) {
                                if (!(item.resampler is WorldlineResampler)) {
                                    VoicebankFiles.Inst.CopySourceTemp(item.inputFile, item.inputTemp);
                                }
                                if (!item.phone.direct) {
                                    try {
                                        item.resampler.DoResamplerReturnsFile(item, Log.Logger);
                                    } catch (Exception ex) {
                                        Log.Error(ex, $"{item.resampler} threw an exception while resampling \"{item.phone.phoneme}\"");
                                    }
                                    if (!File.Exists(item.outputFile) || new FileInfo(item.outputFile).Length < 44) {
                                        DocManager.Inst.Project.timeAxis.TickPosToBarBeat(item.phrase.position + item.phone.position, out int bar, out int beat, out int tick);
                                        Log.Warning($"{item.resampler} produced missing or invalid audio for \"{item.phone.phoneme}\" at {bar}:{beat}.{string.Format("{0:000}", tick)}. Generating silence fallback WAV.");
                                        try {
                                            Wave.WriteMono16Wav(item.outputFile, new float[4410]);
                                        } catch (Exception ex) {
                                            Log.Error(ex, $"Failed to write silence fallback WAV to {item.outputFile}");
                                        }
                                    }
                                }
                                if (!(item.resampler is WorldlineResampler)) {
                                    VoicebankFiles.Inst.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                                }
                            }
                        }
                    }
                    progress.Complete(1, $"Track {trackNo + 1}: {item.resampler} \"{item.phone.phoneme}\"");
                });
                var result = Layout(phrase);
                var wavtool = new SharpWavtool(phrase.wavtool == SharpWavtool.nameConvergence);
                result.samples = wavtool.Concatenate(resamplerItems, string.Empty, cancellation);
                if (result.samples != null) {
                    Renderers.ApplyDynamics(phrase, result);
                }
                return result;
            });
            return task;
        }

        public Task<RenderResult> RenderExternal(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender) {
            var resamplerItems = new List<ResamplerItem>();
            foreach (var phone in phrase.phones) {
                resamplerItems.Add(new ResamplerItem(phrase, phone));
            }
            var task = Task.Run(() => {
                string progressInfo = $"Track {trackNo + 1} : {phrase.wavtool} \"{string.Join(" ", phrase.phones.Select(p => p.phoneme))}\"";
                progress.Complete(0, progressInfo);
                var wavPath = Path.Join(PathManager.Inst.CachePath, $"cat-{phrase.hash:x16}.wav");
                phrase.AddCacheFile(wavPath);
                var result = Layout(phrase);
                lock (Renderers.GetCacheLock(wavPath)) {
                    if (File.Exists(wavPath) && new FileInfo(wavPath).Length >= 44) {
                        try {
                            using (var waveStream = Wave.OpenFile(wavPath)) {
                                result.samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                            }
                        } catch (Exception e) {
                            Log.Error(e, $"Failed to render: failed to open {wavPath}");
                        }
                    }
                    if (result.samples == null) {
                        foreach (var item in resamplerItems) {
                            VoicebankFiles.Inst.CopySourceTemp(item.inputFile, item.inputTemp);
                        }
                        var wavtool = ToolsManager.Inst.GetWavtool(phrase.wavtool);
                        result.samples = wavtool.Concatenate(resamplerItems, wavPath, cancellation);
                        foreach (var item in resamplerItems) {
                            VoicebankFiles.Inst.CopyBackMetaFiles(item.inputFile, item.inputTemp);
                        }
                    }
                }
                progress.Complete(phrase.phones.Length, progressInfo);
                if (result.samples != null) {
                    Renderers.ApplyDynamics(phrase, result);
                }
                return result;
            });
            return task;
        }

        public RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) {
            return null;
        }

        public UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings renderSettings) {
            var manifest = renderSettings.Resampler.Manifest;
            if (manifest == null) {
                return Array.Empty<UExpressionDescriptor>();
            }
            return manifest.expressions.Values.ToArray();
        }

        public override string ToString() => Renderers.CLASSIC;
    }
}