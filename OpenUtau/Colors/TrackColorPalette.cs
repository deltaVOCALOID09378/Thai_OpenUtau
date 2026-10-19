using System;
using Avalonia.Media;

namespace OpenUtau.Colors;

/// <summary>Derives the full track palette from a base accent and a brighter highlight.</summary>
public static class TrackColorPalette {
    public readonly struct GeneratedPalette {
        public string AccentColor { get; init; }
        public string DarkColor { get; init; }
        public string LightColor { get; init; }
        public string CenterKeyColor { get; init; }
        public string NoteColor { get; init; }
        public string NoteColorPressed { get; init; }
        public string NoteBorderColor { get; init; }
        public string NoteBorderColorPressed { get; init; }
        public string NoteColorEmpty { get; init; }
    }

    public static GeneratedPalette Generate(Color normal, Color bright) {
        var dark = Darken(normal, 0.28);
        var centerKey = Lighten(bright, 0.55);
        var noteBase = Desaturate(Darken(normal, 0.12), 0.12);
        return new GeneratedPalette {
            AccentColor = ToRgbHex(normal),
            DarkColor = ToRgbHex(dark),
            LightColor = ToRgbHex(bright),
            CenterKeyColor = ToRgbHex(centerKey),
            NoteColor = ToArgbHex(0x70, noteBase),
            NoteColorPressed = ToRgbHex(Darken(normal, 0.32)),
            NoteBorderColor = ToRgbHex(Mix(normal, bright, 0.45)),
            NoteBorderColorPressed = ToRgbHex(Lighten(bright, 0.35)),
            NoteColorEmpty = ToArgbHex(0x1A, noteBase),
        };
    }

    static Color Mix(Color a, Color b, double t) {
        return Color.FromRgb(
            Lerp(a.R, b.R, t),
            Lerp(a.G, b.G, t),
            Lerp(a.B, b.B, t));
    }

    static Color Darken(Color color, double amount) => Mix(color, Color.FromRgb(0, 0, 0), amount);

    static Color Lighten(Color color, double amount) => Mix(color, Color.FromRgb(255, 255, 255), amount);

    static Color Desaturate(Color color, double amount) {
        byte gray = (byte)((color.R + color.G + color.B) / 3);
        var grayColor = Color.FromRgb(gray, gray, gray);
        return Mix(color, grayColor, amount);
    }

    static byte Lerp(byte a, byte b, double t) =>
        (byte)Math.Clamp((int)Math.Round(a + (b - a) * t), 0, 255);

    static string ToRgbHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    static string ToArgbHex(byte alpha, Color color) => $"#{alpha:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
}
