// ============================================================================
// 1. วัน/เดือน/ปี: 1 พฤศจิกายน พ.ศ. 2569 / 01 November 2026
// 2. ชื่อไฟล์โค้ด: ProcessRunner.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.2
// 4. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Core/Util/ProcessRunner.cs
// 5. หน้าที่: รันโปรเซสภายนอก (เช่น Resampler, Wavtool) แบบ Thread-Safe พร้อมส่งคืนข้อความ Output
// 6. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Agentic AI | Original by OpenUtau Authors
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Serilog;

namespace OpenUtau.Core.Util {
    public static class ProcessRunner {
        public static bool DebugSwitch { get; set; }

        public static string Run(string file, string args, ILogger logger, string workDir = null, int timeoutMs = 60000) {
            if (!File.Exists(file)) {
                throw new FileNotFoundException($"Executable {file} not found.");
            }
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var output = new StringBuilder();
            var outputLock = new object();

            using var stdoutDone = new ManualResetEventSlim(false);
            using var stderrDone = new ManualResetEventSlim(false);
            using (var proc = new Process()) {
                proc.StartInfo = new ProcessStartInfo(file, args) {
                    Environment = { { "LANG", "ja_JP.utf8" } },
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workDir,
                };

                proc.OutputDataReceived += (o, e) => {
                    if (e.Data == null) {
                        stdoutDone.Set();
                        return;
                    }
                    if (DebugSwitch && !string.IsNullOrEmpty(e.Data)) {
                        logger.Information($"ProcessRunner >>> [thread-{threadId}] {e.Data}");
                    }
                    lock (outputLock) {
                        output.AppendLine(e.Data);
                    }
                };

                proc.ErrorDataReceived += (o, e) => {
                    if (e.Data == null) {
                        stderrDone.Set();
                        return;
                    }
                    if (!string.IsNullOrEmpty(e.Data)) {
                        logger.Error($"ProcessRunner >>> [thread-{threadId}] {e.Data}");
                    }
                    lock (outputLock) {
                        output.AppendLine(e.Data);
                    }
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                bool exited;
                if (timeoutMs <= 0) {
                    proc.WaitForExit();
                    exited = true;
                } else {
                    exited = proc.WaitForExit(timeoutMs);
                }

                if (!exited) {
                    logger.Warning($"ProcessRunner >>> [thread-{threadId}] Timeout, killing...");
                    try {
                        proc.Kill(entireProcessTree: true);
                        logger.Warning($"ProcessRunner >>> [thread-{threadId}] Killed.");
                    } catch (Exception e) {
                        logger.Error(e, $"ProcessRunner >>> [thread-{threadId}] Failed to kill");
                    }
                }

                try {
                    proc.WaitForExit();
                } catch {
                }

                stdoutDone.Wait(TimeSpan.FromSeconds(5));
                stderrDone.Wait(TimeSpan.FromSeconds(5));

                lock (outputLock) {
                    if (!exited) {
                        output.AppendLine("Killed due to timeout.");
                    } else {
                        try {
                            output.Append("Exit code ").Append(proc.ExitCode);
                        } catch { }
                    }
                    return output.ToString();
                }
            }
        }
    }
}
