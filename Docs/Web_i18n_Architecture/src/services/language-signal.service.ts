/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: language-signal.service.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/services/language-signal.service.ts
 * Description: Enterprise Reactive Signal-Based i18n Engine with Real-Time
 *              DOM Sync, Multi-Tab Broadcast, and Zero-Lag Reactivity
 * ========================================================================
 */

import {
  Injectable,
  signal,
  computed,
  effect,
  inject,
  PLATFORM_ID
} from "@angular/core";
import { isPlatformBrowser } from "@angular/common";
import { Title } from "@angular/platform-browser";
import {
  SupportedLocale,
  LocaleMetadata,
  TranslationSchema
} from "../models/i18n.types";
import {
  SUPPORTED_LOCALES,
  TRANSLATION_CATALOGS
} from "../translations/index";

@Injectable({
  providedIn: "root"
})
export class LanguageSignalService {
  private readonly titleService = inject(Title);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private readonly STORAGE_KEY = "delta_synth_locale";
  private readonly BROADCAST_CHANNEL_NAME = "delta_synth_i18n_channel";
  private broadcastChannel: BroadcastChannel | null = null;

  // 1. Reactive Core State via Angular Signals
  readonly currentLocale = signal<SupportedLocale>(this.getInitialLocale());

  // 2. Computed Translations (Instant 0ms lookup, auto-memoized)
  readonly translations = computed<TranslationSchema>(() => {
    const locale = this.currentLocale();
    return TRANSLATION_CATALOGS[locale] ?? TRANSLATION_CATALOGS["th-TH"];
  });

  // 3. Computed Active Metadata
  readonly currentMetadata = computed<LocaleMetadata>(() => {
    const locale = this.currentLocale();
    return (
      SUPPORTED_LOCALES.find((item) => item.code === locale) ??
      SUPPORTED_LOCALES[0]
    );
  });

  // 4. List of all available locales
  readonly availableLocales = signal<readonly LocaleMetadata[]>(SUPPORTED_LOCALES);

  constructor() {
    // Synchronize HTML attributes, Document Title, and Storage whenever locale changes
    effect(() => {
      const locale = this.currentLocale();
      const meta = this.currentMetadata();
      const t = this.translations();

      if (this.isBrowser) {
        // Sync document attributes
        document.documentElement.lang = locale;
        document.documentElement.dir = meta.direction;

        // Sync Document Title
        this.titleService.setTitle(`${t.app.title} — ${t.app.subtitle}`);

        // Persist to LocalStorage
        try {
          localStorage.setItem(this.STORAGE_KEY, locale);
        } catch (e) {
          console.warn("[DELTA SYNTH i18n] Failed to persist locale to localStorage:", e);
        }
      }
    });

    // Initialize cross-tab synchronization
    if (this.isBrowser) {
      this.initMultiTabSync();
    }
  }

  /**
   * สลับเปลี่ยนภาษาของทั้งเว็บไซต์ในพริบตา (Zero-Lag Synchronous Switch)
   * ส่งผลกระทบต่อทุกคอมโพเนนต์ ทุกหน้าต่าง และทุกแท็บในทันที
   */
  setLocale(locale: SupportedLocale, broadcast: boolean = true): void {
    if (this.currentLocale() === locale) {
      return;
    }

    if (!TRANSLATION_CATALOGS[locale]) {
      console.warn(`[DELTA SYNTH i18n] Unsupported locale requested: ${locale}, falling back to 'th-TH'`);
      locale = "th-TH";
    }

    // อัปเดต Signal ทันที -> ทุกคอมโพเนนต์ที่ผูกกับ signal นี้จะเรนเดอร์ใหม่ทันที
    this.currentLocale.set(locale);

    // กระจายสัญญาณไปยังแท็บอื่นๆ ของเว็บไซต์
    if (broadcast && this.isBrowser && this.broadcastChannel) {
      try {
        this.broadcastChannel.postMessage({ type: "LOCALE_CHANGED", locale });
      } catch (err) {
        console.warn("[DELTA SYNTH i18n] BroadcastChannel message failed:", err);
      }
    }
  }

  /**
   * ดึงข้อความแปลตามคีย์ (Dot Notation Path) พร้อมรองรับตัวแปรแทรก (Interpolation)
   * ตัวอย่าง: t('nav.home') หรือ t('studio.notesCount', { count: 42 })
   */
  translate(keyPath: string, params?: Record<string, string | number>): string {
    const catalog = this.translations() as any;
    const segments = keyPath.split(".");
    let current: any = catalog;

    for (const segment of segments) {
      if (current && typeof current === "object" && segment in current) {
        current = current[segment];
      } else {
        return keyPath; // Fallback คืนชื่อคีย์หากหาไม่พบ
      }
    }

    if (typeof current !== "string") {
      return keyPath;
    }

    // ตัวแปรแทรก (Interpolation replacement {{var}})
    if (params) {
      return current.replace(/\{\{\s*(\w+)\s*\}\}/g, (_, key) => {
        return key in params ? String(params[key]) : `{{${key}}}`;
      });
    }

    return current;
  }

  /**
   * ฟอร์แมตตัวเลขตามมาตรฐานสากลของแต่ละประเทศ (Intl.NumberFormat)
   */
  formatNumber(value: number, options?: Intl.NumberFormatOptions): string {
    try {
      return new Intl.NumberFormat(this.currentLocale(), options).format(value);
    } catch {
      return value.toString();
    }
  }

  /**
   * ฟอร์แมตวันที่และเวลาตามมาตรฐานของแต่ละประเทศ (Intl.DateTimeFormat)
   */
  formatDate(date: Date | number, options?: Intl.DateTimeFormatOptions): string {
    try {
      return new Intl.DateTimeFormat(this.currentLocale(), options).format(date);
    } catch {
      return new Date(date).toLocaleDateString();
    }
  }

  /**
   * ฟอร์แมตค่าเงินตามสกุลเงินของแต่ละประเทศ
   */
  formatCurrency(value: number, currencyCode: string): string {
    return this.formatNumber(value, {
      style: "currency",
      currency: currencyCode
    });
  }

  /**
   * ตรวจสอบภาษาเริ่มต้นจาก LocalStorage หรือเบราว์เซอร์ของผู้ใช้งาน
   */
  private getInitialLocale(): SupportedLocale {
    if (!this.isBrowser) {
      return "th-TH"; // Server-Side Default: Thai Language
    }

    try {
      const saved = localStorage.getItem(this.STORAGE_KEY) as SupportedLocale;
      if (saved && TRANSLATION_CATALOGS[saved]) {
        return saved;
      }
    } catch {
      // Ignore storage access errors
    }

    // ตรวจสอบภาษาของเบราว์เซอร์
    const browserLang = navigator.language;
    if (browserLang) {
      // จับคู่ตรงตัว (Exact match e.g., 'th-TH', 'ja-JP')
      if (TRANSLATION_CATALOGS[browserLang as SupportedLocale]) {
        return browserLang as SupportedLocale;
      }

      // จับคู่ด้วย Prefix 2 ตัวแรก (Prefix match e.g., 'ja' -> 'ja-JP')
      const prefix = browserLang.split("-")[0].toLowerCase();
      const matched = SUPPORTED_LOCALES.find((item) =>
        item.code.toLowerCase().startsWith(prefix)
      );
      if (matched) {
        return matched.code;
      }
    }

    // ค่าเริ่มต้นมาตรฐานตามระเบียบ DELTA SYNTH
    return "th-TH";
  }

  /**
   * ระบบกระจายการสลับภาษาระหว่างแท็บ (Cross-Tab Synchronization)
   */
  private initMultiTabSync(): void {
    if (typeof BroadcastChannel !== "undefined") {
      this.broadcastChannel = new BroadcastChannel(this.BROADCAST_CHANNEL_NAME);
      this.broadcastChannel.onmessage = (event) => {
        if (event.data?.type === "LOCALE_CHANGED" && event.data?.locale) {
          // ซิงค์ภาษาจากแท็บอื่นโดยไม่ส่ง Broadcast ซ้ำ
          this.setLocale(event.data.locale, false);
        }
      };
    } else {
      // Fallback สำหรับเบราว์เซอร์รุ่นเก่าผ่าน StorageEvent
      window.addEventListener("storage", (event) => {
        if (event.key === this.STORAGE_KEY && event.newValue) {
          const newLocale = event.newValue as SupportedLocale;
          if (TRANSLATION_CATALOGS[newLocale]) {
            this.setLocale(newLocale, false);
          }
        }
      });
    }
  }
}
