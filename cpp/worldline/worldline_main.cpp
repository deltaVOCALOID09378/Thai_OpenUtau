/*
 * วัน/เดือน/ปี: 30/10/2569 (2026)
 * ชื่อไฟล์โค้ด: worldline_main.cpp
 * เวอร์ชั่น: 1.2
 * เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & Staff
 * Original by: StAkira / OpenUtau Contributors
 * รายละเอียดการแก้ไข: เพิ่ม DSP Audio Fidelity (DC Blocker & Normalizer) เพื่อเสียงร้องสมบูรณ์แบบตรงตาม oto.ini ไร้เสียงแทรก
 */

#include <filesystem>
#include <fstream>
#include <iostream>
#include <optional>
#include <string>
#include <vector>
#include <cmath>

#include "absl/debugging/failure_signal_handler.h"
#include "absl/debugging/symbolize.h"
#include "absl/strings/str_join.h"
#include "audioio.h"
#include "world/synthesis.h"
#include "worldline/classic/resampler.h"
#include "worldline/model/effects.h"
#include "worldline/synth_request.h"

namespace DeltaSynth {
    // ระบบยกระดับคุณภาพเสียงร้อง (Audio Fidelity Enhancer)
    void EnhanceAcousticClarity(std::vector<float>& samples) {
        if (samples.empty()) return;

        // 1. DC Blocker Filter (กำจัดเสียงฮัมและคลื่นความถี่ต่ำแทรกซ้อน)
        float prev_x = 0.0f;
        float prev_y = 0.0f;
        float max_abs_val = 0.0f;
        const float R = 0.995f; // Filter coefficient

        for (auto& sample : samples) {
            float x = sample;
            sample = x - prev_x + R * prev_y;
            prev_x = x;
            prev_y = sample;
            
            float abs_val = std::abs(sample);
            if (abs_val > max_abs_val) {
                max_abs_val = abs_val;
            }
        }

        // 2. Safe Peak Normalization (ป้องกันเสียงแตก Clipping รักษาความสดใส)
        const float safe_headroom = 0.99f;
        if (max_abs_val > safe_headroom) {
            float scale = safe_headroom / max_abs_val;
            for (auto& sample : samples) {
                sample *= scale;
            }
        }
    }
}

void LogBilingual(const std::string& th, const std::string& en) {
    std::cout << "[TH] " << th << "\n[EN] " << en << std::endl;
}

int main(int argc, char** argv) {
  absl::InitializeSymbolizer(argv[0]);
  absl::FailureSignalHandlerOptions options;
  absl::InstallFailureSignalHandler(options);

  bool debug = false;
  std::string exe_path = std::string(argv[0]);
  std::vector<std::string> args;
  
  for (int i = 1; i < argc; ++i) {
    args.push_back(std::string(argv[i]));
  }

  if (args.size() < 4) {
    LogBilingual("เครื่องมือสังเคราะห์เสียง Worldline (DELTA SYNTH Edition)", 
                 "Worldline Synthesizer Engine (DELTA SYNTH Edition)");
    std::cout << "Usage / วิธีใช้งาน: <input wavfile> <output file> <pitch_percent> <velocity> "
                 "[<flags> [<offset> <length_require> [<fixed length> [<end_blank> "
                 "[<volume> [<modulation> [<pitch bend>...]]]]]]]"
              << std::endl;
    std::cout << "Flags / พารามิเตอร์เสริม:" << std::endl;
    std::cout << "  P  = 86(0~100)    : Peak compression (ควบคุมจุดพีค)" << std::endl;
    std::cout << "  g  = 0(-100~100)  : Gender (เพศสภาพเสียง)" << std::endl;
    std::cout << "  Mt = 0(-100~100)  : Tension (ความตึงเครียดของเสียง)" << std::endl;
    std::cout << "  Mb = 0(-100~100)  : Breathiness (ปริมาณลมหายใจ)" << std::endl;
    std::cout << "  Mv = 100(0~100)   : Voicing (ความก้องกังวาน)" << std::endl;
    return 0;
  }
  
  std::cout << "[INFO] Arguments: " << absl::StrJoin(args, " ") << std::endl;

  auto resampler = std::make_unique<worldline::Resampler>(args);
  auto y = resampler->Resample();

  // ขัดเกลาคุณภาพเสียงขั้นสุดท้าย ป้องกันเสียงแทรกซ้อน (Zero-Defect Acoustics)
  DeltaSynth::EnhanceAcousticClarity(y);

  std::string out_path =
      args.size() >= 2 ? args[1]
                       : args[0].substr(0, args[0].size() - 4) + ".out.wav";
                       
  LogBilingual("กำลังเขียนไฟล์ผลลัพธ์ไปยัง: " + out_path, 
               "Writing audio output to: " + out_path);
               
  wavwrite(y.data(), y.size(), 44100, 16, out_path.c_str());
  
  LogBilingual("กระบวนการสังเคราะห์เสียงเสร็จสมบูรณ์", "Audio synthesis completed successfully.");
  return 0;
}