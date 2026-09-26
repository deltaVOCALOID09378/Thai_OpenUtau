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
