/*
 * วัน/เดือน/ปี: 30/10/2569 (2026)
 * ชื่อไฟล์โค้ด: DsScriptExportDialog.axaml.cs
 * เวอร์ชั่น: 1.2
 * เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Contributors ]
 * ตำแหน่งไฟล์: OpenUtau.App/Views/DsScriptExportDialog.axaml.cs
 * รายละเอียดการแก้ไข: ยกระดับโครงสร้างและประทับตรามาตรฐานองค์กร
 */

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace OpenUtau.App.Views {
    public partial class DsScriptExportDialog : Window {
        public bool Confirmed { get; private set; }

        public DsScriptExportDialog() {
            InitializeComponent();
        }

        void OnOkClicked(object? sender, RoutedEventArgs e) {
            Confirmed = true;
            Close();
        }

        void OnCancelClicked(object? sender, RoutedEventArgs e) {
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                e.Handled = true;
                Close();
            } else if (e.Key == Key.Return) {
                e.Handled = true;
                Confirmed = true;
                Close();
            } else {
                base.OnKeyDown(e);
            }
        }
    }
}