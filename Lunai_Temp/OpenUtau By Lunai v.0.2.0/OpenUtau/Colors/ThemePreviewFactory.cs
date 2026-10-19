using Avalonia.Media;

namespace OpenUtau.Colors;

public static class ThemePreviewFactory {
    public static ThemeYaml LoadTheme(string name) {
        if (BuiltInThemeLoader.TryCreateThemeByName(name, out var builtInTheme)) {
            return builtInTheme;
        }
        return CustomTheme.Themes.TryGetValue(name, out var path)
            ? ThemeYaml.LoadFromFile(path)
            : BuiltInThemeLoader.CreateFromBuiltIn(name, name);
    }

    public static ThemePreviewColors GetPreviewColors(string name) {
        var yaml = LoadTheme(name);
        return new ThemePreviewColors(
            ToBrush(yaml.WorkspaceCanvasColor),
            ToBrush(yaml.BackgroundColor),
            ToBrush(yaml.WorkspaceCardColor),
            ToBrush(yaml.TrackBackgroundAltColor),
            ToBrush(yaml.AccentColor1Note),
            ToBrush(yaml.AccentColor2));
    }

    static SolidColorBrush ToBrush(string? colorString) {
        return new SolidColorBrush(ThemeColorStorage.ParseOrDefault(colorString, Color.Parse("#808080")));
    }
}

public readonly record struct ThemePreviewColors(
    IBrush Canvas,
    IBrush Background,
    IBrush Card,
    IBrush TrackAlt,
    IBrush Note,
    IBrush Accent);
