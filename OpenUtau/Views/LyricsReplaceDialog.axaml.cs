using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class LyricsReplaceDialog : Window {
        private bool finished;

        public LyricsReplaceDialog() {
            InitializeComponent();
        }

        void OnCancel(object? sender, RoutedEventArgs e) {
            CloseSafe();
        }

        void OnFinish(object? sender, RoutedEventArgs e) {
            if (finished) return;

            if (DataContext is LyricsReplaceViewModel vm && vm.Finish()) {
                CloseSafe();
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    OnFinish(sender, e);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    OnCancel(sender, e);
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        private void CloseSafe() {
            if (finished) return;
            finished = true;
            Close();
        }
    }
}