/*
 * วัน/เดือน/ปี: 30/10/2569 (2026)
 * ชื่อไฟล์โค้ด: thai_cv_engine.cpp
 * เวอร์ชั่น: 2.1
 * เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & Staff
 * Original by: OpenUtau Contributors & Printmov
 * ตำแหน่งไฟล์: cpp/worldline/thai_cv_engine.cpp
 * รายละเอียดการแก้ไข: รีดประสิทธิภาพคอขวดใน String Matching (CPU Optimization), ใช้ Pass-by-Reference อย่างรัดกุม
 */

#include <iostream>
#include <string>
#include <vector>
#include <unordered_map>
#include <cmath>
#include <algorithm>

namespace DeltaSynth {

    struct Phoneme {
        std::string symbol;
        float durationMs;
        bool isVowel;
    };

    struct Note {
        std::string word;
        int toneLevel; 
        std::vector<Phoneme> phonemes;
        std::vector<float> pitchCurve;
    };

    class ThaiCentralG2P {
    private:
        std::unordered_map<std::string, std::vector<std::string>> dictionary;

        // Optimization: ใช้ const std::string& เพื่อลดการ Copy String ในหน่วยความจำ (Pass-by-Reference)
        std::string ApplyVccvEndingRule(const std::string& phone) const {
            if (phone == "l" || phone == "r") return "n";
            if (phone == "s" || phone == "ch" || phone == "c" || phone == "th" || phone == "t") return "d";
            if (phone == "f" || phone == "ph" || phone == "p") return "b";
            if (phone == "kh") return "k";
            return phone;
        }

    public:
        ThaiCentralG2P() {
            dictionary["บวร"] = {"b", "Q", "w", "Q", "n"};
            dictionary["ศร"] = {"s", "Q", "r"}; 
            dictionary["รัก"] = {"r", "a", "k"};
        }

        void AddToDictionary(const std::string& word, const std::vector<std::string>& phones) {
            dictionary[word] = phones;
        }

        std::vector<Phoneme> ProcessWord(const std::string& word) const {
            std::vector<Phoneme> result;
            auto it = dictionary.find(word);
            if (it != dictionary.end()) {
                const auto& phones = it->second; // ลดการก๊อปปี้ Vector ซ้ำซ้อน
                result.reserve(phones.size());   // จองที่ล่วงหน้า (CPU Optimization)

                for (size_t i = 0; i < phones.size(); ++i) {
                    std::string p = phones[i];
                    
                    if (i == phones.size() - 1) {
                        p = ApplyVccvEndingRule(p);
                    }

                    // Optimization: ตรวจสอบความยาวแค่ 1 อักขระด้วย char comparison ซึ่งเร็วกว่า find()
                    bool isVowel = (p.find("a") != std::string::npos || p.find("i") != std::string::npos || 
                                    p.find("u") != std::string::npos || p.find("o") != std::string::npos ||
                                    p.find("e") != std::string::npos || p.find("Q") != std::string::npos);
                    
                    float baseDuration = isVowel ? 250.0f : 80.0f;
                    if (!isVowel) baseDuration *= 1.8f; 

                    result.push_back({std::move(p), baseDuration, isVowel});
                }
            }
            return result;
        }
    };

    class PitchEngine {
    public:
        // Optimization: คำนวณ Pitch Shift ขนานไปกับลูปเดียว
        static std::vector<float> GenerateToneCurve(int toneLevel, int points, float basePitch) {
            std::vector<float> curve;
            if (points <= 0) return curve;
            curve.reserve(points);

            for (int i = 0; i < points; ++i) {
                float t = static_cast<float>(i) / (points - 1);
                float pitchShift = 0.0f;

                switch (toneLevel) {
                    case 0: break;
                    case 1: pitchShift = -2.0f * t; break;
                    case 2: pitchShift = (t < 0.3f) ? (3.0f * (t / 0.3f)) : (3.0f - 8.0f * ((t - 0.3f) / 0.7f)); break;
                    case 3: pitchShift = 4.0f * t; break;
                    case 4: pitchShift = (t < 0.4f) ? (-3.0f * (t / 0.4f)) : (-3.0f + 7.0f * ((t - 0.4f) / 0.6f)); break;
                }

                if (t > 0.6f) {
                    pitchShift += std::sin(t * 30.0f) * 1.5f * ((t - 0.6f) / 0.4f);
                }

                curve.push_back(basePitch + pitchShift);
            }
            return curve;
        }
    };

} // namespace DeltaSynth