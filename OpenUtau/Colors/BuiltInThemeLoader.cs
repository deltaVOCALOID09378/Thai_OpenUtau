namespace OpenUtau.Colors;

using System;
using Avalonia.Media;

/// <summary>
/// Built-in theme snapshots. Values must stay in sync with DarkTheme.axaml / LightTheme.axaml.
/// ResourceInclude dictionaries are not enumerable reliably at theme-creation time.
/// </summary>
public static class BuiltInThemeLoader {
    public const string LightThemeName = "Light";
    public const string DarkThemeName = "Dark";
    public const string OriginalLightThemeName = "OriginalLight";
    public const string OriginalDarkThemeName = "OriginalDark";
    public const string ColdThemeName = "Cold";
    public const string WarmThemeName = "Warm";

    public static readonly string[] BaseThemeNames = [
        LightThemeName,
        DarkThemeName,
    ];

    public static readonly string[] BuiltInCustomThemeNames = [
        OriginalLightThemeName,
        OriginalDarkThemeName,
        ColdThemeName,
        WarmThemeName,
    ];

    public static ThemeYaml CreateFromBuiltIn(string baseTheme, string name) {
        var yaml = string.Equals(baseTheme, DarkThemeName, System.StringComparison.OrdinalIgnoreCase)
            ? CreateDark()
            : CreateLight();
        yaml.Name = name;
        return yaml;
    }

    public static bool IsBuiltInTheme(string? name) {
        return TryCreateThemeByName(name, out _);
    }

    public static bool TryCreateThemeByName(string? name, out ThemeYaml yaml) {
        switch (name) {
            case LightThemeName:
                yaml = CreateLight();
                return true;
            case DarkThemeName:
                yaml = CreateDark();
                return true;
            case OriginalLightThemeName:
                yaml = CreateOriginalLight();
                return true;
            case OriginalDarkThemeName:
                yaml = CreateOriginalDark();
                return true;
            case ColdThemeName:
                yaml = CreateCold();
                return true;
            case WarmThemeName:
                yaml = CreateWarm();
                return true;
            default:
                yaml = default!;
                return false;
        }
    }

    public static ThemeYaml CreateDark() {
        return new ThemeYaml {
            IsDarkMode = true,
            BackgroundColor = "#121212",
            BackgroundColorPointerOver = "#222222",
            TransportToolbarOffHoverColor = "#282828",
            BackgroundColorPressed = "#080808",
            BackgroundColorDisabled = "#121212",
            ForegroundColor = "#FFFFFF",
            ForegroundColorPointerOver = "#FFFFFF",
            ForegroundColorPressed = "#FFFFFF",
            ForegroundColorDisabled = "#888888",
            TextControlBorderColorDisabled = "#3E3E3E",
            BorderColor = "#383838",
            BorderColorPointerOver = "#505050",
            SystemAccentColor = "#D92200",
            SystemAccentColorLight1 = "#FF3B14",
            SystemAccentColorDark1 = "#A01000",
            NeutralAccentColor = "#FFFFFF",
            NeutralAccentColorPointerOver = "#FF3B14",
            AccentColor1 = "#D92200",
            AccentColor1Note = "#A01000",
            AccentColor2 = "#0066FF",
            AccentColor3 = "#FF3399",
            NoteBorderColor = "#FF3B14",
            NoteBorderColorPressed = "#D92200",
            TickLineColor = "#1C1C1C",
            BarNumberColor = "#FFFFFF",
            FinalPitchColor = "#FFFFFFFF",
            TrackBackgroundAltColor = "#161616",
            WarningColor = "#5C4500",
            ToolbarCheckedHoverColor = "#FF3B14",
            ToolTipForegroundColor = "#FFFFFF",
            WorkspaceCanvasColor = "#060606",
            WorkspaceCardColor = "#141414",
            WorkspaceElevatedSurfaceColor = "#202020",
            MutedIconColor = "#9E9E9E",
            PianoRollWaveformPeakColor = "#66FFFFFF",
            PianoRollToolbarStripColor = "#0C0C0C",
            PianoRollToolbarButtonHoverColor = "#FF3B14",
            PianoRollTimelineStripColor = "#0A0A0A",
            AppTopBarTransportStripColor = "#101010",
            AppTopBarTransportHoverColor = "#D92200",
            AppTopBarValueStripColor = "#0C0C0C",
            AppTopBarValueDividerColor = "#222222",
            WhiteKeyColorLeft = "#D5D5D5",
            WhiteKeyColorRight = "#F8F8F8",
            WhiteKeyNameColor = "#0A0A0A",
            CenterKeyColorLeft = "#A01000",
            CenterKeyColorRight = "#D92200",
            CenterKeyNameColor = "#FFFFFF",
            BlackKeyColorLeft = "Transparent",
            BlackKeyColorRight = "Transparent",
            BlackKeyNameColor = "#FFFFFF",
        };
    }

    public static ThemeYaml CreateLight() {
        return new ThemeYaml {
            IsDarkMode = false,
            BackgroundColor = "#FCFCFC",
            BackgroundColorPointerOver = "#F0F0F0",
            TransportToolbarOffHoverColor = "#F6F6F6",
            BackgroundColorPressed = "#E0E0E0",
            BackgroundColorDisabled = "#D0D0D0",
            ForegroundColor = "#111111",
            ForegroundColorPointerOver = "#111111",
            ForegroundColorPressed = "#202020",
            ForegroundColorDisabled = "#808080",
            TextControlBorderColorDisabled = "#BDBDBD",
            BorderColor = "#707070",
            BorderColorPointerOver = "#B0B0B0",
            SystemAccentColor = "#635EA6",
            SystemAccentColorLight1 = "#393957",
            SystemAccentColorDark1 = "#2B2B46",
            NeutralAccentColor = "#A1A1B3",
            NeutralAccentColorPointerOver = "#8A8A9A",
            AccentColor1 = "#A9A6CD",
            AccentColor1Note = "#A9A6CD",
            AccentColor2 = "#635EA6",
            AccentColor3 = "#736DC1",
            NoteBorderColor = "#7B79D9",
            NoteBorderColorPressed = "#4C4B98",
            TickLineColor = "#C0C0CA",
            BarNumberColor = "#4F4F6E",
            FinalPitchColor = "#FF8979FF",
            TrackBackgroundAltColor = "#F0F0F0",
            WarningColor = "#FFF4CE",
            ToolbarCheckedHoverColor = "#E0E0E0",
            ToolTipForegroundColor = "#FFFFFF",
            WorkspaceCanvasColor = "#E4E4E8",
            WorkspaceCardColor = "#FCFCFC",
            WorkspaceElevatedSurfaceColor = "#E8E8EC",
            MutedIconColor = "#808080",
            PianoRollWaveformPeakColor = "#59999999",
            PianoRollToolbarStripColor = "#202020",
            PianoRollToolbarButtonHoverColor = "#313131",
            PianoRollTimelineStripColor = "#D8D8DE",
            AppTopBarTransportStripColor = "#E8E8EC",
            AppTopBarTransportHoverColor = "#F4F4F8",
            AppTopBarValueStripColor = "#D8D8DE",
            AppTopBarValueDividerColor = "#C0C0C8",
            WhiteKeyColorLeft = "#FCFCFC",
            WhiteKeyColorRight = "#FCFCFC",
            WhiteKeyNameColor = "#343434",
            CenterKeyColorLeft = "#A9A6CD",
            CenterKeyColorRight = "#D3CFFF",
            CenterKeyNameColor = "#111111",
            BlackKeyColorLeft = "#45445C",
            BlackKeyColorRight = "#45445C",
            BlackKeyNameColor = "#FCFCFC",
        };
    }

    /// <summary>Classic OpenUtau light palette (stakira/OpenUtau) with fork-only keys filled in.</summary>
    public static ThemeYaml CreateOriginalLight() {
        return new ThemeYaml {
            Name = OriginalLightThemeName,
            IsDarkMode = false,
            BackgroundColor = "#FFFFFF",
            BackgroundColorPointerOver = "#F0F0F0",
            TransportToolbarOffHoverColor = "#F0F0F0",
            BackgroundColorPressed = "#E0E0E0",
            BackgroundColorDisabled = "#D0D0D0",
            ForegroundColor = "#000000",
            ForegroundColorPointerOver = "#000000",
            ForegroundColorPressed = "#202020",
            ForegroundColorDisabled = "#808080",
            TextControlBorderColorDisabled = "#BDBDBD",
            BorderColor = "#707070",
            BorderColorPointerOver = "#B0B0B0",
            SystemAccentColor = "#4EA6EA",
            SystemAccentColorLight1 = "#90CAF9",
            SystemAccentColorDark1 = "#1E88E5",
            NeutralAccentColor = "#ADA1B3",
            NeutralAccentColorPointerOver = "#948A99",
            AccentColor1 = "#4EA6EA",
            AccentColor1Note = "#4EA6EA",
            AccentColor2 = "#FF679D",
            AccentColor3 = "#E62E6E",
            NoteBorderColor = "#4EA6EA",
            NoteBorderColorPressed = "#1E88E5",
            TickLineColor = "#AFA3B5",
            BarNumberColor = "#AFA3B5",
            FinalPitchColor = "#C0C0C0",
            TrackBackgroundAltColor = "#F0F0F0",
            WarningColor = "#FFF4CE",
            ToolbarCheckedHoverColor = "#E0E0E0",
            ToolTipForegroundColor = "#FFFFFF",
            WorkspaceCanvasColor = "#E8E8E8",
            WorkspaceCardColor = "#FFFFFF",
            WorkspaceElevatedSurfaceColor = "#F0F0F0",
            MutedIconColor = "#808080",
            PianoRollWaveformPeakColor = "#59999999",
            PianoRollToolbarStripColor = "#202020",
            PianoRollToolbarButtonHoverColor = "#313131",
            PianoRollTimelineStripColor = "#E8E8E8",
            AppTopBarTransportStripColor = "#F0F0F0",
            AppTopBarTransportHoverColor = "#E0E0E0",
            AppTopBarValueStripColor = "#E8E8E8",
            AppTopBarValueDividerColor = "#D0D0D0",
            WhiteKeyColorLeft = "Transparent",
            WhiteKeyColorRight = "Transparent",
            WhiteKeyNameColor = "#FF347C",
            CenterKeyColorLeft = "#FFDDE6",
            CenterKeyColorRight = "#FFCEDC",
            CenterKeyNameColor = "#FF347C",
            BlackKeyColorLeft = "#FF71A3",
            BlackKeyColorRight = "#FF347C",
            BlackKeyNameColor = "#FFFFFF",
        };
    }

    /// <summary>Classic OpenUtau dark palette (stakira/OpenUtau) with fork-only keys filled in.</summary>
    public static ThemeYaml CreateOriginalDark() {
        return new ThemeYaml {
            Name = OriginalDarkThemeName,
            IsDarkMode = true,
            BackgroundColor = "#303030",
            BackgroundColorPointerOver = "#505050",
            TransportToolbarOffHoverColor = "#505050",
            BackgroundColorPressed = "#707070",
            BackgroundColorDisabled = "#404040",
            ForegroundColor = "#E0E0E0",
            ForegroundColorPointerOver = "#FCFCFC",
            ForegroundColorPressed = "#FFFFFF",
            ForegroundColorDisabled = "#A0A0A0",
            TextControlBorderColorDisabled = "#505050",
            BorderColor = "#707070",
            BorderColorPointerOver = "#B0B0B0",
            SystemAccentColor = "#4EA6EA",
            SystemAccentColorLight1 = "#90CAF9",
            SystemAccentColorDark1 = "#1E88E5",
            NeutralAccentColor = "#808080",
            NeutralAccentColorPointerOver = "#A0A0A0",
            AccentColor1 = "#4EA6EA",
            AccentColor1Note = "#4EA6EA",
            AccentColor2 = "#FF679D",
            AccentColor3 = "#E62E6E",
            NoteBorderColor = "#4EA6EA",
            NoteBorderColorPressed = "#1E88E5",
            TickLineColor = "#707070",
            BarNumberColor = "#D0D0D0",
            FinalPitchColor = "#D0D0D0",
            TrackBackgroundAltColor = "#404040",
            WarningColor = "#433519",
            ToolbarCheckedHoverColor = "#A0A0A0",
            ToolTipForegroundColor = "#FFFFFF",
            WorkspaceCanvasColor = "#242424",
            WorkspaceCardColor = "#303030",
            WorkspaceElevatedSurfaceColor = "#404040",
            MutedIconColor = "#808080",
            PianoRollWaveformPeakColor = "#3BFFFFFF",
            PianoRollToolbarStripColor = "#252525",
            PianoRollToolbarButtonHoverColor = "#505050",
            PianoRollTimelineStripColor = "#303030",
            AppTopBarTransportStripColor = "#353535",
            AppTopBarTransportHoverColor = "#505050",
            AppTopBarValueStripColor = "#282828",
            AppTopBarValueDividerColor = "#505050",
            WhiteKeyColorLeft = "#CC2A63",
            WhiteKeyColorRight = "#FF347C",
            WhiteKeyNameColor = "#FFFFFF",
            CenterKeyColorLeft = "#CCA5B0",
            CenterKeyColorRight = "#FFCEDC",
            CenterKeyNameColor = "#FF347C",
            BlackKeyColorLeft = "Transparent",
            BlackKeyColorRight = "Transparent",
            BlackKeyNameColor = "#FFFFFF",
        };
    }

    public static ThemeYaml CreateCold() {
        var yaml = CreateDark();
        ApplyPresetTint(yaml, surfaceR: -1, surfaceG: -1, surfaceB: 2, accentR: 0, accentG: 0, accentB: 1);
        return yaml;
    }

    public static ThemeYaml CreateWarm() {
        var yaml = CreateDark();
        ApplyPresetTint(yaml, surfaceR: 2, surfaceG: 0, surfaceB: -1, accentR: 1, accentG: 0, accentB: -1);
        return yaml;
    }

    static readonly string[] SurfaceTintKeys = [
        nameof(ThemeYaml.BackgroundColor),
        nameof(ThemeYaml.BackgroundColorPointerOver),
        nameof(ThemeYaml.BackgroundColorPressed),
        nameof(ThemeYaml.BackgroundColorDisabled),
        nameof(ThemeYaml.TextControlBorderColorDisabled),
        nameof(ThemeYaml.TransportToolbarOffHoverColor),
        nameof(ThemeYaml.ToolbarCheckedHoverColor),
        nameof(ThemeYaml.WorkspaceCanvasColor),
        nameof(ThemeYaml.WorkspaceCardColor),
        nameof(ThemeYaml.WorkspaceElevatedSurfaceColor),
        nameof(ThemeYaml.TrackBackgroundAltColor),
        nameof(ThemeYaml.MutedIconColor),
        nameof(ThemeYaml.PianoRollToolbarStripColor),
        nameof(ThemeYaml.PianoRollToolbarButtonHoverColor),
        nameof(ThemeYaml.PianoRollTimelineStripColor),
        nameof(ThemeYaml.AppTopBarTransportStripColor),
        nameof(ThemeYaml.AppTopBarTransportHoverColor),
        nameof(ThemeYaml.AppTopBarValueStripColor),
        nameof(ThemeYaml.AppTopBarValueDividerColor),
        nameof(ThemeYaml.TickLineColor),
        nameof(ThemeYaml.BarNumberColor),
        nameof(ThemeYaml.PianoRollWaveformPeakColor),
        nameof(ThemeYaml.WarningColor),
    ];

    static readonly string[] AccentTintKeys = [
        nameof(ThemeYaml.SystemAccentColor),
        nameof(ThemeYaml.SystemAccentColorLight1),
        nameof(ThemeYaml.SystemAccentColorDark1),
        nameof(ThemeYaml.NeutralAccentColor),
        nameof(ThemeYaml.NeutralAccentColorPointerOver),
        nameof(ThemeYaml.AccentColor1),
        nameof(ThemeYaml.AccentColor1Note),
        nameof(ThemeYaml.AccentColor2),
        nameof(ThemeYaml.AccentColor3),
        nameof(ThemeYaml.NoteBorderColor),
        nameof(ThemeYaml.NoteBorderColorPressed),
    ];

    static void ApplyPresetTint(
        ThemeYaml yaml,
        int surfaceR, int surfaceG, int surfaceB,
        int accentR, int accentG, int accentB) {
        foreach (var key in SurfaceTintKeys) {
            yaml.SetColor(key, TintHex(yaml.GetColor(key), surfaceR, surfaceG, surfaceB));
        }
        foreach (var key in AccentTintKeys) {
            yaml.SetColor(key, TintHex(yaml.GetColor(key), accentR, accentG, accentB));
        }
    }

    static string TintHex(string? color, int deltaR, int deltaG, int deltaB) {
        if (string.IsNullOrWhiteSpace(color)
            || string.Equals(color, "Transparent", StringComparison.OrdinalIgnoreCase)) {
            return color ?? "#000000";
        }
        if (!ThemeColorStorage.TryParse(color, out var parsed)) {
            return color;
        }
        return ThemeColorStorage.ToStorageString(Color.FromArgb(
            parsed.A,
            (byte)Math.Clamp(parsed.R + deltaR, 0, 255),
            (byte)Math.Clamp(parsed.G + deltaG, 0, 255),
            (byte)Math.Clamp(parsed.B + deltaB, 0, 255)));
    }
}
