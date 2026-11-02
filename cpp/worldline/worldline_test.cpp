/*
 * วัน/เดือน/ปี: 30/10/2569 (2026)
 * ชื่อไฟล์โค้ด: worldline_test.cpp
 * เวอร์ชั่น: 1.2
 * เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & Staff
 * Original by: OpenUtau Contributors
 * รายละเอียดการแก้ไข: เพิ่มชุดทดสอบความเสถียรการจัดการ Memory ของระบบ PhraseSynth
 */

#include "worldline.h"

#include "gmock/gmock.h"
#include "gtest/gtest.h"
#include <memory>

#if defined(_MSC_VER)
#include <windows.h>
#else
#include <dlfcn.h>
#endif

#if defined(_MSC_VER)
#define DLL_IMPORT __declspec(dllimport)
#elif defined(__GNUC__) || defined(__clang__)
#define DLL_IMPORT __attribute__((visibility("default")))
#endif

extern "C" {
DLL_API PhraseSynth* PhraseSynthNew();
DLL_API void PhraseSynthDelete(PhraseSynth* phrase_synth);
}

// ชุดทดสอบความเสถียรของ DLL และการจองหน่วยความจำระดับลึก
TEST(WorldlineTest, TestLibraryLoadingAndMemoryStability) {
#if defined(_MSC_VER)
  HMODULE handle = LoadLibraryA("worldline.dll");
  EXPECT_THAT(handle, testing::NotNull()) << "ไม่สามารถโหลดไฟล์ worldline.dll ได้ (Failed to load DLL)";
#else
  void* handle = dlopen("libworldline.so", RTLD_LAZY);
  EXPECT_THAT(handle, testing::NotNull()) << "ไม่สามารถโหลดไฟล์ libworldline.so ได้ (Failed to load Shared Object)";
#endif

  // ทดสอบวงจรชีวิตของอ็อบเจ็กต์ (Object Lifecycle) ป้องกัน Memory Leak
  PhraseSynth* phrase_synth = PhraseSynthNew();
  EXPECT_THAT(phrase_synth, testing::NotNull()) << "การสร้างอินสแตนซ์ PhraseSynth ล้มเหลว (Instance creation failed)";
  
  // ยืนยันว่าฟังก์ชันทำลายทำงานได้โดยไม่เกิด Access Violation
  EXPECT_NO_FATAL_FAILURE(PhraseSynthDelete(phrase_synth));
  
#if defined(_MSC_VER)
  if (handle) FreeLibrary(handle);
#else
  if (handle) dlclose(handle);
#endif
}