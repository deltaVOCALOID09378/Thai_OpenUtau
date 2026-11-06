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
