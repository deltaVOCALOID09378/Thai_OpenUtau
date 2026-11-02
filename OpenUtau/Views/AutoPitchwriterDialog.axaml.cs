#pragma warning disable CS0618, CS0649, CS8632, CS0108
#nullable enable
// ============================================================================
// Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : AutoPitchwriterDialog.axaml.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by Canon AutoPitchwriter ]
// File Location: OpenUtau.App\Views\AutoPitchwriterDialog.axaml.cs
// Description  : Dialog logic for Canon AutoPitchwriter tool. 
//                Parses options and triggers automatic pitch automation generation.
// ============================================================================
using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OpenUtau.App.Views {
    public partial class AutoPitchwriterDialog : Window {
        public Action<AutoPitchwriterOptions>? onFinish;
        private bool finished;

        public AutoPitchwriterDialog() {
            InitializeComponent();
        }

        private void OnDefaultClick(object? sender, RoutedEventArgs e) {
            chkThunderbolt.IsChecked = true;
            chkPitchDropRise.IsChecked = true;
            chkSlope.IsChecked = true;
            chkWarpShape.IsChecked = true;
            chkUPitchDrop.IsChecked = true;
            chkPitchDent.IsChecked = true;
            
            txtPortamentoStart.Text = "-25";
            txtPortamentoLength.Text = "50";
        }

        private void OnRunClick(object? sender, RoutedEventArgs e) {
            if (finished) return;
            finished = true;

            double.TryParse(txtPortamentoStart.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double start);
            double.TryParse(txtPortamentoLength.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double length);
            length = Math.Max(0, length);

            var options = new AutoPitchwriterOptions {
                Thunderbolt = chkThunderbolt.IsChecked ?? false,
                PitchDropRise = chkPitchDropRise.IsChecked ?? false,
                Slope = chkSlope.IsChecked ?? false,
                WarpShape = chkWarpShape.IsChecked ?? false,
                UPitchDrop = chkUPitchDrop.IsChecked ?? false,
                PitchDent = chkPitchDent.IsChecked ?? false,
                PortamentoStart = start,
                PortamentoLength = length
            };

            try {
                onFinish?.Invoke(options);
            } finally {
                Close();
            }
        }
    }

    public class AutoPitchwriterOptions {
        public bool Thunderbolt { get; set; }
        public bool PitchDropRise { get; set; }
        public bool Slope { get; set; }
        public bool WarpShape { get; set; }
        public bool UPitchDrop { get; set; }
        public bool PitchDent { get; set; }
        public double PortamentoStart { get; set; }
        public double PortamentoLength { get; set; }
    }
}