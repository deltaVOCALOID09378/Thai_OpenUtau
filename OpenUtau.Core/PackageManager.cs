// ============================================================================
// วัน/เดือน/ปี: 10 พฤศจิกายน 2569 (2026-11-10)
// ชื่อไฟล์: PackageManager.cs
// ลำดับเวอร์ชัน: 1.4
// เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// ตำแหน่งที่จัดวาง: OpenUtau.Core/PackageManager.cs
// รายละเอียดการแก้ไขงาน: รองรับการเปรียบเทียบเลขเวอร์ชันรูปแบบตัวเลขเดี่ยว/วันที่ (เช่น 230917, 240512) ป้องกันการลบ oudep.yaml เมื่อไฟล์ .onnx ถูกล็อก และป้องกัน Zip-Slip
// ============================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using SharpCompress.Archives;
using SharpCompress.Readers;
using OpenUtau.Core.Util;

namespace OpenUtau.Core {
    public class RegistryMirror {
        public string url = string.Empty;
        public string hash = string.Empty;
    }
    public class RegistryVersion {
        public string version = string.Empty;
        public RegistryMirror[] mirrors = [];
    }
    public class RegistrySoftware {
        public string id = string.Empty;
        public Dictionary<string, string> names = new Dictionary<string, string>();
        public string category = string.Empty;
        public string[] developers = [];
        public string homepage_url = string.Empty;
        public string download_page_url = string.Empty;
        public string[] tags = [];
        public RegistryVersion[] versions = [];

        public string LocalizedName() {
            if (names.TryGetValue("en", out var n)) return n;
            if (names.Values.FirstOrDefault() is string v) return v;
            return id;
        }
    }

    public class OudepEntrypoint {
        /// <summary>
        /// Loader type that determines how this entry point is processed.
        /// E.g. "ThemeLoader", "DllLoader".
        /// </summary>
        public string loader = string.Empty;

        /// <summary>
        /// Path local to the package root pointing to the entry point file.
        /// </summary>
        public string path = string.Empty;
    }

    public class OudepMetadata {
        public string id = string.Empty;
        [Obsolete] public string? name = null;
        public string version = string.Empty;
        public string description = string.Empty;
        [Obsolete] public string @class = string.Empty;
        public OudepEntrypoint[] entrypoints = [];
    }

    public class PackageManager : SingletonBase<PackageManager> {
        public const string OudepExt = ".oudep";
        const string registryUrl = "https://openutau.github.io/svs-index/registry/v1/softwares/all.json";

        public async Task<List<RegistrySoftware>> FetchRegistryAsync() {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "OpenUtau");
            client.Timeout = TimeSpan.FromSeconds(30);
            using var response = await client.GetAsync(registryUrl);
            response.EnsureSuccessStatusCode();
            string body = await response.Content.ReadAsStringAsync();

            List<RegistrySoftware> list = new List<RegistrySoftware>();
            try {
                var token = JToken.Parse(body);
                var items = token["items"];
                if (items != null && items.Type == JTokenType.Array) {
                    list = items.ToObject<List<RegistrySoftware>>() ?? new List<RegistrySoftware>();
                }
            } catch (Exception e) {
                Log.Warning(e, "Failed to parse registry JSON");
                list = new List<RegistrySoftware>();
            }

            return list.Where(s => s.tags != null && s.tags.Contains("oudep")).ToList();
        }

        public class InstalledEntrypoint {
            public OudepMetadata Package { get; set; } = null!;
            public OudepEntrypoint Entrypoint { get; set; } = null!;
            public string PackagePath { get; set; } = string.Empty;
            public string EntrypointPath => Path.Combine(PackagePath, Entrypoint.path);
        }

        public async Task<List<InstalledEntrypoint>> GetInstalledEntrypointsAsync() {
            var list = new List<InstalledEntrypoint>();
            var depPath = PathManager.Inst.DependencyPath;
            if (!Directory.Exists(depPath)) {
                return list;
            }
            return await Task.Run(() => {
                var dirs = Directory.GetDirectories(depPath);
                foreach (var dir in dirs) {
                    OudepMetadata? metadata = null;
                    try {
                        var yamlPath = Path.Combine(dir, "oudep.yaml");
                        if (!File.Exists(yamlPath)) {
                            continue;
                        }
                        using var reader = new StreamReader(yamlPath, Encoding.UTF8);
                        metadata = Core.Yaml.DefaultDeserializer.Deserialize<OudepMetadata>(reader) ?? new OudepMetadata();
                        if (string.IsNullOrEmpty(metadata.id)) {
                            metadata.id = Path.GetFileName(dir);
                        }
                        if (string.IsNullOrEmpty(metadata.version)) {
                            metadata.version = string.Empty;
                        }
                    } catch (Exception e) {
                        Log.Error(e, "Failed to read oudep.yaml in {dir}", dir);
                        continue;
                    }

                    // Collect entry points from the entrypoints array
                    if (metadata.entrypoints != null) {
                        foreach (var ep in metadata.entrypoints) {
                            try {
                                if (string.IsNullOrEmpty(ep.loader) || string.IsNullOrEmpty(ep.path)) {
                                    Log.Warning("Skipping entrypoint in package {id} with empty loader or path", metadata.id);
                                    continue;
                                }
                                list.Add(new InstalledEntrypoint {
                                    Package = metadata,
                                    Entrypoint = ep,
                                    PackagePath = dir,
                                });
                            } catch (Exception e) {
                                Log.Error(e, "Failed to load entrypoint '{path}' in package {id}", ep.path, metadata.id);
                            }
                        }
                    }

                    // Fallback: if no entrypoints defined but @class is set, treat the package root as a single entry point
                    if ((metadata.entrypoints == null || metadata.entrypoints.Length == 0) && !string.IsNullOrEmpty(metadata.@class)) {
                        try {
                            list.Add(new InstalledEntrypoint {
                                Package = metadata,
                                Entrypoint = new OudepEntrypoint { loader = metadata.@class, path = string.Empty },
                                PackagePath = dir,
                            });
                        } catch (Exception e) {
                            Log.Error(e, "Failed to load fallback @class entrypoint in package {id}", metadata.id);
                        }
                    }
                }
                return list;
            });
        }

        public async Task<List<OudepMetadata>> GetInstalledAsync() {
            var list = new List<OudepMetadata>();
            var depPath = PathManager.Inst.DependencyPath;
            if (!Directory.Exists(depPath)) {
                return list;
            }
            return await Task.Run(() => {
                var dirs = Directory.GetDirectories(depPath);
                var results = dirs.Select(dir => {
                    try {
                        var yamlPath = Path.Combine(dir, "oudep.yaml");
                        if (!File.Exists(yamlPath)) {
                            return null;
                        }
                        using var reader = new StreamReader(yamlPath, Encoding.UTF8);
                        var metadata = Core.Yaml.DefaultDeserializer.Deserialize<OudepMetadata>(reader) ?? new OudepMetadata();
                        if (string.IsNullOrEmpty(metadata.id)) {
                            metadata.id = Path.GetFileName(dir);
                        }
                        if (string.IsNullOrEmpty(metadata.version)) {
                            metadata.version = string.Empty;
                        }
                        return metadata;
                    } catch (Exception e) {
                        Log.Error($"Failed to read oudep.yaml in {dir} {e}");
                        return null;
                    }
                }).Where(r => r != null).Select(r => r!).ToList();
                return results;
            });
        }

        static string GetSha256Hex(byte[] data) {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(data);
            var sb = new StringBuilder();
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static bool TryParseNormalizedVersion(string? raw, out Version normalized) {
            normalized = new Version(0, 0, 0, 0);
            if (string.IsNullOrWhiteSpace(raw)) {
                return false;
            }
            string cleaned = raw.Trim().TrimStart('v', 'V');
            int dashIdx = cleaned.IndexOf('-');
            if (dashIdx > 0) {
                cleaned = cleaned.Substring(0, dashIdx);
            }
            int plusIdx = cleaned.IndexOf('+');
            if (plusIdx > 0) {
                cleaned = cleaned.Substring(0, plusIdx);
            }
            if (!cleaned.Contains('.')) {
                if (int.TryParse(cleaned, out int singleInt) && singleInt >= 0) {
                    normalized = new Version(singleInt, 0, 0, 0);
                    return true;
                }
                return false;
            }
            var parts = cleaned.Split('.');
            if (parts.Length > 4) {
                cleaned = string.Join(".", parts.Take(4));
            }
            if (!Version.TryParse(cleaned, out var parsed) || parsed == null) {
                return false;
            }
            normalized = new Version(
                Math.Max(0, parsed.Major),
                Math.Max(0, parsed.Minor),
                Math.Max(0, parsed.Build),
                Math.Max(0, parsed.Revision));
            return true;
        }

        public static int CompareVersions(string? x, string? y) {
            var a = (x ?? string.Empty).Trim();
            var b = (y ?? string.Empty).Trim();
            bool hasA = TryParseNormalizedVersion(a, out var va);
            bool hasB = TryParseNormalizedVersion(b, out var vb);
            if (hasA && hasB) {
                return va.CompareTo(vb);
            }
            if (hasA) return 1;
            if (hasB) return -1;
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private class VersionStringComparer : IComparer<string> {
            public int Compare(string? x, string? y) => CompareVersions(x, y);
        }

        private static readonly IComparer<string> VersionComparer = new VersionStringComparer();

        public static bool IsValidPackageName(string id) {
            if (string.IsNullOrEmpty(id)) return false;
            if (id.Contains("..") || Path.IsPathRooted(id)) return false;
            return !Path.GetInvalidFileNameChars().Any(c => id.Contains(c));
        }

        public static bool IsValidRelativePath(string path) {
            if (string.IsNullOrEmpty(path)) return false;
            if (path.Contains("..") || Path.IsPathRooted(path)) return false;
            return !Path.GetInvalidPathChars().Any(c => path.Contains(c));
        }

        public static string GetLatestVersionString(RegistryVersion[] versions) {
            if (versions == null || versions.Length == 0) return string.Empty;
            return versions.OrderByDescending(v => v.version, VersionComparer).First().version;
        }

        public async Task InstallAsync(RegistrySoftware software, IProgress<int>? progress = null) {
            ArgumentNullException.ThrowIfNull(software);
            if (software.versions == null || software.versions.Length == 0) throw new ArgumentException("No versions available");
            var version = software.versions.OrderByDescending(v => v.version, VersionComparer).First();
            if (version.mirrors == null || version.mirrors.Length == 0) throw new ArgumentException("No mirrors available");

            // Check if the package is already installed on the machine at an equal or newer version before downloading
            var installedList = await GetInstalledAsync();
            var existing = installedList.FirstOrDefault(i => string.Equals(i.id, software.id, StringComparison.OrdinalIgnoreCase));
            if (existing != null && !string.IsNullOrEmpty(existing.version) && CompareVersions(existing.version, version.version) >= 0) {
                Log.Information("Package {Id} version {InstalledVer} is already installed and up-to-date (target: {TargetVer}). Skipping download.",
                    software.id, existing.version, version.version);
                progress?.Report(100);
                return;
            }

            var mirror = version.mirrors[0];
            string cacheDir = PathManager.Inst.CachePath;
            Directory.CreateDirectory(cacheDir);
            byte[] data;
            using (var client = new HttpClient()) {
                client.Timeout = TimeSpan.FromMinutes(5);
                using var response = await client.GetAsync(mirror.url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                var contentLength = response.Content.Headers.ContentLength;
                using var responseStream = await response.Content.ReadAsStreamAsync();
                using var ms = new MemoryStream();
                var buffer = new byte[81920];
                long totalRead = 0;
                int read;
                if (contentLength.HasValue && progress != null) progress.Report(0);
                while ((read = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                    ms.Write(buffer, 0, read);
                    totalRead += read;
                    if (contentLength.HasValue && progress != null) {
                        var percent = (int)(totalRead * 100 / contentLength.Value);
                        progress.Report(Math.Min(100, percent));
                    }
                }
                if (progress != null) progress.Report(100);
                data = ms.ToArray();
            }
            var hash = mirror.hash; // "sha256:..."
            if (!string.IsNullOrEmpty(hash) && hash.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)) {
                var expected = hash.Substring("sha256:".Length).ToLowerInvariant();
                var actual = GetSha256Hex(data);
                if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase)) {
                    throw new InvalidOperationException("Downloaded file hash does not match expected value");
                }
            } else {
                // Not fatal, but silently skipping integrity verification is a stability/
                // trust risk worth surfacing rather than hiding.
                Log.Warning("Registry mirror for {Id} has no sha256 hash; skipping integrity verification.", software.id);
            }
            using (var ms = new MemoryStream(data)) {
                await InstallFromStreamAsync(ms, software.id, version.version);
            }
        }

        public async Task UninstallAsync(string id) {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            var basePath = Path.Combine(PathManager.Inst.DependencyPath, id);
            if (!Directory.Exists(basePath)) return;
            try {
                await Task.Run(() => Directory.Delete(basePath, true));
            } catch (Exception e) {
                Log.Warning(e, "Failed to uninstall dependency {id}", id);
                throw;
            }
        }

        public async Task InstallFromFileAsync(string archivePath) {
            using var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read);
            await InstallFromStreamAsync(stream, string.Empty, string.Empty);
        }

        public async Task InstallFromStreamAsync(Stream stream, string expectedId, string expectedVersion) {
            using var archive = ArchiveFactory.OpenArchive(stream, new ReaderOptions());
            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, "Installing dependency"));
            var metadataEntry = archive.Entries.FirstOrDefault(e => e.Key == "oudep.yaml");
            if (metadataEntry == null) {
                throw new ArgumentException("Missing oudep.yaml");
            }
            OudepMetadata metadata;
            using (var entryStream = metadataEntry.OpenEntryStream()) {
                using var reader = new StreamReader(entryStream, Encoding.UTF8);
                metadata = Core.Yaml.DefaultDeserializer.Deserialize<OudepMetadata>(reader);
            }
            if (!string.IsNullOrEmpty(expectedId) && metadata.id != expectedId ||
                !string.IsNullOrEmpty(expectedVersion) && metadata.version != expectedVersion) {
                throw new ArgumentException("Archive metadata does not match expected id/version");
            }
            var id = metadata.id;
            if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(metadata.name)) {
                id = metadata.name;
            }
            if (string.IsNullOrEmpty(id) || !IsValidPackageName(id)) {
                throw new ArgumentException("Invalid package id");
            }

            // Validate entrypoints if present
            if (metadata.entrypoints != null && metadata.entrypoints.Length > 0) {
                foreach (var ep in metadata.entrypoints) {
                    if (string.IsNullOrEmpty(ep.loader)) {
                        throw new ArgumentException($"Entrypoint is missing 'loader' field: {ep.path}");
                    }
                    if (string.IsNullOrEmpty(ep.path) || !IsValidRelativePath(ep.path)) {
                        throw new ArgumentException($"Invalid entrypoint path: {ep.path}");
                    }
                }
            } else if (string.IsNullOrEmpty(metadata.@class)) {
                throw new ArgumentException("Missing entrypoints and @class in oudep.yaml");
            }

            await Task.Run(() => {
                var basePath = Path.Combine(PathManager.Inst.DependencyPath, id);
                Directory.CreateDirectory(basePath);
                var canonicalBase = Path.GetFullPath(basePath);
                if (!canonicalBase.EndsWith(Path.DirectorySeparatorChar)) {
                    canonicalBase += Path.DirectorySeparatorChar;
                }
                foreach (var entry in archive.Entries) {
                    if (string.IsNullOrEmpty(entry.Key) || !IsValidRelativePath(entry.Key)) {
                        continue;
                    }
                    var filePath = Path.GetFullPath(Path.Combine(basePath, entry.Key));
                    if (!filePath.StartsWith(canonicalBase, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                    var dir = Path.GetDirectoryName(filePath);
                    if (!entry.IsDirectory && !string.IsNullOrEmpty(dir)) {
                        Directory.CreateDirectory(dir);
                        try {
                            entry.WriteToFile(filePath, new SharpCompress.Common.ExtractionOptions {
                                Overwrite = true
                            });
                        } catch (IOException ex) when (File.Exists(filePath)) {
                            // If a model file (e.g. .onnx) is currently memory-mapped/locked by an active session,
                            // keep the existing file intact instead of leaving the package in a broken state without oudep.yaml.
                            Log.Warning(ex, "File {FilePath} is currently in use; keeping existing file in place.", filePath);
                        }
                    }
                }
            });
            DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"Installed dependency \"{id}\""));
        }

        public string? GetInstalledPath(string id) {
            var path = Path.Combine(PathManager.Inst.DependencyPath, id);
            if (Directory.Exists(path)) {
                return path;
            }
            return null;
        }
    }
}
