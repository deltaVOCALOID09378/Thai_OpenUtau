// ============================================================================
// Date         : 02/11/2026 (B.E. 2569 / A.D. 2026)
// File Name    : SliderDialog.axaml.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau/Views/SliderDialog.axaml.cs
// Description  : General-purpose slider input dialog supporting legacy onFinish delegates and fluent Configure API.
// ============================================================================
using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace OpenUtau.App.Views {
    public partial class SliderDialog : Window {
        public double MinValue { get; set; } = 0;
        public double MaxValue { get; set; } = 100;
        public int TickFrequency { get; set; } = 1;
        public Action<double>? onFinish { get => OnOk; set => OnOk = value; }
        public Action<double>? OnOk;

        bool finished;

        public SliderDialog() {
            InitializeComponent();
        }

        public SliderDialog(string title, double value, double min, double max, double tick) : this() {
            Configure(value, title, min, max, (int)Math.Max(1, tick));
        }

        public void Configure(double initialValue, string title = "",
                              double min = 0, double max = 100,
                              int tick = 1, string? format = null,
                              string? prompt = null) {
            Title = title;
            MinValue = min;
            MaxValue = max;
            TickFrequency = tick;

            Slider.Minimum = min;
            Slider.Maximum = max;
            Slider.TickFrequency = tick;
            Slider.Value = Math.Clamp(initialValue, min, max);
            Slider.IsSnapToTickEnabled = true;

            UpdateValueDisplay(Slider.Value);
        }

        protected override void OnOpened(EventArgs e) {
            base.OnOpened(e);
            Slider.Focus();
        }

        void OnSliderValueChanged(object? sender,
                                   Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e) {
            UpdateValueDisplay(e.NewValue);
        }

        void UpdateValueDisplay(double value) {
            if (TickFrequency == 1) {
                TextBlock.Text = value.ToString("F0");
            } else {
                TextBlock.Text = value.ToString("F1");
            }
        }

        void OkButtonClick(object? sender, RoutedEventArgs e) {
            Finish();
        }

        void Finish() {
            if (finished) return;
            finished = true;
            try {
                OnOk?.Invoke(Slider.Value);
            } finally {
                Close();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                e.Handled = true;
                Close();
            } else if (e.Key == Key.Enter) {
                e.Handled = true;
                Finish();
            } else {
                base.OnKeyDown(e);
            }
        }
    }
}
