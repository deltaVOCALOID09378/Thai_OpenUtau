/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: ngupgrade-i18n-bridge.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/bridge/ngupgrade-i18n-bridge.ts
 * Description: Bi-Directional Synchronization Bridge between Modern Angular
 *              LanguageSignalService and Legacy AngularJS $translate
 * ========================================================================
 */

import { downgradeInjectable } from "@angular/upgrade/static";
import { effect, Injector } from "@angular/core";
import { LanguageSignalService } from "../services/language-signal.service";
import { SupportedLocale } from "../models/i18n.types";

declare const angular: any;

/**
 * ติดตั้งระบบเชื่อมต่อการแปลภาษาข้ามระหว่าง AngularJS 1.x และ Modern Angular 18+
 * ทำให้การสลับภาษาส่งผลกระทบพร้อมกัน 100% ทั้งสองรันไทม์ ไร้การหน่วง ไร้การหลุดซิงค์
 */
export function setupNgUpgradeI18nBridge(
  injector: Injector,
  ng1RootScope: any,
  ng1Translate?: any
): void {
  const langService = injector.get(LanguageSignalService);

  let isSyncingFromNg1 = false;
  let isSyncingFromNg2 = false;

  // 1. ทิศทาง: Modern Angular (Signal) -> Legacy AngularJS ($translate & $rootScope)
  effect(
    () => {
      const currentLocale = langService.currentLocale();

      if (isSyncingFromNg1) {
        return; // ป้องกัน infinite loop
      }

      isSyncingFromNg2 = true;
      try {
        if (ng1Translate && typeof ng1Translate.use === "function") {
          ng1Translate.use(currentLocale);
        }

        // ส่ง Broadcast Event เข้าสู่ระบบ $rootScope ของ AngularJS
        if (ng1RootScope) {
          ng1RootScope.$broadcast("delta:localeChanged", { locale: currentLocale });
          ng1RootScope.$applyAsync();
        }
      } finally {
        isSyncingFromNg2 = false;
      }
    },
    { injector }
  );

  // 2. ทิศทาง: Legacy AngularJS ($translateChangeSuccess) -> Modern Angular (Signal)
  if (ng1RootScope) {
    ng1RootScope.$on(
      "$translateChangeSuccess",
      (_event: any, data: { language: string }) => {
        if (isSyncingFromNg2) {
          return;
        }

        const newLocale = data.language as SupportedLocale;
        isSyncingFromNg1 = true;
        try {
          langService.setLocale(newLocale);
        } finally {
          isSyncingFromNg1 = false;
        }
      }
    );
  }

  console.log("[DELTA SYNTH i18n Bridge] Bi-directional i18n synchronization registered successfully.");
}

/**
 * ทะเบียน Downgrade Service สำหรับ AngularJS โมดูลเดิม
 */
export function registerLegacyI18nModule(angularModule: any): void {
  angularModule.factory(
    "languageSignalService",
    downgradeInjectable(LanguageSignalService)
  );
}
