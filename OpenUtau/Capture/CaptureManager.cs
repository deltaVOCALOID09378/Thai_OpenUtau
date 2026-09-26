using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.App.Capture
{
    public class CaptureManager
    {
        private static CaptureManager? instance;
        public static CaptureManager Inst => instance ??= new CaptureManager();

        private Process? ffmpegProcess;
        private Stream? ffmpegStdin;
        private DispatcherTimer? recordTimer;
        private Control? targetControl;
        private string currentOutputFile = string.Empty;
        private bool isRecording;
        private int currentCountdown;
        private DispatcherTimer? countdownTimer;

        public bool IsRecording => isRecording;
        public Action<string>? OnStatusChanged { get; set; }
        public Action<bool>? OnRecordingStateChanged { get; set; }
        public Action<int>? OnCountdownTick { get; set; }

        public string GetResolvedExportLocation()
        {
            string loc = Preferences.Default.CaptureExportLocation;
            if (string.IsNullOrWhiteSpace(loc))
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                loc = Path.Combine(userProfile, "OneDrive", "Videos", "OpenUtau");
                if (!Directory.Exists(Path.GetDirectoryName(loc)))
                {
                    loc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "OpenUtau");
                }
            }
            return loc;
        }

        public string GenerateFilename(string extension, string? projectName = null)
        {
            string format = Preferences.Default.CaptureFilenameFormat;
            if (string.IsNullOrWhiteSpace(format))
            {
                format = "name-dd-mm-yy-hh:mm";
            }

            string name = projectName;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = DocManager.Inst.Project?.name;
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "OpenUtau";
            }

            // Sanitize name
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            DateTime now = DateTime.Now;
            string filename = format;
            filename = Regex.Replace(filename, @"\bname\b", name, RegexOptions.IgnoreCase);
            filename = filename.Replace("dd", now.ToString("dd"));
            filename = filename.Replace("mm", now.ToString("MM")); // Month
            filename = filename.Replace("MM", now.ToString("MM"));
            filename = filename.Replace("yy", now.ToString("yy"));
            filename = filename.Replace("yyyy", now.ToString("yyyy"));
            filename = filename.Replace("hh", now.ToString("HH"));
            filename = filename.Replace("HH", now.ToString("HH"));
            filename = filename.Replace("mm", now.ToString("mm")); // Minute
            filename = filename.Replace("ss", now.ToString("ss"));

            // Colons and other invalid characters replaced with safe chars
            filename = filename.Replace(":", "-");
            filename = filename.Replace("/", "-");
            filename = filename.Replace("\\", "-");
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c, '-');
            }

            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            return filename + extension;
        }

        public (int width, int height, double scale) CalculateTargetDimensions(Control control)
        {
            double boundsWidth = control.Bounds.Width;
            double boundsHeight = control.Bounds.Height;
            if (boundsWidth <= 0) boundsWidth = 1920;
            if (boundsHeight <= 0) boundsHeight = 1080;

            int targetHeight = Preferences.Default.CaptureResolution switch
            {
                720 => 720,
                1080 => 1080,
                2048 => 1440,
                4096 => 2160,
                _ => 1080
            };

            double scale = (double)targetHeight / boundsHeight;
            int targetWidth = (int)Math.Round(boundsWidth * scale);

            // Ensure even numbers for video encoders
            if (targetWidth % 2 != 0) targetWidth++;
            if (targetHeight % 2 != 0) targetHeight++;

            return (targetWidth, targetHeight, scale);
        }

        public async Task<string?> CapturePictureAsync(Control control)
        {
            try
            {
                string exportDir = GetResolvedExportLocation();
                Directory.CreateDirectory(exportDir);

                string filename = GenerateFilename(".png");
                string fullPath = Path.Combine(exportDir, filename);

                var (width, height, scale) = CalculateTargetDimensions(control);
                var dpi = new Vector(96 * scale, 96 * scale);

                using var rtb = new RenderTargetBitmap(new PixelSize(width, height), dpi);
                rtb.Render(control);
                rtb.Save(fullPath);

                Log.Information($"Piano Roll picture captured: {fullPath}");
                OnStatusChanged?.Invoke($"Saved picture: {filename}");
                return fullPath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to capture Piano Roll picture");
                OnStatusChanged?.Invoke("Failed to capture picture");
                return null;
            }
        }

        public string? FindFfmpeg()
        {
            // 1. Next to DLLs / BaseDirectory
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string exe = OS.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
            string p1 = Path.Combine(baseDir, exe);
            if (File.Exists(p1)) return p1;

            // 2. Next to MainModule / Entry Assembly / RootPath
            if (!string.IsNullOrEmpty(PathManager.Inst.RootPath))
            {
                string p2 = Path.Combine(PathManager.Inst.RootPath, exe);
                if (File.Exists(p2)) return p2;

                string p3 = Path.Combine(PathManager.Inst.RootPath, "runtimes", OS.IsWindows() ? "win-x64" : "linux-x64", "native", exe);
                if (File.Exists(p3)) return p3;
            }

            // 3. Check system PATH
            string? envPath = Environment.GetEnvironmentVariable("PATH");
            if (envPath != null)
            {
                foreach (string dir in envPath.Split(Path.PathSeparator))
                {
                    try
                    {
                        string candidate = Path.Combine(dir, exe);
                        if (File.Exists(candidate)) return candidate;
                    }
                    catch { }
                }
            }

            return null;
        }

        public void StartRecordingWithCountdown(Control control)
        {
            if (isRecording)
            {
                StopRecording();
                return;
            }

            int countdown = Preferences.Default.CaptureCountdown;
            if (countdown <= 0)
            {
                StartRecording(control);
                return;
            }

            currentCountdown = countdown;
            targetControl = control;
            OnStatusChanged?.Invoke($"Recording in {currentCountdown}s...");
            OnCountdownTick?.Invoke(currentCountdown);

            countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            countdownTimer.Tick += (s, e) =>
            {
                currentCountdown--;
                if (currentCountdown > 0)
                {
                    OnStatusChanged?.Invoke($"Recording in {currentCountdown}s...");
                    OnCountdownTick?.Invoke(currentCountdown);
                }
                else
                {
                    countdownTimer.Stop();
                    countdownTimer = null;
                    StartRecording(targetControl);
                }
            };
            countdownTimer.Start();
        }

        public void CancelCountdown()
        {
            if (countdownTimer != null)
            {
                countdownTimer.Stop();
                countdownTimer = null;
                OnStatusChanged?.Invoke("Recording cancelled");
                OnCountdownTick?.Invoke(0);
            }
        }

        private void StartRecording(Control? control)
        {
            if (control == null || isRecording) return;
            targetControl = control;

            string? ffmpegPath = FindFfmpeg();
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                Log.Warning("FFmpeg executable not found. Video recording requires ffmpeg.exe next to OpenUtau.dll.");
                OnStatusChanged?.Invoke("FFmpeg not found! Place ffmpeg.exe next to OpenUtau.dll");
                return;
            }

            try
            {
                string exportDir = GetResolvedExportLocation();
                Directory.CreateDirectory(exportDir);

                string ext = Preferences.Default.CaptureVideoFormat.TrimStart('.');
                if (string.IsNullOrWhiteSpace(ext)) ext = "mp4";
                string filename = GenerateFilename("." + ext);
                currentOutputFile = Path.Combine(exportDir, filename);

                var (width, height, scale) = CalculateTargetDimensions(control);
                int fps = 30;

                // FFmpeg arguments: image2pipe PNG stream from stdin -> encoded video
                string args;
                if (ext.Equals("gif", StringComparison.OrdinalIgnoreCase))
                {
                    args = $"-y -f image2pipe -vcodec png -r {fps} -i - -vf \"fps={fps},split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\" \"{currentOutputFile}\"";
                }
                else
                {
                    args = $"-y -f image2pipe -vcodec png -r {fps} -i - -c:v libx264 -pix_fmt yuv420p -preset ultrafast -crf 20 \"{currentOutputFile}\"";
                }

                var psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                ffmpegProcess = new Process { StartInfo = psi };
                ffmpegProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Log.Debug($"[FFmpeg] {e.Data}");
                    }
                };

                ffmpegProcess.Start();
                ffmpegProcess.BeginErrorReadLine();
                ffmpegStdin = ffmpegProcess.StandardInput.BaseStream;

                isRecording = true;
                OnRecordingStateChanged?.Invoke(true);
                OnStatusChanged?.Invoke("Recording started...");

                var dpi = new Vector(96 * scale, 96 * scale);

                // Periodic frame capture on UI thread
                recordTimer = new DispatcherTimer(DispatcherPriority.Render)
                {
                    Interval = TimeSpan.FromMilliseconds(1000.0 / fps)
                };

                recordTimer.Tick += (s, e) =>
                {
                    if (!isRecording || ffmpegStdin == null || targetControl == null) return;
                    try
                    {
                        using var rtb = new RenderTargetBitmap(new PixelSize(width, height), dpi);
                        rtb.Render(targetControl);
                        rtb.Save(ffmpegStdin);
                        ffmpegStdin.Flush();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error writing video frame");
                    }
                };

                recordTimer.Start();
                Log.Information($"Piano Roll recording started: {currentOutputFile}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start recording");
                OnStatusChanged?.Invoke("Failed to start recording");
                StopRecording();
            }
        }

        public void StopRecording()
        {
            if (!isRecording && countdownTimer != null)
            {
                CancelCountdown();
                return;
            }

            if (!isRecording) return;
            isRecording = false;

            recordTimer?.Stop();
            recordTimer = null;

            Task.Run(() =>
            {
                try
                {
                    if (ffmpegStdin != null)
                    {
                        ffmpegStdin.Flush();
                        ffmpegStdin.Close();
                        ffmpegStdin = null;
                    }

                    if (ffmpegProcess != null)
                    {
                        if (!ffmpegProcess.WaitForExit(5000))
                        {
                            ffmpegProcess.Kill();
                        }
                        ffmpegProcess.Dispose();
                        ffmpegProcess = null;
                    }

                    Log.Information($"Piano Roll recording stopped and saved: {currentOutputFile}");
                    Dispatcher.UIThread.Post(() =>
                    {
                        OnRecordingStateChanged?.Invoke(false);
                        OnStatusChanged?.Invoke($"Saved video: {Path.GetFileName(currentOutputFile)}");
                    });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error stopping recording");
                    Dispatcher.UIThread.Post(() =>
                    {
                        OnRecordingStateChanged?.Invoke(false);
                        OnStatusChanged?.Invoke("Error saving video");
                    });
                }
            });
        }
    }
}
