// ============================================================================
// 1. วัน/เดือน/ปี: 1 พฤศจิกายน พ.ศ. 2569 / 01 November 2026
// 2. ชื่อไฟล์โค้ด: ExeResampler.cs
// 3. ลำดับเวอร์ชันของไฟล์: 3.1
// 4. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Core/Classic/ExeResampler.cs
// 5. หน้าที่: จัดการรัน Resampler ภายนอกแบบแยกไฟล์ Staging พร้อม Atomic Cache Swap
// 6. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Agentic AI | Original by OpenUtau Contributors
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.Wave;
using OpenUtau.Core;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Classic {
    internal class ExeResampler : IResampler {
        public string Name { get; private set; }
        public string FilePath { get; private set; }
        public bool isLegalPlugin => _isLegalPlugin;
        public ResamplerManifest Manifest { get; private set; }
        readonly string _name;
        readonly bool _isLegalPlugin = false;
        readonly string winePath;
        readonly bool useWine;

        public ResamplerManifest LoadManifest() {
            try {
                var ManifestPath = Path.ChangeExtension(FilePath, ".yaml");
                if (!File.Exists(ManifestPath)) {
                    return new ResamplerManifest();
                }
                return ResamplerManifest.Load(ManifestPath);
            } catch (Exception ex) {
                Log.Error($"Failed loading resampler manifest for {_name}: {ex}");
                return new ResamplerManifest();
            }
        }

        void FixMoreConfig(string moreConfigPath) {
            var lines = new List<string>();
            if (File.Exists(moreConfigPath)) {
                using var fs = new FileStream(moreConfigPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                string line;
                while ((line = sr.ReadLine()) != null) {
                    lines.Add(line);
                }
            }
            for (int i = 0; i < lines.Count; i++) {
                if (lines[i].StartsWith("resampler-compatibility")) {
                    if (lines[i] == "resampler-compatibility on") {
                        return;
                    } else {
                        lines[i] = "resampler-compatibility on";
                        File.WriteAllLines(moreConfigPath, lines);
                        return;
                    }
                }
            }
            lines.Add("resampler-compatibility on");
            File.WriteAllLines(moreConfigPath, lines);
        }

        public ExeResampler(string filePath, string basePath) {
            if (File.Exists(filePath)) {
                FilePath = filePath;
                _name = Path.GetRelativePath(basePath, filePath);
                Name = _name;
                _isLegalPlugin = true;
            }
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            winePath = Preferences.Default.WinePath;
            useWine = !OS.IsWindows() && !string.IsNullOrEmpty(winePath) && (ext == ".exe" || ext == ".bat");
            Manifest = LoadManifest();
            try {
                if (Path.GetFileNameWithoutExtension(filePath) == "moresampler") {
                    var moreConfigPath = Path.Combine(Path.GetDirectoryName(filePath), "moreconfig.txt");
                    FixMoreConfig(moreConfigPath);
                }
            } catch (Exception ex) {
                Log.Error($"Failed fixing moreconfig.txt for {filePath}: {ex}");
            }
        }

        public float[] DoResampler(ResamplerItem args, ILogger logger) {
            string tmpFile = DoResamplerReturnsFile(args, logger);
            if (!string.IsNullOrEmpty(tmpFile) && File.Exists(tmpFile)) {
                using (var waveStream = Wave.OpenFile(tmpFile)) {
                    return Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                }
            }
            return Array.Empty<float>();
        }

        public string DoResamplerReturnsFile(ResamplerItem args, ILogger logger) {
            if (!_isLegalPlugin) {
                return null;
            }
            var threadId = Thread.CurrentThread.ManagedThreadId;
            string finalFile = args.outputFile;
            string stagingFile = $"{finalFile}.{threadId}.{Guid.NewGuid():N}.tmp.wav";
            try {
                string ArgParam = FormattableString.Invariant(
                    $"\"{args.inputTemp}\" \"{stagingFile}\" {MusicMath.GetToneName(args.tone)} {args.velocity} \"{args.GetFlagsString()}\" {args.offset} {args.durRequired} {args.consonant} {args.cutoff} {args.volume} {args.modulation} !{args.tempo} {Base64.Base64EncodeInt12(args.pitches)}");
                logger.Information($" > [thread-{threadId}] {FilePath} {ArgParam}");
                string resamplerOutput;
                if (useWine) {
                    resamplerOutput = ProcessRunner.Run(winePath, $"\"{FilePath}\" {ArgParam}", logger);
                } else {
                    resamplerOutput = ProcessRunner.Run(FilePath, ArgParam, logger);
                }
                if (!File.Exists(stagingFile)) {
                    throw new ResamplerFailedException($" > [thread-{threadId}]\n{resamplerOutput}");
                }
                lock (Renderers.GetCacheLock(finalFile)) {
                    try {
                        File.Move(stagingFile, finalFile, overwrite: true);
                    } catch (IOException) {
                        using var src = new FileStream(stagingFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                        using var dst = new FileStream(finalFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                        src.CopyTo(dst);
                    }
                }
            } finally {
                try {
                    if (File.Exists(stagingFile)) {
                        File.Delete(stagingFile);
                    }
                } catch { }
            }
            return finalFile;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);

        public void CheckPermissions() {
            if (OS.IsWindows() || !File.Exists(FilePath)) {
                return;
            }
            int mode = (7 << 6) | (5 << 3) | 5;
            chmod(FilePath, mode);
        }

        public bool SupportsFlag(string abbr) {
            if (Manifest == null || !Manifest.expressionFilter) {
                return true;
            }
            return Manifest.expressions.ContainsKey(abbr);
        }

        public override string ToString() => _name;
    }
}
