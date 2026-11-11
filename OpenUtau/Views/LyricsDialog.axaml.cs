// ======================================================================
// 1. วัน/เดือน/ปี (Date)        : 11 พฤศจิกายน พ.ศ. 2569 / 11 November 2026 (B.E. 2569 / A.D. 2026)
// 2. ชื่อไฟล์ (File Name)       : LyricsDialog.axaml.cs
// 3. ลำดับเวอร์ชัน (Version)    : v1.3
// 4. รายชื่อเครดิต (Credit)     : Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Contributors ]
// 5. ตำแหน่งที่จัดวาง (Path)    : OpenUtau/Views/LyricsDialog.axaml.cs
// 6. รายละเอียดการแก้ไข (Summary): ปรับปรุงพฤติกรรมกล่องข้อความให้เหมือน Notepad 100% โดยกด Enter เป็นการขึ้นบรรทัดใหม่ และใช้ Ctrl+Enter ในการยืนยัน
// ======================================================================

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
            if (DataContext is LyricsViewModel vm) {
                vm.Cancel();
            }
            Close();
        }

        void OnFinish(object? sender, RoutedEventArgs e) {
            if (DataContext is LyricsViewModel vm) {
                vm.Finish();
            }
            Close();
        }

        private void TextBoxKeyDown(object? sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    // Notepad behavior: Enter inserts a newline in multiline text box.
                    // Submitting via keyboard is Ctrl+Enter.
                    if (e.KeyModifiers == KeyModifiers.Control) {
                        OnFinish(sender, e);
                        e.Handled = true;
                    }
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
