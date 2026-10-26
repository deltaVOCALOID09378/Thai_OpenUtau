/**
 * ========================================================================
 * Date: 26/10/2569 (BE) / 26/10/2026 (CE)
 * File: language-switcher.component.ts
 * Version: 1.0.0
 * Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
 * [ Original by DELTA SYNTH ]
 * Location: Docs/Web_i18n_Architecture/src/components/language-switcher.component.ts
 * Description: DELTA SYNTH Standard Modern Language Switcher Component
 *              (Leelawadee UI, Corporate Colors, Signal-Driven)
 * ========================================================================
 */

import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { LanguageSignalService } from "../services/language-signal.service";
import { SupportedLocale, LocaleMetadata } from "../models/i18n.types";
import { TranslatePipe } from "../pipes/translate.pipe";

@Component({
  selector: "delta-language-switcher",
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="delta-lang-switcher" (clickOutside)="isOpen.set(false)">
      <!-- ปุ่มเลือกภาษาหลัก (Trigger Button) -->
      <button
        type="button"
        class="delta-lang-btn"
        [attr.aria-expanded]="isOpen()"
        aria-haspopup="listbox"
        (click)="toggleDropdown()"
        [title]="'nav.language' | translate"
      >
        <span class="flag-icon" aria-hidden="true">{{ currentMetadata().flag }}</span>
        <span class="lang-text">{{ currentMetadata().name }}</span>
        <svg
          class="arrow-icon"
          [class.open]="isOpen()"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
        >
          <polyline points="6 9 12 15 18 9"></polyline>
        </svg>
      </button>

      <!-- รายการภาษาที่เปิดแสดง (Dropdown List) -->
      @if (isOpen()) {
        <div class="delta-lang-menu" role="listbox">
          @for (item of availableLocales(); track item.code) {
            <button
              type="button"
              class="delta-lang-option"
              [class.active]="item.code === currentLocale()"
              role="option"
              [attr.aria-selected]="item.code === currentLocale()"
              (click)="selectLocale(item.code)"
            >
              <span class="flag-icon">{{ item.flag }}</span>
              <div class="option-details">
                <span class="option-native-name">{{ item.name }}</span>
                <span class="option-english-name">{{ item.englishName }}</span>
              </div>
              @if (item.code === currentLocale()) {
                <svg class="check-icon" viewBox="0 0 24 24" fill="none" stroke="#CC2200" stroke-width="2.5">
                  <polyline points="20 6 9 17 4 12"></polyline>
                </svg>
              }
            </button>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: inline-block;
      font-family: "Leelawadee UI", "Leelawadee", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    }

    .delta-lang-switcher {
      position: relative;
      user-select: none;
    }

    .delta-lang-btn {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      height: 36px;
      padding: 0 12px;
      border-radius: 6px;
      border: 1px solid #333333;
      background-color: #1A1A1A;
      color: #F0F0F0;
      font-size: 13px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.18s cubic-bezier(0.4, 0, 0.2, 1);
    }

    .delta-lang-btn:hover {
      border-color: #CC2200;
      background-color: #242424;
      color: #FFFFFF;
    }

    .delta-lang-btn:active {
      background-color: #991100;
    }

    .flag-icon {
      font-size: 16px;
      line-height: 1;
    }

    .arrow-icon {
      width: 14px;
      height: 14px;
      margin-left: 2px;
      transition: transform 0.2s ease;
    }

    .arrow-icon.open {
      transform: rotate(180deg);
    }

    .delta-lang-menu {
      position: absolute;
      top: calc(100% + 6px);
      right: 0;
      min-width: 200px;
      max-height: 380px;
      overflow-y: auto;
      background-color: #1A1A1A;
      border: 1px solid #333333;
      border-top: 2px solid #CC2200;
      border-radius: 6px;
      box-shadow: 0 8px 24px rgba(0, 0, 0, 0.6);
      z-index: 1000;
      padding: 4px;
    }

    .delta-lang-option {
      display: flex;
      align-items: center;
      gap: 10px;
      width: 100%;
      padding: 8px 10px;
      border: none;
      border-radius: 4px;
      background: transparent;
      color: #F0F0F0;
      text-align: left;
      cursor: pointer;
      font-size: 13px;
      transition: background-color 0.15s ease, color 0.15s ease;
    }

    .delta-lang-option:hover {
      background-color: #2A2A2A;
      color: #FF4422;
    }

    .delta-lang-option.active {
      background-color: rgba(204, 34, 0, 0.15);
      color: #FFFFFF;
      font-weight: 600;
    }

    .option-details {
      flex: 1;
      display: flex;
      flex-direction: column;
    }

    .option-native-name {
      font-size: 13px;
    }

    .option-english-name {
      font-size: 11px;
      color: #888888;
    }

    .check-icon {
      width: 16px;
      height: 16px;
    }

    /* Scrollbar styling */
    .delta-lang-menu::-webkit-scrollbar {
      width: 6px;
    }
    .delta-lang-menu::-webkit-scrollbar-thumb {
      background: #444444;
      border-radius: 3px;
    }
    .delta-lang-menu::-webkit-scrollbar-thumb:hover {
      background: #CC2200;
    }
  `]
})
export class LanguageSwitcherComponent {
  private readonly langService = inject(LanguageSignalService);

  readonly isOpen = signal<boolean>(false);
  readonly currentLocale = this.langService.currentLocale;
  readonly currentMetadata = this.langService.currentMetadata;
  readonly availableLocales = this.langService.availableLocales;

  toggleDropdown(): void {
    this.isOpen.update((v) => !v);
  }

  selectLocale(locale: SupportedLocale): void {
    this.langService.setLocale(locale);
    this.isOpen.set(false);
  }
}
