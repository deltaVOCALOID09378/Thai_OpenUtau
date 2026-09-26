using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenUtau.Core.Util {
    public class ScreenRecorder {
        [DllImport("ntdll.dll", PreserveSig = false, ExactSpelling = true)]
        private static extern void NtSuspendProcess(IntPtr processHandle);

        [DllImport("ntdll.dll", PreserveSig = false, ExactSpelling = true)]
        private static extern void NtResumeProcess(IntPtr processHandle);

        private Process? process;
        private string? outputPath;

        public bool IsRecording { get; private set; }
        public bool IsPaused { get; private set; }

        public ScreenRecorder() {
        }

        public void StartRecord(string outputFilePath) {
            if (IsRecording) return;
            outputPath = outputFilePath;

            process = new Process();
            process.StartInfo.FileName = "ffmpeg";
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                process.StartInfo.Arguments = $"-y -f gdigrab -framerate 30 -i desktop -vf scale=1920:-2 -c:v libx264 -preset ultrafast -crf 18 \"{outputPath}\"";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                process.StartInfo.Arguments = $"-y -f avfoundation -framerate 30 -i \"1\" -vf scale=1920:-2 -c:v libx264 -preset ultrafast -crf 18 \"{outputPath}\"";
            } else {
                process.StartInfo.Arguments = $"-y -f x11grab -framerate 30 -i :0.0 -vf scale=1920:-2 -c:v libx264 -preset ultrafast -crf 18 \"{outputPath}\"";
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            
            try {
                process.Start();
                IsRecording = true;
                IsPaused = false;
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, "Recording started..."));
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification("Failed to start FFmpeg. Ensure ffmpeg is installed and in your PATH.", e));
                process = null;
            }
        }

        public void PauseRecord() {
            if (!IsRecording || IsPaused || process == null) return;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                try {
                    NtSuspendProcess(process.Handle);
                    IsPaused = true;
                    DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, "Recording paused."));
                } catch (Exception e) {
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification("Failed to pause recording.", e));
                }
            } else {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification("Pause is only supported on Windows."));
            }
        }

        public void ResumeRecord() {
            if (!IsRecording || !IsPaused || process == null) return;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                try {
                    NtResumeProcess(process.Handle);
                    IsPaused = false;
                    DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, "Recording resumed..."));
                } catch (Exception e) {
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification("Failed to resume recording.", e));
                }
            }
        }

        public void StopRecord() {
            if (!IsRecording || process == null) return;
            
            if (IsPaused) {
                ResumeRecord();
            }

            try {
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, "Saving recording..."));
                process.StandardInput.WriteLine("q");
                process.WaitForExit(5000);
            } catch { }
            finally {
                try {
                    if (!process.HasExited) {
                        process.Kill();
                    }
                } catch { }
                
                process.Dispose();
                process = null;
                IsRecording = false;
                IsPaused = false;
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, "Recording saved."));
            }
        }
    }
}
