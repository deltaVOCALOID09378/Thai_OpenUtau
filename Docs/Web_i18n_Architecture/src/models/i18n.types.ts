/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: i18n.types.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/models/i18n.types.ts
 * Description: Strong TypeScript Contracts & Interfaces for Synchronized
 *              Multi-Country i18n/L10n Architecture
 * ========================================================================
 */

export type SupportedLocale =
  | "th-TH" // ภาษาไทย (Thailand) - ระบบหลัก DELTA SYNTH
  | "en-US" // English (United States / Global)
  | "ja-JP" // 日本語 (Japan)
  | "zh-CN" // 简体中文 (China)
  | "zh-TW" // 繁體中文 (Taiwan / Hong Kong)
  | "ko-KR" // 한국어 (South Korea)
  | "de-DE" // Deutsch (Germany)
  | "fr-FR" // Français (France)
  | "es-ES" // Español (Spain / Latin America)
  | "vi-VN"; // Tiếng Việt (Vietnam)

export type TextDirection = "ltr" | "rtl";

export interface LocaleMetadata {
  code: SupportedLocale;
  name: string;        // Native display name (e.g., "ภาษาไทย", "日本語")
  englishName: string; // International reference name
  flag: string;        // Country flag emoji / icon identifier
  countryCode: string; // ISO 3166-1 alpha-2
  direction: TextDirection;
  dateFormat: string;
}

export interface TranslationSchema {
  app: {
    title: string;
    subtitle: string;
    version: string;
    tagline: string;
  };
  nav: {
    home: string;
    studio: string;
    synthesis: string;
    models: string;
    settings: string;
    docs: string;
    community: string;
    language: string;
  };
  studio: {
    projectTitle: string;
    track: string;
    tempo: string;
    timeSignature: string;
    play: string;
    pause: string;
    stop: string;
    render: string;
    rendering: string;
    exportWav: string;
    importMidi: string;
    timeline: string;
    notesCount: string;
  };
  synthesis: {
    pitchShift: string;
    f0Method: string;
    indexRate: string;
    protectVoice: string;
    resampler: string;
    batchConvert: string;
    renderStatus: string;
    vocalQuality: string;
    autoPitchVibrato: string;
  };
  telemetry: {
    cpu: string;
    vram: string;
    latency: string;
    epoch: string;
    bufferSize: string;
    statusReady: string;
    statusBusy: string;
  };
  settings: {
    title: string;
    language: string;
    theme: string;
    audioDriver: string;
    sampleRate: string;
    saveSuccess: string;
    resetDefaults: string;
  };
  dialog: {
    confirm: string;
    cancel: string;
    delete: string;
    warning: string;
    close: string;
  };
  toast: {
    renderSuccess: string;
    renderError: string;
    languageChanged: string;
    fileLoaded: string;
  };
  validation: {
    required: string;
    invalidNumber: string;
    fileTooLarge: string;
  };
}
