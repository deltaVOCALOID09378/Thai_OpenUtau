/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: translate.directive.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/directives/translate.directive.ts
 * Description: DOM Attribute & Text Translation Directive with Fine-Grained Signals
 * ========================================================================
 */

import {
  Directive,
  ElementRef,
  Input,
  effect,
  inject,
  signal
} from "@angular/core";
import { LanguageSignalService } from "../services/language-signal.service";

@Directive({
  selector: "[translate], [translateAttr]",
  standalone: true
})
export class TranslateDirective {
  private readonly el = inject(ElementRef<HTMLElement>);
  private readonly langService = inject(LanguageSignalService);

  private readonly keySignal = signal<string>("");
  private readonly paramsSignal = signal<Record<string, string | number> | undefined>(undefined);
  private readonly attrsSignal = signal<Record<string, string> | undefined>(undefined);

  @Input("translate")
  set key(value: string) {
    this.keySignal.set(value);
  }

  @Input("translateParams")
  set params(value: Record<string, string | number> | undefined) {
    this.paramsSignal.set(value);
  }

  @Input("translateAttr")
  set attrs(value: Record<string, string> | undefined) {
    this.attrsSignal.set(value);
  }

  constructor() {
    // ซิงค์ข้อความ textContent เมื่อ locale หรือ key เปลี่ยนแปลง
    effect(() => {
      const key = this.keySignal();
      const params = this.paramsSignal();
      // การอ่าน currentLocale ทำให้ effect ทำงานทุกครั้งที่เปลี่ยนภาษา
      const _ = this.langService.currentLocale();

      if (key) {
        this.el.nativeElement.textContent = this.langService.translate(key, params);
      }
    });

    // ซิงค์ attributes (เช่น placeholder, title, aria-label)
    effect(() => {
      const attrs = this.attrsSignal();
      const _ = this.langService.currentLocale();

      if (attrs) {
        for (const [attrName, translationKey] of Object.entries(attrs)) {
          const translatedValue = this.langService.translate(translationKey);
          this.el.nativeElement.setAttribute(attrName, translatedValue);
        }
      }
    });
  }
}
