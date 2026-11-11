#nullable enable
// ============================================================================
// 1. วัน/เดือน/ปี: 07 พฤศจิกายน พ.ศ. 2569 / 07 November 2026
// 2. ชื่อไฟล์ที่ทำ: ThaiDictionaryManager.cs
// 3. ลำดับเวอร์ชันของไฟล์: 2.0
// 4. รายชื่อเครดิต: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
// 5. ตำแหน่งที่จัดวางของไฟล์: OpenUtau.Plugin.Builtin/ThaiDictionaryManager.cs
// 6. รายละเอียดการแก้ไขงาน: ผนวกพจนานุกรมฝังในไฟล์ .dll (Embedded Resource) แยกฐานข้อมูล
//                       TH-VCCV_Dict.txt และ TH-ARP_Dict.txt ออกจากกันเพื่อความแม่นยำ 100%
//                       พร้อมรองรับการโหลดและแก้ไขคำอ่านเพิ่มเติมแบบเรียลไทม์ (Real-Time Reload)
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    public static class ThaiDictionaryManager {
        private static readonly Dictionary<string, string> vccvDictionary = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> arpDictionary = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, DateTime> watchedFileTimestamps = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static bool isLoaded = false;
        private static int dictionaryVersion = 0;
        private static DateTime lastTimestampCheckUtc = DateTime.MinValue;
        private static readonly object lockObj = new object();

        public static int CurrentVersion => dictionaryVersion;

        private static readonly Dictionary<string, string> ArpVowelTokenMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "AA", "ah" }, { "AH", "ah" },
            { "IY", "ih" }, { "IH", "ih" },
            { "UW", "uh" },
            { "EH", "eh" }, { "EY", "eh" },
            { "OW", "oh" },
            { "AE", "ae" },
            { "AO", "ao" },
            { "ER", "er" },
            { "UE", "ue" }, { "UEA", "ue" },
            { "UH", "eu" }, { "EU", "eu" },
            { "IA", "ia" },
            { "UA", "ua" },
            { "AY", "ay" },
            { "AW", "aw" },
            { "AM", "am" }
        };

        private static readonly Dictionary<string, string> ArpConsonantTokenMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "B", "b" }, { "CH", "ch" }, { "D", "d" }, { "F", "f" },
            { "H", "h" }, { "HH", "h" }, { "J", "j" }, { "JH", "j" },
            { "K", "k" }, { "KH", "kh" }, { "L", "l" }, { "M", "m" },
            { "N", "n" }, { "NG", "ng" }, { "P", "p" }, { "PH", "ph" },
            { "R", "r" }, { "S", "s" }, { "T", "t" }, { "TH", "th" },
            { "W", "w" }, { "Y", "y" }
        };

        private static readonly Dictionary<string, string> ArpCodaTokenMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "P", "b" }, { "B", "b" },
            { "T", "d" }, { "D", "d" },
            { "K", "k" },
            { "M", "m" }, { "N", "n" }, { "NG", "ng" },
            { "W", "w" }, { "Y", "y" }
        };

        private static readonly HashSet<string> ArpDiphthongSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "r", "l", "w"
        };

        /// <summary>
        /// Converts space-separated TH-ARP_Dict.txt tokens (e.g. "K OW K K AO T") into
        /// ThaiArpasingPhonemizer syllable tokens (e.g. "kohk kawd").
        /// </summary>
        public static string ConvertArpTokensToSyllables(string rawArpPhonemes) {
            if (string.IsNullOrWhiteSpace(rawArpPhonemes)) return string.Empty;
            var tokens = rawArpPhonemes.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length <= 1) return rawArpPhonemes.Trim().ToLowerInvariant();

            var syllables = new List<string>();
            var current = new StringBuilder();

            for (int i = 0; i < tokens.Length; i++) {
                string tk = tokens[i];
                if (ArpVowelTokenMap.TryGetValue(tk, out string? mappedVowel)) {
                    current.Append(mappedVowel);

                    if (mappedVowel == "ay" || mappedVowel == "aw" || mappedVowel == "am") {
                        syllables.Add(current.ToString());
                        current.Clear();
                        continue;
                    }

                    // Check if next token is a coda consonant for this syllable
                    bool hasCoda = false;
                    if (i + 1 < tokens.Length && ArpCodaTokenMap.TryGetValue(tokens[i + 1], out string? codaCandidate)) {
                        if (i + 2 >= tokens.Length) {
                            hasCoda = true;
                        } else {
                            string afterNext = tokens[i + 2];
                            if (ArpVowelTokenMap.ContainsKey(afterNext)) {
                                hasCoda = false;
                            } else if (ArpDiphthongSet.Contains(afterNext) && i + 3 < tokens.Length && ArpVowelTokenMap.ContainsKey(tokens[i + 3])) {
                                hasCoda = false;
                            } else {
                                hasCoda = true;
                            }
                        }

                        if (hasCoda) {
                            current.Append(codaCandidate);
                            i++;
                        }
                    }

                    syllables.Add(current.ToString());
                    current.Clear();
                } else if (ArpConsonantTokenMap.TryGetValue(tk, out string? mappedCons)) {
                    current.Append(mappedCons);
                } else {
                    current.Append(tk.ToLowerInvariant());
                }
            }

            if (current.Length > 0) {
                syllables.Add(current.ToString());
            }

            return string.Join(" ", syllables);
        }

        private static List<string> GetSearchDirectories() {
            var searchDirs = new List<string>();
            try {
                string? asmLoc = typeof(ThaiDictionaryManager).Assembly.Location;
                if (!string.IsNullOrEmpty(asmLoc)) {
                    string? asmDir = Path.GetDirectoryName(asmLoc);
                    if (!string.IsNullOrEmpty(asmDir)) {
                        searchDirs.Add(asmDir);
                        searchDirs.Add(Path.Combine(asmDir, "Dictionary"));
                        searchDirs.Add(Path.Combine(asmDir, "Dictionaries"));
                    }
                }
            } catch { }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir)) {
                searchDirs.Add(Path.Combine(baseDir, "Dictionaries"));
                searchDirs.Add(Path.Combine(baseDir, "Dictionary"));
                searchDirs.Add(Path.Combine(baseDir, "Plugins"));
                searchDirs.Add(Path.Combine(baseDir, "..", "Dictionaries"));
                searchDirs.Add(Path.Combine(baseDir, "..", "..", "Dictionaries"));
                searchDirs.Add(Path.Combine(baseDir, "..", "..", "..", "Dictionaries"));
                searchDirs.Add(Path.Combine(baseDir, "..", "..", "..", "..", "Dictionaries"));
            }

            try {
                if (PathManager.Inst != null) {
                    if (!string.IsNullOrEmpty(PathManager.Inst.PluginsPath)) {
                        searchDirs.Add(PathManager.Inst.PluginsPath);
                        searchDirs.Add(Path.Combine(PathManager.Inst.PluginsPath, "Dictionary"));
                        searchDirs.Add(Path.Combine(PathManager.Inst.PluginsPath, "Dictionaries"));
                    }
                    if (!string.IsNullOrEmpty(PathManager.Inst.DictionariesPath)) {
                        searchDirs.Add(PathManager.Inst.DictionariesPath);
                    }
                    if (!string.IsNullOrEmpty(PathManager.Inst.RootPath)) {
                        searchDirs.Add(Path.Combine(PathManager.Inst.RootPath, "Dictionaries"));
                        searchDirs.Add(Path.Combine(PathManager.Inst.RootPath, "Dictionary"));
                    }
                }
            } catch { }

            return searchDirs;
        }

        private static int ParseStreamIntoDictionary(Stream stream, Dictionary<string, string> targetDict, bool isArpFormat) {
            int count = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 65536, leaveOpen: true);
            string? line;
            while ((line = reader.ReadLine()) != null) {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith(";")) continue;
                int eqIdx = line.IndexOf('=');
                if (eqIdx < 0) eqIdx = line.IndexOf('\t');
                if (eqIdx > 0 && eqIdx + 1 < line.Length) {
                    string key = line.Substring(0, eqIdx).Trim().Normalize(NormalizationForm.FormC);
                    string val = line.Substring(eqIdx + 1).Trim().Normalize(NormalizationForm.FormC);
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val)) {
                        targetDict[key] = isArpFormat ? ConvertArpTokensToSyllables(val) : val;
                        count++;
                    }
                }
            }
            return count;
        }

        private static bool HasExternalDictionaryUpdates() {
            DateTime now = DateTime.UtcNow;
            if ((now - lastTimestampCheckUtc).TotalMilliseconds < 500) {
                return false;
            }
            lastTimestampCheckUtc = now;
            foreach (var kvp in watchedFileTimestamps) {
                try {
                    if (File.Exists(kvp.Key)) {
                        DateTime currentWrite = File.GetLastWriteTimeUtc(kvp.Key);
                        if (currentWrite != kvp.Value) {
                            return true;
                        }
                    }
                } catch { }
            }
            return false;
        }

        public static int EnsureUpToDate() {
            LoadDictionary();
            return dictionaryVersion;
        }

        public static void LoadDictionary(bool forceReload = false) {
            if (isLoaded && !forceReload && !HasExternalDictionaryUpdates()) return;
            lock (lockObj) {
                if (isLoaded && !forceReload && !HasExternalDictionaryUpdates()) return;
                try {
                    int totalLoadedEntries = 0;
                    var asm = typeof(ThaiDictionaryManager).Assembly;

                    // 1. Load embedded dictionaries inside the plugin DLL first (Plug-and-Play standalone baseline)
                    using (var vccvStream = asm.GetManifestResourceStream("OpenUtau.Plugin.Builtin.Dictionary.TH-VCCV_Dict.txt")) {
                        if (vccvStream != null) {
                            totalLoadedEntries += ParseStreamIntoDictionary(vccvStream, vccvDictionary, isArpFormat: false);
                        }
                    }

                    using (var arpStream = asm.GetManifestResourceStream("OpenUtau.Plugin.Builtin.Dictionary.TH-ARP_Dict.txt")) {
                        if (arpStream != null) {
                            totalLoadedEntries += ParseStreamIntoDictionary(arpStream, arpDictionary, isArpFormat: true);
                        }
                    }

                    // 2. Load external dictionary files on disk to append/override entries in real time
                    var searchDirs = GetSearchDirectories();
                    var visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var dir in searchDirs) {
                        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;

                        string vccvPath = Path.GetFullPath(Path.Combine(dir, "TH-VCCV_Dict.txt"));
                        if (!visitedPaths.Contains(vccvPath) && File.Exists(vccvPath)) {
                            visitedPaths.Add(vccvPath);
                            try {
                                using var fs = new FileStream(vccvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                totalLoadedEntries += ParseStreamIntoDictionary(fs, vccvDictionary, isArpFormat: false);
                                watchedFileTimestamps[vccvPath] = File.GetLastWriteTimeUtc(vccvPath);
                            } catch (Exception ex) {
                                Log.Warning(ex, $"[ThaiDictionaryManager] Skipped unreadable dictionary file: {vccvPath}");
                            }
                        }

                        string arpPath = Path.GetFullPath(Path.Combine(dir, "TH-ARP_Dict.txt"));
                        if (!visitedPaths.Contains(arpPath) && File.Exists(arpPath)) {
                            visitedPaths.Add(arpPath);
                            try {
                                using var fs = new FileStream(arpPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                totalLoadedEntries += ParseStreamIntoDictionary(fs, arpDictionary, isArpFormat: true);
                                watchedFileTimestamps[arpPath] = File.GetLastWriteTimeUtc(arpPath);
                            } catch (Exception ex) {
                                Log.Warning(ex, $"[ThaiDictionaryManager] Skipped unreadable dictionary file: {arpPath}");
                            }
                        }
                    }

                    dictionaryVersion++;
                    lastTimestampCheckUtc = DateTime.UtcNow;
                    Log.Information($"[DELTA SYNTH] ThaiDictionaries loaded successfully (v{dictionaryVersion}): VCCV={vccvDictionary.Count} words, ARP={arpDictionary.Count} words (Total entries parsed: {totalLoadedEntries}).");
                } catch (Exception ex) {
                    Log.Error(ex, "Failed to load Thai pronunciation dictionaries.");
                }
                isLoaded = true;
            }
        }

        /// <summary>
        /// Applies dictionary data to target phonemizer dictionary (supports real-time updates and appending after existing entries).
        /// </summary>
        public static void ApplyToDictionary(Dictionary<string, string> targetDict, Func<string, string>? transformer = null) {
            LoadDictionary();
            lock (lockObj) {
                if (transformer != null) {
                    // VCCV / VCV / C+V phonemizer dictionary
                    foreach (var kvp in vccvDictionary) {
                        targetDict[kvp.Key] = transformer(kvp.Value);
                    }
                } else {
                    // Thai Arpasing phonemizer dictionary: normalize any space-separated phoneme sequences in existing curated entries
                    var existingKeys = new List<string>(targetDict.Keys);
                    foreach (var k in existingKeys) {
                        string v = targetDict[k];
                        if (!string.IsNullOrEmpty(v) && v.Contains(' ')) {
                            targetDict[k] = ConvertArpTokensToSyllables(v);
                        }
                    }
                    // Append extended dictionary words after existing curated rules
                    foreach (var kvp in arpDictionary) {
                        if (!targetDict.ContainsKey(kvp.Key)) {
                            targetDict[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
        }
    }
}
