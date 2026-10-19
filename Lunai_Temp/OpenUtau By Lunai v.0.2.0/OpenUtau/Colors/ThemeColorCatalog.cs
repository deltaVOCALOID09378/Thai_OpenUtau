using System.Collections.Generic;
using System.Linq;

namespace OpenUtau.Colors;

public static class ThemeColorCatalog {
    public sealed record Section(string Title, params string[] Keys);

    /// <summary>Applied at runtime but not shown in the theme editor.</summary>
    public static readonly string[] HiddenResourceKeys = [
        "SystemAccentColor",
        "BlackKeyColorRight",
    ];

    static readonly Dictionary<string, string> DisplayNames = new() {
        ["BackgroundColor"] = "Panel surface (menus, stripes)",
        ["BackgroundColorPointerOver"] = "Panel hover",
        ["BackgroundColorPressed"] = "Panel pressed",
        ["BackgroundColorDisabled"] = "Panel disabled",
        ["ForegroundColor"] = "Text",
        ["ForegroundColorPointerOver"] = "Text hover",
        ["ForegroundColorPressed"] = "Text pressed",
        ["ForegroundColorDisabled"] = "Text disabled",
        ["TextControlBorderColorDisabled"] = "Input border disabled",
        ["BorderColor"] = "Border",
        ["BorderColorPointerOver"] = "Border hover",
        ["WarningColor"] = "Warning banner",

        ["SystemAccentColorLight1"] = "Selection border hover",
        ["SystemAccentColorDark1"] = "Selection border pressed",
        ["NeutralAccentColor"] = "Chrome tint (scrollbars, playback)",
        ["NeutralAccentColorPointerOver"] = "Chrome tint hover",
        ["AccentColor1"] = "Curve / secondary UI accent",
        ["AccentColor1Note"] = "Note fill (default)",
        ["AccentColor2"] = "Primary accent",
        ["AccentColor3"] = "Favorite / tertiary accent",
        ["NoteBorderColor"] = "Note border",
        ["NoteBorderColorPressed"] = "Note border pressed",

        ["WorkspaceCanvasColor"] = "Workspace canvas",
        ["WorkspaceCardColor"] = "Card / panel fill",
        ["WorkspaceElevatedSurfaceColor"] = "Elevated surface",
        ["TrackBackgroundAltColor"] = "Track stripe",
        ["MutedIconColor"] = "Muted icon",

        ["TransportToolbarOffHoverColor"] = "Transport toolbar hover",
        ["ToolbarCheckedHoverColor"] = "Lit toggle hover",
        ["ToolTipForegroundColor"] = "Tooltip text",
        ["PianoRollToolbarStripColor"] = "Piano roll toolbar strip",
        ["PianoRollToolbarButtonHoverColor"] = "Piano roll toolbar hover",

        ["AppTopBarTransportStripColor"] = "Top bar transport strip",
        ["AppTopBarTransportHoverColor"] = "Top bar transport hover",
        ["AppTopBarValueStripColor"] = "Top bar value strip",
        ["AppTopBarValueDividerColor"] = "Top bar divider",

        ["TickLineColor"] = "Timeline tick",
        ["BarNumberColor"] = "Bar / tempo label",
        ["FinalPitchColor"] = "Final pitch curve",
        ["PianoRollWaveformPeakColor"] = "Waveform peak",
        ["PianoRollTimelineStripColor"] = "Timeline strip",

        ["WhiteKeyColorLeft"] = "White key left",
        ["WhiteKeyColorRight"] = "White key right",
        ["WhiteKeyNameColor"] = "White key label",
        ["CenterKeyColorLeft"] = "Center key left",
        ["CenterKeyColorRight"] = "Center key right",
        ["CenterKeyNameColor"] = "Center key label",
        ["BlackKeyColorLeft"] = "Black key",
        ["BlackKeyNameColor"] = "Black key label",
    };

    public static readonly Section[] Sections = [
        new("Surfaces", [
            "WorkspaceCanvasColor",
            "WorkspaceCardColor",
            "WorkspaceElevatedSurfaceColor",
            "TrackBackgroundAltColor",
            "BackgroundColor",
            "BackgroundColorPointerOver",
            "BackgroundColorPressed",
            "BackgroundColorDisabled",
        ]),
        new("Text & borders", [
            "ForegroundColor",
            "ForegroundColorPointerOver",
            "ForegroundColorPressed",
            "ForegroundColorDisabled",
            "TextControlBorderColorDisabled",
            "BorderColor",
            "BorderColorPointerOver",
            "MutedIconColor",
            "WarningColor",
        ]),
        new("Accents", [
            "AccentColor2",
            "SystemAccentColorLight1",
            "SystemAccentColorDark1",
            "AccentColor1",
            "AccentColor1Note",
            "AccentColor3",
            "NeutralAccentColor",
            "NeutralAccentColorPointerOver",
            "NoteBorderColor",
            "NoteBorderColorPressed",
        ]),
        new("Toolbars", [
            "TransportToolbarOffHoverColor",
            "ToolbarCheckedHoverColor",
            "ToolTipForegroundColor",
            "PianoRollToolbarStripColor",
            "PianoRollToolbarButtonHoverColor",
            "AppTopBarTransportStripColor",
            "AppTopBarTransportHoverColor",
            "AppTopBarValueStripColor",
            "AppTopBarValueDividerColor",
        ]),
        new("Piano roll", [
            "TickLineColor",
            "BarNumberColor",
            "FinalPitchColor",
            "PianoRollWaveformPeakColor",
            "PianoRollTimelineStripColor",
        ]),
        new("Piano keyboard", [
            "WhiteKeyColorLeft",
            "WhiteKeyColorRight",
            "WhiteKeyNameColor",
            "CenterKeyColorLeft",
            "CenterKeyColorRight",
            "CenterKeyNameColor",
            "BlackKeyColorLeft",
            "BlackKeyNameColor",
        ]),
    ];

    public static string GetDisplayName(string key) {
        return DisplayNames.TryGetValue(key, out var name) ? name : key;
    }

    public static string[] AllResourceKeys { get; } = Sections
        .SelectMany(section => section.Keys)
        .Concat(HiddenResourceKeys)
        .Distinct()
        .ToArray();
}
