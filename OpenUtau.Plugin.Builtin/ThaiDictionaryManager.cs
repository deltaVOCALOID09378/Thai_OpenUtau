// ============================================================================
// 1. วัน/เดือน/ปี: 5 พฤศจิกายน พ.ศ. 2569 / 5 November 2026
// 2. ชื่อไฟล์โค้ด: ThaiDictionaryManager.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.3
// 4. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by Patiphat Wongyai ]
// 5. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Plugin.Builtin/ThaiDictionaryManager.cs
// 6. รายละเอียดการแก้ไข: แยกการจัดเก็บและโหลดพจนานุกรม VCCV และ ARPABET ออกจากกัน ป้องกันไม่ให้ TH-ARP_Dict เขียนทับ TH-VCCV_Dict เพื่อความถูกต้องของ Phonemizer ทุกระบบ
// ============================================================================

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    public static class ThaiDictionaryManager {
        private static readonly Dictionary<string, string> customVccvDictionary = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> customArpDictionary = new Dictionary<string, string>(StringComparer.Ordinal);
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

                    string[] vccvDictFileNames = {
                        "TH-VCCV_Dict.txt",
                        "th_vccv_custom_dict.txt",
                        "words_th_vccv.txt",
                        "words_th_dict.txt",
                        "words_th.txt",
                        "th_custom_dict.txt"
                    };

                    string[] arpDictFileNames = {
                        "English-ARP.txt",
                        "TH-ARP_Dict.txt",
                        "dsdict-th.txt"
                    };

                    int totalVccvEntries = 0;
                    int totalArpEntries = 0;
                    var visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var dir in searchDirs) {
                        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;

                        // โหลด VCCV Dictionary
                        foreach (var fileName in vccvDictFileNames) {
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
                                            customVccvDictionary[key] = val;
                                            totalVccvEntries++;
                                        }
                                    }
                                }
                            } catch (Exception ex) {
                                Log.Warning(ex, $"[ThaiDictionaryManager] ข้ามไฟล์พจนานุกรม VCCV ที่ไม่สามารถอ่านได้: {fullPath}");
                            }
                        }

                        // โหลด ARPABET Dictionary
                        foreach (var fileName in arpDictFileNames) {
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
                                            customArpDictionary[key] = val;
                                            totalArpEntries++;
                                        }
                                    }
                                }
                            } catch (Exception ex) {
                                Log.Warning(ex, $"[ThaiDictionaryManager] ข้ามไฟล์พจนานุกรม ARP ที่ไม่สามารถอ่านได้: {fullPath}");
                            }
                        }
                    }
                    Log.Information($"[DELTA SYNTH] โหลดพจนานุกรมภาษาไทยสำเร็จ: VCCV {customVccvDictionary.Count} คำ ({totalVccvEntries} entries), ARP {customArpDictionary.Count} คำ ({totalArpEntries} entries).");
                } catch (Exception ex) {
                    Log.Error(ex, "เกิดข้อผิดพลาดในการโหลดพจนานุกรมภาษาไทย (Failed to load custom Thai dictionary).");
                }
                isLoaded = true;
            }
        }

        /// <summary>
        /// นำข้อมูลไปใช้กับพจนานุกรมเป้าหมาย (Apply data to target dictionary: "vccv" or "arp")
        /// </summary>
        public static void ApplyToDictionary(Dictionary<string, string> targetDict, Func<string, string>? transformer = null, string dictType = "vccv") {
            LoadDictionary();
            lock (lockObj) {
                var source = string.Equals(dictType, "arp", StringComparison.OrdinalIgnoreCase) 
                    ? customArpDictionary 
                    : customVccvDictionary;

                foreach (var kvp in source) {
                    targetDict[kvp.Key] = transformer != null ? transformer(kvp.Value) : kvp.Value;
                }
            }
        }
    }
}
