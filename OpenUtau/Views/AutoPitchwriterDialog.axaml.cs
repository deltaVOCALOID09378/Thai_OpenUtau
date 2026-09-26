using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.Core;
using OpenUtau.Core.Editing;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.Views {
    public partial class AutoPitchwriterDialog : Window {
        public Action<AutoPitchwriterOptions> onFinish;

        public AutoPitchwriterDialog() {
            InitializeComponent();
        }

        private void OnDefaultClick(object sender, RoutedEventArgs e) {
            chkThunderbolt.IsChecked = true;
            chkPitchDropRise.IsChecked = false;
            chkSlope.IsChecked = false;
            txtPortamentoStart.Text = "-25";
            txtPortamentoLength.Text = "50";
        }

        private void OnRunClick(object sender, RoutedEventArgs e) {
            double.TryParse(txtPortamentoStart.Text, out double start);
            double.TryParse(txtPortamentoLength.Text, out double length);

            var options = new AutoPitchwriterOptions {
                Thunderbolt = chkThunderbolt.IsChecked ?? false,
                PitchDropRise = chkPitchDropRise.IsChecked ?? false,
                Slope = chkSlope.IsChecked ?? false,
                PortamentoStart = start,
                PortamentoLength = length
            };

            onFinish?.Invoke(options);
            Close();
        }
    }
}
