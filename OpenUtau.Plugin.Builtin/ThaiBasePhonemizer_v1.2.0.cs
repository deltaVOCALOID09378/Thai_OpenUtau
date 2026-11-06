// ============================================================================
// 1. วัน/เดือน/ปี: 6 พฤศจิกายน พ.ศ. 2569 / 6 November 2026
// 2. ชื่อไฟล์โค้ด: ThaiBasePhonemizer_v1.2.0.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.5
// 4. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by Patiphat Wongyai (Delta) ]
// 5. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Plugin.Builtin/ThaiBasePhonemizer_v1.2.0.cs
// 6. รายละเอียดการแก้ไข: ป้องกันหน่วยเสียงชนกันด้วย Monotonic Spacing และเกลี่ยตัวสะกดหลายตัวให้กระจายอย่างสม่ำเสมอ
// ============================================================================

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin
{
    // คลาสสำหรับรับค่าจาก YAML
    public class ThaiTimingConfig
    {
        public Dictionary<string, double> timing_ratios { get; set; }
        public Dictionary<string, List<string>> rules { get; set; }
    }

    public class ThaiPhonemeTimingManager
    {
        private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        private ThaiTimingConfig _config;

        // ฟังก์ชันโหลดไฟล์ YAML
        public void LoadConfig(string yamlFilePath)
        {
            if (!File.Exists(yamlFilePath))
            {
                _config = new ThaiTimingConfig { 
                    timing_ratios = new Dictionary<string, double>(), 
                    rules = new Dictionary<string, List<string>>() 
                };
                return;
            }

            string yamlContent = File.ReadAllText(yamlFilePath);
            _config = YamlDeserializer.Deserialize<ThaiTimingConfig>(yamlContent) ?? new ThaiTimingConfig();
            if (_config.timing_ratios == null) _config.timing_ratios = new Dictionary<string, double>();
            if (_config.rules == null) _config.rules = new Dictionary<string, List<string>>();
        }

        // ชุดเสียงสระที่รู้จักจากทุก Phonemizer (Arpasing + VCCV/VCV/C+V)
        private static readonly HashSet<string> KnownVowelPhonemes = new HashSet<string> {
            // Arpasing
            "ah", "ih", "uh", "eh", "oh", "ae", "ao", "er", "ue", "eu",
            "ia", "ua", "ay", "aw", "am",
            // VCCV / VCV / C+V
            "a", "i", "u", "e", "o", "@", "Q", "3", "6", "1",
            "ay", "aw", "am", "I", "8"
        };

        // ฟังก์ชันคำนวณและหั่นเสียงตามกฎ (ใช้ position แทน duration ตาม OpenUtau API)
        public void ApplyTimingRules(OpenUtau.Api.Phonemizer.Note note, List<OpenUtau.Api.Phonemizer.Phoneme> phonemes, bool isShortVowel)
        {
            if (_config == null || phonemes.Count == 0) return;

            int totalDuration = note.duration;
            int vowelIdx = GetVowelPosition(phonemes);

            // 1. จัดการกลุ่มพยัญชนะต้น (Leading Consonants)
            var leadingConsonants = phonemes.Take(vowelIdx).ToList();
            double consonantRatio = 0;
            if (leadingConsonants.Count == 1)
            {
                consonantRatio = _config.timing_ratios.ContainsKey("single_consonant")
                    ? _config.timing_ratios["single_consonant"] : 0.10;
            }
            else if (leadingConsonants.Count > 1)
            {
                // บังคับ: คำควบกล้ำ พื้นที่รวมไม่เกิน 5%
                double rawCluster = _config.timing_ratios.ContainsKey("cluster_consonant")
                    ? _config.timing_ratios["cluster_consonant"] : 0.10;
                consonantRatio = Math.Min(rawCluster, 0.05);
            }

            // 2. จัดการกลุ่มตัวสะกด (Final Consonants)
            var finalConsonants = phonemes.Skip(vowelIdx + 1).Where(p => IsConsonant(p.phoneme)).ToList();
            double finalConsonantRatio = 0;
            if (finalConsonants.Count > 0)
            {
                finalConsonantRatio = isShortVowel
                    ? (_config.timing_ratios.ContainsKey("final_consonant_short_vowel") ? _config.timing_ratios["final_consonant_short_vowel"] : 0.15)
                    : (_config.timing_ratios.ContainsKey("final_consonant_long_vowel")  ? _config.timing_ratios["final_consonant_long_vowel"]  : 0.20);
            }

            // 3. คำนวณ position ของแต่ละหน่วยเสียง
            int clusterArea  = (int)(totalDuration * consonantRatio);
            int finalArea    = (int)(totalDuration * finalConsonantRatio);
            int vowelStart   = clusterArea;
            int finalStart   = Math.Max(vowelStart + 10, totalDuration - finalArea);

            var excludeSet = (_config.rules != null && _config.rules.ContainsKey("exclude_from_note"))
                ? _config.rules["exclude_from_note"] : new List<string>();

            for (int i = 0; i < phonemes.Count; i++)
            {
                var p = phonemes[i];
                if (excludeSet.Contains(p.phoneme))
                {
                    // หน่วยเสียงพิเศษ: ยึดติดกับจุดเริ่มต้นโน้ต ไม่กินพื้นที่
                    p.position = 0;
                    phonemes[i] = p;
                    continue;
                }

                if (i < vowelIdx)
                {
                    // พยัญชนะต้น — กระจายอย่างสม่ำเสมอในพื้นที่ cluster
                    p.position = leadingConsonants.Count > 1
                        ? (int)(clusterArea * i / (double)leadingConsonants.Count)
                        : 0;
                }
                else if (i == vowelIdx)
                {
                    // สระ — เริ่มต้นหลังพื้นที่พยัญชนะต้น
                    p.position = vowelStart;
                }
                else
                {
                    // ตัวสะกด — กระจายอย่างเป็นระเบียบในช่วงท้ายของโน้ต ไม่ให้ชนกันที่ tick เดียวกัน
                    int finalIdx = i - vowelIdx - 1;
                    int subFinalOffset = (finalConsonants.Count > 1)
                        ? (int)(finalArea * finalIdx / (double)finalConsonants.Count)
                        : 0;
                    p.position = finalStart + subFinalOffset;
                }
                // Safety: ป้องกันการชนกันของหน่วยเสียง (Monotonic Spacing >= 10 ticks) และห้าม position ทะลุขอบโน้ต
                int prevPos = (i > 0) ? phonemes[i - 1].position : -120;
                p.position = Math.Max(p.position, prevPos + 10);
                if (p.position > totalDuration - 10)
                {
                    p.position = Math.Max(prevPos + 10, totalDuration - 10);
                }
                phonemes[i] = p;
            }
        }

        // ตรวจสอบว่า phoneme เป็นพยัญชนะหรือไม่ (ถ้าไม่ใช่สระ → คือพยัญชนะ)
        private bool IsConsonant(string phoneme)
        {
            if (string.IsNullOrEmpty(phoneme)) return false;
            return !KnownVowelPhonemes.Contains(phoneme);
        }

        // หาตำแหน่ง index ของสระในลำดับ phonemes
        private int GetVowelPosition(List<OpenUtau.Api.Phonemizer.Phoneme> phonemes)
        {
            for (int i = 0; i < phonemes.Count; i++)
            {
                if (!IsConsonant(phonemes[i].phoneme)) return i;
            }
            // ถ้าหาสระไม่เจอ ให้ถือว่าสระอยู่ท้ายสุด (กรณีพยัญชนะล้วน)
            return phonemes.Count > 0 ? phonemes.Count - 1 : 0;
        }
    }
}
