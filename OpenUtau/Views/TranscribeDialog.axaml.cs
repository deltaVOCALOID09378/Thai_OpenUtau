/*
File: TranscribeDialog.xaml.cs
Version: 1.0
Made and Checked By DELTA SYNTH And Gemini Claude and ChatGPT
Original By OpenUtau Contributors
*/
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Analysis;

namespace OpenUtau.App.Views
{
    public partial class TranscribeDialog : Window
    {
        public bool Confirmed { get; private set; }

        public TranscribeDialog()
        {
            InitializeComponent();
        }

        private void OnOkClicked(object? sender, RoutedEventArgs e)
        {
            Confirmed = true;
            Close();
        }

        private void OnCancelClicked(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnRmvpeRowPressed(object? sender, PointerPressedEventArgs e)
        {
            e.Handled = true;
            _ = HandleRmvpeRowClick();
        }

        // จัดการเมื่อคลิกเลือกใช้งาน Rmvpe (ใช้ Pattern Matching ป้องกัน Null Reference)
        private async Task HandleRmvpeRowClick()
        {
            if (DataContext is TranscribeViewModel vm)
            {
                if (!vm.RmvpeAvailable)
                {
                    if (RmvpeCheck != null)
                    {
                        RmvpeCheck.IsChecked = false;
                    }
                    
                    var modelPath = RmvpeTranscriber.GetModelPath();
                    await MessageBox.ShowError(this, new MessageCustomizableException(
                        "RMVPE not found",
                        "<translate:errors.failed.transcribe.rmvpe>",
                        new FileNotFoundException(modelPath),
                        false,
                        new[] { modelPath }));
                    return;
                }
                
                var current = vm.PredictPitd;
                vm.PredictPitd = !current;
                
                if (RmvpeCheck != null)
                {
                    RmvpeCheck.IsChecked = !current;
                }
            }
        }

        // จับคีย์ลัดสำหรับสั่งประมวลผล
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
            else if (e.Key == Key.Return)
            {
                if (DataContext is TranscribeViewModel vm && vm.CanRun)
                {
                    e.Handled = true;
                    Confirmed = true;
                    Close();
                }
            }
            else
            {
                base.OnKeyDown(e);
            }
        }
    }
}