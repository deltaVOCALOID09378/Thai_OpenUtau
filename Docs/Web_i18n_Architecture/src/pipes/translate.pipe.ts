/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: translate.pipe.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/pipes/translate.pipe.ts
 * Description: Ultra-Fast Reactive Translation Pipe with Signal Binding
 * ========================================================================
 */

import { Pipe, PipeTransform, inject } from "@angular/core";
import { LanguageSignalService } from "../services/language-signal.service";

@Pipe({
  name: "translate",
  standalone: true,
  pure: false // ช่วยให้ Template อัปเดตทันทีเมื่อ currentLocale signal เปลี่ยนแปลง
})
export class TranslatePipe implements PipeTransform {
  private readonly langService = inject(LanguageSignalService);

  transform(key: string, params?: Record<string, string | number>): string {
    // ผูกสัญญาณ (Signal subscription) ให้ Template รับรู้การเปลี่ยนภาษาทันที
    const _ = this.langService.currentLocale();
    return this.langService.translate(key, params);
  }
}
