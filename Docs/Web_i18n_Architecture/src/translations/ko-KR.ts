/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: ko-KR.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/translations/ko-KR.ts
 * Description: Korean (한국어) Translation Dictionary
 * ========================================================================
 */

import { TranslationSchema } from "../models/i18n.types";

export const ko_KR: TranslationSchema = {
  app: {
    title: "DELTA SYNTH 보이스 스튜디오",
    subtitle: "엔터프라이즈급 뉴럴 보컬 및 음성 합성 플랫폼",
    version: "버전 35.1.7",
    tagline: "현대적 아키텍처 기반의 초고속, 저지연 오디오 인텔리전스"
  },
  nav: {
    home: "홈",
    studio: "편집 스튜디오",
    synthesis: "음성 합성 & DSP",
    models: "음성 모델 및 데이터셋",
    settings: "시스템 설정",
    docs: "기술 문서 및 API",
    community: "커뮤니티",
    language: "언어 / 국가"
  },
  studio: {
    projectTitle: "프로젝트 제목",
    track: "보컬 트랙",
    tempo: "템포 (BPM)",
    timeSignature: "박자표",
    play: "재생",
    pause: "일시정지",
    stop: "정지",
    render: "오디오 렌더링",
    rendering: "보컬 퍼포먼스 합성 중...",
    exportWav: "WAV 내보내기",
    importMidi: "MIDI / MusicXML 가져오기",
    timeline: "뮤직 타임라인",
    notesCount: "총 음표 수"
  },
  synthesis: {
    pitchShift: "음정 조절 (반음 단위)",
    f0Method: "피치 추출 알고리즘 (F0)",
    indexRate: "모델 특징 인덱스 비율 (Index Rate)",
    protectVoice: "무성음 및 숨소리 보호",
    resampler: "리샘플러 엔진",
    batchConvert: "일괄 오디오 변환",
    renderStatus: "처리 상태",
    vocalQuality: "음색 품질",
    autoPitchVibrato: "자동 피치 및 자연스러운 바이브레이션"
  },
  telemetry: {
    cpu: "CPU 사용률",
    vram: "GPU / VRAM 할당량",
    latency: "실시간 대기시간",
    epoch: "현재 에포크 (Epoch)",
    bufferSize: "오디오 버퍼 크기",
    statusReady: "시스템 준비 완료",
    statusBusy: "고속 DSP 연산 처리 중"
  },
  settings: {
    title: "시스템 및 엔진 환경설정",
    language: "인터페이스 언어",
    theme: "테마 (다크 / 라이트)",
    audioDriver: "오디오 출력 드라이버",
    sampleRate: "오디오 샘플 레이트",
    saveSuccess: "환경설정이 성공적으로 저장되었습니다",
    resetDefaults: "초기값으로 재설정"
  },
  dialog: {
    confirm: "확인",
    cancel: "취소",
    delete: "삭제",
    warning: "중요 경고",
    close: "닫기"
  },
  toast: {
    renderSuccess: "오디오 렌더링이 성공적으로 완료되었습니다!",
    renderError: "합성에 실패했습니다. 로그를 확인해 주십시오.",
    languageChanged: "인터페이스 언어가 '한국어'로 동기화되었습니다.",
    fileLoaded: "프로젝트 파일을 성공적으로 불러왔습니다."
  },
  validation: {
    required: "이 항목은 필수 입력 사항입니다.",
    invalidNumber: "올바른 숫자를 입력해 주십시오.",
    fileTooLarge: "업로드한 파일이 최대 허용 크기를 초과했습니다."
  }
};
