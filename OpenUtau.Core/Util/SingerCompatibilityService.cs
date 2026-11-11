// ============================================================================
// 1. วัน/เดือน/ปี: 11 พฤศจิกายน พ.ศ. 2569 / 11 November 2026
// 2. ชื่อไฟล์โค้ด: SingerCompatibilityService.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.0
// 4. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
// 5. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Core/Util/SingerCompatibilityService.cs
// 6. รายละเอียดการแก้ไข: บริการตรวจหาระบบเสียงที่รองรับ (Compatible Singing Systems) และวิเคราะห์แนะนำ Phonemizer ที่เหมาะสมที่สุดสำหรับนักร้องแต่ละราย
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Util {
    public static class SingerCompatibilityService {
        /// <summary>
        /// ตรวจหาว่านักร้องคลังเสียงที่กำหนดรองรับการร้องในระบบใดได้บ้าง (Compatible Singing Systems)
        /// เช่น UTAU VCV, UTAU CVVC, UTAU VCCV, DiffSinger AI, Arpasing ฯลฯ
        /// </summary>
        public static List<string> GetCompatibleSingingSystems(USinger? singer) {
            var systems = new List<string>();
            if (singer == null || !singer.Found) {
                systems.Add("ไม่พบคลังเสียง (Missing Voicebank)");
                return systems;
            }

            // 1. ระบบ DiffSinger AI
            if (singer.SingerType == USingerType.DiffSinger) {
                systems.Add("DiffSinger AI");
                return systems;
            }

            // 2. ระบบ Enunu / NNSVS
            if (singer.SingerType == USingerType.Enunu) {
                systems.Add("Enunu / NNSVS");
                return systems;
            }

            // 3. ระบบ Voicevox
            if (singer.SingerType == USingerType.Voicevox) {
                systems.Add("Voicevox");
                return systems;
            }

            // 4. นักร้องคลาสสิก UTAU (ClassicSinger)
            if (singer is ClassicSinger classic) {
                var otos = classic.Otos;
                if (otos != null && otos.Count > 0) {
                    bool hasVcv = false;
                    bool hasCvvc = false;
                    bool hasVccv = false;
                    bool hasCv = false;
                    bool hasArpa = false;
                    bool hasThai = false;

                    // สุ่มตัวอย่าง alias สูงสุด 500 รายการเพื่อประสิทธิภาพสูงสุด
                    int step = Math.Max(1, otos.Count / 500);
                    for (int i = 0; i < otos.Count; i += step) {
                        string alias = otos[i].Alias;
                        if (string.IsNullOrEmpty(alias)) continue;

                        if (!hasThai && (alias.Any(c => c >= 0x0E00 && c <= 0x0E7F) ||
                            classic.Location?.Contains("Thai", StringComparison.OrdinalIgnoreCase) == true ||
                            classic.Name?.Contains("Thai", StringComparison.OrdinalIgnoreCase) == true)) {
                            hasThai = true;
                        }

                        // ตรวจ VCV: มีสระนำหน้า เช่น "a か", "- か", "i ki", "- a"
                        if (!hasVcv && (alias.StartsWith("- ") || alias.StartsWith("a ") || alias.StartsWith("i ") ||
                                       alias.StartsWith("u ") || alias.StartsWith("e ") || alias.StartsWith("o ") ||
                                       alias.StartsWith("n "))) {
                            hasVcv = true;
                        }

                        // ตรวจ CVVC: มีตัวสะกดเชื่อม เช่น "a k", "a n", "k a", "a -"
                        if (!hasCvvc && (alias.Contains(" -") || alias.EndsWith(" -") ||
                                        (alias.Length >= 3 && alias[1] == ' ' && !alias.StartsWith("- ")))) {
                            hasCvvc = true;
                        }

                        // ตรวจ VCCV: มีควบกล้ำหรือตัวสะกดผสม เช่น "CC", "CCV", "- tr"
                        if (!hasVccv && (alias.Contains("tr") || alias.Contains("kr") || alias.Contains("pr") ||
                                        alias.Contains("br") || alias.Contains("pl") || alias.Contains("kl") ||
                                        alias.Contains("kw") || alias.StartsWith("- k") || alias.StartsWith("- t"))) {
                            hasVccv = true;
                        }

                        // ตรวจ CV: ตัวอักษรเดี่ยวๆ หรือ คะตะคะนะ/ฮิระงะนะเดี่ยว
                        if (!hasCv && (alias.Length <= 2 || alias.Any(c => (c >= 0x3040 && c <= 0x309F) || (c >= 0x30A0 && c <= 0x30FF)))) {
                            if (!alias.Contains(" ") && !alias.Contains("-")) {
                                hasCv = true;
                            }
                        }

                        // ตรวจ Arpasing: รูปแบบ [k ae] หรือ [- k]
                        if (!hasArpa && ((alias.StartsWith("[-") || alias.EndsWith("-]") || alias.Contains(" ae") || alias.Contains(" ey")))) {
                            hasArpa = true;
                        }
                    }

                    if (hasThai) {
                        systems.Add("Thai VCCV / CVVC");
                    }
                    if (hasVcv || classic.Name.Contains("VCV", StringComparison.OrdinalIgnoreCase)) {
                        systems.Add("UTAU VCV");
                    }
                    if (hasCvvc || classic.Name.Contains("CVVC", StringComparison.OrdinalIgnoreCase)) {
                        systems.Add("UTAU CVVC");
                    }
                    if (hasVccv || classic.Name.Contains("VCCV", StringComparison.OrdinalIgnoreCase)) {
                        systems.Add("UTAU VCCV");
                    }
                    if (hasArpa || classic.Name.Contains("Arpasing", StringComparison.OrdinalIgnoreCase)) {
                        systems.Add("English Arpasing");
                    }
                    if (hasCv && !hasVcv) {
                        systems.Add("UTAU CV (Tansho)");
                    }
                }
            }

            if (systems.Count == 0) {
                if (singer.Name.Contains("VCV", StringComparison.OrdinalIgnoreCase)) {
                    systems.Add("UTAU VCV");
                } else if (singer.Name.Contains("CVVC", StringComparison.OrdinalIgnoreCase)) {
                    systems.Add("UTAU CVVC");
                } else if (singer.Name.Contains("VCCV", StringComparison.OrdinalIgnoreCase)) {
                    systems.Add("UTAU VCCV");
                } else {
                    systems.Add("UTAU Classic");
                }
            }

            return systems;
        }

        /// <summary>
        /// ดึงคำอธิบายสรุประบบเสียงที่รองรับเป็นข้อความสั้นกระชับ
        /// </summary>
        public static string GetCompatibleSystemsSummary(USinger? singer) {
            var list = GetCompatibleSingingSystems(singer);
            return string.Join(", ", list);
        }

        /// <summary>
        /// วิเคราะห์และแนะนำ Phonemizer ที่เหมาะสมที่สุดสำหรับนักร้องนี้
        /// </summary>
        public static string GetRecommendationDescription(USinger? singer, Phonemizer? currentPhonemizer) {
            if (singer == null || !singer.Found) {
                return "โปรดเลือกนักร้องสำหรับแทร็กนี้ (Please select a singer)";
            }

            var systems = GetCompatibleSingingSystems(singer);
            string systemsStr = string.Join(", ", systems);

            if (currentPhonemizer == null) {
                return $"ระบบที่รองรับ: {systemsStr}";
            }

            string pTag = currentPhonemizer.Tag ?? string.Empty;
            string pName = currentPhonemizer.ToString() ?? string.Empty;

            // ตรวจสอบความสอดคล้องระหว่าง Phonemizer และระบบคลังเสียง
            bool isMatching = false;
            string matchNote = string.Empty;

            if (singer.SingerType == USingerType.DiffSinger) {
                if (PhonemizerIsDiffSinger(currentPhonemizer)) {
                    isMatching = true;
                    matchNote = "ตรงตามระบบ DiffSinger AI 100%";
                } else {
                    matchNote = "คำเตือน: นักร้องเป็น DiffSinger แต่เลือก Phonemizer คลาสสิก";
                }
            } else if (systems.Contains("UTAU VCV")) {
                if (pTag.Contains("VCV", StringComparison.OrdinalIgnoreCase) || pName.Contains("VCV", StringComparison.OrdinalIgnoreCase)) {
                    isMatching = true;
                    matchNote = "ตรงตามระบบ VCV 100% (แนะนำสูงสุดสำหรับการร้องต่อเนื่อง)";
                } else if (pTag.Contains("CV", StringComparison.OrdinalIgnoreCase)) {
                    isMatching = true;
                    matchNote = "ใช้งานได้ (คลังเสียง VCV รองรับการร้องแบบ CV ได้)";
                } else {
                    matchNote = "แนะนำ: เลือก [JA VCV] เพื่อให้การร้องลื่นไหลสมบูรณ์ที่สุด";
                }
            } else if (systems.Contains("Thai VCCV / CVVC") || systems.Contains("UTAU VCCV")) {
                if (pTag.Contains("TH", StringComparison.OrdinalIgnoreCase) || pTag.Contains("VCCV", StringComparison.OrdinalIgnoreCase)) {
                    isMatching = true;
                    matchNote = "ตรงตามระบบ VCCV ภาษาไทย 100%";
                }
            } else if (systems.Contains("UTAU CVVC")) {
                if (pTag.Contains("CVVC", StringComparison.OrdinalIgnoreCase)) {
                    isMatching = true;
                    matchNote = "ตรงตามระบบ CVVC 100%";
                }
            } else if (systems.Contains("English Arpasing")) {
                if (pTag.Contains("ARPA", StringComparison.OrdinalIgnoreCase)) {
                    isMatching = true;
                    matchNote = "ตรงตามระบบ English Arpasing 100%";
                }
            }

            if (isMatching) {
                return $"ระบบที่รองรับ: {systemsStr}\n★ แนะนำ: สอดคล้องกับคลังเสียงนี้ ({matchNote})";
            } else if (!string.IsNullOrEmpty(matchNote)) {
                return $"ระบบที่รองรับ: {systemsStr}\n💡 {matchNote}";
            } else {
                return $"ระบบที่รองรับ: {systemsStr}";
            }
        }

        private static bool PhonemizerIsDiffSinger(Phonemizer p) {
            return p.GetType().FullName?.Contains("DiffSinger", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
