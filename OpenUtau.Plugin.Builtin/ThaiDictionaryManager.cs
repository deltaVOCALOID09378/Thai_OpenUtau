using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    public static class ThaiDictionaryManager {
        private static readonly Dictionary<string, string> customDictionary = new Dictionary<string, string>(StringComparer.Ordinal);
        private static bool isLoaded = false;
        private static readonly object lockObj = new object();


        public static void LoadDictionary() {
            if (isLoaded) return;
            lock (lockObj) {
                if (isLoaded) return;
                try {
                    var searchDirs = new List<string>();

                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    if (!string.IsNullOrEmpty(baseDir)) {
                        searchDirs.Add(Path.Combine(baseDir, "Dictionaries"));
                        searchDirs.Add(Path.Combine(baseDir, "Dictionary"));
                        searchDirs.Add(Path.Combine(baseDir, "..", "Dictionaries"));
                        searchDirs.Add(Path.Combine(baseDir, "..", "..", "Dictionaries"));
                        searchDirs.Add(Path.Combine(baseDir, "..", "..", "..", "Dictionaries"));
                        searchDirs.Add(Path.Combine(baseDir, "..", "..", "..", "..", "Dictionaries"));
                    }

                    try {
                        if (PathManager.Inst != null) {
                            if (!string.IsNullOrEmpty(PathManager.Inst.DictionariesPath)) {
                                searchDirs.Add(PathManager.Inst.DictionariesPath);
                            }
                            if (!string.IsNullOrEmpty(PathManager.Inst.RootPath)) {
                                searchDirs.Add(Path.Combine(PathManager.Inst.RootPath, "Dictionaries"));
                                searchDirs.Add(Path.Combine(PathManager.Inst.RootPath, "Dictionary"));
                            }
                        }
                    } catch { }

                    string[] dictFileNames = {
                        "English-ARP.txt",
                        "TH-VCCV_Dict.txt",
                        "TH-ARP_Dict.txt",
                    };

                    int totalLoadedEntries = 0;
                    var visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var dir in searchDirs) {
                        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;

                        foreach (var fileName in dictFileNames) {
                            string fullPath = Path.GetFullPath(Path.Combine(dir, fileName));
                            if (visitedPaths.Contains(fullPath) || !File.Exists(fullPath)) continue;
                            visitedPaths.Add(fullPath);

                            try {
                                var lines = File.ReadAllLines(fullPath, Encoding.UTF8);
                                foreach (var line in lines) {
                                    if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;
                                    var parts = line.Split(new[] { '=', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length >= 2) {
                                        string key = parts[0].Trim().Normalize(NormalizationForm.FormC);
                                        string val = parts[1].Trim().Normalize(NormalizationForm.FormC);
                                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val)) {
                                            customDictionary[key] = val;
                                            totalLoadedEntries++;
                                        }
                                    }
                                }
                            } catch (Exception ex) {
                                Log.Warning(ex, $"[ThaiDictionaryManager] ข้ามไฟล์พจนานุกรมที่ไม่สามารถอ่านได้: {fullPath}");
                            }
                        }
                    }
                    Log.Information($"[DELTA SYNTH] โหลดพจนานุกรมภาษาไทยส่วนกลางสำเร็จ จำนวน {customDictionary.Count} คำ (Total entries parsed: {totalLoadedEntries}).");
                } catch (Exception ex) {
                    Log.Error(ex, "เกิดข้อผิดพลาดในการโหลดพจนานุกรมภาษาไทย (Failed to load custom Thai dictionary).");
                }
                isLoaded = true;
            }
        }

        /// <summary>
        /// นำข้อมูลไปใช้กับพจนานุกรมเป้าหมาย (Apply data to target dictionary)
        /// </summary>
        public static void ApplyToDictionary(Dictionary<string, string> targetDict, Func<string, string>? transformer = null) {
            LoadDictionary();
            lock (lockObj) {
                foreach (var kvp in customDictionary) {
                    targetDict[kvp.Key] = transformer != null ? transformer(kvp.Value) : kvp.Value;
                }
            }
        }
    }
}
