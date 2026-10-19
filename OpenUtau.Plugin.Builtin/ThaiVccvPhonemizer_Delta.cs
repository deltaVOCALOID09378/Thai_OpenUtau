using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core;
using OpenUtau.Core.G2p;
using OpenUtau.Core.Ustx;
using Serilog;

// ============================================================================
// Made And Checked By DELTA SYNTH & All Agentic AI
// Original by StAkira, Ferina and Printmov
// Version: 6.1 (Zero-Defect Pure Vowels & Advanced R-Vowels Edition)
// Description: The most complete and stable Thai VCCV Phonemizer system.
// อัปเดตล่าสุด: เพิ่มการรองรับกลุ่มคำ ฤ ฤา (ฤาษี, ฤทธิ์, กฤษณ์) ลงใน Internal Dictionary
// ============================================================================

namespace OpenUtau.Plugin.Builtin {
    
    [Phonemizer("Thai VCCV Phonemizer", "TH VCCV", "DELTA SYNTH", "TH")]
    public class ThaiVccvPhonemizer : Phonemizer {
        
        // =======================================================
        // 1. STRICT THAI VCCV DICTIONARY MAPPING (PURE VOWELS)
        // =======================================================
        
        private static readonly string[] ThaiConsonants = { 
            "b", "ch", "d", "f", "h", "j", "k", "kh", "l", "m", 
            "n", "g", "p", "ph", "r", "s", "t", "th", "w", "y", "-" 
        };
        
        // สระ (Vowel) - Pure All-in-1 (ถอด am, aw, I, ay ออกทั้งหมด)
        private static readonly string[] ThaiVowels = { 
            "a", "i", "u", "e", "o", "@", "Q", "3", "6", "1", 
            "ia", "ua" 
        };
        
        // ตัวสะกด (Consonant ending)
        private static readonly string[] ThaiEndings = { 
            "n", "m", "y", "w", "g", "k", "b", "d" 
        };

        // ฐานข้อมูลคำศัพท์พิเศษแบบฝังตัว (อัปเดตสระเป็น Pure Vowels + Endings)
        private static readonly Dictionary<string, string[]> SpecialWords = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
            {"ขวัญ", new[] {"kh", "w", "a", "n"}},
            {"ก็", new[] {"k", "Q"}},
            {"ธ", new[] {"th", "a"}},
            {"ณ", new[] {"n", "a"}},
            {"สถิตย์", new[] {"s", "a", "th", "i", "d"}},
            {"อยู่", new[] {"y", "u"}},
            {"จักร", new[] {"j", "a", "k"}},
            {"วร", new[] {"w", "Q", "n"}},
            {"วราพร", new[] {"w", "a", "r", "a", "ph", "Q", "n"}},
            {"กฤษณ์", new[] {"k", "r", "i", "d"}},
            {"ฤาษี", new[] {"r", "1", "s", "i"}},
            {"ฤทธิ์", new[] {"r", "i", "d"}},
            {"คอนเฟิร์ม", new[] {"kh", "Q", "n", "f", "3", "m"}},
            {"เพลีย", new[] {"ph", "l", "ia"}},
            {"เจาะ", new[] {"j", "Q"}},
            {"ข่าว", new[] {"kh", "a", "w"}},
            {"ตื้น", new[] {"t", "1", "n"}},
            {"หมาย", new[] {"m", "a", "y"}},
            {"เอื้อม", new[] {"-", "6", "m"}},
            {"เปลี่ยน", new[] {"p", "l", "ia", "n"}},
            {"ไป", new[] {"p", "a", "y"}},
            {"ใคร", new[] {"kh", "r", "a", "y"}},
            {"มัน", new[] {"m", "a", "n"}},
            {"การ", new[] {"k", "a", "n"}},
            {"ครั้ง", new[] {"kh", "r", "a", "g"}},
            {"คือ", new[] {"kh", "1"}},
            {"ผี", new[] {"ph", "i"}},
            {"เกียรติยศ", new[] {"k", "ia", "d", "t", "i", "y", "o", "d"}},
            {"ทน", new[] {"th", "o", "n"}},
            {"ทุน", new[] {"th", "u", "n"}},
            {"ครู", new[] {"kh", "r", "u"}},
            {"เคร่ง", new[] {"kh", "r", "e", "g"}},
            {"แปร", new[] {"p", "r", "@"}},
            {"แสม", new[] {"s", "a", "m", "@"}},
            {"แสบ", new[] {"s", "@", "b"}},
            {"เฟิร์น", new[] {"f", "3", "n"}},
            {"สาย", new[] {"s", "a", "y"}},
            {"กวน", new[] {"k", "ua", "n"}},
            {"คลาย", new[] {"kh", "l", "a", "y"}},
            {"เสือ", new[] {"s", "6"}},
            {"อำ", new[] {"-", "a", "m"}},
            {"สาม", new[] {"s", "a", "m"}},
            {"ฤกษ์", new[] {"r", "3", "k"}},
            {"แต่ง", new[] {"t", "@", "g"}},
            {"ออม", new[] {"-", "Q", "m"}},
            {"เทอม", new[] {"th", "3", "m"}},
            {"แปลง", new[] {"p", "l", "@", "g"}},
            {"ลำ", new[] {"l", "a", "m"}},
            {"คราม", new[] {"kh", "r", "a", "m"}},
            {"กระ", new[] {"k", "r", "a"}},
            {"ปะ", new[] {"p", "a"}},
            {"กัด", new[] {"k", "a", "d"}},
            {"ปราถนา", new[] {"p", "r", "a", "d", "th", "a", "n", "a"}},
            {"ปรารภ", new[] {"p", "r", "a", "r", "o", "b"}},
            {"พิศวง", new[] {"ph", "i", "d", "s", "a", "w", "o", "g"}},
            {"คมนาคม", new[] {"kh", "o", "m", "m", "a", "n", "a", "kh", "o", "m"}},
            {"สรรพสินค้า", new[] {"s", "a", "b", "ph", "a", "s", "i", "n", "kh", "a"}},
            {"พรรค", new[] {"ph", "a", "k"}},
            {"ธรรม", new[] {"th", "a", "m"}},
            {"บรรยากาศ", new[] {"b", "a", "n", "y", "a", "k", "a", "d"}},
            {"ครรภ์", new[] {"kh", "a", "n"}},
            {"หนู", new[] {"n", "u"}},
            {"หวาน", new[] {"w", "a", "n"}},
            {"แล้ว", new[] {"l", "@", "w"}},
            {"กวาง", new[] {"k", "w", "a", "g"}},
            {"สวม", new[] {"s", "ua", "m"}},
            {"ได้", new[] {"d", "a", "y"}},
            {"ประมาณ", new[] {"p", "r", "a", "m", "a", "n"}},
            {"เดือน", new[] {"d", "6", "n"}},
            {"เกี่ยว", new[] {"k", "ia", "w"}},
            {"กับ", new[] {"k", "a", "b"}},
            {"ครับ", new[] {"kh", "r", "a", "b"}},
            {"เก่า", new[] {"k", "a", "w"}},
            {"เหล่า", new[] {"l", "a", "w"}},
            {"เปล่งปลั่ง", new[] {"p", "l", "e", "g", "p", "l", "a", "g"}},
            {"สาว", new[] {"s", "a", "w"}},
            {"พลาด", new[] {"ph", "l", "a", "d"}},
            {"แหงน", new[] {"g", "@", "n"}},
            {"สัก", new[] {"s", "a", "k"}},
            {"แคว้น", new[] {"kh", "w", "@", "n"}},
            {"มอง", new[] {"m", "Q", "g"}},
            {"ต่าง", new[] {"t", "a", "g"}},
            {"ประเทศ", new[] {"p", "r", "a", "th", "e", "d"}},
            {"เเดง", new[] {"d", "@", "g"}},
            {"ดำน้ำเงิน", new[] {"d", "a", "m", "n", "a", "m", "g", "3", "n"}},
            {"ดำ", new[] {"d", "a", "m"}},
            {"น้ำ", new[] {"n", "a", "m"}},
            {"เงิน", new[] {"g", "3", "n"}},
            {"เกียร์", new[] {"k", "ia"}},
            {"หวั่น", new[] {"w", "a", "n"}},
            {"หยาบ", new[] {"y", "a", "b"}},
            {"หวัง", new[] {"w", "a", "g"}},
            {"ปราบ", new[] {"p", "r", "a", "b"}},
            {"พวง", new[] {"ph", "ua", "g"}},
            {"พุ่ม", new[] {"ph", "u", "m"}},
            {"กลุ่ม", new[] {"k", "l", "u", "m"}},
            {"กาย", new[] {"k", "a", "y"}},
            {"พลาย", new[] {"ph", "l", "a", "y"}},
            {"เบื้อง", new[] {"b", "6", "g"}},
            {"แสร้ง", new[] {"s", "@", "g"}},
            {"เศร้า", new[] {"s", "a", "w"}},
            {"หมองคล้ำ", new[] {"m", "Q", "g", "kh", "l", "a", "m"}},
            {"หมอง", new[] {"m", "Q", "g"}},
            {"คล้ำ", new[] {"kh", "l", "a", "m"}},
            {"สัม", new[] {"s", "a", "m"}},
            {"ภาษณ์", new[] {"ph", "a", "d"}},
            {"สริวัฒน์", new[] {"s", "a", "r", "i", "w", "a", "d"}},
            {"สริ", new[] {"s", "a", "r", "i"}},
            {"วัฒน์", new[] {"w", "a", "d"}},
            {"ที่", new[] {"th", "i"}},
            {"สุด", new[] {"s", "u", "d"}},
            {"ครุฑ", new[] {"kh", "r", "u", "d"}},
            {"เหลือ", new[] {"l", "6"}},
            {"เครื่อง", new[] {"kh", "r", "6", "g"}}
        };
        
        private IG2p? thaiDict;
        private USinger? currentSinger;

        public ThaiVccvPhonemizer() {
            try {
                InitializeDictionary();
            } catch (Exception e) {
                Log.Error(e, "[DELTA SYNTH] Failed to initialize Thai VCCV Dictionary.");
            }
        }

        private void InitializeDictionary() {
            var builder = G2pDictionary.NewBuilder();
            string dictPath = Path.Combine(PathManager.Inst.DictionariesPath, "thai_dict.txt");
            
            if (File.Exists(dictPath)) {
                string[] dictLines = File.ReadAllLines(dictPath);
                foreach (var line in dictLines) {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";;;")) continue;
                    
                    var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2) {
                        string grapheme = parts[0];
                        string[] phones = ApplyPrintmovEndingRules(parts.Skip(1).ToArray());
                        builder.AddEntry(grapheme, phones);
                    }
                }
                Log.Information("[DELTA SYNTH] Thai VCCV Dictionary loaded successfully.");
            } else {
                Log.Warning($"[DELTA SYNTH] Dictionary not found at: {dictPath}. Fallback to basic mapping.");
            }
            thaiDict = builder.Build();
        }

        /// <summary>
        /// กฎ Printmov: ปรับมาตราตัวสะกดให้เข้ากับรูปแบบ VCCV มาตรฐานอย่างเข้มงวด
        /// Standardizes ending consonants to strict VCCV formats.
        /// </summary>
        private string[] ApplyPrintmovEndingRules(string[] originalPhones) {
            if (originalPhones.Length == 0) return originalPhones;

            string[] newPhones = (string[])originalPhones.Clone();
            int lastIndex = newPhones.Length - 1;
            string finalPhone = newPhones[lastIndex];

            if (!ThaiVowels.Contains(finalPhone) && !ThaiEndings.Contains(finalPhone)) {
                switch (finalPhone) {
                    case "l": case "r": newPhones[lastIndex] = "n"; break; 
                    case "s": case "ch": case "c": case "th": case "t": newPhones[lastIndex] = "d"; break; 
                    case "f": case "ph": case "p": newPhones[lastIndex] = "b"; break; 
                    case "kh": newPhones[lastIndex] = "k"; break; 
                }
            }
            return newPhones;
        }

        private string[] GetPhonemes(string lyric) {
            // Priority 1: Check Internal Custom Dictionary for Special Words (Zero-Defect Base)
            if (SpecialWords.TryGetValue(lyric, out var customPhones)) {
                return customPhones;
            }
            
            // Priority 2: Query Standard Thai Dictionary
            string[] rawPhones;
            if (thaiDict != null && thaiDict.Query(lyric) is string[] dictResult && dictResult.Length > 0) {
                rawPhones = dictResult;
            } else {
                rawPhones = lyric.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }

            // Priority 3: Interceptor Rule - แยกสระประสมเป็น a + ตัวสะกด อัตโนมัติ (Sanitization)
            var parsedList = new List<string>();
            foreach (var p in rawPhones) {
                if (p.Equals("am", StringComparison.OrdinalIgnoreCase)) { 
                    parsedList.Add("a"); parsedList.Add("m"); 
                }
                else if (p.Equals("aw", StringComparison.OrdinalIgnoreCase)) { 
                    parsedList.Add("a"); parsedList.Add("w"); 
                }
                else if (p.Equals("ay", StringComparison.OrdinalIgnoreCase) || p == "I") { 
                    parsedList.Add("a"); parsedList.Add("y"); 
                }
                else { 
                    parsedList.Add(p); 
                }
            }

            return parsedList.ToArray();
        }

        // =======================================================
        // 2. PHONEMIZER LIFECYCLE & PROCESSING
        // =======================================================
        
        public override void SetSinger(USinger singer) {
            currentSinger = singer;
        }

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
            var note = notes[0];
            string lyric = note.lyric;
            
            if (string.IsNullOrEmpty(lyric) || lyric == "-" || lyric.Equals("r", StringComparison.OrdinalIgnoreCase) || lyric.Equals("br", StringComparison.OrdinalIgnoreCase)) {
                return MakeSimpleResult(lyric);
            }

            string[] phones = GetPhonemes(lyric);
            if (phones.Length == 0) {
                return MakeSimpleResult(lyric);
            }

            var phonemes = new List<Phoneme>();
            
            // =======================================================
            // 3. VCCV ALIAS GENERATION & TIMING DISTRIBUTION
            // =======================================================
            
            string vowel = phones.LastOrDefault(p => ThaiVowels.Contains(p)) ?? phones.Last();
            List<string> consonants = phones.TakeWhile(p => !ThaiVowels.Contains(p)).ToList();
            string endingConsonant = phones.Length > consonants.Count + 1 ? phones.Last() : "";

            // 3.1: Starting Transition ([- CV] or [V C])
            string startAlias = "";
            if (prevNeighbour == null) {
                startAlias = consonants.Count > 0 ? $"- {consonants[0]}" : $"- {vowel}";
            } else {
                string prevLyric = prevNeighbour.Value.lyric;
                string[] prevPhones = GetPhonemes(prevLyric);
                string prevVowel = prevPhones.LastOrDefault(p => ThaiVowels.Contains(p)) ?? "a"; 
                startAlias = consonants.Count > 0 ? $"{prevVowel} {consonants[0]}" : $"{prevVowel} {vowel}";
            }

            if (currentSinger != null) {
                string color = "";
                string alt = "";
                
                int preUtterancePosition = timeAxis != null ? -timeAxis.MsPosToTickPos(60) : -MsToTick(60);

                phonemes.Add(new Phoneme {
                    phoneme = MapPhoneme(startAlias, note.tone, color, alt, currentSinger),
                    position = preUtterancePosition
                });

                // 3.2: Main Vowel Phoneme (CV)
                string mainAlias = consonants.Count > 0 ? $"{consonants.Last()}{vowel}" : vowel;
                phonemes.Add(new Phoneme {
                    phoneme = MapPhoneme(mainAlias, note.tone, color, alt, currentSinger),
                    position = 0 
                });

                // 3.3: Ending Consonant (VC)
                if (!string.IsNullOrEmpty(endingConsonant) && endingConsonant != vowel && ThaiEndings.Contains(endingConsonant)) {
                    string endAlias = $"{vowel} {endingConsonant}";
                    int tailOffset = timeAxis != null ? timeAxis.MsPosToTickPos(80) : MsToTick(80);
                    int tailPosition = Math.Max(0, note.duration - tailOffset); 
                    
                    phonemes.Add(new Phoneme {
                        phoneme = MapPhoneme(endAlias, note.tone, color, alt, currentSinger),
                        position = tailPosition
                    });
                }
                
                // 3.4: Automated Vocal Tuning Enhancements
                double stretchRatio = 1.0 * 1.8; 
                for (int i = 0; i < phonemes.Count; i++) {
                    var p = phonemes[i];
                    p.expressions = new List<PhonemeExpression> {
                        new PhonemeExpression { abbr = "vel", value = (float)(100 / stretchRatio) },
                        new PhonemeExpression { abbr = "vib", value = 75f } 
                    };
                    phonemes[i] = p;
                }
            }

            return new Result {
                phonemes = phonemes.ToArray()
            };
        }
    }
}