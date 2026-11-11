// ============================================================================
// วัน/เดือน/ปี: 10 พฤศจิกายน 2569 (2026-11-10)
// ชื่อไฟล์: KeyTranslator.cs
// ลำดับเวอร์ชัน: 2.0
// เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// ตำแหน่งที่จัดวาง: OpenUtau/ViewModels/KeyTranslator.cs
// รายละเอียดการแก้ไขงาน: ขยายระบบตรวจสอบคีย์ลัดให้รองรับ Numpad 0-9, Enter/Return, Delete/Back เพื่อให้กดปุ่มลัดได้สะดวกไร้อุปสรรค
// ============================================================================
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Input;
using OpenUtau.Core.Util;

namespace OpenUtau.App.ViewModels {
    public static class KeyTranslator {
        public static readonly bool IsMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Converts ugly internal Avalonia key names into human-readable strings.
        /// Accounts for macOS and Linux naming conventions.
        /// </summary>
        public static string GetFriendlyName(string keyName) {
            return keyName switch {
                // Modifiers
                "Windows" or "LWin" or "RWin" => IsMac ? "⌘" : "Win",
                "LeftAlt" or "RightAlt" or "Alt" => IsMac ? "⌥" : "Alt",
                "Control" or "LeftCtrl" or "RightCtrl" or "LControl" or "RControl" => IsMac ? "⌘" : "Ctrl",
                "Shift" or "LeftShift" or "RightShift" => IsMac ? "⇧" : "Shift",
                
                // Navigation & Editing
                "Escape" => "Esc",
                "Return" => "Enter",
                "Back" => IsMac ? "Delete" : "Backspace",
                "Delete" => IsMac ? "Forward Del" : "Del",
                "Insert" => "Ins",
                "PageUp" => "PgUp",
                "PageDown" => "PgDn",
                "Capital" => "Caps Lock",
                "Scroll" => "Scroll Lock",
                "NumLock" => "Num Lock",
                "Snapshot" => "Print Screen",

                // Numpad
                "Divide" => "(Num /)",
                "Multiply" => "(Num *)",
                "Subtract" => "(Num -)",
                "Add" => "(Num +)",
                "Decimal" => "(Num .)",
                "NumPad0" => "Num 0", "NumPad1" => "Num 1", "NumPad2" => "Num 2",
                "NumPad3" => "Num 3", "NumPad4" => "Num 4", "NumPad5" => "Num 5",
                "NumPad6" => "Num 6", "NumPad7" => "Num 7", "NumPad8" => "Num 8",
                "NumPad9" => "Num 9",

                // Digits
                "D1" => "1", "D2" => "2", "D3" => "3", "D4" => "4", "D5" => "5",
                "D6" => "6", "D7" => "7", "D8" => "8", "D9" => "9", "D0" => "0",

                // OEM Symbols
                "OemTilde" or "Oem8" or "Oem3" => "~",
                "OemMinus" or "OemMinusSign" => "-",
                "OemPlus" or "OemPlusSign" => "=",
                "OemOpenBrackets" or "Oem4" => "[",
                "OemCloseBrackets" or "Oem6" => "]",
                "OemPipe" or "Oem5" or "OemBackslash" => "\\",
                "OemSemicolon" or "Oem1" => ";",
                "OemQuotes" or "Oem7" => "'",
                "OemComma" or "OemCommaSign" => ",",
                "OemPeriod" or "OemPeriodSign" => ".",
                "OemQuestion" or "Oem2" => "/",

                _ => keyName
            };
        }

        /// <summary>
        /// Fuzzy matcher for cross-platform keyboard quirks.
        /// If the exact enum doesn't match, it checks known hardware equivalents.
        /// </summary>
        public static bool IsKeyMatch(Key savedKey, Key pressedKey) {
            if (savedKey == pressedKey) return true;

            return savedKey switch {
                Key.D0 => pressedKey == Key.NumPad0,
                Key.D1 => pressedKey == Key.NumPad1,
                Key.D2 => pressedKey == Key.NumPad2,
                Key.D3 => pressedKey == Key.NumPad3,
                Key.D4 => pressedKey == Key.NumPad4,
                Key.D5 => pressedKey == Key.NumPad5,
                Key.D6 => pressedKey == Key.NumPad6,
                Key.D7 => pressedKey == Key.NumPad7,
                Key.D8 => pressedKey == Key.NumPad8,
                Key.D9 => pressedKey == Key.NumPad9,
                Key.NumPad0 => pressedKey == Key.D0,
                Key.NumPad1 => pressedKey == Key.D1,
                Key.NumPad2 => pressedKey == Key.D2,
                Key.NumPad3 => pressedKey == Key.D3,
                Key.NumPad4 => pressedKey == Key.D4,
                Key.NumPad5 => pressedKey == Key.D5,
                Key.NumPad6 => pressedKey == Key.D6,
                Key.NumPad7 => pressedKey == Key.D7,
                Key.NumPad8 => pressedKey == Key.D8,
                Key.NumPad9 => pressedKey == Key.D9,
                Key.Delete => pressedKey == Key.Back,
                Key.Back => pressedKey == Key.Delete,
                Key.OemPipe => pressedKey == Key.Oem5 || pressedKey == Key.OemBackslash,
                Key.OemOpenBrackets => pressedKey == Key.Oem4,
                Key.OemCloseBrackets => pressedKey == Key.Oem6,
                Key.OemQuotes => pressedKey == Key.Oem7,
                Key.OemSemicolon => pressedKey == Key.Oem1,
                Key.OemTilde => pressedKey == Key.Oem3 || pressedKey == Key.Oem8,
                Key.OemMinus => pressedKey == Key.Subtract,
                Key.Subtract => pressedKey == Key.OemMinus,
                Key.OemPlus => pressedKey == Key.Add,
                Key.Add => pressedKey == Key.OemPlus,
                Key.OemQuestion => pressedKey == Key.Oem2,
                _ => false
            };
        }

        /// <summary>
        /// Converts raw Avalonia KeyModifiers into a clean, OS-specific string.
        /// Windows: "Ctrl + Shift" | macOS: "⇧⌘"
        /// </summary>
        public static string GetFriendlyModifiersName(KeyModifiers modifiers) {
            if (modifiers == KeyModifiers.None) return "";
            var parts = new System.Collections.Generic.List<string>();
            if (modifiers.HasFlag(KeyModifiers.Control)) {
                parts.Add(IsMac ? "⌃" : "Ctrl");
            }
            if (modifiers.HasFlag(KeyModifiers.Alt)) {
                parts.Add(IsMac ? "⌥" : "Alt");
            }
            if (modifiers.HasFlag(KeyModifiers.Shift)) {
                parts.Add(IsMac ? "⇧" : "Shift");
            }
            if (modifiers.HasFlag(KeyModifiers.Meta)) {
                parts.Add(IsMac ? "⌘" : "Win");
            }

            // Windows joins with " + ". Mac joins with nothing.
            return string.Join(IsMac ? "" : " + ", parts);
        }

        /// <summary>
        /// Generates a native Avalonia KeyGesture for UI Menus (Top Menu, Right-Click).
        /// Automatically formats for Windows (Ctrl+S) or Mac (⌘S).
        /// </summary>
        public static Avalonia.Input.KeyGesture? GetGesture(string actionId) {
            var sc = Preferences.Default.Shortcuts?.FirstOrDefault(s => s.ActionId == actionId);
            
            if (sc != null && 
                Enum.TryParse<Avalonia.Input.Key>(sc.KeyName, out var k) && 
                Enum.TryParse<Avalonia.Input.KeyModifiers>(sc.ModifiersName, out var m) && 
                k != Avalonia.Input.Key.None) {
                
                return new Avalonia.Input.KeyGesture(k, m);
            }
            return null;
        }
    }
}
