/*
 * วัน/เดือน/ปี: 30/10/2569 (2026)
 * ชื่อไฟล์โค้ด: worldline.cpp
 * เวอร์ชั่น: 1.1
 * เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & Staff
 * Original by: OpenUtau Contributors
 * รายละเอียดการแก้ไข: ปิดช่องโหว่ Memory Leak จาก 2D Array C-Style และใช้ RAII อย่างรัดกุม
 */

#include "worldline.h"

#include <algorithm>
#include <iterator>
#include <vector>
#include <memory>

#include "world/codec.h"
#include "world/synthesis.h"
#include "worldline/classic/resampler.h"
#include "worldline/common/vec_utils.h"
#include "worldline/f0/dio_estimator.h"
#include "worldline/f0/dio_ss_estimator.h"
#include "worldline/f0/f0_estimator.h"
#include "worldline/f0/pyin_estimator.h"
#include "worldline/model/effects.h"

// ตัวช่วยจอง Memory 2D Array ชั่วคราว (RAII) เพื่อป้องกัน Leak เมื่อเกิด Exception
class Scoped2DArray {
public:
    Scoped2DArray(int length, int width) : length_(length) {
        arr_ = new double*[length_];
        for (int i = 0; i < length_; ++i) {
            arr_[i] = new double[width];
        }
    }
    ~Scoped2DArray() {
        for (int i = 0; i < length_; ++i) {
            delete[] arr_[i];
        }
        delete[] arr_;
    }
    double** get() const { return arr_; }
private:
    double** arr_;
    int length_;
};

static double** to2d(double* const arr, int length, int width) {
  double** arr2d = new double*[length];
  for (int i = 0; i < length; ++i) {
    arr2d[i] = arr + i * width;
  }
  return arr2d;
}

DLL_API int F0(float* samples, int length, int fs, double frame_period,
               int method, double** f0) {
  std::unique_ptr<worldline::F0Estimator> estimator;
  switch (method) {
    case 1:
      estimator = std::make_unique<worldline::DioSsEstimator>();
      break;
    case 2:
      estimator = std::make_unique<worldline::PyinEstimator>();
      break;
    default:
      estimator = std::make_unique<worldline::DioEstimator>();
      break;
  }
  
  std::vector<double> samples_vec(samples, samples + length);
  std::vector<double> f0_vec;
  std::vector<double> ts_vec;
  
  estimator->Estimate(samples_vec, fs, frame_period, &f0_vec, &ts_vec);
  int f0_length = static_cast<int>(f0_vec.size());
  
  *f0 = new double[f0_length];
  std::copy(f0_vec.begin(), f0_vec.end(), *f0);
  
  return f0_length;
}

DLL_API int DecodeMgc(int f0_length, double* mgc, int mgc_size, int fft_size,
                      int fs, double** spectrogram) {
  int sp_size = fft_size / 2 + 1;
  double** mgc2d = to2d(mgc, f0_length, mgc_size);
  
  *spectrogram = new double[f0_length * sp_size];
  double** sp2d = to2d(*spectrogram, f0_length, sp_size);
  
  DecodeSpectralEnvelope(mgc2d, f0_length, fs, fft_size, mgc_size, sp2d);
  
  delete[] mgc2d;
  delete[] sp2d;
  return sp_size;
}

DLL_API int DecodeBap(int f0_length, double* bap, int fft_size, int fs,
                      double** aperiodicity) {
  int bap_size = GetNumberOfAperiodicities(fs);
  int ap_size = fft_size / 2 + 1;
  double** bap2d = to2d(bap, f0_length, bap_size);
  
  *aperiodicity = new double[f0_length * ap_size];
  double** ap2d = to2d(*aperiodicity, f0_length, ap_size);
  
  DecodeAperiodicity(bap2d, f0_length, fs, fft_size, ap2d);
  
  delete[] bap2d;
  delete[] ap2d;
  return ap_size;
}

DLL_API int WorldSynthesis(double* const f0, int f0_length,
                           double* const mgc_or_sp, bool is_mgc, int mgc_size,
                           double* const bap_or_ap, bool is_bap, int fft_size,
                           double frame_period, int fs, double** y,
                           double* const gender, double* const tension,
                           double* const breathiness, double* const voicing) {
  int bap_size = GetNumberOfAperiodicities(fs);
  int sp_size = fft_size / 2 + 1;

  std::unique_ptr<Scoped2DArray> sp_scoped = nullptr;
  std::unique_ptr<Scoped2DArray> ap_scoped = nullptr;
  double** sp = nullptr;
  double** ap = nullptr;
  double** mgc2d = nullptr;
  double** bap2d = nullptr;

  if (is_mgc) {
    mgc2d = to2d(mgc_or_sp, f0_length, mgc_size);
    sp_scoped = std::make_unique<Scoped2DArray>(f0_length, sp_size);
    sp = sp_scoped->get();
    DecodeSpectralEnvelope(mgc2d, f0_length, fs, fft_size, mgc_size, sp);
    delete[] mgc2d;
  } else {
    sp = to2d(mgc_or_sp, f0_length, sp_size);
  }

  if (is_bap) {
    bap2d = to2d(bap_or_ap, f0_length, bap_size);
    ap_scoped = std::make_unique<Scoped2DArray>(f0_length, sp_size);
    ap = ap_scoped->get();
    DecodeAperiodicity(bap2d, f0_length, fs, fft_size, ap);
    delete[] bap2d;
  } else {
    ap = to2d(bap_or_ap, f0_length, sp_size);
  }

  int y_length = 1 + static_cast<int>((f0_length - 1) * frame_period / 1000.0 * fs);
  *y = new double[y_length];

  if (gender != nullptr) {
    for (int i = 0; i < f0_length; ++i) {
      worldline::ShiftGender(sp[i], sp_size, (gender[i] - 0.5) * 200.0);
    }
  }

  std::vector<std::vector<double>> ten = worldline::vec2d(sp_size, f0_length, 1.0);
  if (tension != nullptr) {
    for (int i = 0; i < f0_length; ++i) {
      ten[i] = worldline::GetTensionCoefficients(f0[i], fs, (tension[i] - 0.5) * 200.0, sp_size);
    }
  }

  std::vector<double> bre(f0_length, 1.0);
  if (breathiness != nullptr) {
    for (int i = 0; i < f0_length; ++i) {
      bre[i] = breathiness[i] > 0.5 ? breathiness[i] * 4.0 : breathiness[i] * 2.0;
    }
  }

  std::vector<double> voi(f0_length, 1.0);
  if (voicing != nullptr) {
    for (int i = 0; i < f0_length; ++i) {
      voi[i] = voicing[i];
    }
  }

  auto ten_wrapper = worldline::vec2d_wrapper(ten);
  Synthesis(f0, f0_length, sp, ap, fft_size, frame_period, fs,
            ten_wrapper.data(), bre.data(), voi.data(), y_length, *y);

  if (!is_mgc) delete[] sp;
  if (!is_bap) delete[] ap;

  return y_length;
}

DLL_API int Resample(const SynthRequest* request, float** y) {
  auto resampler = std::make_unique<worldline::Resampler>(*request);
  std::vector<double> out = resampler->Resample();
  *y = new float[out.size()];
  std::copy(out.begin(), out.end(), *y);
  return static_cast<int>(out.size());
}

DLL_API PhraseSynth* PhraseSynthNew() { return new PhraseSynth(); }

DLL_API void PhraseSynthDelete(PhraseSynth* phrase_synth) {
  delete phrase_synth;
}

DLL_API void PhraseSynthAddRequest(PhraseSynth* phrase_synth,
                                   const SynthRequest* request, double pos_ms,
                                   double skip_ms, double length_ms,
                                   double fade_in_ms, double fade_out_ms,
                                   worldline::LogCallback logCallback) {
  phrase_synth->AddRequest(*request, pos_ms, skip_ms, length_ms, fade_in_ms,
                           fade_out_ms, logCallback);
}

DLL_API void PhraseSynthSetCurves(PhraseSynth* phrase_synth, double* f0,
                                  double* gender, double* tension,
                                  double* breathiness, double* voicing,
                                  int length,
                                  worldline::LogCallback logCallback) {
  phrase_synth->SetCurves(f0, gender, tension, breathiness, voicing, length,
                          logCallback);
}

DLL_API int PhraseSynthSynth(PhraseSynth* phrase_synth, float** y,
                             worldline::LogCallback logCallback) {
  std::vector<double> samples = phrase_synth->Synth(logCallback);
  int yLength = static_cast<int>(samples.size());
  *y = new float[samples.size()];
  std::copy(samples.begin(), samples.end(), *y);
  return yLength;
}