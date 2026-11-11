// ============================================================================
// 1. วัน/เดือน/ปี: 11 พฤศจิกายน พ.ศ. 2569 / 11 November 2026
// 2. ชื่อไฟล์โค้ด: StandbyCaptureService.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.0
// 4. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
// 5. ตำแหน่งที่จัดวางไฟล์: Tools/OpenUtau.Media.Capture/StandbyCaptureService.cs
// 6. รายละเอียดการแก้ไขงาน: Standby DLL service for high-performance screen recording and lossless capture
// ============================================================================

#pragma warning disable CS8632, CS0618
#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Serilog;

namespace OpenUtau.Media.Capture {
    /// <summary>
    /// Standby Capture Engine DLL for OpenUtau Ecosystem.
    /// Provides high-throughput screen recording, snapshot capture, and media encoding.
    /// </summary>
    public sealed class StandbyCaptureService {
        private static readonly Lazy<StandbyCaptureService> instance = new(() => new StandbyCaptureService());
        public static StandbyCaptureService Inst => instance.Value;

        private Process? activeProcess;
        private bool isRecording;
        private readonly object syncLock = new();

        public bool IsRecording {
            get { lock (syncLock) { return isRecording; } }
            private set { lock (syncLock) { isRecording = value; } }
        }

        public string OutputDirectory { get; set; } = string.Empty;

        public event Action<bool>? RecordingStateChanged;

        public StandbyCaptureService() {
            OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Thai_OpenUtau_Captures");
            try {
                if (!Directory.Exists(OutputDirectory)) {
                    Directory.CreateDirectory(OutputDirectory);
                }
            } catch (Exception ex) {
                Log.Warning(ex, "Failed to create default capture folder: {Path}", OutputDirectory);
            }
        }

        public async Task<string?> CaptureSnapshotAsync(Control targetControl, string? customFilename = null) {
            if (targetControl == null) return null;
            try {
                int width = (int)Math.Max(1, targetControl.Bounds.Width);
                int height = (int)Math.Max(1, targetControl.Bounds.Height);
                var pixelSize = new Avalonia.PixelSize(width, height);
                var dpi = new Avalonia.Vector(96, 96);

                using var rtb = new RenderTargetBitmap(pixelSize, dpi);
                rtb.Render(targetControl);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = customFilename ?? $"Snapshot_{timestamp}.png";
                string fullPath = Path.Combine(OutputDirectory, filename);

                await Task.Run(() => {
                    using var stream = File.OpenWrite(fullPath);
                    rtb.Save(stream);
                });

                Log.Information("Captured snapshot saved to: {Path}", fullPath);
                return fullPath;
            } catch (Exception ex) {
                Log.Error(ex, "Failed to capture snapshot.");
                return null;
            }
        }

        public bool StartRecording(string ffmpegPath, string arguments) {
            lock (syncLock) {
                if (isRecording) return false;
                try {
                    var psi = new ProcessStartInfo {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardError = true
                    };
                    activeProcess = Process.Start(psi);
                    if (activeProcess != null && !activeProcess.HasExited) {
                        isRecording = true;
                        RecordingStateChanged?.Invoke(true);
                        Log.Information("Started standby screen recording process.");
                        return true;
                    }
                } catch (Exception ex) {
                    Log.Error(ex, "Failed to start recording process.");
                }
                return false;
            }
        }

        public async Task<bool> StopRecordingAsync() {
            Process? procToStop = null;
            lock (syncLock) {
                if (!isRecording) return false;
                procToStop = activeProcess;
                activeProcess = null;
                isRecording = false;
            }

            if (procToStop != null) {
                try {
                    if (!procToStop.HasExited) {
                        procToStop.StandardInput.WriteLine("q");
                        await Task.Delay(500);
                        if (!procToStop.HasExited) {
                            procToStop.Kill();
                        }
                    }
                    procToStop.Dispose();
                    RecordingStateChanged?.Invoke(false);
                    Log.Information("Standby screen recording stopped cleanly.");
                    return true;
                } catch (Exception ex) {
                    Log.Error(ex, "Error while stopping recording process.");
                }
            }
            return false;
        }
    }
}
