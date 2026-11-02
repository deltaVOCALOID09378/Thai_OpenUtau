// ============================================================================
// Date         : 29/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : DependencyInstaller.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau.Core/DependencyInstaller.cs
// Description  : Installation of dependencies. Optimized with using declarations.
// ============================================================================
using System;
using System.IO;
using System.Linq;
using System.Text;
using SharpCompress.Archives;

namespace OpenUtau.Core {
    [Serializable]
    public class DependencyConfig {
        public string name;
    }

    public class DependencyInstaller {
        public static string FileExt = ".oudep";
        public static void Install(string archivePath) {
            DependencyConfig dependencyConfig;
            using var archive = ArchiveFactory.OpenArchive(archivePath, new SharpCompress.Readers.ReaderOptions());
            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, "Installing dependency"));
            var configEntry = archive.Entries.FirstOrDefault(e => e.Key == "oudep.yaml") 
                ?? throw new ArgumentException("missing oudep.yaml");
            
            using (var stream = configEntry.OpenEntryStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8)) {
                dependencyConfig = Core.Yaml.DefaultDeserializer.Deserialize<DependencyConfig>(reader);
            }
            
            string name = dependencyConfig?.name;
            if (string.IsNullOrEmpty(name) || name.Contains("..") || Path.IsPathRooted(name)) {
                throw new ArgumentException("missing or invalid name in oudep.yaml");
            }
            
            var basePath = Path.GetFullPath(Path.Combine(PathManager.Inst.DependencyPath, name));
            var dependencyRoot = Path.GetFullPath(PathManager.Inst.DependencyPath);
            if (!dependencyRoot.EndsWith(Path.DirectorySeparatorChar)) {
                dependencyRoot += Path.DirectorySeparatorChar;
            }
            if (!basePath.StartsWith(dependencyRoot, StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException("invalid dependency path in oudep.yaml");
            }
            var canonicalBase = basePath.EndsWith(Path.DirectorySeparatorChar) ? basePath : basePath + Path.DirectorySeparatorChar;
            
            foreach (var entry in archive.Entries) {
                if (string.IsNullOrEmpty(entry.Key) || entry.Key.Contains("..")) continue;
                var filePath = Path.GetFullPath(Path.Combine(basePath, entry.Key));
                if (!filePath.StartsWith(canonicalBase, StringComparison.OrdinalIgnoreCase)) continue;
                
                var dirPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dirPath)) {
                    Directory.CreateDirectory(dirPath);
                }
                if (!entry.IsDirectory) {
                    entry.WriteToFile(filePath);
                }
            }
            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"Installed dependency \"{name}\""));
        }
    }
}