using Avalonia.Media;
using OpenUtau.Colors;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class ThemePickerItemViewModel : ViewModelBase {
        public string Name { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public bool IsBuiltIn { get; init; }
        public bool IsPackageTheme { get; init; }
        public bool IsCreateTile { get; init; }
        public bool IsEditable => !IsBuiltIn && !IsPackageTheme && !IsCreateTile;
        [Reactive] public bool IsSelected { get; set; }
        public IBrush BackgroundBrush { get; init; } = Brushes.Transparent;
        public IBrush CanvasBrush { get; init; } = Brushes.Transparent;
        public IBrush CardBrush { get; init; } = Brushes.Transparent;
        public IBrush TrackAltBrush { get; init; } = Brushes.Transparent;
        public IBrush NoteBrush { get; init; } = Brushes.Transparent;
        public IBrush AccentBrush { get; init; } = Brushes.Transparent;

        public static ThemePickerItemViewModel FromThemeName(string name) {
            var colors = ThemePreviewFactory.GetPreviewColors(name);
            var displayName = GetDisplayName(name);
            return new ThemePickerItemViewModel {
                Name = name,
                DisplayName = displayName,
                IsBuiltIn = BuiltInThemeLoader.IsBuiltInTheme(name),
                IsPackageTheme = CustomTheme.IsPackageTheme(name),
                BackgroundBrush = colors.Background,
                CanvasBrush = colors.Canvas,
                CardBrush = colors.Card,
                TrackAltBrush = colors.TrackAlt,
                NoteBrush = colors.Note,
                AccentBrush = colors.Accent,
            };
        }

        public static ThemePickerItemViewModel CreateAddTile() {
            return new ThemePickerItemViewModel {
                IsCreateTile = true,
                DisplayName = ThemeManager.GetString("prefs.appearance.customtheme.create.short"),
            };
        }

        static string GetDisplayName(string name) {
            var key = $"prefs.appearance.theme.name.{name.ToLowerInvariant()}";
            var localized = ThemeManager.GetString(key);
            return localized == key ? name : localized;
        }
    }
}
