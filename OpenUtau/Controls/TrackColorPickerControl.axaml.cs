using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using OpenUtau.Colors;

namespace OpenUtau.App.Controls;

public partial class TrackColorPickerControl : UserControl {
    public static readonly DirectProperty<TrackColorPickerControl, TrackColor?> SelectedColorProperty =
        AvaloniaProperty.RegisterDirect<TrackColorPickerControl, TrackColor?>(
            nameof(SelectedColor),
            o => o.SelectedColor,
            (o, v) => o.SelectedColor = v,
            defaultBindingMode: BindingMode.TwoWay);

    readonly ObservableCollection<TrackColorPickerItemViewModel> items = new();
    TrackColor? selectedColor;

    public TrackColor? SelectedColor {
        get => selectedColor;
        set {
            if (SetAndRaise(SelectedColorProperty, ref selectedColor, value)) {
                UpdateSelectionState();
            }
        }
    }

    public event EventHandler? ColorsChanged;

    public TrackColorPickerControl() {
        InitializeComponent();
        ItemsHost.ItemsSource = items;
        RefreshItems();
    }

    public void RefreshItems() {
        string? selectedName = SelectedColor?.Name;
        TrackColorPickerOperations.PopulateItems(items);
        if (!string.IsNullOrEmpty(selectedName)) {
            SelectedColor = ThemeManager.GetTrackColor(selectedName);
        } else {
            UpdateSelectionState();
        }
    }

    void UpdateSelectionState() {
        foreach (var item in items) {
            item.IsSelected = item.Color != null
                && SelectedColor != null
                && string.Equals(item.Color.Name, SelectedColor.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    Window? GetOwnerWindow() => TopLevel.GetTopLevel(this) as Window;

    void OnSwatchPointerPressed(object? sender, PointerPressedEventArgs e) {
        if (e.GetCurrentPoint(sender as Control).Properties.IsRightButtonPressed) {
            return;
        }
        if (sender is not Border { DataContext: TrackColorPickerItemViewModel item }) {
            return;
        }
        if (item.IsCreateTile) {
            OpenCreateDialog();
            e.Handled = true;
            return;
        }
        if (item.Color != null) {
            SelectedColor = item.Color;
            e.Handled = true;
        }
    }

    void OnContextEditClick(object? sender, RoutedEventArgs e) {
        if (sender is not MenuItem { DataContext: TrackColorPickerItemViewModel { Color: { IsCustom: true } color } }) {
            return;
        }
        OpenEditDialog(color);
        e.Handled = true;
    }

    void OnContextDeleteClick(object? sender, RoutedEventArgs e) {
        if (sender is not MenuItem { DataContext: TrackColorPickerItemViewModel { Color: { IsCustom: true } color } }) {
            return;
        }
        _ = DeleteColorAsync(color);
        e.Handled = true;
    }

    async System.Threading.Tasks.Task DeleteColorAsync(TrackColor color) {
        var owner = GetOwnerWindow();
        if (owner == null) {
            return;
        }
        if (!await TrackColorPickerOperations.ConfirmAndDeleteAsync(owner, color)) {
            return;
        }

        string deletedName = color.Name;
        if (string.Equals(SelectedColor?.Name, deletedName, StringComparison.OrdinalIgnoreCase)) {
            SelectedColor = ThemeManager.GetTrackColor("Blue");
        }
        RefreshItems();
        ColorsChanged?.Invoke(this, EventArgs.Empty);
    }

    void OpenEditDialog(TrackColor color) {
        TrackColorPickerOperations.OpenEditDialog(GetOwnerWindow(), color, updated => {
            string previousName = color.Name;
            if (string.Equals(SelectedColor?.Name, previousName, StringComparison.OrdinalIgnoreCase)) {
                SelectedColor = updated;
            }
            RefreshItems();
            ColorsChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    void OpenCreateDialog() {
        TrackColorPickerOperations.OpenCreateDialog(GetOwnerWindow(), created => {
            ThemeManager.ReloadTrackColors();
            SelectedColor = created;
            RefreshItems();
            ColorsChanged?.Invoke(this, EventArgs.Empty);
        });
    }
}
