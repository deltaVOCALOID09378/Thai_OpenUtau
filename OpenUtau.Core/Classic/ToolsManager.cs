// ============================================================================
// Date         : 06/11/2026 (B.E. 2569 / A.D. 2026)
// File Name    : ToolsManager.cs
// Version      : 1.0
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau.Core/Classic/ToolsManager.cs
// Description  : ToolsManager managing resamplers and wavtools with Moresampler prioritization as default.
// ============================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Classic {
    public class ToolsManager : SingletonBase<ToolsManager> {
        static object _locker = new object();

        private readonly List<IResampler> resamplers = new List<IResampler>();
        private readonly List<IWavtool> wavtools = new List<IWavtool>();
        private readonly Dictionary<string, IResampler> resamplersMap
            = new Dictionary<string, IResampler>();
        private readonly Dictionary<string, IWavtool> wavtoolsMap
            = new Dictionary<string, IWavtool>();

        public List<IResampler> Resamplers {
            get {
                lock (_locker) {
                    return resamplers.ToList();
                }
            }
        }

        public List<IWavtool> Wavtools {
            get {
                lock (_locker) {
                    return wavtools.ToList();
                }
            }
        }

        IResampler LoadResampler(string filePath, string basePath) {
            if (!File.Exists(filePath)) {
                return null;
            }
            string ext = Path.GetExtension(filePath).ToLower();
            if ((OS.IsWindows() || !string.IsNullOrEmpty(Preferences.Default.WinePath)) && (ext == ".exe" || ext == ".bat")) {
                return new ExeResampler(filePath, basePath);
            } 
            if (!OS.IsWindows() && (ext == ".sh" || string.IsNullOrEmpty(ext))) {
                return new ExeResampler(filePath, basePath);
            }
            return null;
        }

        IWavtool LoadWavtool(string filePath, string basePath) {
            if (!File.Exists(filePath)) {
                return null;
            }
            string ext = Path.GetExtension(filePath).ToLower();
            if ((OS.IsWindows() || !string.IsNullOrEmpty(Preferences.Default.WinePath)) && (ext == ".exe" || ext == ".bat")) {
                return new ExeWavtool(filePath, basePath);
            } 
            if (!OS.IsWindows() && (ext == ".sh" || string.IsNullOrEmpty(ext))) {
                return new ExeWavtool(filePath, basePath);
            }
            return null;
        }

        public void Initialize() {
            lock (_locker) {
                SearchResamplers();
                SearchWavtools();
            }
        }

        public void SearchResamplers() {
            resamplers.Clear();
            resamplersMap.Clear();
            resamplers.Add(new WorldlineResampler());
            string basePath = PathManager.Inst.ResamplersPath;
            try {
                Directory.CreateDirectory(basePath);
                foreach (var file in Directory.EnumerateFiles(basePath, "*", new EnumerationOptions() {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true
                })) {
                    var driver = LoadResampler(file, basePath);
                    if (driver != null) {
                        resamplers.Add(driver);
                    }
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to search resamplers.");
                resamplers.Clear();
            }
            foreach (var resampler in resamplers) {
                resamplersMap[resampler.ToString()] = resampler;
            }
        }

        public void SearchWavtools() {
            wavtools.Clear();
            wavtoolsMap.Clear();
            wavtools.Add(new SharpWavtool(true));
            wavtools.Add(new SharpWavtool(false));
            string basePath = PathManager.Inst.WavtoolsPath;
            try {
                Directory.CreateDirectory(basePath);
                foreach (var file in Directory.EnumerateFiles(basePath, "*", new EnumerationOptions() {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true
                })) {
                    var driver = LoadWavtool(file, basePath);
                    if (driver != null) {
                        wavtools.Add(driver);
                    }
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to search resamplers.");
                wavtools.Clear();
            }
            foreach (var wavtool in wavtools) {
                wavtoolsMap[wavtool.ToString()] = wavtool;
            }
        }

        public IResampler GetResampler(string name) {
            lock (_locker) {
                if (!string.IsNullOrEmpty(name)) {
                    if (resamplersMap.TryGetValue(name, out var resampler)) {
                        return resampler;
                    }
                    var match = resamplers.FirstOrDefault(r =>
                        string.Equals(r.ToString(), name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(Path.GetFileName(r.ToString()), Path.GetFileName(name), StringComparison.OrdinalIgnoreCase));
                    if (match != null) {
                        return match;
                    }
                }
                // [DELTA SYNTH] กำหนดให้ Moresampler เป็นชุดเริ่มต้นสำหรับการเรนเดอร์ (Default Classic Resampler)
                var moresampler = resamplers.FirstOrDefault(r =>
                    r.ToString().ToLowerInvariant().Contains("moresampler"));
                if (moresampler != null) {
                    return moresampler;
                }
                if (resamplersMap.TryGetValue(WorldlineResampler.name, out var worldline)) {
                    return worldline;
                }
                return resamplers.FirstOrDefault();
            }
        }

        public IWavtool GetWavtool(string name) {
            lock (_locker) {
                if (!string.IsNullOrEmpty(name)) {
                    if (wavtoolsMap.TryGetValue(name, out var wavtool)) {
                        return wavtool;
                    }
                    var match = wavtools.FirstOrDefault(w =>
                        string.Equals(w.ToString(), name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(Path.GetFileName(w.ToString()), Path.GetFileName(name), StringComparison.OrdinalIgnoreCase));
                    if (match != null) {
                        return match;
                    }
                }
                return wavtoolsMap.TryGetValue(SharpWavtool.nameConvergence, out var defaultWavtool)
                    ? defaultWavtool
                    : wavtools.FirstOrDefault();
            }
        }
    }
}
