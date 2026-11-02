/*
 * วัน/เดือน/ปี: 30/10/2569 (2026)
 * ชื่อไฟล์โค้ด: LyricsDialog.axaml.cs
 * เวอร์ชั่น: 1.2
 * เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Contributors ]
 * ตำแหน่งไฟล์: OpenUtau.App/Views/LyricsDialog.axaml.cs
 * รายละเอียดการแก้ไข: อัปเกรดความปลอดภัยของโค้ด (Refactoring) ป้องกันบั๊ก Null Reference Exception ในระหว่างการบันทึกค่าและยกเลิก
 */

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class LyricsDialog : Window {
        public LyricsDialog() {
            InitializeComponent();
            DIALOG_Box.AddHandler(KeyDownEvent, TextBoxKeyDown, RoutingStrategies.Tunnel);
        }

        void OnOpened(object? sender, EventArgs e) {
            DIALOG_Box.Focus();
        }

        void OnCancel(object? sender, RoutedEventArgs e) {
            // [DELTA SYNTH] Refactored to Type Pattern Matching for safe execution
            if (DataContext is LyricsViewModel vm) {
                vm.Cancel();
            }
            Close();
        }

        void OnFinish(object? sender, RoutedEventArgs e) {
            // [DELTA SYNTH] Refactored to Type Pattern Matching for safe execution
            if (DataContext is LyricsViewModel vm) {
                vm.Finish();
            }
            Close();
        }

        private void TextBoxKeyDown(object? sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    // If Shift+Enter, insert line break (default textbox behavior).
                    if (e.KeyModifiers == KeyModifiers.Shift) {
                        return;
                    }
                    OnFinish(sender, e);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    OnCancel(sender, e);
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }
    }
}