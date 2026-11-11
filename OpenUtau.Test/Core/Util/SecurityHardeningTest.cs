// =================================================================================================
// 1. Date: 10/11/2569 (10/11/2026)
// 2. File Name: SecurityHardeningTest.cs
// 3. Version: 1.2
// 4. Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// 5. Location: OpenUtau.Test/Core/Util/SecurityHardeningTest.cs
// 6. Summary: Unit tests verifying SR-002 (Zip-Slip / path traversal defenses in VoicebankInstaller,
//             DependencyInstaller, and PackageManager), SR-003 (OS.OpenWeb URI validation), and
//             pre-install semantic/integer version comparison in PackageManager and SingerHubClient.
// =================================================================================================

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using OpenUtau.Classic;
using OpenUtau.Core.SingerHub;
using Xunit;

namespace OpenUtau.Core.Util {
    public class SecurityHardeningTest {

        [Theory]
        [InlineData("https://github.com/stakira/OpenUtau", true)]
        [InlineData("http://example.com/voicebank", true)]
        [InlineData("file:///C:/Windows/System32/calc.exe", false)]
        [InlineData("javascript:alert(1)", false)]
        [InlineData("cmd.exe /c calc.exe", false)]
        [InlineData("\\\\evil-server\\share\\payload.exe", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        public void ValidateWebUrlSchemeTest(string url, bool expected) {
            Assert.Equal(expected, OS.IsValidWebUrl(url));
            if (!expected) {
                Assert.Throws<ArgumentException>(() => OS.OpenWeb(url));
            }
        }

        [Theory]
        [InlineData("model.onnx", true)]
        [InlineData("sub/folder/file.yaml", true)]
        [InlineData("../evil.dll", false)]
        [InlineData("sub/../../evil.dll", false)]
        [InlineData("C:\\Windows\\System32\\cmd.exe", false)]
        [InlineData("/etc/passwd", false)]
        [InlineData("", false)]
        public void PackageManagerRelativePathTest(string path, bool expected) {
            Assert.Equal(expected, OpenUtau.Core.PackageManager.IsValidRelativePath(path));
        }

        [Theory]
        [InlineData("1.0", "1.0.0", 0)]
        [InlineData("v1.2.0", "1.2.0.0", 0)]
        [InlineData("2.0.0", "1.9.9", 1)]
        [InlineData("3.9.5", "3.9.4", 1)]
        [InlineData("3.9.4", "3.9.5", -1)]
        [InlineData("1.2.3-beta1", "1.2.3", 0)]
        [InlineData("2025.02", "2024.02", 1)]
        [InlineData("230917", "230917", 0)]
        [InlineData("240512", "230917", 1)]
        [InlineData("1000000", "230917", 1)]
        [InlineData("1.2.3.4.5", "1.2.3.4", 0)]
        public void PackageManagerCompareVersionsTest(string installed, string remote, int expectedSign) {
            int actual = Math.Sign(OpenUtau.Core.PackageManager.CompareVersions(installed, remote));
            Assert.Equal(expectedSign, actual);
            int singerHubActual = Math.Sign(SingerHubClient.CompareVersions(installed, remote));
            Assert.Equal(expectedSign, singerHubActual);
        }

        [Fact]
        public void PackageManagerGetLatestVersionStringTest() {
            var versions = new[] {
                new OpenUtau.Core.RegistryVersion { version = "1.0.0" },
                new OpenUtau.Core.RegistryVersion { version = "v1.2.0" },
                new OpenUtau.Core.RegistryVersion { version = "1.1.5" }
            };
            Assert.Equal("v1.2.0", OpenUtau.Core.PackageManager.GetLatestVersionString(versions));
        }

        [Fact]
        public void VoicebankInstallerBlocksZipSlipAndTouchesTraversalTest() {
            string tempRoot = Path.Combine(Path.GetTempPath(), "OpenUtau_Test_" + Guid.NewGuid().ToString("N"));
            string targetSingersDir = Path.Combine(tempRoot, "Singers");
            string zipPath = Path.Combine(tempRoot, "malicious.zip");
            Directory.CreateDirectory(targetSingersDir);

            try {
                using (var fs = new FileStream(zipPath, FileMode.Create))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create)) {
                    // Attempt traversal via directory entry and file entry
                    archive.CreateEntry("../escaped_dir/");
                    var evilEntry = archive.CreateEntry("../escaped_file.txt");
                    using (var writer = new StreamWriter(evilEntry.Open(), Encoding.UTF8)) {
                        writer.Write("pwned");
                    }
                    var safeEntry = archive.CreateEntry("safe_bank/character.txt");
                    using (var writer = new StreamWriter(safeEntry.Open(), Encoding.UTF8)) {
                        writer.Write("name=SafeSinger\n");
                    }
                }

                var installer = new VoicebankInstaller(
                    targetSingersDir,
                    (p, info) => { },
                    Encoding.UTF8,
                    Encoding.UTF8);
                installer.Install(zipPath, "utau");

                // Verify nothing escaped outside targetSingersDir into tempRoot
                Assert.False(File.Exists(Path.Combine(tempRoot, "escaped_file.txt")));
                Assert.False(Directory.Exists(Path.Combine(tempRoot, "escaped_dir")));
                Assert.False(File.Exists(Path.Combine(tempRoot, "character.txt")));
                Assert.True(File.Exists(Path.Combine(targetSingersDir, "safe_bank", "character.txt")));
                Assert.True(File.Exists(Path.Combine(targetSingersDir, "safe_bank", "character.yaml")));
            } finally {
                if (Directory.Exists(tempRoot)) {
                    for (int retry = 0; retry < 5; retry++) {
                        try {
                            Directory.Delete(tempRoot, true);
                            break;
                        } catch (IOException) {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            System.Threading.Thread.Sleep(50);
                        }
                    }
                }
            }
        }

        [Fact]
        public void DependencyInstallerMissingOudepYamlThrowsArgumentExceptionTest() {
            string tempRoot = Path.Combine(Path.GetTempPath(), "OpenUtau_DepTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            string archivePath = Path.Combine(tempRoot, "missing_config.oudep");

            try {
                using (var fs = new FileStream(archivePath, FileMode.Create))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create)) {
                    var dummy = archive.CreateEntry("dummy.txt");
                    using (var writer = new StreamWriter(dummy.Open(), Encoding.UTF8)) {
                        writer.Write("no oudep.yaml here");
                    }
                }

                var ex = Assert.Throws<ArgumentException>(() => DependencyInstaller.Install(archivePath));
                Assert.Contains("missing oudep.yaml", ex.Message);
            } finally {
                if (Directory.Exists(tempRoot)) {
                    for (int retry = 0; retry < 5; retry++) {
                        try {
                            Directory.Delete(tempRoot, true);
                            break;
                        } catch (IOException) {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            System.Threading.Thread.Sleep(50);
                        }
                    }
                }
            }
        }
    }
}

