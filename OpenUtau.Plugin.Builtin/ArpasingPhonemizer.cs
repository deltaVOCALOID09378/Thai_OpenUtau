#pragma warning disable CS0618, CS0649, CS8632, CS0108
// ============================================================================
// Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : ArpasingPhonemizer.cs
// Version      : 1.5
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Contributors (stakira) ]
// File Location: OpenUtau.Plugin.Builtin\ArpasingPhonemizer.cs
// Description  : Professional English Arpasing Phonemizer implementation with optimized
//                Consonant Space Widening and precise VC/CC alignment for Thai OpenUtau.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OpenUtau.Api;
using OpenUtau.Core.G2p;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
/// 
/// ตัวจัดการหน่วยเสียงภาษาอังกฤษแบบ Arpasing (The English Arpasing Phonemizer)
/// 
/// Arpasing คือระบบที่ใช้ CMUdict เป็นพจนานุกรมในการแปลงคำศัพท์ภาษาอังกฤษให้เป็นสัญลักษณ์หน่วยเสียง
/// อ้างอิงจาก: http://www.speech.cs.cmu.edu/cgi-bin/cmudict และ https://arpasing.neocities.org/en/faq.html
/// 
/// 
[Phonemizer("English Arpasing Phonemizer", "EN ARPA", language: "EN")]
public class ArpasingPhonemizer : LatinDiphonePhonemizer {

    // ชุดสระภาษาอังกฤษมาตรฐานของ Arpasing (Standard English Arpasing Vowels)
    private static readonly HashSet<string> vowels = new HashSet<string> {
        "aa", "ae", "ah", "ao", "aw", "ay", "eh", "er", "ey", "ih", "iy", "ow", "oy", "uh", "uw"
    };

    public ArpasingPhonemizer() {
        try {
            // [DELTA SYNTH] ปรับความกว้างของพยัญชนะ (ConsonantLength) เป็น 40 ticks 
            // เพื่อให้เสียงพยัญชนำมีพื้นที่ในการออกเสียงที่ชัดเจนเบื้องต้น
            ConsonantLength = 40; 
            Initialize();
        } catch (Exception e) {
            Log.Error(e, "เกิดข้อผิดพลาดในการเริ่มต้นระบบ (Failed to initialize).");
        }
    }

    protected override IG2p LoadG2p() {
        var g2ps = new List<IG2p>();

        // โหลดพจนานุกรมจากโฟลเดอร์ปลั๊กอิน (Load dictionary from plugin folder)
        string path = Path.Combine(PluginDir, "arpasing.yaml");
        if (!File.Exists(path)) {
            Directory.CreateDirectory(PluginDir);
            File.WriteAllBytes(path, Data.Resources.arpasing_template);
        }
        g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(path)).Build());

        // โหลดพจนานุกรมจากโฟลเดอร์ของนักร้อง (Load dictionary from singer folder)
        if (singer != null && singer.Found && singer.Loaded) {
            string file = Path.Combine(singer.Location, "arpasing.yaml");
            if (File.Exists(file)) {
                try {
                    g2ps.Add(G2pDictionary.NewBuilder().Load(File.ReadAllText(file)).Build());
                } catch (Exception e) {
                    Log.Error(e, $"ไม่สามารถโหลดไฟล์ได้ (Failed to load): {file}");
                }
            }
        }

        // โหลดระบบ G2P พื้นฐาน (Load base G2P)
        g2ps.Add(new ArpabetG2p());

        return new G2pFallbacks(g2ps.ToArray());
    }

    protected override Dictionary<string, string[]> LoadVowelFallbacks() {
        return "aa=ah,ae;ae=ah,aa;ah=aa,ae;ao=ow;ow=ao;eh=ae;ih=iy;iy=ih;uh=uw;uw=uh;aw=ao".Split(';')
            .Select(entry => entry.Split('='))
            .ToDictionary(parts => parts[0], parts => parts[1].Split(','));
    }

    protected override string GetPhonemeOrFallback(string prevSymbol, string symbol, int tone, string color, string alt) {
        if (g2p != null && g2p.IsVowel(prevSymbol) && g2p.IsVowel(symbol)) {
            prevSymbol = "-";
        }
        if (!string.IsNullOrEmpty(alt) && singer.TryGetMappedOto($"{prevSymbol} {symbol}{alt}", tone, color, out var oto)) {
            return oto.Alias;
        }
        if (singer.TryGetMappedOto($"{prevSymbol} {symbol}", tone, color, out var oto1)) {
            return oto1.Alias;
        }
        if (vowelFallback.TryGetValue(symbol, out string[] fallbacks)) {
            foreach (var fallback in fallbacks) {
                if (singer.TryGetMappedOto($"{prevSymbol} {fallback}", tone, color, out var oto2)) {
                    return oto2.Alias;
                }
            }
        }
        // ใช้ค่าสำรอง "- symbol" เฉพาะเมื่อเริ่มต้นวลี (prevSymbol == "-") เท่านั้น
        if (prevSymbol == "-" && singer.TryGetMappedOto($"- {symbol}", tone, color, out var oto3)) {
            return oto3.Alias;
        }
        return $"{prevSymbol} {symbol}{alt}";
    }

    // ============================================================================
    // [DELTA SYNTH] CUSTOM PHONEME ALIGNMENT FOR ENGLISH ARPASING
    // Override the base Process method เพื่อปรับแต่งการจัดสรรพื้นที่พยัญชนะแบบละเอียด 100%
    // ============================================================================
    public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevNeighbours) {
        // เรียกใช้ประมวลผลพื้นฐาน (Call base processing to get initial alignment)
        var result = base.Process(notes, prev, next, prevNeighbour, nextNeighbour, prevNeighbours);
        if (result.phonemes == null || result.phonemes.Length == 0) return result;

        int noteDur = notes.Sum(n => n.duration);
        
        // กฎระยะห่างของ DELTA SYNTH (DELTA SYNTH Spacing Rules)
        int cvMaxBack = (int)(noteDur * 0.05);     // แผ่ขยาย CV ไปที่โน้ตตัวก่อนหน้าสูงสุดไม่เกิน 5% (Max CV back-expansion <= 5%)
        int vcStartPos = (int)(noteDur * 0.65);    // ตัวสะกด VC ให้จัดสรรพื้นที่ขั้นต่ำ 35% ดังนั้นต้องเริ่มที่ตำแหน่งไม่เกิน 65% ของโน้ต
        int maxGap = (int)(noteDur * 0.05);        // ช่องว่างพยัญชนะชนพยัญชนะ (CC, C_C) กว้างสูงสุดไม่เกิน 5% (Max cluster gap <= 5%)

        for (int i = 0; i < result.phonemes.Length; i++) {
            var phoneme = result.phonemes[i];
            string alias = phoneme.phoneme ?? "";
            var parts = alias.Split(' ');

            // ตรวจสอบโครงสร้างคู่เสียง Arpasing ปกติ ("X Y")
            if (parts.Length == 2) {
                // กำจัดตัวเลขวรรณยุกต์ถ้ามีหลงมา (Strip numerical characters to match vowel list)
                string p1 = Regex.Replace(parts[0], @"\d+", "");
                string p2 = Regex.Replace(parts[1], @"\d+", "");
                
                bool isV1 = vowels.Contains(p1);
                bool isV2 = vowels.Contains(p2);
                
                if (p1 == "-" && !isV2) {
                    // รูปแบบ [- C] : เริ่มต้นพยัญชนะ
                    phoneme.position = Math.Max(phoneme.position, -cvMaxBack * 2);
                } 
                else if (!isV1 && isV2) {
                    // รูปแบบ [C V] : กลุ่มพยัญชนะต้น (CV)
                    // แผ่ขยายพื้นที่เสียงกลับไปโน้ตก่อนหน้าเล็กน้อย แต่ห้ามเกิน 5%
                    phoneme.position = Math.Max(phoneme.position, -cvMaxBack);
                } 
                else if (isV1 && !isV2 && p2 != "-") {
                    // รูปแบบ [V C] : สระชนตัวสะกด (VC)
                    // บังคับให้เริ่มที่ตำแหน่งไม่เกิน 65% เพื่อรับประกันพื้นที่ของ VC ขั้นต่ำ 35% 
                    if (phoneme.position > vcStartPos) {
                        phoneme.position = vcStartPos;
                    }
                } 
                else if (!isV1 && !isV2 && p1 != "-" && p2 != "-") {
                    // รูปแบบ [C C] : พยัญชนะชนพยัญชนะ (CC, C_C, R_C, CCV)
                    // รักษาระยะห่างให้แคบเพื่อความกระชับ เว้นกว้างไม่เกิน 5%
                    if (i > 0 && phoneme.position > result.phonemes[i-1].position + maxGap) {
                        phoneme.position = result.phonemes[i-1].position + maxGap;
                    }
                }
                else if (!isV1 && p2 == "-") {
                    // รูปแบบ [C -] : สิ้นสุดพยัญชนะ
                    // รักษาระยะห่างให้แคบ เว้นกว้างไม่เกิน 5% จากตัวก่อนหน้า
                    if (i > 0 && phoneme.position > result.phonemes[i-1].position + maxGap) {
                        phoneme.position = result.phonemes[i-1].position + maxGap;
                    }
                }
            }
            
            // ระบบป้องกันเสียงผีสิง (Safety Position Buffer: Prevent overlapping bug)
            if (i > 0 && phoneme.position <= result.phonemes[i-1].position) {
                phoneme.position = result.phonemes[i-1].position + 10;
            }
            
            // ควบคุมไม่ให้หลุดขอบของตัวโน้ต (Boundary Guard)
            phoneme.position = Math.Min(phoneme.position, noteDur - 10);
            
            result.phonemes[i] = phoneme;
        }

        return result;
    }
}


}