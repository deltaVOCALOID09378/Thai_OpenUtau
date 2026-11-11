// ============================================================================
// 1. วัน/เดือน/ปี: 11 พฤศจิกายน พ.ศ. 2569 / 11 November 2026
// 2. ชื่อไฟล์โค้ด: PhonemeTimingConfigDialog.axaml.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.0
// 4. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
// 5. ตำแหน่งที่จัดวางไฟล์: OpenUtau/Views/PhonemeTimingConfigDialog.axaml.cs
// 6. รายละเอียดการแก้ไข: คลาสหน้าต่างควบคุมการปรับปรุงสัดส่วนเวลาและ Alias ของหน่วยเสียง พร้อมรองรับการปิดหน้าต่างและรับค่าจาก ViewModel
// ============================================================================

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class PhonemeTimingConfigDialog : Window {
        public PhonemeTimingConfigDialog() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnDataContextChanged(System.EventArgs e) {
            base.OnDataContextChanged(e);
            if (DataContext is PhonemeTimingConfigViewModel vm) {
                vm.RequestClose = Close;
            }
        }
    }
}
