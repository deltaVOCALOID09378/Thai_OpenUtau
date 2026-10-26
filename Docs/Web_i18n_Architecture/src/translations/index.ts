/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: index.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/translations/index.ts
 * Description: Master Index and Metadata Registry for All Supported Locales
 * ========================================================================
 */

import { LocaleMetadata, SupportedLocale, TranslationSchema } from "../models/i18n.types";
import { th_TH } from "./th-TH";
import { en_US } from "./en-US";
import { ja_JP } from "./ja-JP";
import { zh_CN } from "./zh-CN";
import { zh_TW } from "./zh-TW";
import { ko_KR } from "./ko-KR";
import { de_DE } from "./de-DE";
import { fr_FR } from "./fr-FR";
import { es_ES } from "./es-ES";
import { vi_VN } from "./vi-VN";

export const SUPPORTED_LOCALES: LocaleMetadata[] = [
  {
    code: "th-TH",
    name: "ภาษาไทย",
    englishName: "Thai",
    flag: "🇹🇭",
    countryCode: "TH",
    direction: "ltr",
    dateFormat: "dd/MM/yyyy"
  },
  {
    code: "en-US",
    name: "English",
    englishName: "English (US)",
    flag: "🇺🇸",
    countryCode: "US",
    direction: "ltr",
    dateFormat: "MM/dd/yyyy"
  },
  {
    code: "ja-JP",
    name: "日本語",
    englishName: "Japanese",
    flag: "🇯🇵",
    countryCode: "JP",
    direction: "ltr",
    dateFormat: "yyyy/MM/dd"
  },
  {
    code: "zh-CN",
    name: "简体中文",
    englishName: "Chinese (Simplified)",
    flag: "🇨🇳",
    countryCode: "CN",
    direction: "ltr",
    dateFormat: "yyyy-MM-dd"
  },
  {
    code: "zh-TW",
    name: "繁體中文",
    englishName: "Chinese (Traditional)",
    flag: "🇹🇼",
    countryCode: "TW",
    direction: "ltr",
    dateFormat: "yyyy/MM/dd"
  },
  {
    code: "ko-KR",
    name: "한국어",
    englishName: "Korean",
    flag: "🇰🇷",
    countryCode: "KR",
    direction: "ltr",
    dateFormat: "yyyy. MM. dd."
  },
  {
    code: "de-DE",
    name: "Deutsch",
    englishName: "German",
    flag: "🇩🇪",
    countryCode: "DE",
    direction: "ltr",
    dateFormat: "dd.MM.yyyy"
  },
  {
    code: "fr-FR",
    name: "Français",
    englishName: "French",
    flag: "🇫🇷",
    countryCode: "FR",
    direction: "ltr",
    dateFormat: "dd/MM/yyyy"
  },
  {
    code: "es-ES",
    name: "Español",
    englishName: "Spanish",
    flag: "🇪🇸",
    countryCode: "ES",
    direction: "ltr",
    dateFormat: "dd/MM/yyyy"
  },
  {
    code: "vi-VN",
    name: "Tiếng Việt",
    englishName: "Vietnamese",
    flag: "🇻🇳",
    countryCode: "VN",
    direction: "ltr",
    dateFormat: "dd/MM/yyyy"
  }
];

export const TRANSLATION_CATALOGS: Record<SupportedLocale, TranslationSchema> = {
  "th-TH": th_TH,
  "en-US": en_US,
  "ja-JP": ja_JP,
  "zh-CN": zh_CN,
  "zh-TW": zh_TW,
  "ko-KR": ko_KR,
  "de-DE": de_DE,
  "fr-FR": fr_FR,
  "es-ES": es_ES,
  "vi-VN": vi_VN
};

export { th_TH, en_US, ja_JP, zh_CN, zh_TW, ko_KR, de_DE, fr_FR, es_ES, vi_VN };
