/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: es-ES.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/translations/es-ES.ts
 * Description: Spanish (Español) Translation Dictionary
 * ========================================================================
 */

import { TranslationSchema } from "../models/i18n.types";

export const es_ES: TranslationSchema = {
  app: {
    title: "DELTA SYNTH Voice Studio",
    subtitle: "Plataforma empresarial de síntesis vocal y canto neuronal",
    version: "Versión 35.1.7",
    tagline: "Inteligencia de audio de alto rendimiento y ultra baja latencia con arquitectura moderna."
  },
  nav: {
    home: "Inicio",
    studio: "Estudio de edición",
    synthesis: "Síntesis de voz y DSP",
    models: "Modelos de voz y conjuntos de datos",
    settings: "Configuración",
    docs: "Documentación y API",
    community: "Comunidad",
    language: "Idioma / Región"
  },
  studio: {
    projectTitle: "Título del proyecto",
    track: "Pista vocal",
    tempo: "Tempo (BPM)",
    timeSignature: "Compás",
    play: "Reproducir",
    pause: "Pausar",
    stop: "Detener",
    render: "Renderizar audio",
    rendering: "Sintetizando interpretación vocal...",
    exportWav: "Exportar WAV",
    importMidi: "Importar MIDI / MusicXML",
    timeline: "Línea de tiempo musical",
    notesCount: "Notas totales"
  },
  synthesis: {
    pitchShift: "Transposición de tono (semitonos)",
    f0Method: "Extracción de tono (Método F0)",
    indexRate: "Tasa de recuperación del modelo",
    protectVoice: "Protección de consonantes sordas y respiración",
    resampler: "Motor de remuestreo (Resampler)",
    batchConvert: "Conversión de audio por lotes",
    renderStatus: "Estado del procesamiento",
    vocalQuality: "Calidad del timbre vocal",
    autoPitchVibrato: "AutoPitch y Vibrato natural"
  },
  telemetry: {
    cpu: "Uso de CPU",
    vram: "Asignación de GPU / VRAM",
    latency: "Latencia en tiempo real",
    epoch: "Época actual (Epoch)",
    bufferSize: "Tamaño del búfer de audio",
    statusReady: "Sistema listo",
    statusBusy: "Procesamiento intensivo DSP"
  },
  settings: {
    title: "Preferencias del sistema y motor",
    language: "Idioma de la interfaz",
    theme: "Tema (Oscuro / Claro)",
    audioDriver: "Controlador de salida de audio",
    sampleRate: "Frecuencia de muestreo",
    saveSuccess: "Preferencias guardadas con éxito",
    resetDefaults: "Restablecer valores predeterminados"
  },
  dialog: {
    confirm: "Confirmar",
    cancel: "Cancelar",
    delete: "Eliminar",
    warning: "Advertencia importante",
    close: "Cerrar"
  },
  toast: {
    renderSuccess: "¡Síntesis de audio completada con éxito!",
    renderError: "¡Falló la síntesis! Verifique los registros del sistema.",
    languageChanged: "Idioma sincronizado a 'Español'.",
    fileLoaded: "Archivo de proyecto cargado con éxito."
  },
  validation: {
    required: "Este campo es obligatorio.",
    invalidNumber: "Por favor, introduzca un número válido.",
    fileTooLarge: "El archivo supera el tamaño máximo permitido."
  }
};
