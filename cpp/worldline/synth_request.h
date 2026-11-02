/*
 * วัน/เดือน/ปี: 30/10/2569 (2026)
 * ชื่อไฟล์โค้ด: synth_request.h
 * เวอร์ชั่น: 1.1
 * เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & Staff
 * Original by: OpenUtau Contributors
 * รายละเอียดการแก้ไข: ป้องกัน Undefined Behavior โดยกำหนด Default Initializers ให้ตัวแปรทั้งหมด
 */

#ifndef WORLDLINE_SYNTH_REQUEST_H_
#define WORLDLINE_SYNTH_REQUEST_H_

#include <cstdint>

extern "C" {

struct SynthRequest {
  std::int32_t sample_fs = 44100;
  std::int32_t sample_length = 0;
  double* sample = nullptr;
  
  std::int32_t frq_length = 0;
  char* frq = nullptr;
  
  std::int32_t tone = 0;
  double con_vel = 100.0;
  double offset = 0.0;
  double required_length = 0.0;
  double consonant = 0.0;
  double cut_off = 0.0;
  double volume = 100.0;
  double modulation = 0.0;
  double tempo = 120.0;
  
  std::int32_t pitch_bend_length = 0;
  std::int32_t* pitch_bend = nullptr;

  int flag_g = 0;
  int flag_O = 0;
  int flag_P = 86;
  int flag_Mt = 0;
  int flag_Mb = 0;
  int flag_Mv = 100;
};

struct SynthOutput {
  std::int64_t data_length = 0;
  char* data = nullptr;
};

} // extern "C"

#endif  // WORLDLINE_SYNTH_REQUEST_H_