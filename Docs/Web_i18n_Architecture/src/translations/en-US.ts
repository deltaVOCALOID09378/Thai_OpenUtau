/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: en-US.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/translations/en-US.ts
 * Description: English (United States / Global) Translation Dictionary
 * ========================================================================
 */

import { TranslationSchema } from "../models/i18n.types";

export const en_US: TranslationSchema = {
  app: {
    title: "DELTA SYNTH Voice Studio",
    subtitle: "Enterprise-grade Neural Singing & Speech Synthesis Platform",
    version: "Version 35.1.7",
    tagline: "High-performance, low-latency audio intelligence powered by modern architecture."
  },
  nav: {
    home: "Home",
    studio: "Editor Studio",
    synthesis: "Voice Synthesis & DSP",
    models: "Models & Datasets",
    settings: "Settings",
    docs: "Docs & API",
    community: "Community",
    language: "Language / Region"
  },
  studio: {
    projectTitle: "Project Title",
    track: "Vocal Track",
    tempo: "Tempo (BPM)",
    timeSignature: "Time Signature",
    play: "Play",
    pause: "Pause",
    stop: "Stop",
    render: "Render Audio",
    rendering: "Synthesizing vocal performance...",
    exportWav: "Export WAV",
    importMidi: "Import MIDI / MusicXML",
    timeline: "Musical Timeline",
    notesCount: "Total Notes"
  },
  synthesis: {
    pitchShift: "Key Transpose (Semitones)",
    f0Method: "Pitch Extraction (F0 Method)",
    indexRate: "Model Retrieval Index Rate",
    protectVoice: "Voiceless & Consonant Protection",
    resampler: "Resampler Engine",
    batchConvert: "Batch Audio Converter",
    renderStatus: "Processing Status",
    vocalQuality: "Vocal Timbre Quality",
    autoPitchVibrato: "AutoPitch & Natural Vibrato"
  },
  telemetry: {
    cpu: "CPU Utilization",
    vram: "GPU / VRAM Allocation",
    latency: "Realtime Latency",
    epoch: "Current Epoch",
    bufferSize: "Audio Buffer Size",
    statusReady: "System Ready",
    statusBusy: "Processing High-Load DSP"
  },
  settings: {
    title: "System & Engine Preferences",
    language: "Interface Language",
    theme: "Theme (Dark / Light)",
    audioDriver: "Audio Output Driver",
    sampleRate: "Audio Sample Rate",
    saveSuccess: "Preferences saved successfully",
    resetDefaults: "Reset to Factory Defaults"
  },
  dialog: {
    confirm: "Confirm",
    cancel: "Cancel",
    delete: "Delete",
    warning: "Important Warning",
    close: "Close"
  },
  toast: {
    renderSuccess: "Audio synthesis finished successfully!",
    renderError: "Synthesis failed! Please check GPU/VRAM logs.",
    languageChanged: "Application language synchronized to 'English (US)'.",
    fileLoaded: "Project file loaded successfully."
  },
  validation: {
    required: "This field is required.",
    invalidNumber: "Please input a valid numeric value.",
    fileTooLarge: "Uploaded file exceeds the maximum permitted size."
  }
};
