// ============================================================================
// Date         : 31/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : AutoPitchRandomizerDialog.axaml.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
// File Location: OpenUtau/Views/AutoPitchRandomizerDialog.axaml.cs
// Description  : Multi-Accent AutoPitch Randomizer and Global Multi-Track Lyric Replacer dialog.
// ============================================================================
using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OpenUtau.App.Views {
    public partial class AutoPitchRandomizerDialog : Window {
        public int SelectedPattern { get; private set; } = 0;
        public double Intensity { get; private set; } = 50.0;
        public string SelectedAccentLanguage { get; private set; } = "Default";
        
        // ข้อมูลสำหรับการแทนที่เนื้อเพลงมัลติแทร็ก
        public string SearchTarget { get; private set; } = string.Empty;
        public string ReplaceTarget { get; private set; } = string.Empty;
        public string BatchSequenceLyrics { get; private set; } = string.Empty;
        public bool IsGlobalReplaceTriggered { get; private set; } = false;

        public bool IsApplied { get; private set; } = false;
        private bool finished;

        public AutoPitchRandomizerDialog() {
            InitializeComponent();

            // เชื่อมโยงสไลเดอร์กับ TextBlock แสดงเปอร์เซ็นต์แบบเรียลไทม์
            var slider = this.FindControl<Slider>("IntensitySlider");
            var txtVal = this.FindControl<TextBlock>("TxtIntensityVal");
            if (slider != null && txtVal != null) {
                slider.PropertyChanged += (s, e) => {
                    if (e.Property.Name == nameof(Slider.Value)) {
                        Intensity = slider.Value;
                        txtVal.Text = $"{Math.Round(Intensity)}%";
                    }
                };
            }

            // ปุ่มกดสั่งแทนที่เนื้อเพลงแบบฉับพลัน (Replace All)
            var btnReplaceAll = this.FindControl<Button>("BtnApplyReplace");
            if (btnReplaceAll != null) {
                btnReplaceAll.Click += (s, e) => {
                    var searchBox = this.FindControl<TextBox>("TxtSearchLyric");
                    var replaceBox = this.FindControl<TextBox>("TxtReplaceLyric");
                    if (searchBox != null && replaceBox != null) {
                        SearchTarget = searchBox.Text ?? string.Empty;
                        ReplaceTarget = replaceBox.Text ?? string.Empty;
                        IsGlobalReplaceTriggered = !string.IsNullOrEmpty(SearchTarget);
                        if (IsGlobalReplaceTriggered) {
                            btnReplaceAll.Content = "✔ Ready / เตรียมแทนที่แล้ว";
                        }
                    }
                };
            }

            var applyBtn = this.FindControl<Button>("ApplyBtn");
            if (applyBtn != null) {
                applyBtn.Click += OnApplyClicked;
            }

            var cancelBtn = this.FindControl<Button>("CancelBtn");
            if (cancelBtn != null) {
                cancelBtn.Click += OnCancelClicked;
            }
        }

        private void OnApplyClicked(object? sender, RoutedEventArgs e) {
            if (finished) return;
            finished = true;

            var combo = this.FindControl<ComboBox>("PatternComboBox");
            if (combo != null) {
                SelectedPattern = combo.SelectedIndex;
                if (combo.SelectedItem is ComboBoxItem item && item.Tag is string tag) {
                    SelectedAccentLanguage = tag;
                }
            }

            var slider = this.FindControl<Slider>("IntensitySlider");
            if (slider != null) {
                Intensity = slider.Value;
            }

            // ตรวจสอบคำค้นหาและคำแทนที่หากผู้ใช้กรอกไว้โดยตรง
            var searchBox = this.FindControl<TextBox>("TxtSearchLyric");
            var replaceBox = this.FindControl<TextBox>("TxtReplaceLyric");
            if (searchBox != null && !string.IsNullOrEmpty(searchBox.Text)) {
                SearchTarget = searchBox.Text;
                ReplaceTarget = replaceBox?.Text ?? string.Empty;
                IsGlobalReplaceTriggered = true;
            }

            // ดึงค่าเนื้อเพลงชุดใหม่แบบยกแทร็กจากกล่องข้อความด้านล่าง
            var batchBox = this.FindControl<TextBox>("TxtBatchLyrics");
            if (batchBox != null) {
                BatchSequenceLyrics = batchBox.Text ?? string.Empty;
            }

            IsApplied = true;
            Close();
        }

        private void OnCancelClicked(object? sender, RoutedEventArgs e) {
            if (finished) return;
            finished = true;

            IsApplied = false;
            Close();
        }
    }
}
