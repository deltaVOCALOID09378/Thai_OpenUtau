// ============================================================================
// 1. วัน/เดือน/ปี: 31 ตุลาคม พ.ศ. 2569 / 31 October 2026
// 2. ชื่อไฟล์โค้ด: ArpasingPhonemizer.cs
// 3. ลำดับเวอร์ชันของไฟล์: 2.0
// 4. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Plugin.Builtin/ArpasingPhonemizer.cs
// 5. หน้าที่: ตัวจัดการหน่วยเสียง English Arpasing มาตรฐาน แก้ไขการโหลดพจนานุกรม
//            English-ARP.txt ให้ครบทุกคำ 100% และจัดสัดส่วนพื้นที่เสียงตามกฎเหล็ก
// 6. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Agentic AI | Original by OpenUtau Contributors
// ============================================================================

#pragma warning disable CS0618, CS0649, CS8632, CS0108
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.G2p;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("English Arpasing Phonemizer", "EN ARPA", language: "EN")]
    public class ArpasingPhonemizer : LatinDiphonePhonemizer {

        private static readonly string[] arpVowels = new string[] {
            "aa", "ae", "ah", "ao", "aw", "ay", "eh", "er", "ey", "ih", "iy", "ow", "oy", "uh", "uw", "ax"
        };

        private static readonly string[] arpConsonants = new string[] {
            "b", "ch", "d", "dh", "dr", "dx", "f", "g", "hh", "jh", "k", "l", "m", "n", "ng", "p", "q", "r", "s", "sh", "t", "th", "tr", "v", "w", "y", "z", "zh", "-"
        };

        private static readonly HashSet<string> vowelSet = new HashSet<string>(arpVowels, StringComparer.OrdinalIgnoreCase);
        private static IG2p? cachedEnglishArpDict;
        private static readonly object dictLock = new object();

        public ArpasingPhonemizer() {
            try {
                ConsonantLength = 25;
                Initialize();
            } catch (Exception e) {
                Log.Error(e, "Failed to initialize ArpasingPhonemizer.");
                throw;
            }
        }

        internal static IG2p? GetOrLoadEnglishArpDictionary(string pluginDir) {
            if (cachedEnglishArpDict != null) return cachedEnglishArpDict;

            lock (dictLock) {
                if (cachedEnglishArpDict != null) return cachedEnglishArpDict;

                var candidatePaths = new List<string>();
                if (!string.IsNullOrEmpty(pluginDir)) {
                    candidatePaths.Add(Path.Combine(pluginDir, "English-ARP.txt"));
                    candidatePaths.Add(Path.Combine(pluginDir, "Dictionary", "English-ARP.txt"));
                    candidatePaths.Add(Path.Combine(pluginDir, "Dictionaries", "English-ARP.txt"));
                }
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (!string.IsNullOrEmpty(baseDir)) {
                    candidatePaths.Add(Path.Combine(baseDir, "Dictionaries", "English-ARP.txt"));
                    candidatePaths.Add(Path.Combine(baseDir, "Dictionary", "English-ARP.txt"));
                    candidatePaths.Add(Path.Combine(baseDir, "Plugins", "English-ARP.txt"));
                }
                try {
                    if (PathManager.Inst != null && !string.IsNullOrEmpty(PathManager.Inst.DictionariesPath)) {
                        candidatePaths.Add(Path.Combine(PathManager.Inst.DictionariesPath, "English-ARP.txt"));
                    }
                } catch { }

                string? arpPath = candidatePaths.FirstOrDefault(File.Exists);
                if (string.IsNullOrEmpty(arpPath)) {
                    return null;
                }

                var arpBuilder = G2pDictionary.NewBuilder();
                // ต้องลงทะเบียน Symbol ทั้งหมดก่อนเรียก AddEntry มิฉะนั้น G2pDictionary จะกรองคำอ่านทิ้งทั้งหมด
                foreach (var v in arpVowels) {
                    arpBuilder.AddSymbol(v, true);
                }
                foreach (var c in arpConsonants) {
                    bool isGlide = c == "w" || c == "y" || c == "r" || c == "l";
                    arpBuilder.AddSymbol(c, false, isGlide);
                }

                foreach (string rawLine in File.ReadLines(arpPath)) {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("!") || line.StartsWith("#")) {
                        continue;
                    }

                    // รองรับทั้งการคั่นด้วย Single Space, Double Space, Tab และเครื่องหมาย '='
                    int splitIndex = line.IndexOf("  ", StringComparison.Ordinal);
                    if (splitIndex < 0) splitIndex = line.IndexOf('\t');
                    if (splitIndex < 0) splitIndex = line.IndexOf('=');
                    if (splitIndex < 0) splitIndex = line.IndexOf(' ');

                    if (splitIndex > 0) {
                        string word = line.Substring(0, splitIndex).Trim().ToLowerInvariant();
                        string phnStr = line.Substring(splitIndex + 1).Trim().ToLowerInvariant();

                        // ลบวงเล็บลำดับคำพ้องรูป เช่น word(1) หรือ a() แต่เก็บเฉพาะคำหลักตัวแรก
                        bool isVariant = word.Contains('(');
                        word = Regex.Replace(word, @"\(\d*\)$", "");
                        if (string.IsNullOrEmpty(word) || isVariant) {
                            continue;
                        }

                        string[] phonemes = phnStr.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => Regex.Replace(p, @"\d+$", ""))
                            .ToArray();

                        if (phonemes.Length > 0) {
                            arpBuilder.AddEntry(word, phonemes);
                        }
                    }
                }

                cachedEnglishArpDict = arpBuilder.Build();
                return cachedEnglishArpDict;
            }
        }

        protected override IG2p LoadG2p() {
            var g2ps = new List<IG2p>();

            try {
                // 1. โหลดพจนานุกรมเฉพาะของนักร้อง (Singer's custom arpasing.yaml)
                if (singer != null && singer.Found && singer.Loaded && !string.IsNullOrEmpty(singer.Location)) {
                    string file = Path.Combine(singer.Location, "arpasing.yaml");
                    if (File.Exists(file)) {
                        g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(file)).Build());
                    }
                }

                // 2. โหลดพจนานุกรมจากโฟลเดอร์ปลั๊กอิน (arpasing.yaml)
                if (!string.IsNullOrEmpty(PluginDir)) {
                    string path = Path.Combine(PluginDir, "arpasing.yaml");
                    if (!File.Exists(path)) {
                        Directory.CreateDirectory(PluginDir);
                        File.WriteAllBytes(path, Data.Resources.arpasing_template);
                    }
                    g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(path)).Build());
                }

                // 3. โหลดพจนานุกรมหลัก English-ARP.txt แบบครบทุกคำ
                var englishArp = GetOrLoadEnglishArpDictionary(PluginDir);
                if (englishArp != null) {
                    g2ps.Add(englishArp);
                }

                // 4. โหลด ArpabetG2p เป็นปราการด่านสุดท้ายสำหรับคำเกิดใหม่/คำสแลง
                g2ps.Add(new ArpabetG2p());
            } catch (Exception e) {
                Log.Error(e, "Dictionary Loading Error in ArpasingPhonemizer.");
                throw;
            }

            return new G2pFallbacks(g2ps.ToArray());
        }

        protected override Dictionary<string, string[]> LoadVowelFallbacks() {
            return "aa=ah,ae;ae=ah,aa;ah=aa,ae;ao=ow,aa;ow=ao;eh=ae;ih=iy;iy=ih;uh=uw;uw=uh;aw=ao,ah;ax=ah,eh".Split(';')
                .Select(entry => entry.Split('='))
                .ToDictionary(parts => parts[0], parts => parts[1].Split(','));
        }

        protected override string GetPhonemeOrFallback(string prevSymbol, string symbol, int tone, string color, string alt) {
            if (g2p != null && g2p.IsVowel(prevSymbol) && g2p.IsVowel(symbol)) {
                if (singer != null && singer.TryGetMappedOto($"{prevSymbol} {symbol}", tone, color, out var vvOto)) {
                    return vvOto.Alias;
                }
                prevSymbol = "-";
            }
            if (singer != null) {
                if (!string.IsNullOrEmpty(alt) && singer.TryGetMappedOto($"{prevSymbol} {symbol}{alt}", tone, color, out var oto)) {
                    return oto.Alias;
                }
                if (singer.TryGetMappedOto($"{prevSymbol} {symbol}", tone, color, out var oto1)) {
                    return oto1.Alias;
                }
                if (vowelFallback.TryGetValue(symbol, out string[]? fallbacks)) {
                    foreach (var fallback in fallbacks) {
                        if (singer.TryGetMappedOto($"{prevSymbol} {fallback}", tone, color, out var oto2)) {
                            return oto2.Alias;
                        }
                    }
                }
                if (prevSymbol == "-" && singer.TryGetMappedOto($"- {symbol}", tone, color, out var oto3)) {
                    return oto3.Alias;
                }
                if (singer.TryGetMappedOto(symbol, tone, color, out var otoSingle)) {
                    return otoSingle.Alias;
                }
            }
            return $"{prevSymbol} {symbol}{alt}";
        }
    }
}