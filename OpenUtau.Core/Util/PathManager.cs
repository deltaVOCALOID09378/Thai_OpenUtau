// ============================================================================
// 1. วัน/เดือน/ปี: 6 พฤศจิกายน พ.ศ. 2569 / 6 November 2026
// 2. ชื่อไฟล์โค้ด: PathManager.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.2
// 4. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Contributors ]
// 5. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Core/Util/PathManager.cs
// 6. รายละเอียดการแก้ไข: เพิ่ม LegacyDataPath เพื่อรักษาความเข้ากันได้แบบ 100% ย้อนหลัง ป้องกัน MissingMethodException
// ============================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core {

    public class PathManager : SingletonBase<PathManager> {
        public PathManager() {
            RootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (OS.IsMacOS()) {
                string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                DataPath = Path.Combine(userHome, "Library", "OpenUtau");
                CachePath = Path.Combine(userHome, "Library", "Caches", "OpenUtau");
                HomePathIsAscii = true;
                try {
                    // Deletes old cache.
                    string oldCache = Path.Combine(DataPath, "Cache");
                    if (Directory.Exists(oldCache)) {
                        Directory.Delete(oldCache, true);
                    }
                } catch { }
            } else if (OS.IsLinux()) {
                string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                if (string.IsNullOrEmpty(dataHome)) {
                    dataHome = Path.Combine(userHome, ".local", "share");
                }
                DataPath = Path.Combine(dataHome, "OpenUtau");
                string cacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
                if (string.IsNullOrEmpty(cacheHome)) {
                    cacheHome = Path.Combine(userHome, ".cache");
                }
                CachePath = Path.Combine(cacheHome, "OpenUtau");
                HomePathIsAscii = true;
            } else {
                string exePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                IsInstalled = File.Exists(Path.Combine(exePath, "installed.txt"));
                if (!IsInstalled) {
                    DataPath = exePath;
                } else {
                    string dataHome = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    DataPath = Path.Combine(dataHome, "OpenUtau");
                }
                DefaultDataPath = DataPath;
                // DELTA SYNTH: user-selected data folder (after reinstalling Windows / restoring a backup).
                string customDataPath = ReadCustomDataPath();
                if (!string.IsNullOrEmpty(customDataPath)) {
                    DataPath = customDataPath;
                    IsCustomDataPath = true;
                }
                CachePath = Path.Combine(DataPath, "Cache");
                HomePathIsAscii = true;
                var etor = StringInfo.GetTextElementEnumerator(DataPath);
                while (etor.MoveNext()) {
                    string s = etor.GetTextElement();
                    if (s.Length != 1 || s[0] >= 128) {
                        HomePathIsAscii = false;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Pointer file stored outside the data folder so the selected location survives
        /// moving or restoring the data folder itself. Contains a single absolute path.
        /// </summary>
        public static string CustomDataPathFile => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ThaiOpenUtau", "datapath.txt");

        static string ReadCustomDataPath() {
            try {
                if (!File.Exists(CustomDataPathFile)) {
                    return null;
                }
                string path = File.ReadAllText(CustomDataPathFile).Trim();
                if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path) || !Directory.Exists(path)) {
                    return null; // Missing drive/folder: fall back to default instead of failing startup.
                }
                return Path.GetFullPath(path);
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>
        /// Saves (or clears with null/empty) the custom user data folder. Takes effect after restart.
        /// </summary>
        public static void SetCustomDataPath(string path) {
            string file = CustomDataPathFile;
            if (string.IsNullOrWhiteSpace(path)) {
                if (File.Exists(file)) {
                    File.Delete(file);
                }
                return;
            }
            string full = Path.GetFullPath(path);
            if (!Directory.Exists(full)) {
                throw new DirectoryNotFoundException(full);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            string tmp = file + ".tmp";
            File.WriteAllText(tmp, full);
            File.Move(tmp, file, true);
        }

        public string DefaultDataPath { get; private set; } = string.Empty;
        public string LegacyDataPath => DefaultDataPath;
        public bool IsCustomDataPath { get; private set; }
        public string RootPath { get; private set; }
        public string DataPath { get; private set; }
        public string CachePath { get; private set; }
        public bool HomePathIsAscii { get; private set; }
        public bool IsInstalled { get; private set; }
        public string SingersPathOld => Path.Combine(DataPath, "Content", "Singers");
        public string SingersPath => Path.Combine(DataPath, "Singers");
        public string AdditionalSingersPath => Preferences.Default.AdditionalSingerPath;
        public string SingersInstallPath => Preferences.Default.InstallToAdditionalSingersPath
            && !string.IsNullOrEmpty(Preferences.Default.AdditionalSingerPath)
                ? AdditionalSingersPath
                : SingersPath;
        public string ResamplersPath => Path.Combine(DataPath, "Resamplers");
        public string WavtoolsPath => Path.Combine(DataPath, "Wavtools");
        public string DependencyPath => Path.Combine(DataPath, "Dependencies");
        public string PluginsPath => Path.Combine(DataPath, "Plugins");
        public string DictionariesPath => Path.Combine(DataPath, "Dictionaries");
        public string TemplatesPath => Path.Combine(DataPath, "Templates");
        public string LogsPath => Path.Combine(DataPath, "Logs");
        public string LogFilePath => Path.Combine(DataPath, "Logs", "log.txt");
        public string PrefsFilePath => Path.Combine(DataPath, "prefs.json");
        public string ThemesPath => Path.Combine(DataPath, "Themes");
        public string TrackColorsPath => Path.Combine(DataPath, "TrackColors");
        public string NotePresetsFilePath => Path.Combine(DataPath, "notepresets.json");
        public string BackupsPath => Path.Combine(DataPath, "Backups");

        public List<string> SingersPaths {
            get {
                var list = new List<string> { SingersPath };
                if (Directory.Exists(SingersPathOld)) {
                    list.Add(SingersPathOld);
                }
                if (Directory.Exists(AdditionalSingersPath)) {
                    list.Add(AdditionalSingersPath);
                }
                if (Preferences.Default.SingerSearchPaths != null) {
                    foreach (var searchPath in Preferences.Default.SingerSearchPaths) {
                        if (!string.IsNullOrEmpty(searchPath) && Directory.Exists(searchPath)) {
                            list.Add(searchPath);
                        }
                    }
                }
                // DELTA SYNTH Standard Central Voicebank Directory Auto-Detection
                const string deltaSynthDefault = @"E:\All Voicebank in DELTA SYNTH";
                if (Directory.Exists(deltaSynthDefault)) {
                    list.Add(deltaSynthDefault);
                }
                const string deltaSynthOnnx = @"D:\All Onnx File";
                if (Directory.Exists(deltaSynthOnnx)) {
                    list.Add(deltaSynthOnnx);
                }
                return list.Distinct().ToList();
            }
        }

        Regex invalid = new Regex("[\\x00-\\x1f<>:\"/\\\\|?*]|^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9]|CLOCK\\$)(\\.|$)|[\\.]$", RegexOptions.IgnoreCase);

        public string GetPartSavePath(string exportPath, string partName, int partNo) {
            var dir = Path.GetDirectoryName(exportPath);
            Directory.CreateDirectory(dir);
            var filename = Path.GetFileNameWithoutExtension(exportPath);
            var name = invalid.Replace(partName, "_");
            if (DocManager.Inst.Project.parts.FindAll(p => p is UVoicePart).Count(p => p.DisplayName == partName) > 1) {
                name += $"_{partNo:D2}";
            }
            return Path.Combine(dir, $"{filename}_{name}.ust");
        }

        public string GetExportPath(string exportPath, UTrack track) {
            var dir = Path.GetDirectoryName(exportPath);
            Directory.CreateDirectory(dir);
            var filename = Path.GetFileNameWithoutExtension(exportPath);
            var trackName = invalid.Replace(track.TrackName, "_");
            if (DocManager.Inst.Project.tracks.Count(t => t.TrackName == track.TrackName) > 1) {
                trackName += $"_{track.TrackNo:D2}";
            }
            return Path.Combine(dir, $"{filename}_{trackName}.wav");
        }

        public void ClearCache() {
            if (!Directory.Exists(CachePath)) {
                return;
            }
            var files = Directory.GetFiles(CachePath);
            foreach (var file in files) {
                try {
                    File.Delete(file);
                } catch (Exception e) {
                    Log.Error(e, $"Failed to delete file {file}");
                }
            }
            var dirs = Directory.GetDirectories(CachePath);
            foreach (var dir in dirs) {
                try {
                    Directory.Delete(dir, true);
                } catch (Exception e) {
                    Log.Error(e, $"Failed to delete dir {dir}");
                }
            }
        }

        readonly static string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        public string GetCacheSize() {
            if (!Directory.Exists(CachePath)) {
                return "0B";
            }
            var dir = new DirectoryInfo(CachePath);
            double size = dir.GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
            int order = 0;
            while (size >= 1024 && order < sizes.Length - 1) {
                order++;
                size = size / 1024;
            }
            return $"{size:0.##}{sizes[order]}";
        }
    }
}
