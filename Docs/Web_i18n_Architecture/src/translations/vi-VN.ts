/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: vi-VN.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/translations/vi-VN.ts
 * Description: Vietnamese (Tiếng Việt) Translation Dictionary
 * ========================================================================
 */

import { TranslationSchema } from "../models/i18n.types";

export const vi_VN: TranslationSchema = {
  app: {
    title: "DELTA SYNTH Voice Studio",
    subtitle: "Nền tảng tổng hợp giọng nói và tiếng hát nơ-ron chuyên nghiệp",
    version: "Phiên bản 35.1.7",
    tagline: "Xử lý âm thanh thông minh tốc độ cao, độ trễ cực thấp trên nền kiến trúc hiện đại."
  },
  nav: {
    home: "Trang chủ",
    studio: "Phòng thu",
    synthesis: "Tổng hợp giọng hát & DSP",
    models: "Mô hình giọng & Bộ dữ liệu",
    settings: "Cài đặt hệ thống",
    docs: "Tài liệu & API",
    community: "Cộng đồng",
    language: "Ngôn ngữ / Quốc gia"
  },
  studio: {
    projectTitle: "Tên dự án",
    track: "Rãnh giọng hát",
    tempo: "Nhịp độ (BPM)",
    timeSignature: "Số chỉ nhịp",
    play: "Phát",
    pause: "Tạm dừng",
    stop: "Dừng",
    render: "Kết xuất âm thanh",
    rendering: "Đang tổng hợp giọng hát...",
    exportWav: "Xuất tệp WAV",
    importMidi: "Nhập tệp MIDI / MusicXML",
    timeline: "Dòng thời gian nốt nhạc",
    notesCount: "Tổng số nốt"
  },
  synthesis: {
    pitchShift: "Chuyển giọng (Nửa cung)",
    f0Method: "Thuật toán trích xuất cao độ (F0)",
    indexRate: "Tỷ lệ chỉ mục mô hình (Index Rate)",
    protectVoice: "Bảo vệ phụ âm vô thanh & tiếng thở",
    resampler: "Bộ lấy mẫu lại (Resampler)",
    batchConvert: "Chuyển đổi âm thanh hàng loạt",
    renderStatus: "Trạng thái xử lý",
    vocalQuality: "Chất lượng âm sắc",
    autoPitchVibrato: "AutoPitch & Rung tự nhiên"
  },
  telemetry: {
    cpu: "Mức sử dụng CPU",
    vram: "Bộ nhớ đồ họa (VRAM)",
    latency: "Độ trễ thời gian thực",
    epoch: "Epoch hiện tại",
    bufferSize: "Kích thước bộ đệm âm thanh",
    statusReady: "Hệ thống sẵn sàng",
    statusBusy: "Đang xử lý thuật toán DSP"
  },
  settings: {
    title: "Tùy chọn hệ thống & Động cơ",
    language: "Ngôn ngữ giao diện",
    theme: "Chủ đề (Tối / Sáng)",
    audioDriver: "Trình điều khiển âm thanh",
    sampleRate: "Tần số lấy mẫu",
    saveSuccess: "Đã lưu cài đặt thành công",
    resetDefaults: "Khôi phục cài đặt gốc"
  },
  dialog: {
    confirm: "Xác nhận",
    cancel: "Hủy bỏ",
    delete: "Xóa",
    warning: "Cảnh báo quan trọng",
    close: "Đóng"
  },
  toast: {
    renderSuccess: "Đã kết xuất âm thanh thành công!",
    renderError: "Tổng hợp thất bại! Vui lòng kiểm tra nhật ký GPU.",
    languageChanged: "Đã đồng bộ ngôn ngữ sang 'Tiếng Việt'.",
    fileLoaded: "Đã tải tệp dự án thành công."
  },
  validation: {
    required: "Trường này là bắt buộc.",
    invalidNumber: "Vui lòng nhập một số hợp lệ.",
    fileTooLarge: "Kích thước tệp vượt quá giới hạn tối đa."
  }
};
