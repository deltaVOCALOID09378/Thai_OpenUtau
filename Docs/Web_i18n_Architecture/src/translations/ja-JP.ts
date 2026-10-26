/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: ja-JP.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/translations/ja-JP.ts
 * Description: Japanese (日本語) Translation Dictionary
 * ========================================================================
 */

import { TranslationSchema } from "../models/i18n.types";

export const ja_JP: TranslationSchema = {
  app: {
    title: "DELTA SYNTH ボイススタジオ",
    subtitle: "プロフェッショナル向けニューラル歌声・音声合成プラットフォーム",
    version: "バージョン 35.1.7",
    tagline: "最先端アーキテクチャによる超高速・低遅延の音声処理インテリジェンス"
  },
  nav: {
    home: "ホーム",
    studio: "エディタースタジオ",
    synthesis: "音声合成 & DSP",
    models: "音声モデル & データセット",
    settings: "システム設定",
    docs: "ドキュメント & API",
    community: "コミュニティ",
    language: "言語 / 地域"
  },
  studio: {
    projectTitle: "プロジェクト名",
    track: "ボーカルトラック",
    tempo: "テンポ (BPM)",
    timeSignature: "拍子記号",
    play: "再生",
    pause: "一時停止",
    stop: "停止",
    render: "音声をレンダリング",
    rendering: "歌声を合成中...",
    exportWav: "WAVファイル書き出し",
    importMidi: "MIDI / MusicXMLの読み込み",
    timeline: "タイムライン",
    notesCount: "総音符数"
  },
  synthesis: {
    pitchShift: "ピッチシフト (半音単位)",
    f0Method: "ピッチ抽出アルゴリズム (F0)",
    indexRate: "モデル特徴量検索率 (Index Rate)",
    protectVoice: "無声子音・ブレス保護",
    resampler: "リサンプラーエンジン",
    batchConvert: "一括バッチ変換",
    renderStatus: "レンダリング状態",
    vocalQuality: "音色クオリティ",
    autoPitchVibrato: "自動ピッチ・ビブラート"
  },
  telemetry: {
    cpu: "CPU使用率",
    vram: "GPU / VRAM使用量",
    latency: "リアルタイム遅延",
    epoch: "現在のエポック",
    bufferSize: "オーディオバッファサイズ",
    statusReady: "待機中・準備完了",
    statusBusy: "高速演算中"
  },
  settings: {
    title: "システム・エンジン環境設定",
    language: "表示言語",
    theme: "テーマ (ダーク / ライト)",
    audioDriver: "オーディオドライバ",
    sampleRate: "サンプリングレート",
    saveSuccess: "設定を正常に保存しました",
    resetDefaults: "初期設定にリセット"
  },
  dialog: {
    confirm: "確認",
    cancel: "キャンセル",
    delete: "削除",
    warning: "重要警告",
    close: "閉じる"
  },
  toast: {
    renderSuccess: "音声のレンダリングが正常に完了しました！",
    renderError: "合成に失敗しました。ログを確認してください。",
    languageChanged: "表示言語を「日本語」に切り替えました。",
    fileLoaded: "プロジェクトを読み込みました。"
  },
  validation: {
    required: "この項目は必須です。",
    invalidNumber: "有効な数値を入力してください。",
    fileTooLarge: "ファイルサイズが制限を超えています。"
  }
};
