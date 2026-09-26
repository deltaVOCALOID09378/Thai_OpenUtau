/*
File: TrackSettingsDialog.xaml.cs
Version: 1.0
Made and Checked By DELTA SYNTH And Gemini Claude and ChatGPT
Original By OpenUtau Contributors
*/
using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.Views
{
    public partial class TrackSettingsDialog : Window
    {
        // ปรับระดับความปลอดภัยให้ฟิลด์ (Readonly encapsulation)
        private readonly TrackSettingsViewModel _viewModel;

        public TrackSettingsDialog() : this(new UTrack(DocManager.Inst.Project)) { }

        public TrackSettingsDialog(UTrack track)
        {
            InitializeComponent();
            _viewModel = new TrackSettingsViewModel(track);
            DataContext = _viewModel;
        }

        // จัดการเมื่อกดปุ่มบันทึก (OK)
        public void OnOkClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.Finish();
            Close();
        }
    }
}