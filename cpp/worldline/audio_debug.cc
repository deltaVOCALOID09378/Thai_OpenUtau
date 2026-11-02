/*
 * วัน/เดือน/ปี: 30/10/2569 (2026)
 * ชื่อไฟล์โค้ด: audio_debug.cc
 * เวอร์ชั่น: 1.1
 * เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & Staff
 * Original by: OpenUtau Contributors
 * รายละเอียดการแก้ไข: อัปเกรดระบบ Log สองภาษา (Bilingual) และอุดรอยรั่วจากการเช็คตัวแปร Null
 */

#include <cstdint>
#include <string_view>

#include "absl/debugging/failure_signal_handler.h"
#include "absl/debugging/symbolize.h"
#include "absl/flags/flag.h"
#include "absl/flags/parse.h"
#include "absl/log/globals.h"
#include "absl/log/initialize.h"
#include "absl/log/log.h"
#include "absl/strings/escaping.h"
#include "absl/strings/str_cat.h"
#include "miniaudio.h"
#include "worldline/audio_output.h"
#include "xxhash.h"

int main(int argc, char** argv) {
  absl::InitializeSymbolizer(argv[0]);
  absl::FailureSignalHandlerOptions options;
  absl::InstallFailureSignalHandler(options);

  absl::InitializeLog();
  absl::SetStderrThreshold(absl::LogSeverity::kInfo);

  absl::ParseCommandLine(argc, argv);

  for (int i = 0; i < ma_backend_coreaudio; i++) {
    LOG(INFO) << "============================================";
    LOG(INFO) << "[TH] กำลังทดสอบ Backend: " << ma_get_backend_name((ma_backend)i);
    LOG(INFO) << "[EN] Trying backend: " << ma_get_backend_name((ma_backend)i);

    ma_context context;
    ma_backend backends[1] = {(ma_backend)i};
    ma_result result = ma_context_init(backends, 1, NULL, &context);

    if (result != MA_SUCCESS) {
      LOG(ERROR) << "[TH] ล้มเหลวในการเริ่มต้น Context / [EN] Failed to initialize context";
      LOG(ERROR) << "Error: " << ma_result_description(result);
      continue;
    }

    ma_device_info* playback_device_infos = NULL;
    ma_uint32 playback_device_count = 0;
    ma_device_info* capture_device_infos = NULL;
    ma_uint32 capture_device_count = 0;

    result = ma_context_get_devices(
        &context, &playback_device_infos, &playback_device_count,
        &capture_device_infos, &capture_device_count);

    if (result != MA_SUCCESS) {
      LOG(ERROR) << "[TH] ไม่สามารถดึงข้อมูลอุปกรณ์ได้ / [EN] Failed to get devices";
      LOG(ERROR) << "Error: " << ma_result_description(result);
      ma_context_uninit(&context);
      continue;
    }
    
    LOG(INFO) << "[TH] จำนวนอุปกรณ์เสียงขาออก: " << playback_device_count;
    LOG(INFO) << "[EN] Playback device count: " << playback_device_count;

    for (ma_uint32 j = 0; j < playback_device_count; j++) {
      LOG(INFO) << "------------------------";
      LOG(INFO) << "Device: #" << j;
      ma_device_info* info = &playback_device_infos[j];
      
      if (info && info->name) {
          LOG(INFO) << "Device name: " << info->name;
          LOG(INFO) << "Device name bytes: "
                    << absl::BytesToHexString(std::string_view(info->name));
          uint64_t id = XXH64(&(info->id), sizeof(ma_device_id), 0);
          LOG(INFO) << "Device ID: " << absl::Hex(id);
      }
    }
    ma_context_uninit(&context); // คืนค่าหน่วยความจำหลังจบการใช้งาน backend
  }

  LOG(INFO) << "============================================";
  LOG(INFO) << "[TH] การตรวจสอบอุปกรณ์เสียงเสร็จสมบูรณ์";
  LOG(INFO) << "[EN] Audio device debugging finished successfully.";

  return 0;
}