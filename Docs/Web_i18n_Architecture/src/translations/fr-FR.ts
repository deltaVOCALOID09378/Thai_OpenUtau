/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: fr-FR.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/translations/fr-FR.ts
 * Description: French (Français) Translation Dictionary
 * ========================================================================
 */

import { TranslationSchema } from "../models/i18n.types";

export const fr_FR: TranslationSchema = {
  app: {
    title: "DELTA SYNTH Voice Studio",
    subtitle: "Plateforme de synthèse vocale et de chant neuronal de niveau entreprise",
    version: "Version 35.1.7",
    tagline: "Intelligence audio ultra-rapide et à faible latence propulsée par une architecture moderne."
  },
  nav: {
    home: "Accueil",
    studio: "Studio d'édition",
    synthesis: "Synthèse vocale & DSP",
    models: "Modèles vocaux & Jeux de données",
    settings: "Paramètres",
    docs: "Documentation & API",
    community: "Communauté",
    language: "Langue / Région"
  },
  studio: {
    projectTitle: "Titre du projet",
    track: "Piste vocale",
    tempo: "Tempo (BPM)",
    timeSignature: "Signature rythmique",
    play: "Lecture",
    pause: "Pause",
    stop: "Arrêt",
    render: "Rendre l'audio",
    rendering: "Synthèse de la performance vocale...",
    exportWav: "Exporter en WAV",
    importMidi: "Importer MIDI / MusicXML",
    timeline: "Chronologie musicale",
    notesCount: "Nombre de notes"
  },
  synthesis: {
    pitchShift: "Transposition (demi-tons)",
    f0Method: "Algorithme d'extraction de hauteur (F0)",
    indexRate: "Taux d'indexation du modèle",
    protectVoice: "Protection des consonnes sourdes & souffles",
    resampler: "Moteur de rééchantillonnage",
    batchConvert: "Conversion audio par lots",
    renderStatus: "État du traitement",
    vocalQuality: "Qualité du timbre",
    autoPitchVibrato: "AutoPitch & Vibrato naturel"
  },
  telemetry: {
    cpu: "Utilisation du processeur (CPU)",
    vram: "Allocation GPU / VRAM",
    latency: "Latence en temps réel",
    epoch: "Époque actuelle",
    bufferSize: "Taille du tampon audio",
    statusReady: "Système prêt",
    statusBusy: "Traitement DSP haute performance"
  },
  settings: {
    title: "Préférences du système et du moteur",
    language: "Langue de l'interface",
    theme: "Thème (Sombre / Clair)",
    audioDriver: "Pilote de sortie audio",
    sampleRate: "Taux d'échantillonnage",
    saveSuccess: "Paramètres enregistrés avec succès",
    resetDefaults: "Rétablir les valeurs par défaut"
  },
  dialog: {
    confirm: "Confirmer",
    cancel: "Annuler",
    delete: "Supprimer",
    warning: "Avertissement important",
    close: "Fermer"
  },
  toast: {
    renderSuccess: "Synthèse audio terminée avec succès !",
    renderError: "Échec de la synthèse ! Veuillez vérifier les journaux.",
    languageChanged: "Langue de l'application synchronisée sur 'Français'.",
    fileLoaded: "Fichier projet chargé avec succès."
  },
  validation: {
    required: "Ce champ est obligatoire.",
    invalidNumber: "Veuillez entrer un nombre valide.",
    fileTooLarge: "Le fichier dépasse la taille maximale autorisée."
  }
};
