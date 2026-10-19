using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class TrackColorDialog : Window {
        TrackColorViewModel ViewModel => (TrackColorViewModel)DataContext!;

        public TrackColorDialog() {
            InitializeComponent();
        }

        void OnContextEditClick(object? sender, RoutedEventArgs e) {
            if (sender is MenuItem { DataContext: TrackColorPickerItemViewModel { Color: { IsCustom: true } color } }) {
                ViewModel.EditColorAsync(color, this);
            }
            e.Handled = true;
        }

        void OnContextDeleteClick(object? sender, RoutedEventArgs e) {
            if (sender is MenuItem { DataContext: TrackColorPickerItemViewModel { Color: { IsCustom: true } color } }) {
                _ = ViewModel.DeleteColorAsync(color, this);
            }
            e.Handled = true;
        }

        void OnCancel(object? sender, RoutedEventArgs e) {
            Close();
        }

        void OnFinish(object? sender, RoutedEventArgs e) {
            ViewModel.Finish();
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                e.Handled = true;
                Close();
            } else if (e.Key == Key.Enter) {
                e.Handled = true;
                OnFinish(this, e);
            } else {
                base.OnKeyDown(e);
            }
        }
    }
}
