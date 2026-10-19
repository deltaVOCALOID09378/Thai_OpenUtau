using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using OpenUtau.App;
using OpenUtau.App.ViewModels;
using OpenUtau.App.Views;

namespace OpenUtau.Colors;

public static class TrackColorPickerOperations {
    public static void PopulateItems(ObservableCollection<TrackColorPickerItemViewModel> items) {
        items.Clear();
        foreach (var color in ThemeManager.TrackColors) {
            items.Add(TrackColorPickerItemViewModel.FromColor(color));
        }
        items.Add(TrackColorPickerItemViewModel.CreateAddTile());
    }

    public static TrackColorPickerItemViewModel? FindItem(
        ObservableCollection<TrackColorPickerItemViewModel> items,
        string? colorName) {
        if (string.IsNullOrEmpty(colorName)) {
            return null;
        }
        return items.FirstOrDefault(item =>
            item.Color != null
            && string.Equals(item.Color.Name, colorName, StringComparison.OrdinalIgnoreCase));
    }

    public static void OpenCreateDialog(Window? owner, Action<TrackColor> onCreated) {
        var dialog = new CreateCustomTrackColorDialog();
        dialog.InitializeForCreate();
        dialog.onFinish = (name, baseColor, brightColor) => {
            if (!TryCreateColor(name, baseColor, brightColor, out var created, out var errorKey)) {
                MessageBox.ShowModal(dialog,
                    ThemeManager.GetString(errorKey),
                    ThemeManager.GetString("prefs.appearance.customtrackcolor.create.title"));
                return false;
            }
            onCreated(created!);
            return true;
        };
        ShowDialog(dialog, owner);
    }

    public static void OpenEditDialog(Window? owner, TrackColor color, Action<TrackColor> onUpdated) {
        if (!color.IsCustom || string.IsNullOrEmpty(color.StoragePath)) {
            return;
        }
        var dialog = new CreateCustomTrackColorDialog();
        dialog.InitializeForEdit(color);
        dialog.onFinish = (name, baseColor, brightColor) => {
            if (!TryUpdateColor(color, name, baseColor, brightColor, out var updated, out var errorKey)) {
                MessageBox.ShowModal(dialog,
                    ThemeManager.GetString(errorKey),
                    ThemeManager.GetString("prefs.appearance.customtrackcolor.edit.title"));
                return false;
            }
            onUpdated(updated!);
            return true;
        };
        ShowDialog(dialog, owner);
    }

    static void ShowDialog(Window dialog, Window? owner) {
        if (owner != null) {
            dialog.ShowDialog(owner);
        } else {
            dialog.Show();
        }
    }

    public static bool TryCreateColor(
        string name,
        Color baseColor,
        Color brightColor,
        out TrackColor? created,
        out string errorKey) {
        created = null;
        errorKey = "prefs.appearance.customtrackcolor.create.empty";
        name = CustomTrackColorStore.SanitizeNewName(name);
        if (string.IsNullOrEmpty(name)) {
            return false;
        }
        if (ThemeManager.IsBuiltInTrackColorName(name)
            || ThemeManager.TrackColors.Any(color => string.Equals(color.Name, name, StringComparison.OrdinalIgnoreCase))) {
            errorKey = "prefs.appearance.customtrackcolor.create.exists";
            return false;
        }

        var yaml = new TrackColorYaml {
            Name = name,
            BaseColor = ToStorageColor(baseColor),
            BrightColor = ToStorageColor(brightColor),
        };
        if (!CustomTrackColorStore.TrySave(yaml, out _)) {
            errorKey = "prefs.appearance.customtrackcolor.create.exists";
            return false;
        }

        ThemeManager.ReloadTrackColors();
        created = ThemeManager.GetTrackColor(name);
        return true;
    }

    public static bool TryUpdateColor(
        TrackColor existing,
        string name,
        Color baseColor,
        Color brightColor,
        out TrackColor? updated,
        out string errorKey) {
        updated = null;
        errorKey = "prefs.appearance.customtrackcolor.create.empty";
        name = CustomTrackColorStore.SanitizeNewName(name);
        if (string.IsNullOrEmpty(name)) {
            return false;
        }
        if (!string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase)
            && (ThemeManager.IsBuiltInTrackColorName(name)
                || ThemeManager.TrackColors.Any(color =>
                    !string.Equals(color.Name, existing.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(color.Name, name, StringComparison.OrdinalIgnoreCase)))) {
            errorKey = "prefs.appearance.customtrackcolor.create.exists";
            return false;
        }

        var yaml = new TrackColorYaml {
            Name = name,
            BaseColor = ToStorageColor(baseColor),
            BrightColor = ToStorageColor(brightColor),
        };
        if (!CustomTrackColorStore.TryUpdate(existing, yaml, out _)) {
            errorKey = "prefs.appearance.customtrackcolor.create.exists";
            return false;
        }

        ThemeManager.ReloadTrackColors();
        updated = ThemeManager.GetTrackColor(name);
        return true;
    }

    public static async System.Threading.Tasks.Task<bool> ConfirmAndDeleteAsync(Window owner, TrackColor color) {
        if (!color.IsCustom) {
            return false;
        }
        var result = await MessageBox.Show(
            owner,
            ThemeManager.GetString("prefs.appearance.customtrackcolor.delete.message"),
            ThemeManager.GetString("prefs.appearance.customtrackcolor.delete.title"),
            MessageBox.MessageBoxButtons.YesNo);
        if (result != MessageBox.MessageBoxResult.Yes) {
            return false;
        }

        string deletedName = color.Name;
        CustomTrackColorStore.TryDelete(color);
        ThemeManager.ReloadTrackColors();
        return true;
    }

    public static string ToStorageColor(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
