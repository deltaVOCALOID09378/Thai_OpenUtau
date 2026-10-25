// ========================================================================
// Date: 25/10/2569 (BE) / 25/10/2026 (CE)
// File: ScreenCaptureService.cs
// Version: 2.4.0
// Made And Checked By DELTA SYNTH [Patiphat Wongyai] & All Code Agentic AI Engine
// [ Original by "OpenUtau Authors" ]
// Location: OpenUtau/Capture/ScreenCaptureService.cs
// Description: Multi-resolution screen capture and high-throughput non-blocking video recorder engine for Thai OpenUtau.
// ========================================================================

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.App.Capture {
    /// <summary>
    /// เอนจินบันทึกภาพหน้าจอและถ่ายคลิปวิดีโอความเร็วสูง (High-Performance Screen Capture & Video Recording Engine)
    /// ออกแบบมาเพื่อการทำงานที่ลื่นไหล 60 FPS ไร้การหน่วงบน UI Thread พร้อมรองรับความละเอียดตั้งแต่ 240p จนถึง 4K (2160p)
    /// </summary>
    public class ScreenCaptureService {
        private static ScreenCaptureService? instance;
        public static ScreenCaptureService Inst => instance ??= new ScreenCaptureService();

        private Process? ffmpegProcess;
        private Stream? ffmpegStdin;
        private DispatcherTimer? recordTimer;
        private Control? targetControl;
        private string currentOutputFile = string.Empty;
        private bool isRecording;
        private BlockingCollection<byte[]>? frameQueue;
        private Task? encodeWorkerTask;
        private CancellationTokenSource? cts;

        public bool IsRecording => isRecording;
        public Action<string>? OnStatusChanged { get; set; }
        public Action<bool>? OnRecordingStateChanged { get; set; }

        /// <summary>
        /// คำนวณและดึงตำแหน่งโฟลเดอร์สำหรับส่งออกไฟล์ที่ปลอดภัย
        /// รองรับทั้ง Custom Path ใน Preferences และ Default Path ในโฟลเดอร์ Videos
        /// </summary>
        public string GetResolvedExportLocation() {
            string customLocation = Preferences.Default.CaptureExportLocation;
            if (!string.IsNullOrWhiteSpace(customLocation) && Directory.Exists(customLocation)) {
                return customLocation;
            }

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string defaultPath = Path.Combine(userProfile, "Videos", "ThaiOpenUtau Captures");
            if (!Directory.Exists(defaultPath)) {
                defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ThaiOpenUtau Captures");
            }
            try {
                Directory.CreateDirectory(defaultPath);
            } catch { }

            return defaultPath;
        }

        /// <summary>
        /// สร้างชื่อไฟล์ตามรูปแบบ Timestamp หรือ Custom Format จาก Preferences
        /// </summary>
        public string GenerateFilename(string extension, string? projectName = null) {
            string format = Preferences.Default.CaptureFilenameFormat;
            if (string.IsNullOrWhiteSpace(format)) {
                format = "ThaiOpenUtau-name-dd-MM-yyyy-HH-mm-ss";
            }

            string name = projectName ?? DocManager.Inst.Project?.name ?? "ThaiOpenUtau";
            if (string.IsNullOrWhiteSpace(name)) {
                name = "ThaiOpenUtau";
            }

            foreach (char c in Path.GetInvalidFileNameChars()) {
                name = name.Replace(c, '_');
            }

            DateTime now = DateTime.Now;
            string filename = format;
            filename = Regex.Replace(filename, @"\bname\b", name, RegexOptions.IgnoreCase);
            filename = filename.Replace("dd", now.ToString("dd"));
            filename = filename.Replace("MM", now.ToString("MM"));
            filename = filename.Replace("yyyy", now.ToString("yyyy"));
            filename = filename.Replace("yy", now.ToString("yy"));
            filename = filename.Replace("HH", now.ToString("HH"));
            filename = filename.Replace("hh", now.ToString("HH"));
            filename = filename.Replace("mm", now.ToString("mm"));
            filename = filename.Replace("ss", now.ToString("ss"));

            filename = filename.Replace(":", "-").Replace("/", "-").Replace("\\", "-");
            foreach (char c in Path.GetInvalidFileNameChars()) {
                filename = filename.Replace(c, '-');
            }

            if (!extension.StartsWith(".")) {
                extension = "." + extension;
            }

            return filename + extension;
        }

        /// <summary>
        /// คำนวณมิติความละเอียดตามมาตรฐานที่กำหนด: 240p, 480p, 720p, 1080p, 2K (1440p), 2160p / 4K
        /// </summary>
        public (int width, int height, double scale) CalculateTargetDimensions(Control control) {
            double boundsWidth = control.Bounds.Width;
            double boundsHeight = control.Bounds.Height;
            if (boundsWidth <= 0) boundsWidth = 1920;
            if (boundsHeight <= 0) boundsHeight = 1080;

            // ตรวจสอบค่าความละเอียดจาก Preferences.Default.CaptureResolution
            int targetHeight = Preferences.Default.CaptureResolution switch {
                240 => 240,
                480 => 480,
                720 => 720,
                1080 => 1080,
                1440 or 2048 => 1440,       // 2K QHD (2560x1440)
                2160 or 4096 => 2160,       // 4K UHD (3840x2160)
                _ => 1080
            };

            double scale = (double)targetHeight / boundsHeight;
            int targetWidth = (int)Math.Round(boundsWidth * scale);

            // บังคับให้เป็นเลขคู่เสมอตามข้อกำหนดของ H.264 Video Encoder
            if (targetWidth % 2 != 0) targetWidth++;
            if (targetHeight % 2 != 0) targetHeight++;

            return (targetWidth, targetHeight, scale);
        }

        /// <summary>
        /// ถ่ายภาพหน้าจอ Piano Roll ในทันที (Capture Screenshot Lossless PNG)
        /// </summary>
        public async Task<string?> CapturePictureAsync(Control control) {
            try {
                string exportDir = GetResolvedExportLocation();
                Directory.CreateDirectory(exportDir);

                string filename = GenerateFilename(".png");
                string fullPath = Path.Combine(exportDir, filename);

                var (width, height, scale) = CalculateTargetDimensions(control);
                var dpi = new Vector(96 * scale, 96 * scale);

                using var rtb = new RenderTargetBitmap(new PixelSize(width, height), dpi);
                rtb.Render(control);
                await Task.Run(() => rtb.Save(fullPath));

                Log.Information("Piano Roll screenshot captured: {FullPath}", fullPath);
                DocManager.Inst.ExecuteCmd(new UserMessageNotification($"📸 แคปภาพสำเร็จ: {filename}"));
                OnStatusChanged?.Invoke($"Saved picture: {filename}");
                return fullPath;
            } catch (Exception ex) {
                Log.Error(ex, "Failed to capture Piano Roll picture");
                DocManager.Inst.ExecuteCmd(new UserMessageNotification("เกิดข้อผิดพลาดในการบันทึกภาพ"));
                OnStatusChanged?.Invoke("Failed to capture picture");
                return null;
            }
        }

        /// <summary>
        /// ตรวจสอบและค้นหา ffmpeg executable ภายในสภาพแวดล้อมระบบ
        /// </summary>
        public string? FindFfmpeg() {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string exe = OS.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
            string p1 = Path.Combine(baseDir, exe);
            if (File.Exists(p1)) return p1;

            if (!string.IsNullOrEmpty(PathManager.Inst.RootPath)) {
                string p2 = Path.Combine(PathManager.Inst.RootPath, exe);
                if (File.Exists(p2)) return p2;

                string p3 = Path.Combine(PathManager.Inst.RootPath, "runtimes", OS.IsWindows() ? "win-x64" : "linux-x64", "native", exe);
                if (File.Exists(p3)) return p3;
            }

            string? envPath = Environment.GetEnvironmentVariable("PATH");
            if (envPath != null) {
                foreach (string dir in envPath.Split(Path.PathSeparator)) {
                    try {
                        string candidate = Path.Combine(dir, exe);
                        if (File.Exists(candidate)) return candidate;
                    } catch { }
                }
            }

            return null;
        }

        /// <summary>
        /// เริ่มต้นการบันทึกวิดีโอแบบ Non-blocking Smooth Pipeline ไร้การกระตุกบน UI Thread
        /// </summary>
        public bool StartRecording(Control? control, int fps = 60) {
            if (control == null || isRecording) return false;
            targetControl = control;

            string? ffmpegPath = FindFfmpeg();
            if (string.IsNullOrEmpty(ffmpegPath)) {
                Log.Warning("FFmpeg executable not found. Video recording requires ffmpeg.exe.");
                DocManager.Inst.ExecuteCmd(new UserMessageNotification("⚠️ ไม่พบ FFmpeg ในระบบ กรุณาวาง ffmpeg.exe ในโฟลเดอร์โปรแกรม"));
                OnStatusChanged?.Invoke("FFmpeg not found! Place ffmpeg.exe next to OpenUtau.dll");
                return false;
            }

            try {
                string exportDir = GetResolvedExportLocation();
                Directory.CreateDirectory(exportDir);

                string ext = Preferences.Default.CaptureVideoFormat?.TrimStart('.') ?? "mp4";
                if (string.IsNullOrWhiteSpace(ext)) ext = "mp4";
                string filename = GenerateFilename("." + ext);
                currentOutputFile = Path.Combine(exportDir, filename);

                var (width, height, scale) = CalculateTargetDimensions(control);

                // พารามิเตอร์ FFmpeg แบบ Ultra-fast Pipe Stream (yuv420p สำหรับรองรับการเล่นบนทุกแพลตฟอร์ม)
                string args = $"-y -f image2pipe -vcodec png -r {fps} -i - -c:v libx264 -pix_fmt yuv420p -preset veryfast -crf 18 \"{currentOutputFile}\"";

                var psi = new ProcessStartInfo {
                    FileName = ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                ffmpegProcess = new Process { StartInfo = psi };
                ffmpegProcess.Start();
                ffmpegStdin = new BufferedStream(ffmpegProcess.StandardInput.BaseStream, 8 * 1024 * 1024);

                isRecording = true;
                cts = new CancellationTokenSource();
                frameQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>(), boundedCapacity: 120);

                // Background Encoding Worker: ทำหน้าที่สตรีมข้อมูลเข้า FFmpeg Stdin แยกจาก UI Thread อย่างเด็ดขาด
                encodeWorkerTask = Task.Run(() => {
                    try {
                        while (!cts.Token.IsCancellationRequested || frameQueue.Count > 0) {
                            if (frameQueue.TryTake(out var frameBytes, 100)) {
                                ffmpegStdin.Write(frameBytes, 0, frameBytes.Length);
                            }
                        }
                    } catch (Exception ex) {
                        Log.Error(ex, "Error streaming frame to FFmpeg worker pipe");
                    }
                }, cts.Token);

                var dpi = new Vector(96 * scale, 96 * scale);

                // UI Capture Timer: จับภาพหน้าจอและโยนเข้าคิวทันทีโดยไม่ประมวลผลไฟล์บน UI Thread
                recordTimer = new DispatcherTimer(DispatcherPriority.Render) {
                    Interval = TimeSpan.FromMilliseconds(1000.0 / fps)
                };

                recordTimer.Tick += (s, e) => {
                    if (!isRecording || ffmpegStdin == null || targetControl == null || frameQueue.IsAddingCompleted) return;
                    try {
                        using var rtb = new RenderTargetBitmap(new PixelSize(width, height), dpi);
                        rtb.Render(targetControl);
                        using var ms = new MemoryStream();
                        rtb.Save(ms);
                        frameQueue.TryAdd(ms.ToArray());
                    } catch (Exception ex) {
                        Log.Error(ex, "Frame capture tick error");
                    }
                };

                recordTimer.Start();
                OnRecordingStateChanged?.Invoke(true);
                DocManager.Inst.ExecuteCmd(new UserMessageNotification($"🔴 เริ่มบันทึกวิดีโอ: {width}x{height} @ {fps}fps"));
                Log.Information("Piano Roll recording started: {CurrentOutputFile}", currentOutputFile);
                return true;
            } catch (Exception ex) {
                Log.Error(ex, "Failed to start recording");
                DocManager.Inst.ExecuteCmd(new UserMessageNotification("เกิดข้อผิดพลาดในการเริ่มต้นบันทึกวิดีโอ"));
                _ = StopRecordingAsync();
                return false;
            }
        }

        /// <summary>
        /// หยุดการบันทึกวิดีโอ ระบายคิวเฟรมคงเหลือ และปิดเซสชันอย่างสมบูรณ์
        /// </summary>
        public async Task<string> StopRecordingAsync() {
            if (!isRecording) return string.Empty;
            isRecording = false;

            recordTimer?.Stop();
            recordTimer = null;
            frameQueue?.CompleteAdding();
            cts?.Cancel();

            if (encodeWorkerTask != null) {
                await encodeWorkerTask;
            }

            try {
                if (ffmpegStdin != null) {
                    ffmpegStdin.Flush();
                    ffmpegStdin.Close();
                    ffmpegStdin = null;
                }

                if (ffmpegProcess != null) {
                    if (!ffmpegProcess.WaitForExit(5000)) {
                        ffmpegProcess.Kill();
                    }
                    ffmpegProcess.Dispose();
                    ffmpegProcess = null;
                }

                Log.Information("Piano Roll recording saved: {CurrentOutputFile}", currentOutputFile);
                Dispatcher.UIThread.Post(() => {
                    OnRecordingStateChanged?.Invoke(false);
                    DocManager.Inst.ExecuteCmd(new UserMessageNotification($"🎬 บันทึกวิดีโอสำเร็จ: {Path.GetFileName(currentOutputFile)}"));
                    OnStatusChanged?.Invoke($"Saved video: {Path.GetFileName(currentOutputFile)}");
                });
            } catch (Exception ex) {
                Log.Error(ex, "Error finalizing video recording");
                Dispatcher.UIThread.Post(() => {
                    OnRecordingStateChanged?.Invoke(false);
                    DocManager.Inst.ExecuteCmd(new UserMessageNotification("เกิดข้อผิดพลาดในการบันทึกวิดีโอ"));
                });
            }

            return currentOutputFile;
        }
    }
}
