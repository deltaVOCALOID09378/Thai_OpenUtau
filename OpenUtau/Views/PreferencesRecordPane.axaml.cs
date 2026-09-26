using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OpenUtau.App.Views {
    public partial class PreferencesRecordPane : UserControl {
        public PreferencesRecordPane() {
            InitializeComponent();
        }
        
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        public async void OnSelectCaptureExportLocation(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is Window window) {
                var path = await FilePicker.OpenFolder(window, "Export Location", null);
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path)) {
                    if (DataContext is ViewModels.PreferencesViewModel vm) {
                        vm.CaptureExportLocation = path;
                    }
                }
            }
        }
    }
}
