#pragma warning disable CS0618, CS0649, CS8632, CS0108
#nullable enable
// ============================================================================
// Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : AutoPitchRandomizerDialog.axaml.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by Delta Synth ]
// File Location: OpenUtau.App\Views\AutoPitchRandomizerDialog.axaml.cs
// Description  : Code-behind logic for AutoPitch Randomizer dialog window.
//                Handles UI interaction, pattern selection, and intensity value mapping.
// ============================================================================

using Avalonia.Controls;

namespace OpenUtau.App.Views
{
    public partial class AutoPitchRandomizerDialog : Window
    {
        public int SelectedPattern { get; private set; } = 0;
        public double Intensity { get; private set; } = 50.0;
        public bool IsApplied { get; private set; } = false;

        public AutoPitchRandomizerDialog()
        {
            InitializeComponent();

            var applyBtn = this.FindControl<Button>("ApplyBtn");
            if (applyBtn != null)
            {
                applyBtn.Click += (s, e) =>
                {
                    var combo = this.FindControl<ComboBox>("PatternComboBox");
                    if (combo != null) SelectedPattern = combo.SelectedIndex;

                    var slider = this.FindControl<Slider>("IntensitySlider");
                    if (slider != null) Intensity = slider.Value;

                    IsApplied = true;
                    Close();
                };
            }

            var cancelBtn = this.FindControl<Button>("CancelBtn");
            if (cancelBtn != null)
            {
                cancelBtn.Click += (s, e) =>
                {
                    IsApplied = false;
                    Close();
                };
            }
        }
    }
}