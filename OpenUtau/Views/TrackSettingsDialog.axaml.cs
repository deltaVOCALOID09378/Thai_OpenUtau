// ============================================================================
// Date         : 02/11/2026 (B.E. 2569 / A.D. 2026)
// File Name    : TrackSettingsDialog.axaml.cs
// Version      : 1.1
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau/Views/TrackSettingsDialog.axaml.cs
// Description  : Track settings configuration window controller with UTrack parameter binding.
// ============================================================================
using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.App.Views {
    public partial class TrackSettingsDialog : Window {
        private TrackSettingsViewModel? viewModel;

        public TrackSettingsDialog() : this(new UTrack(DocManager.Inst.Project)) { }

        public TrackSettingsDialog(UTrack track) {
            InitializeComponent();
            DataContext = viewModel = new TrackSettingsViewModel(track);
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                Close();
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        void OnOkClicked(object? sender, RoutedEventArgs e) {
            try {
                viewModel?.Finish();
            } catch (Exception ex) {
                Log.Error(ex, "Failed to apply track settings.");
            } finally {
                Close();
            }
        }
    }
}
