/*
 * วัน/เดือน/ปี: 30/10/2569 (2026)
 * ชื่อไฟล์โค้ด: phrase_synth.cpp
 * เวอร์ชั่น: 1.1
 * เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & Staff
 * Original by: OpenUtau Contributors
 * รายละเอียดการแก้ไข: รีดประสิทธิภาพ CPU ในลูป DSP (Cache-Friendly), ป้องกัน Memory Leak ด้วย std::move
 */

#include "phrase_synth.h"

#include <algorithm>
#include <cmath>
#include <iterator>
#include <vector>

#include "world/constantnumbers.h"
#include "worldline/classic/timing.h"
#include "worldline/common/vec_utils.h"
#include "worldline/f0/f0_estimator.h"
#include "worldline/f0/frq_estimator.h"
#include "worldline/f0/pyin_estimator.h"
#include "worldline/model/effects.h"

namespace worldline {

constexpr double frame_ms = 10.0;
constexpr int padding = 2;

static inline int ceil_int(double v) { return static_cast<int>(std::ceil(v)); }
static inline int floor_int(double v) { return static_cast<int>(std::floor(v)); }
static inline int round_int(double v) { return static_cast<int>(std::round(v)); }

void PhraseSynth::AddRequest(const SynthRequest& request, double pos_ms,
                             double skip_ms, double length_ms,
                             double fade_in_ms, double fade_out_ms,
                             LogCallback logCallback) {
  std::vector<double> samples;
  samples.reserve(request.sample_length);
  // ใช้ std::copy ได้ แต่ std::copy_n ปลอดภัยและรวดเร็วกว่า
  std::copy_n(request.sample, request.sample_length, std::back_inserter(samples));

  std::unique_ptr<F0Estimator> f0_estimator = nullptr;
  if (request.frq_length > 0) {
    std::string_view frq_data(request.frq, request.frq_length);
    f0_estimator = std::make_unique<FrqEstimator>(frq_data);
  } else {
    f0_estimator = std::make_unique<PyinEstimator>();
  }

  Model model(std::move(samples), request.sample_fs, frame_ms, std::move(f0_estimator));

  double src_max = vec_maxabs(model.samples());

  model.BuildF0();

  auto mapping = GetTimeMapping(model, request);

  // Trim model to input region.
  double in_start_ms = request.offset;
  double in_length_ms = GetInTotalMs(model, request);
  int in_start_frame = static_cast<int>(in_start_ms / frame_ms);
  int in_length_frame = static_cast<int>(std::ceil(in_start_ms + in_length_ms) / frame_ms) - in_start_frame;
  double left_trimmed = in_start_frame * frame_ms;

  model.Trim(in_start_frame, in_length_frame);

  double seg_max = vec_maxabs(model.samples());
  AutoGain(model.samples(), src_max, seg_max, model.GetVoicedRatio(), request.volume, request.flag_P);

  model.BuildSp();
  model.BuildAp();

  ShiftTimeMapping(mapping, -left_trimmed);
  PadTimeMapping(mapping, padding);

  model.Remap(mapping);

  models_.push_back(std::move(model));
  ModelTiming timing;
  timing.left_extra = padding;
  timing.skip = round_int(skip_ms / frame_ms);
  timing.p0 = round_int(pos_ms / frame_ms);
  timing.p1 = round_int((pos_ms + fade_in_ms) / frame_ms);
  timing.p3 = round_int((pos_ms + length_ms - fade_out_ms) / frame_ms);
  timing.p4 = round_int((pos_ms + length_ms) / frame_ms);
  
  timing.p0 = std::max(0, timing.p0);
  timing.p1 = std::max(timing.p0 + 1, timing.p1);
  timing.p3 = std::min(timing.p4 - 1, timing.p3);
  timings_.push_back(std::move(timing));
}

void PhraseSynth::SetCurves(double* const f0, double* gender, double* tension,
                            double* breathiness, double* voicing, int length,
                            LogCallback logCallback) {
  // Pre-allocate เพื่อป้องกันการ Reallocation ขยายขนาดซ้ำซ้อน (CPU Optimization)
  f0_.reserve(f0_.size() + length);
  gender_.reserve(gender_.size() + length);
  tension_.reserve(tension_.size() + length);
  breathiness_.reserve(breathiness_.size() + length);
  voicing_.reserve(voicing_.size() + length);

  std::copy_n(f0, length, std::back_inserter(f0_));
  std::copy_n(gender, length, std::back_inserter(gender_));
  std::copy_n(tension, length, std::back_inserter(tension_));
  std::copy_n(breathiness, length, std::back_inserter(breathiness_));
  std::copy_n(voicing, length, std::back_inserter(voicing_));
}

std::vector<double> PhraseSynth::Synth(LogCallback logCallback) {
  if (models_.empty()) return {};

  int fs = models_[0].fs();
  int fft_size = models_[0].fft_size();
  int width = models_[0].sp()[0].size();

  std::vector<double> f0;
  std::vector<std::vector<double>> sp;
  std::vector<std::vector<double>> ap;
  std::vector<int> dirty;

  for (size_t k = 0; k < models_.size(); ++k) {
    const auto& model = models_[k];
    const auto& timing = timings_[k];
    
    // รีไซส์ครั้งเดียว
    if (f0.size() < static_cast<size_t>(timing.p4)) {
      f0.resize(timing.p4, 0.0);
      sp.resize(timing.p4, std::vector<double>(width, world::kMySafeGuardMinimum));
      ap.resize(timing.p4, std::vector<double>(width, 1.0));
      dirty.resize(timing.p4, 0);
    }

    for (int i = timing.p0; i < timing.p4; ++i) {
      double weight = 1.0;
      if (i < timing.p1) {
        weight = static_cast<double>(i - timing.p0) / (timing.p1 - timing.p0);
      } else if (i >= timing.p3) {
        weight = static_cast<double>(timing.p4 - i) / (timing.p4 - timing.p3);
      }
      
      int model_i = timing.left_extra + timing.skip + i - timing.p0;
      if (model_i < timing.left_extra) {
        continue;
      }
      
      if (dirty[i] == 0 || weight > 0.5) {
        f0[i] = model.f0()[model_i];
      }

      double wa = (dirty[i] == 0) ? 0.0 : (1.0 - weight);
      double wb = (dirty[i] == 0) ? 1.0 : weight;

      // ลูปด้านใน (Inner loop) จัดการ Pointer ตรงๆ เพื่อความเร็ว (Cache-Friendly)
      double* sp_row = sp[i].data();
      const double* model_sp_row = model.sp()[model_i].data();
      double* ap_row = ap[i].data();
      const double* model_ap_row = model.ap()[model_i].data();

      for (int j = 0; j < width; ++j) {
        sp_row[j] += model_sp_row[j] * weight;
        ap_row[j] = ap_row[j] * wa + model_ap_row[j] * wb;
      }
      dirty[i] = 1;
    }
  }

  int length = static_cast<int>(f0.size()) + 1;
  f0.resize(length, f0.empty() ? 0.0 : f0.back());
  sp.resize(length, sp.empty() ? std::vector<double>(width, world::kMySafeGuardMinimum) : sp.back());
  ap.resize(length, ap.empty() ? std::vector<double>(width, 1.0) : ap.back());

  f0_.resize(length, f0_.empty() ? 0.0 : f0_.back());
  gender_.resize(length, gender_.empty() ? 0.5 : gender_.back());
  tension_.resize(length, tension_.empty() ? 0.5 : tension_.back());
  breathiness_.resize(length, breathiness_.empty() ? 0.5 : breathiness_.back());
  voicing_.resize(length, voicing_.empty() ? 1.0 : voicing_.back());

  std::vector<std::vector<double>> ten = vec2d(width, length, 1.0);
  std::vector<double> bre(length, 1.0);
  std::vector<double> voi(length, 1.0);
  
  for (int i = 0; i < length; ++i) {
    if (f0[i] > 0.0) {
      f0[i] = f0_[i];
    }
    ShiftGender(sp[i].data(), width, (gender_[i] - 0.5) * 200.0);
    bre[i] = (breathiness_[i] > 0.5) ? (breathiness_[i] * 4.0) : (breathiness_[i] * 2.0);
    ten[i] = GetTensionCoefficients(f0_[i], fs, (tension_[i] - 0.5) * 200.0, width);
    voi[i] = voicing_[i];
  }

  Model final_model(fs, models_[0].frame_ms(), fft_size);
  final_model.f0() = std::move(f0);
  final_model.ap() = std::move(ap);
  final_model.sp() = std::move(sp);

  final_model.Synth(ten, bre, voi);
  std::vector<double> samples = std::move(final_model.samples());

  // 10ms fade out to ease abruptive ending.
  int fade_out_samples = static_cast<int>(fs * 10.0 / 1000.0);
  const int sample_size = static_cast<int>(samples.size());
  for (int i = 0; i < fade_out_samples && i < sample_size; ++i) {
    samples[sample_size - 1 - i] *= (static_cast<double>(i) / fade_out_samples);
  }
  return samples;
}

}  // namespace worldline