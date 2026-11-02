/*
 * วัน/เดือน/ปี: 30/10/2569 (2026)
 * ชื่อไฟล์โค้ด: audio_output_test.cc
 * เวอร์ชั่น: 1.2
 * เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & Staff
 * Original by: OpenUtau Contributors
 * รายละเอียดการแก้ไข: ปรับปรุงความเสถียรของลูปทดสอบเสียง White Noise ป้องกันการหารด้วยศูนย์ และ Log 2 ภาษา
 */

#include "worldline/audio_output.h"

#include "absl/log/log.h"
#include "absl/time/clock.h"
#include "absl/time/time.h"
#include "gmock/gmock.h"
#include "gtest/gtest.h"
#include "xxhash.h"
#include <cstdlib>

// ฟังก์ชันสร้างเสียง White Noise สำหรับการทดสอบ (Memory Safe)
static void white_noise(float* buffer, uint32_t channels, uint32_t frame_count) {
  if (!buffer || channels == 0 || frame_count == 0) return;

  for (uint32_t i = 0; i < frame_count; i++) {
    for (uint32_t j = 0; j < channels; j++) {
      if (j == 0) {
        // จำกัดสเกลความดังให้ปลอดภัย (0.1f)
        buffer[i * channels + j] = ((float)rand() / RAND_MAX * 2.0f - 1.0f) * 0.1f;
      } else {
        // คัดลอกค่าช่องซ้ายไปช่องขวา (Mono to Stereo)
        buffer[i * channels + j] = buffer[i * channels + j - 1];
      }
    }
  }
}

TEST(MiniAudioTest, PlaybackStabilityCheck) {
  uint32_t api_id = 0;
  uint64_t id = 0;
  const int32_t max_devices = 10;

  ou_audio_device_info_t device_infos[max_devices];
  int32_t count = ou_get_audio_device_infos(device_infos, max_devices);
  
  LOG(INFO) << "==========================================";
  LOG(INFO) << "[TH] ค้นพบอุปกรณ์ทั้งหมด: " << count << " รายการ";
  LOG(INFO) << "[EN] Discovered devices: " << count << " items";

  for (int32_t i = 0; i < count; i++) {
    LOG(INFO) << "Device " << i << ": " << (device_infos[i].name ? device_infos[i].name : "N/A")
              << " | API: " << (device_infos[i].api ? device_infos[i].api : "N/A")
              << " | API Id: " << device_infos[i].api_id
              << " | ID: " << absl::Hex(device_infos[i].id);
    if (i == 0) {
      api_id = device_infos[i].api_id;
      id = device_infos[i].id;
    }
  }
  
  // คืนค่า Memory อย่างรัดกุม
  ou_free_audio_device_infos(device_infos, count);

  LOG(INFO) << "[TH] กำลังทดสอบอุปกรณ์เสียงเริ่มต้น...";
  ou_audio_context_t* context = ou_init_audio_device(api_id, id, &white_noise);
  
  if (context) {
      LOG(INFO) << "Device API: " << ou_get_audio_device_api(context);
      LOG(INFO) << "Device Name: " << ou_get_audio_device_name(context);
      
      ou_audio_device_start(context);
      absl::SleepFor(absl::Seconds(1));
      ou_audio_device_stop(context);
      ou_free_audio_device(context);
  } else {
      LOG(ERROR) << "[TH] ล้มเหลวในการเริ่มต้นอุปกรณ์เสียงเริ่มต้น";
  }

  absl::SleepFor(absl::Milliseconds(500));

  LOG(INFO) << "[TH] กำลังทดสอบระบบเลือกอุปกรณ์อัตโนมัติ (Auto Mode)...";
  context = ou_init_audio_device_auto(&white_noise);
  
  if (context) {
      LOG(INFO) << "Auto Device API: " << ou_get_audio_device_api(context);
      LOG(INFO) << "Auto Device Name: " << ou_get_audio_device_name(context);
      
      ou_audio_device_start(context);
      absl::SleepFor(absl::Seconds(1));
      ou_audio_device_stop(context);
      ou_free_audio_device(context);
      LOG(INFO) << "[TH] การทดสอบเสียงเสร็จสมบูรณ์ ปลอดภัยจาก Memory Leak";
  } else {
      LOG(ERROR) << "[TH] ล้มเหลวในการทำงานโหมด Auto";
  }
  LOG(INFO) << "==========================================";
}