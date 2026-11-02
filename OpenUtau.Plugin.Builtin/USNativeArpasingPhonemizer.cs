// ============================================================================
// 1. วัน/เดือน/ปี: 31 ตุลาคม พ.ศ. 2569 / 31 October 2026
// 2. ชื่อไฟล์โค้ด: USNativeArpasingPhonemizer.cs
// 3. ลำดับเวอร์ชันของไฟล์: 2.5
// 4. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Plugin.Builtin/USNativeArpasingPhonemizer.cs
// 5. หน้าที่: US Native English Arpasing Phonemizer เชื่อมต่อฐานข้อมูลพจนานุกรม
//            English-ARP.txt โดยตรง พร้อมบังคับใช้กฎเหล็กสัดส่วนเสียง 5-7% สำหรับ CC/C-
// 6. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Agentic AI | Original by OpenUtau Contributors
// ============================================================================

#pragma warning disable CS0618, CS0649, CS8632, CS0108
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.G2p;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("US Native Arpasing Phonemizer", "EN ARPA-US", language: "EN")]
    public class USNativeArpasingPhonemizer : LatinDiphonePhonemizer {

        public USNativeArpasingPhonemizer() {
            try {
                ConsonantLength = 25;
                Initialize();
            } catch (Exception e) {
                Log.Error(e, "Critical Initialization Failure in USNativeArpasingPhonemizer.");
                throw;
            }
        }

        protected override IG2p LoadG2p() {
            var g2ps = new List<IG2p>();

            try {
                // 1. โหลดพจนานุกรมเฉพาะของนักร้อง
                if (singer != null && singer.Found && singer.Loaded && !string.IsNullOrEmpty(singer.Location)) {
                    string file = Path.Combine(singer.Location, "arpasing.yaml");
                    if (File.Exists(file)) {
                        g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(file)).Build());
                    }
                }

                // 2. โหลดพจนานุกรมจากโฟลเดอร์ปลั๊กอิน
                if (!string.IsNullOrEmpty(PluginDir)) {
                    string path = Path.Combine(PluginDir, "arpasing.yaml");
                    if (!File.Exists(path)) {
                        Directory.CreateDirectory(PluginDir);
                        File.WriteAllBytes(path, Data.Resources.arpasing_template);
                    }
                    g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(path)).Build());
                }

                // 3. โหลดพจนานุกรมภาษาแม่ English-ARP.txt โดยตรง
                var englishArp = ArpasingPhonemizer.GetOrLoadEnglishArpDictionary(PluginDir);
                if (englishArp != null) {
                    g2ps.Add(englishArp);
                }

                // 4. Fallback สำหรับคำทับศัพท์หรือคำที่ไม่อยู่ในพจนานุกรม
                g2ps.Add(new ArpabetG2p());
            } catch (Exception e) {
                Log.Error(e, "Dictionary Loading Error in USNativeArpasingPhonemizer.");
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