using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.App;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.Colors;

public static class CustomTrackColorStore {
    static readonly Dictionary<string, string> colorPaths = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, string> ColorPaths => colorPaths;

    public static IEnumerable<TrackColor> LoadAll() {
        colorPaths.Clear();
        Directory.CreateDirectory(PathManager.Inst.TrackColorsPath);
        var loaded = new List<TrackColor>();
        foreach (var file in Directory.EnumerateFiles(PathManager.Inst.TrackColorsPath, "*.yaml")) {
            try {
                var yaml = TrackColorYaml.LoadFromFile(file);
                if (string.IsNullOrWhiteSpace(yaml.Name)) {
                    yaml.Name = Path.GetFileNameWithoutExtension(file);
                }
                var resolvedName = ResolveUniqueDisplayName(yaml.Name);
                yaml.Name = resolvedName;
                var trackColor = TrackColor.FromCustomYaml(yaml, file);
                if (colorPaths.ContainsKey(trackColor.Name)) {
                    continue;
                }
                colorPaths.Add(trackColor.Name, file);
                loaded.Add(trackColor);
            } catch (Exception exception) {
                Log.Error(exception, "Failed to parse track color yaml in {Path}", file);
            }
        }
        return loaded.OrderBy(color => color.Name, StringComparer.OrdinalIgnoreCase);
    }

    public static bool TrySave(TrackColorYaml yaml, out string? savedPath) {
        savedPath = null;
        if (string.IsNullOrWhiteSpace(yaml.Name)) {
            return false;
        }
        if (ThemeManager.IsBuiltInTrackColorName(yaml.Name)
            || ThemeManager.TrackColors.Any(color => string.Equals(color.Name, yaml.Name, StringComparison.OrdinalIgnoreCase))) {
            return false;
        }

        string fileName = SanitizeFileName(yaml.Name) + ".yaml";
        string path = Path.Combine(PathManager.Inst.TrackColorsPath, fileName);
        if (File.Exists(path)) {
            return false;
        }

        Directory.CreateDirectory(PathManager.Inst.TrackColorsPath);
        yaml.SaveToFile(path);
        savedPath = path;
        return true;
    }

    public static bool TryUpdate(TrackColor existing, TrackColorYaml yaml, out string? savedPath) {
        savedPath = null;
        if (!existing.IsCustom || string.IsNullOrEmpty(existing.StoragePath)) {
            return false;
        }
        if (string.IsNullOrWhiteSpace(yaml.Name)) {
            return false;
        }

        string oldName = existing.Name;
        string newName = yaml.Name.Trim();
        if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) {
            if (ThemeManager.IsBuiltInTrackColorName(newName)
                || ThemeManager.TrackColors.Any(color =>
                    !string.Equals(color.Name, oldName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(color.Name, newName, StringComparison.OrdinalIgnoreCase))) {
                return false;
            }
        }

        string oldPath = existing.StoragePath;
        string newPath = Path.Combine(PathManager.Inst.TrackColorsPath, SanitizeFileName(newName) + ".yaml");
        if (!string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase) && File.Exists(newPath)) {
            return false;
        }

        Directory.CreateDirectory(PathManager.Inst.TrackColorsPath);
        yaml.Name = newName;
        yaml.SaveToFile(newPath);
        if (!string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase) && File.Exists(oldPath)) {
            File.Delete(oldPath);
            colorPaths.Remove(oldName);
        }
        colorPaths[newName] = newPath;
        savedPath = newPath;
        return true;
    }

    public static bool TryDelete(TrackColor trackColor) {
        if (!trackColor.IsCustom || string.IsNullOrEmpty(trackColor.StoragePath)) {
            return false;
        }
        if (File.Exists(trackColor.StoragePath)) {
            File.Delete(trackColor.StoragePath);
        }
        colorPaths.Remove(trackColor.Name);
        return true;
    }

    public static string SanitizeNewName(string name) => name.Trim();

    static string SanitizeFileName(string name) =>
        string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c == ' '))
            .Replace(' ', '-')
            .ToLowerInvariant();

    static string ResolveUniqueDisplayName(string baseName) {
        string resolved = baseName.Trim();
        int dupIter = 1;
        while (ThemeManager.IsBuiltInTrackColorName(resolved) || colorPaths.ContainsKey(resolved)) {
            resolved = $"{baseName.Trim()} ({dupIter})";
            dupIter++;
        }
        return resolved;
    }
}
