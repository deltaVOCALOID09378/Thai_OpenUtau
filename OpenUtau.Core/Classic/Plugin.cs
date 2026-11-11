// ============================================================================
// 1. วัน/เดือน/ปี: 11 พฤศจิกายน พ.ศ. 2569 / 11 November 2026
// 2. ชื่อไฟล์โค้ด: Plugin.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.1
// 4. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau ]
// 5. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Core/Classic/Plugin.cs
// 6. รายละเอียดการแก้ไขงาน: เพิ่มระบบตรวจสอบและ Sanitize PE Characteristics ป้องกัน Win32Exception (193) สำหรับปลั๊กอินคลาสสิก
// ============================================================================

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Classic {
    public class Plugin : IPlugin {
        public string Name;
        public string Executable;
        public bool AllNotes;
        public bool UseShell;
        private string encoding = "shift_jis";
        public string Shortcut;

        public string Encoding { get => encoding; set => encoding = value; }

        private static void EnsureValidPeHeader(string exePath) {
            if (!OS.IsWindows() || !File.Exists(exePath)) return;
            try {
                using var fs = new FileStream(exePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                if (fs.Length < 0x120) return;
                using var reader = new BinaryReader(fs, System.Text.Encoding.Default, leaveOpen: true);
                using var writer = new BinaryWriter(fs, System.Text.Encoding.Default, leaveOpen: true);
                fs.Seek(0x3C, SeekOrigin.Begin);
                uint peOffset = reader.ReadUInt32();
                if (peOffset + 24 > fs.Length) return;
                fs.Seek(peOffset, SeekOrigin.Begin);
                uint peSig = reader.ReadUInt32();
                if (peSig != 0x00004550) return; // 'PE\0\0'
                long charOffset = peOffset + 4 + 18;
                fs.Seek(charOffset, SeekOrigin.Begin);
                ushort characteristics = reader.ReadUInt16();
                long dllCharOffset = peOffset + 4 + 20 + 70;
                ushort dllCharacteristics = 0;
                if (dllCharOffset + 2 <= fs.Length) {
                    fs.Seek(dllCharOffset, SeekOrigin.Begin);
                    dllCharacteristics = reader.ReadUInt16();
                }
                if (characteristics == 0xFFFF || (characteristics & 0x2000) != 0 || dllCharacteristics == 0xFFFF) {
                    fs.Seek(charOffset, SeekOrigin.Begin);
                    writer.Write((ushort)0x0102); // IMAGE_FILE_EXECUTABLE_IMAGE | IMAGE_FILE_32BIT_MACHINE
                    if (dllCharOffset + 2 <= fs.Length) {
                        fs.Seek(dllCharOffset, SeekOrigin.Begin);
                        writer.Write((ushort)0x0000);
                    }
                    fs.Flush();
                    Log.Information("Sanitized invalid PE Characteristics in plugin executable: {Path}", exePath);
                }
            } catch (Exception ex) {
                Log.Debug(ex, "Could not check/sanitize PE header for {Path}", exePath);
            }
        }

        public async Task Run(string tempFile) {
            if (!File.Exists(Executable)) {
                throw new FileNotFoundException($"Executable {Executable} not found.");
            }
            string winePath = Preferences.Default.WinePath;
            string ext = Path.GetExtension(Executable).ToLowerInvariant();
            bool useWine = !OS.IsWindows()
                && !string.IsNullOrEmpty(winePath)
                && (ext == ".exe" || ext == ".bat");
            string resolvedWinePath = string.Empty;
            if (useWine) {
                resolvedWinePath = Path.GetFullPath(winePath);
                if (!Path.IsPathRooted(resolvedWinePath) || !File.Exists(resolvedWinePath)) {
                    throw new FileNotFoundException($"Wine executable {resolvedWinePath} not found.");
                }
            }
            var startInfo = new ProcessStartInfo() {
                WorkingDirectory = Path.GetDirectoryName(Executable),
            };
            if (useWine) {
                startInfo.FileName = resolvedWinePath;
                startInfo.Arguments = $"\"{Executable}\" \"{tempFile}\"";
                startInfo.UseShellExecute = false;
                startInfo.Environment.Add("LANG", "ja_JP.utf8");
            } else if (OS.IsWindows() && (ext == ".bat" || ext == ".cmd")) {
                startInfo.FileName = "cmd.exe";
                // DELTA SYNTH: Wrap entire command in quotes so cmd.exe handles spaces in OneDrive paths perfectly
                startInfo.Arguments = $"/c \"\"{Executable}\" \"{tempFile}\"\"";
                startInfo.UseShellExecute = UseShell;
            } else {
                if (OS.IsWindows() && ext == ".exe") {
                    EnsureValidPeHeader(Executable);
                }
                startInfo.FileName = Executable;
                startInfo.Arguments = $"\"{tempFile}\"";
                startInfo.UseShellExecute = UseShell;
            }

            Process? process = null;
            try {
                process = Process.Start(startInfo);
            } catch (Win32Exception ex) when (ex.NativeErrorCode == 193 && OS.IsWindows() && ext == ".exe") {
                Log.Warning(ex, "Executable {Path} failed with Win32 Error 193. Attempting PE header repair and retrying...", Executable);
                EnsureValidPeHeader(Executable);
                process = Process.Start(startInfo);
            }

            if (process != null) {
                using (process) {
                    await process.WaitForExitAsync();
                }
            }
        }

        public override string ToString() => Name;
    }
}
