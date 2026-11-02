// ============================================================================
// Date         : 29/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : Renderers.cs
// Version      : 1.0
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau.Core/Render/Renderers.cs
// Description  : Renderer management definitions and factory integration.
// ============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;

namespace OpenUtau.Core.Render {
    public static class Renderers {
        public const string CLASSIC = "CLASSIC";
        public const string WORLDLINE_R = "WORLDLINE-R";
        public const string WORLDLINE_R2 = "WORLDLINE-R2";
        public const string ENUNU = "ENUNU";
        public const string VOGEN = "VOGEN";
        public const string DIFFSINGER = "DIFFSINGER";
        public const string VOICEVOX = "VOICEVOX";

        static readonly string[] classicRenderers = { WORLDLINE_R, CLASSIC };
        static readonly string[] enunuRenderers = { ENUNU };
        static readonly string[] vogenRenderers = { VOGEN };
        static readonly string[] diffSingerRenderers = { DIFFSINGER };
        static readonly string[] voicevoxRenderers = { VOICEVOX };
        static readonly string[] noRenderers = Array.Empty<string>();

        // [DELTA SYNTH] Optimized using expression body format & switch statement
        public static string[] GetSupportedRenderers(USingerType singerType) => singerType switch {
            USingerType.Classic => classicRenderers,
            USingerType.Enunu => enunuRenderers,
            USingerType.Vogen => vogenRenderers,
            USingerType.DiffSinger => diffSingerRenderers,
            USingerType.Voicevox => voicevoxRenderers,
            _ => noRenderers,
        };

        public static List<string> getRendererOptions() => new List<string> {
            "WORLDLINE-R",
            "Classic"
        };

        public static string GetDefaultRenderer(USingerType singerType) {
            if (Preferences.Default.DefaultRenderer == "Classic" && singerType == USingerType.Classic) {
                return CLASSIC;
            }
            return GetSupportedRenderers(singerType)[0];
        }

        public static IRenderer CreateRenderer(string renderer) {
            if (renderer == CLASSIC) return new ClassicRenderer();
            if (renderer == WORLDLINE_R2) return new WorldlineRenderer(version: 2);
            if (renderer?.StartsWith(WORLDLINE_R.Substring(0, 9)) == true) return new WorldlineRenderer(version: 1);
            if (renderer == ENUNU) return new Enunu.EnunuRenderer();
            if (renderer == VOGEN) return new Vogen.VogenRenderer();
            if (renderer == DIFFSINGER) return new DiffSinger.DiffSingerRenderer();
            if (renderer == VOICEVOX) return new Voicevox.VoicevoxRenderer();
            return null;
        }

        readonly static ConcurrentDictionary<string, object> cacheLockMap = new ConcurrentDictionary<string, object>();

        public static object GetCacheLock(string key) {
            return cacheLockMap.GetOrAdd(key, _ => new object());
        }

        public static void ApplyDynamics(RenderPhrase phrase, RenderResult result) {
            const int interval = 5;
            if (phrase.dynamics == null) return;

            int startTick = phrase.position - phrase.leading;
            double startMs = result.positionMs - result.leadingMs;
            int startSample = 0;
            
            for (int i = 0; i < phrase.dynamics.Length; ++i) {
                int endTick = startTick + interval;
                double endMs = phrase.timeAxis.TickPosToMsPos(endTick);
                int endSample = Math.Min((int)((endMs - startMs) / 1000 * 44100), result.samples.Length);
                
                float a = phrase.dynamics[i];
                float b = (i + 1) == phrase.dynamics.Length ? phrase.dynamics[i] : phrase.dynamics[i + 1];
                
                for (int j = startSample; j < endSample; ++j) {
                    float val = result.samples[j] * (a + (b - a) * (j - startSample) / (endSample - startSample));
                    if (val > 1f) val = 1f;
                    else if (val < -1f) val = -1f;
                    result.samples[j] = val;
                }
                startTick = endTick;
                startSample = endSample;
            }
        }

        public static IReadOnlyList<IResampler> GetSupportedResamplers(IWavtool? wavtool) {
            if (wavtool is SharpWavtool) {
                return ToolsManager.Inst.Resamplers;
            }
            return ToolsManager.Inst.Resamplers.Where(r => !(r is WorldlineResampler)).ToArray();
        }

        public static IReadOnlyList<IWavtool> GetSupportedWavtools(IResampler? resampler) {
            return ToolsManager.Inst.Wavtools;
        }
    }
}