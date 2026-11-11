// ============================================================================
// 1. วัน/เดือน/ปี: 10 พฤศจิกายน พ.ศ. 2569 / 10 November 2026
// 2. ชื่อไฟล์ที่ทำ: ArpasingPhonemizer.cs
// 3. ลำดับเวอร์ชันของไฟล์: 2.4
// 4. รายชื่อเครดิต: Made And Checked By DELTA SYNTH & Gemini AI [ Original by OpenUtau Contributors ]
// 5. ตำแหน่งที่จัดวางของไฟล์: OpenUtau.Plugin.Builtin/ArpasingPhonemizer.cs
// 6. รายละเอียดการแก้ไขงาน: บังคับให้คำว่า "a" ร้องเป็น "aa" ด้วย Priority Override Dict
//                       ควบคู่กับพจนานุกรม Embedded Resource และการอ่านไฟล์ภายนอกแบบเรียลไทม์
// ============================================================================

#pragma warning disable CS0618, CS0649, CS8632, CS0108
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private static string? cachedArpFilePath;
        private static DateTime cachedArpLastWriteUtc;
        private static readonly object dictLock = new object();

        private static IG2p CreatePriorityOverrideDict() {
            var builder = G2pDictionary.NewBuilder();
            foreach (var v in arpVowels) {
                builder.AddSymbol(v, true);
            }
            foreach (var c in arpConsonants) {
                bool isGlide = c == "w" || c == "y" || c == "r" || c == "l";
                builder.AddSymbol(c, false, isGlide);
            }
            builder.AddEntry("a", new[] { "aa" });
            return builder.Build();
        }

        public ArpasingPhonemizer() {
            try {
                ConsonantLength = 25;
                Initialize();
            } catch (Exception e) {
                Log.Error(e, "Failed to initialize ArpasingPhonemizer.");
                throw;
            }
        }

        private static void ParseArpStreamIntoBuilder(Stream stream, G2pDictionary.Builder arpBuilder) {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 65536, leaveOpen: true);
            string? rawLine;
            while ((rawLine = reader.ReadLine()) != null) {
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
        }

        public static IG2p? GetOrLoadEnglishArpDictionary(string pluginDir) {
            var candidatePaths = new List<string>();
            if (!string.IsNullOrEmpty(pluginDir)) {
                candidatePaths.Add(Path.Combine(pluginDir, "English-ARP.txt"));
                candidatePaths.Add(Path.Combine(pluginDir, "Dictionary", "English-ARP.txt"));
                candidatePaths.Add(Path.Combine(pluginDir, "Dictionaries", "English-ARP.txt"));
            }
            try {
                string? asmLoc = typeof(ArpasingPhonemizer).Assembly.Location;
                if (!string.IsNullOrEmpty(asmLoc)) {
                    string? asmDir = Path.GetDirectoryName(asmLoc);
                    if (!string.IsNullOrEmpty(asmDir)) {
                        candidatePaths.Add(Path.Combine(asmDir, "English-ARP.txt"));
                        candidatePaths.Add(Path.Combine(asmDir, "Dictionary", "English-ARP.txt"));
                        candidatePaths.Add(Path.Combine(asmDir, "Dictionaries", "English-ARP.txt"));
                    }
                }
            } catch { }
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir)) {
                candidatePaths.Add(Path.Combine(baseDir, "Dictionaries", "English-ARP.txt"));
                candidatePaths.Add(Path.Combine(baseDir, "Dictionary", "English-ARP.txt"));
                candidatePaths.Add(Path.Combine(baseDir, "Plugins", "English-ARP.txt"));
            }
            try {
                if (PathManager.Inst != null) {
                    if (!string.IsNullOrEmpty(PathManager.Inst.PluginsPath)) {
                        candidatePaths.Add(Path.Combine(PathManager.Inst.PluginsPath, "English-ARP.txt"));
                        candidatePaths.Add(Path.Combine(PathManager.Inst.PluginsPath, "Dictionary", "English-ARP.txt"));
                    }
                    if (!string.IsNullOrEmpty(PathManager.Inst.DictionariesPath)) {
                        candidatePaths.Add(Path.Combine(PathManager.Inst.DictionariesPath, "English-ARP.txt"));
                    }
                }
            } catch { }

            string? arpPath = candidatePaths.FirstOrDefault(File.Exists);
            DateTime currentWriteUtc = (!string.IsNullOrEmpty(arpPath) && File.Exists(arpPath))
                ? File.GetLastWriteTimeUtc(arpPath)
                : DateTime.MinValue;

            if (cachedEnglishArpDict != null && cachedArpFilePath == arpPath && cachedArpLastWriteUtc == currentWriteUtc) {
                return cachedEnglishArpDict;
            }

            lock (dictLock) {
                if (cachedEnglishArpDict != null && cachedArpFilePath == arpPath && cachedArpLastWriteUtc == currentWriteUtc) {
                    return cachedEnglishArpDict;
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

                bool loadedAny = false;

                // 1. Load embedded English-ARP.txt from plugin DLL first
                var asm = typeof(ArpasingPhonemizer).Assembly;
                using (var embeddedStream = asm.GetManifestResourceStream("OpenUtau.Plugin.Builtin.Dictionary.English-ARP.txt")) {
                    if (embeddedStream != null) {
                        ParseArpStreamIntoBuilder(embeddedStream, arpBuilder);
                        loadedAny = true;
                    }
                }

                // 2. Overlay external English-ARP.txt on disk if present (supports real-time editing)
                if (!string.IsNullOrEmpty(arpPath) && File.Exists(arpPath)) {
                    using var fs = new FileStream(arpPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    ParseArpStreamIntoBuilder(fs, arpBuilder);
                    loadedAny = true;
                }

                if (!loadedAny) {
                    return null;
                }

                cachedEnglishArpDict = arpBuilder.Build();
                cachedArpFilePath = arpPath;
                cachedArpLastWriteUtc = currentWriteUtc;
                return cachedEnglishArpDict;
            }
        }

        protected override IG2p LoadG2p() {
            var g2ps = new List<IG2p>();

            try {
                // 0. Priority Override: บังคับให้ "a" ร้องเป็น "aa" ก่อนพจนานุกรมอื่นทั้งหมด
                g2ps.Add(CreatePriorityOverrideDict());

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
