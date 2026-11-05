// ============================================================================
// 1. วัน/เดือน/ปี: 5 พฤศจิกายน พ.ศ. 2569 / 05 November 2026
// 2. ชื่อไฟล์โค้ด: AutoPitchwriterDialog.axaml.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.4
// 4. ตำแหน่งที่จัดวางไฟล์: OpenUtau/Views/AutoPitchwriterDialog.axaml.cs
// 5. หน้าที่: ลอจิกควบคุมหน้าต่าง AutoPitchwriter เชื่อมโยงตัวเลือกรูปแบบพิตช์ทั้ง 6 รูปแบบครบถ้วน
// 6. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Agentic AI | Original by Canon AutoPitchwriter
// ============================================================================

#pragma warning disable CS0618, CS0649, CS8632, CS0108
#nullable enable

using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OpenUtau.App.Views {
    public partial class AutoPitchwriterDialog : Window {
        public Action<AutoPitchwriterOptions>? onFinish;

        public AutoPitchwriterDialog() {
            InitializeComponent();
        }

        private void OnDefaultClick(object sender, RoutedEventArgs e) {
            if (chkThunderbolt != null) chkThunderbolt.IsChecked = true;
            if (chkPitchDropRise != null) chkPitchDropRise.IsChecked = true;
            if (chkSlope != null) chkSlope.IsChecked = true;
            if (chkWarpShape != null) chkWarpShape.IsChecked = true;
            if (chkUPitchDrop != null) chkUPitchDrop.IsChecked = true;
            if (chkPitchDent != null) chkPitchDent.IsChecked = true;
            if (txtPortamentoStart != null) txtPortamentoStart.Text = "-25";
            if (txtPortamentoLength != null) txtPortamentoLength.Text = "50";
        }

        private void OnRunClick(object sender, RoutedEventArgs e) {
            double.TryParse(txtPortamentoStart?.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double start);
            double.TryParse(txtPortamentoLength?.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double length);

            var options = new AutoPitchwriterOptions {
                Thunderbolt = chkThunderbolt?.IsChecked ?? false,
                PitchDropRise = chkPitchDropRise?.IsChecked ?? false,
                Slope = chkSlope?.IsChecked ?? false,
                WShape = chkWarpShape?.IsChecked ?? false,
                UPitchDrop = chkUPitchDrop?.IsChecked ?? false,
                PitchDent = chkPitchDent?.IsChecked ?? false,
                PortamentoStart = start,
                PortamentoLength = length
            };

            onFinish?.Invoke(options);
            Close();
        }
    }

    public class AutoPitchwriterOptions {
        public bool Thunderbolt { get; set; }
        public bool PitchDropRise { get; set; }
        public bool Slope { get; set; }
        public bool WShape { get; set; }
        public bool WarpShape { get => WShape; set => WShape = value; }
        public bool UPitchDrop { get; set; }
        public bool PitchDent { get; set; }
        public double PortamentoStart { get; set; }
        public double PortamentoLength { get; set; }
    }
}
