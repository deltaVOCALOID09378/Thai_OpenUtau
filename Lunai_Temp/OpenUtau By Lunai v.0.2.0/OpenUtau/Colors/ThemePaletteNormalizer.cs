namespace OpenUtau.Colors;

/// <summary>
/// Keeps legacy / hidden palette keys in sync so fewer fields need editing.
/// </summary>
public static class ThemePaletteNormalizer {
    public static void Normalize(ThemeYaml yaml) {
        CopyIfPresent(yaml, "AccentColor2", "SystemAccentColor");
        CopyIfPresent(yaml, "BlackKeyColorLeft", "BlackKeyColorRight");
    }

    static void CopyIfPresent(ThemeYaml yaml, string sourceKey, string targetKey) {
        var value = yaml.GetColor(sourceKey);
        if (!string.IsNullOrWhiteSpace(value)) {
            yaml.SetColor(targetKey, value);
        }
    }
}
