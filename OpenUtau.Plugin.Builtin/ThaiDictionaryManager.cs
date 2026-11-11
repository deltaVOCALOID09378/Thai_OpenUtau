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
        public static void ApplyToDictionary(Dictionary<string, string> targetDict, Func<string, string>? transformer = null, string dictType = "vccv") {
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

        // พจนานุกรมคำศัพท์พื้นฐานเฉพาะระบบ UTAU VCCV (Pure UTAU VCCV Core Entries)
        private static readonly (string Word, string Phonemes)[] BuiltinCoreVccvEntries = new[] {
            ("ไม่", "m ay"), ("มี", "m i"), ("อีก", "i k"), ("ฝน", "f o n"), ("ฟ้า", "f a"),
            ("สวย", "s ua y"), ("ไม้", "m a y"), ("งาม", "g a m"), ("ขาด", "kh a d"), ("ความ", "kh w a m"),
            ("รัก", "r a k"), ("ไร้", "r ay"), ("ค่า", "kh a"), ("สิ่ง", "s i g"), ("หัว", "h ua"),
            ("ใจ", "j ay"), ("หัวใจ", "h ua j ay"), ("ให้", "h ay"), ("ใน", "n ay"), ("ได้", "d a y"),
            ("ไป", "p ay"), ("มา", "m a"), ("คน", "kh o n"), ("เธอ", "th 3"), ("ฉัน", "ch a n"),
            ("เรา", "r aw"), ("เขา", "kh aw"), ("คิด", "kh i d"), ("ถึง", "th 1 g"), ("คิดถึง", "kh i d th 1 g"),
            ("วัน", "w a n"), ("คืน", "kh 1 n"), ("ดาว", "d a w"), ("เดือน", "d 6 n"), ("ลม", "l o m"),
            ("หนาว", "n a w"), ("ร้อน", "r Q n"), ("แสง", "s @ g"), ("เงา", "g aw"), ("ทาง", "th a g"),
            ("ฝัน", "f a n"), ("ความฝัน", "kh w a m f a n"), ("ความรัก", "kh w a m r a k"), ("ความจริง", "kh w a m j i g"),
            ("ชีวิต", "ch i w i d"), ("เวลา", "w e l a"), ("น้ำตา", "n a m t a"), ("รอยยิ้ม", "r Q y y i m"),
            ("เสียง", "s ia g"), ("เพลง", "ph l e g"), ("เรื่องราว", "r 6 g r a w"), ("ตลอดไป", "t a l Q d p ay"),
            ("เมื่อไหร่", "m 6 r ay"), ("เท่าไหร่", "th aw r ay"), ("ทำไม", "th a m m ay"), ("อย่างไร", "y a g r ay"),
            ("เพราะ", "ph r Q"), ("เพื่อ", "ph 6"), ("แต่", "t @"), ("และ", "l @"), ("หรือ", "r 1"),
            ("ก็", "k Q"), ("แล้ว", "l @ w"), ("อยู่", "y u"), ("อย่าง", "y a g"), ("อยาก", "y a k"),
            ("อย่า", "y a"), ("อาจ", "a d"), ("ออก", "Q k"), ("อ่อน", "Q n"), ("อ้อมกอด", "Q m k Q d")
        };

        /// <summary>
        /// อ่านไฟล์ YAML เฉพาะของระบบ UTAU VCCV เท่านั้น
        /// </summary>
        public static void ParseVccvYamlDictionary(string fullPath, Dictionary<string, string> targetDict, ref int counter) {
            if (!File.Exists(fullPath)) return;
            bool inDictSection = false;
            foreach (var rawLine in File.ReadAllLines(fullPath, Encoding.UTF8)) {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                if (line.StartsWith("dictionary:", StringComparison.OrdinalIgnoreCase)) {
                    inDictSection = true;
                    continue;
                }
                if (!rawLine.StartsWith(" ") && !rawLine.StartsWith("\t") && line.EndsWith(":")) {
                    inDictSection = false;
                    continue;
                }
                if (inDictSection && line.Contains(":")) {
                    int idx = line.IndexOf(':');
                    string k = line.Substring(0, idx).Trim().Trim('"', '\'').Normalize(NormalizationForm.FormC);
                    string v = line.Substring(idx + 1).Trim().Trim('"', '\'').Normalize(NormalizationForm.FormC);
                    if (!string.IsNullOrEmpty(k) && !string.IsNullOrEmpty(v)) {
                        targetDict[k] = v;
                        counter++;
                    }
                }
            }
        }

        /// <summary>
        /// สร้างไฟล์ตั้งค่าหน่วยเสียงเบื้องต้นเฉพาะสำหรับ UTAU VCCV ในคลังเสียงของนักร้อง (Pure UTAU VCCV YAML)
        /// </summary>
        public static string EnsureSingerVccvYaml(string singerLocation) {
            if (string.IsNullOrEmpty(singerLocation) || !Directory.Exists(singerLocation)) return string.Empty;
            string yamlPath = Path.Combine(singerLocation, "th_vccv_phonemes.yaml");
            if (File.Exists(yamlPath)) return yamlPath;

            try {
                string content = @"# ============================================================================
# Thai UTAU VCCV Voicebank Phoneme Configuration (th_vccv_phonemes.yaml)
# Pure UTAU VCCV Phoneme Set (Strictly separated from DiffSinger)
# ============================================================================
language: TH
phonemizer: Thai VCCV Phonemizer
vowels: [a, i, u, e, o, '@', Q, '3', '6', '1', ia, ua, I, aw, am]
diphthongs: [r, l, w]
consonants: [b, ch, d, f, g, h, j, k, kh, l, m, n, p, ph, r, s, t, th, w, y, '-']
endingConsonants: [b, ch, d, f, g, h, j, k, kh, l, m, n, p, ph, r, s, t, th, w, y]
dictionary:
  ไม่: m ay
  มี: m i
  ฝน: f o n
  ฟ้า: f a
  หัวใจ: h ua j ay
  เธอ: th 3
  ฉัน: ch a n
  เรา: r aw
  ชีวิต: ch i w i d
  รัก: r a k
";
                File.WriteAllText(yamlPath, content, Encoding.UTF8);
            } catch (Exception ex) {
                Log.Warning(ex, $"[ThaiDictionaryManager] ไม่สามารถสร้างไฟล์ th_vccv_phonemes.yaml ในคลังเสียงได้: {singerLocation}");
            }
            return yamlPath;
        }
    }
}
