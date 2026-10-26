/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: th-TH.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/translations/th-TH.ts
 * Description: Master Thai Language Translation Dictionary for DELTA SYNTH
 * ========================================================================
 */

import { TranslationSchema } from "../models/i18n.types";

export const th_TH: TranslationSchema = {
  app: {
    title: "เดลต้า ซินธ์ ว้อยซ์ สตูดิโอ",
    subtitle: "ระบบสังเคราะห์เสียงพูดและเสียงร้องปัญญาประดิษฐ์ระดับมืออาชีพ",
    version: "เวอร์ชัน 35.1.7",
    tagline: "แม่นยำ รวดเร็ว ไร้การหน่วง ด้วยพลังสถาปัตยกรรมระดับองค์กร"
  },
  nav: {
    home: "หน้าหลัก",
    studio: "สตูดิโอตัดต่อ",
    synthesis: "สังเคราะห์เสียง & DSP",
    models: "โมเดลเสียง & ข้อมูลเทรน",
    settings: "การตั้งค่าระบบ",
    docs: "คู่มือการใช้งาน & API",
    community: "ชุมชนผู้พัฒนา",
    language: "ภาษา / ประเทศ"
  },
  studio: {
    projectTitle: "ชื่อโปรเจกต์งานสร้าง",
    track: "แทร็กเสียงร้อง",
    tempo: "ความเร็วเพลง (BPM)",
    timeSignature: "อัตราจังหวะ",
    play: "เล่นเสียง",
    pause: "หยุดชั่วคราว",
    stop: "หยุดการทำงาน",
    render: "เรนเดอร์เสียง",
    rendering: "กำลังสังเคราะห์เสียงร้อง...",
    exportWav: "ส่งออกไฟล์เสียง WAV",
    importMidi: "นำเข้าไฟล์ MIDI / MusicXML",
    timeline: "ไทม์ไลน์โน้ตดนตรี",
    notesCount: "จำนวนโน้ตทั้งหมด"
  },
  synthesis: {
    pitchShift: "ปรับระดับคีย์เสียง (Semitones)",
    f0Method: "อัลกอริทึมสกัดพิตช์ (F0 Method)",
    indexRate: "อัตราความคล้ายคลึงของโมเดล (Index Rate)",
    protectVoice: "ป้องกันเสียงลมและพยัญชนะแตกพร่า",
    resampler: "เอ็นจิ้นรีแซมเปิลเลอร์ (Resampler)",
    batchConvert: "แปลงไฟล์แบบกลุ่มอัตโนมัติ",
    renderStatus: "สถานะการประมวลผล",
    vocalQuality: "คุณภาพเนื้อเสียงร้อง",
    autoPitchVibrato: "ระบบลูกคอและพิตช์อัตโนมัติ"
  },
  telemetry: {
    cpu: "การใช้งานซีพียู",
    vram: "หน่วยความจำการ์ดจอ (VRAM)",
    latency: "ความหน่วงเรียลไทม์",
    epoch: "ลำดับเอพ็อก (Epoch)",
    bufferSize: "ขนาดบัฟเฟอร์เสียง",
    statusReady: "ระบบพร้อมปฏิบัติการ",
    statusBusy: "กำลังประมวลผลความเร็วสูง"
  },
  settings: {
    title: "การตั้งค่าระบบและความปลอดภัย",
    language: "ภาษาที่ใช้แสดงผล",
    theme: "ธีมหน้าต่าง (Dark / Light)",
    audioDriver: "ไดรเวอร์ระบบเสียง (WASAPI / ASIO / DirectSound)",
    sampleRate: "อัตราการสุ่มตัวอย่าง (Sample Rate)",
    saveSuccess: "บันทึกการตั้งค่าเรียบร้อยแล้ว",
    resetDefaults: "คืนค่าเริ่มต้นจากโรงงาน"
  },
  dialog: {
    confirm: "ยืนยันการทำรายการ",
    cancel: "ยกเลิก",
    delete: "ลบรายการ",
    warning: "คำเตือนสำคัญ",
    close: "ปิดหน้าต่าง"
  },
  toast: {
    renderSuccess: "เรนเดอร์ไฟล์เสียงสำเร็จเรียบร้อยแล้ว!",
    renderError: "เกิดข้อผิดพลาดในการสังเคราะห์เสียง โปรดตรวจสอบล็อกการทำงาน",
    languageChanged: "เปลี่ยนภาษาของเว็บไซต์เป็น 'ภาษาไทย' เรียบร้อยแล้ว",
    fileLoaded: "โหลดไฟล์โปรเจกต์สำเร็จ"
  },
  validation: {
    required: "ช่องนี้จำเป็นต้องกรอกข้อมูล",
    invalidNumber: "โปรดระบุค่าตัวเลขที่ถูกต้อง",
    fileTooLarge: "ขนาดไฟล์เกินขีดจำกัดที่ระบบกำหนด"
  }
};
