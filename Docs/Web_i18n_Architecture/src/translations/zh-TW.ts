/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: zh-TW.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/translations/zh-TW.ts
 * Description: Traditional Chinese (繁體中文) Translation Dictionary
 * ========================================================================
 */

import { TranslationSchema } from "../models/i18n.types";

export const zh_TW: TranslationSchema = {
  app: {
    title: "DELTA SYNTH 語音工作室",
    subtitle: "企業級神經歌聲與語音合成平台",
    version: "版本 35.1.7",
    tagline: "依託現代架構，實現極速、低延遲的高效能音訊智慧"
  },
  nav: {
    home: "首頁",
    studio: "製作工作室",
    synthesis: "語音合成與DSP",
    models: "聲線模型與資料集",
    settings: "系統設定",
    docs: "開發文件與API",
    community: "社群",
    language: "語言 / 地區"
  },
  studio: {
    projectTitle: "專案名稱",
    track: "人聲音軌",
    tempo: "曲速 (BPM)",
    timeSignature: "拍子記號",
    play: "播放",
    pause: "暫停",
    stop: "停止",
    render: "算圖 / 渲染音訊",
    rendering: "正在合成人聲演出...",
    exportWav: "匯出WAV音訊",
    importMidi: "匯入MIDI / MusicXML",
    timeline: "音符時間軸",
    notesCount: "音符總數"
  },
  synthesis: {
    pitchShift: "變調 (半音單位)",
    f0Method: "音高提取演算法 (F0 Method)",
    indexRate: "特徵檢索索引率 (Index Rate)",
    protectVoice: "清輔音與呼吸保護",
    resampler: "重採樣引擎 (Resampler)",
    batchConvert: "批次音訊轉換",
    renderStatus: "處理狀態",
    vocalQuality: "音色品質",
    autoPitchVibrato: "自動音高與自然顫音"
  },
  telemetry: {
    cpu: "CPU 使用率",
    vram: "顯示卡記憶體 / VRAM 佔用",
    latency: "即時延遲",
    epoch: "當前週期 (Epoch)",
    bufferSize: "音訊緩衝區大小",
    statusReady: "系統就緒",
    statusBusy: "正在進行高速DSP運算"
  },
  settings: {
    title: "系統與引擎偏好設定",
    language: "介面語言",
    theme: "佈景主題 (深色 / 淺色)",
    audioDriver: "音訊輸出驅動程式",
    sampleRate: "音訊取樣率",
    saveSuccess: "設定已成功儲存",
    resetDefaults: "恢復原廠預設值"
  },
  dialog: {
    confirm: "確認",
    cancel: "取消",
    delete: "刪除",
    warning: "重要警告",
    close: "關閉"
  },
  toast: {
    renderSuccess: "音訊渲染成功完成！",
    renderError: "合成失敗，請檢查顯示卡或記憶體紀錄檔。",
    languageChanged: "介面語言已成功同步為「繁體中文」。",
    fileLoaded: "專案檔案載入完成。"
  },
  validation: {
    required: "此欄位為必填。",
    invalidNumber: "請輸入有效的數值。",
    fileTooLarge: "上傳檔案超出最大限制。"
  }
};
