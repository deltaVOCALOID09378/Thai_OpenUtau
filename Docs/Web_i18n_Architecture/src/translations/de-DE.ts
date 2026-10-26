/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: de-DE.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/translations/de-DE.ts
 * Description: German (Deutsch) Translation Dictionary
 * ========================================================================
 */

import { TranslationSchema } from "../models/i18n.types";

export const de_DE: TranslationSchema = {
  app: {
    title: "DELTA SYNTH Voice Studio",
    subtitle: "Professionelle Plattform für neuronale Gesangs- und Sprachsynthese",
    version: "Version 35.1.7",
    tagline: "Hochleistungsfähige, latenzarme Audio-Intelligenz auf moderner Architektur."
  },
  nav: {
    home: "Startseite",
    studio: "Editor-Studio",
    synthesis: "Sprachsynthese & DSP",
    models: "Stimmmodelle & Datensätze",
    settings: "Einstellungen",
    docs: "Dokumentation & API",
    community: "Community",
    language: "Sprache / Region"
  },
  studio: {
    projectTitle: "Projektname",
    track: "Vokalspur",
    tempo: "Tempo (BPM)",
    timeSignature: "Taktart",
    play: "Wiedergabe",
    pause: "Pause",
    stop: "Stopp",
    render: "Audio rendern",
    rendering: "Gesang wird synthetisiert...",
    exportWav: "WAV exportieren",
    importMidi: "MIDI / MusicXML importieren",
    timeline: "Zeitleiste",
    notesCount: "Gesamte Noten"
  },
  synthesis: {
    pitchShift: "Tonhöhenverschiebung (Halbtöne)",
    f0Method: "Tonhöhen-Extraktion (F0-Methode)",
    indexRate: "Index-Suchrate des Modells",
    protectVoice: "Schutz stimmloser Laute & Konsonanten",
    resampler: "Resampler-Engine",
    batchConvert: "Stapelverarbeitung",
    renderStatus: "Verarbeitungsstatus",
    vocalQuality: "Klangqualität",
    autoPitchVibrato: "AutoPitch & Natürliches Vibrato"
  },
  telemetry: {
    cpu: "CPU-Auslastung",
    vram: "GPU / VRAM-Belegung",
    latency: "Echtzeit-Latenz",
    epoch: "Aktuelle Epoche",
    bufferSize: "Audiopuffergröße",
    statusReady: "System bereit",
    statusBusy: "Berechnung mit hoher DSP-Last"
  },
  settings: {
    title: "System- & Engine-Einstellungen",
    language: "Oberflächensprache",
    theme: "Design (Dunkel / Hell)",
    audioDriver: "Audio-Ausgabetreiber",
    sampleRate: "Abtastrate (Sample Rate)",
    saveSuccess: "Einstellungen erfolgreich gespeichert",
    resetDefaults: "Auf Werkseinstellungen zurücksetzen"
  },
  dialog: {
    confirm: "Bestätigen",
    cancel: "Abbrechen",
    delete: "Löschen",
    warning: "Wichtiger Hinweis",
    close: "Schließen"
  },
  toast: {
    renderSuccess: "Audiosynthese erfolgreich abgeschlossen!",
    renderError: "Synthese fehlgeschlagen! Bitte Protokolle prüfen.",
    languageChanged: "Sprache erfolgreich auf 'Deutsch' umgestellt.",
    fileLoaded: "Projektdatei erfolgreich geladen."
  },
  validation: {
    required: "Dieses Feld ist erforderlich.",
    invalidNumber: "Bitte geben Sie eine gültige Zahl ein.",
    fileTooLarge: "Die Dateigröße überschreitet das zulässige Limit."
  }
};
