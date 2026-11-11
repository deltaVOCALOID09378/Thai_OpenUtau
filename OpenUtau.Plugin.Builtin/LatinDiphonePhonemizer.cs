// ============================================================================
// 1. วัน/เดือน/ปี: 10 พฤศจิกายน พ.ศ. 2569 / 10 November 2026
// 2. ชื่อไฟล์โค้ด: LatinDiphonePhonemizer.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.2
// 4. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Plugin.Builtin/LatinDiphonePhonemizer.cs
// 5. หน้าที่: คลาสแม่สำหรับ Diphone Phonemizer ตระกูลภาษาละตินและอังกฤษ (Arpasing)
//            รองรับการตรวจสอบ Oto Alias และระบบ Fallback สระอย่างปลอดภัย
// 6. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Agentic AI | Original by OpenUtau Contributors
// ============================================================================

#pragma warning disable CS0618, CS0649, CS8632, CS0108
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    /// <summary>
    /// A base diphone phonemizer for latin languages.
    /// </summary>
    public abstract class LatinDiphonePhonemizer : PhonemeBasedPhonemizer {
        protected override string GetPhonemeOrFallback(string prevSymbol, string symbol, int tone, string color, string alt) {
            if (singer != null) {
                if (!string.IsNullOrEmpty(alt) && singer.TryGetMappedOto($"{prevSymbol} {symbol}{alt}", tone, color, out var oto)) {
                    return oto.Alias;
                }
                if (singer.TryGetMappedOto($"{prevSymbol} {symbol}", tone, color, out var oto1)) {
                    return oto1.Alias;
                }
                if (vowelFallback != null && vowelFallback.TryGetValue(symbol, out string[]? fallbacks) && fallbacks != null) {
                    foreach (var fallback in fallbacks) {
                        if (singer.TryGetMappedOto($"{prevSymbol} {fallback}", tone, color, out var oto2)) {
                            return oto2.Alias;
                        }
                    }
                }
                if (singer.TryGetMappedOto($"- {symbol}", tone, color, out var oto3)) {
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
