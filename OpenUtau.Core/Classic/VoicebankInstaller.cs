using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using K4os.Hash.xxHash;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace OpenUtau.Classic {

    public class VoicebankInstaller {
        const string kCharacterTxt = "character.txt";
        const string kCharacterYaml = "character.yaml";
        const string kInstallTxt = "install.txt";

        private string basePath;
        private readonly Action<double, string> progress;
        private readonly Encoding archiveEncoding;
        private readonly Encoding textEncoding;

        public VoicebankInstaller(string basePath, Action<double, string> progress, Encoding archiveEncoding, Encoding textEncoding) {
            Directory.CreateDirectory(basePath);
            this.basePath = basePath;
            this.progress = progress;
            this.archiveEncoding = archiveEncoding;
            this.textEncoding = textEncoding;
        }

        public void Install(string path, string singerType) {
            progress.Invoke(0, "Analyzing archive...");
            var readerOptions = new ReaderOptions {
                ArchiveEncoding = new ArchiveEncoding {
                    Forced = archiveEncoding,
                }
            };
            var extractionOptions = new ExtractionOptions {
                Overwrite = true,
            };
            using (var archive = ArchiveFactory.OpenArchive(path, readerOptions)) {
                var initialCanonicalBase = Path.GetFullPath(basePath);
                if (!initialCanonicalBase.EndsWith(Path.DirectorySeparatorChar)) {
                    initialCanonicalBase += Path.DirectorySeparatorChar;
                }
                var touches = new List<string>();
                AdjustBasePath(archive, path, touches);
                var canonicalBase = Path.GetFullPath(basePath);
                if (!canonicalBase.EndsWith(Path.DirectorySeparatorChar)) {
                    canonicalBase += Path.DirectorySeparatorChar;
                }
                if (!canonicalBase.StartsWith(initialCanonicalBase, StringComparison.OrdinalIgnoreCase)) {
                    throw new InvalidOperationException("Archive base path escaped target directory.");
                }
                int total = archive.Entries.Count();
                int count = 0;
                bool hasCharacterYaml = archive.Entries.Any(e => Path.GetFileName(e.Key) == kCharacterYaml);
                foreach (var entry in archive.Entries) {
                    if (string.IsNullOrEmpty(entry.Key)) {
                        continue;
                    }
                    string fixedKey = entry.Key.Replace("\\", "/");
                    progress.Invoke(100.0 * ++count / total, fixedKey);
                    if (entry.Key.Contains("..")) {
                        // Prevent zipSlip attack
                        continue;
                    }
                    var filePath = Path.GetFullPath(Path.Combine(basePath, fixedKey));
                    if (!filePath.StartsWith(canonicalBase, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                    var dirPath = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(dirPath)) {
                        Directory.CreateDirectory(dirPath);
                    }
                    if (!entry.IsDirectory && fixedKey != kInstallTxt) {
                        entry.WriteToFile(filePath, extractionOptions);
                        if (!hasCharacterYaml && Path.GetFileName(filePath) == kCharacterTxt) {
                            var config = new VoicebankConfig() {
                                TextFileEncoding = textEncoding.WebName,
                                SingerType = singerType,
                            };
                            using (var stream = File.Open(Path.ChangeExtension(filePath, ".yaml"), FileMode.Create)) {
                                config.Save(stream);
                            }
                        }
                        if (hasCharacterYaml && Path.GetFileName(filePath) == kCharacterYaml) {
                            VoicebankConfig? config = null;
                            using (var stream = File.Open(filePath, FileMode.Open)) {
                                config = VoicebankConfig.Load(stream);
                            }
                            if (string.IsNullOrEmpty(config.SingerType)) {
                                config.SingerType = singerType;
                                using (var stream = File.Open(filePath, FileMode.Open)) {
                                    config.Save(stream);
                                }
                            }
                        }
                    }
                }
                foreach (var touch in touches) {
                    var fullTouch = Path.GetFullPath(touch);
                    if (!fullTouch.StartsWith(canonicalBase, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                    var touchDir = Path.GetDirectoryName(fullTouch);
                    if (!string.IsNullOrEmpty(touchDir)) {
                        Directory.CreateDirectory(touchDir);
                    }
                    File.WriteAllText(fullTouch, "\n");
                    var config = new VoicebankConfig() {
                        TextFileEncoding = textEncoding.WebName,
                    };
                    using (var stream = File.Open(Path.ChangeExtension(fullTouch, ".yaml"), FileMode.Create)) {
                        config.Save(stream);
                    }
                }
            }
        }

        private void AdjustBasePath(IArchive archive, string archivePath, List<string> touches) {
            var dirsAndFiles = archive.Entries
                .Select(e => e.Key)
                .Where(k => !string.IsNullOrEmpty(k))
                .ToHashSet();
            var rootDirs = archive.Entries
                .Where(e => e.IsDirectory && !string.IsNullOrEmpty(e.Key))
                .Where(e => !e.Key.Contains("..") && !Path.IsPathRooted(e.Key))
                .Where(e => (e.Key.IndexOf('\\') < 0 || e.Key.IndexOf('\\') == e.Key.Length - 1)
                         && (e.Key.IndexOf('/') < 0 || e.Key.IndexOf('/') == e.Key.Length - 1))
                .ToArray();
            var rootFiles = archive.Entries
                .Where(e => !e.IsDirectory && !string.IsNullOrEmpty(e.Key))
                .Where(e => !e.Key.Contains("..") && !Path.IsPathRooted(e.Key))
                .Where(e => !e.Key.Contains('\\') && !e.Key.Contains('/') && e.Key != kInstallTxt)
                .ToArray();
            if (rootFiles.Count() > 0) {
                // Need to create root folder.
                var folderName = Path.GetFileNameWithoutExtension(archivePath).Trim();
                if (string.IsNullOrEmpty(folderName) || folderName == "." || folderName.Contains("..")) {
                    folderName = "voicebank";
                }
                basePath = Path.Combine(basePath, folderName);
                if (rootFiles.Where(e => e.Key == kCharacterTxt).Count() == 0) {
                    // Need to create character.txt.
                    touches.Add(Path.Combine(basePath, kCharacterTxt));
                }
                return;
            }
            foreach (var rootDir in rootDirs) {
                if (!dirsAndFiles.Contains($"{rootDir.Key}{kCharacterTxt}") &&
                    !dirsAndFiles.Contains($"{rootDir.Key}\\{kCharacterTxt}") &&
                    !dirsAndFiles.Contains($"{rootDir.Key}/{kCharacterTxt}")) {
                    touches.Add(Path.Combine(basePath, rootDir.Key, kCharacterTxt));
                }
            }
        }

        static string HashPath(string path) {
            string file = Path.GetFileName(path);
            string dir = Path.GetDirectoryName(path);
            file = $"{XXH32.DigestOf(Encoding.UTF8.GetBytes(file)):x8}";
            if (string.IsNullOrEmpty(dir)) {
                return file;
            }
            return Path.Combine(HashPath(dir), file);
        }
    }
}
