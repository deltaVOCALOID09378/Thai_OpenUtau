// ============================================================================
// Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : Program.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau/Program.cs
// Description  : Thai OpenUtau startup and shutdown infrastructure with SHA-256
//                Named Mutex single-instance coordination, structured Serilog
//                initialization, Leelawadee UI font configuration, and bounded
//                async Linux font lookup.
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;

using OpenUtau.App.ViewModels;
using OpenUtau.Core;

using Serilog;

namespace OpenUtau.App {
    public class Program {
        private const int ExitSuccess = 0;
        private const int ExitFailure = 1;

        private const int FontLookupTimeoutMilliseconds = 2500;

        private const string MutexPrefix =
            "ThaiOpenUtau_SingleInstance_";

        /// <summary>
        /// Synchronous entry point is intentional:
        /// named mutex ownership is associated with the acquiring thread.
        /// Do not change this to async Main without revisiting ownership.
        /// </summary>
        [STAThread]
        public static int Main(string[] args) {
            Mutex? singleInstanceMutex = null;
            bool ownsMutex = false;
            bool loggingInitialized = false;
            bool applicationStartupAttempted = false;

            int exitCode = ExitSuccess;

            try {
                // Acquire before initializing logging so a second instance
                // does not attempt to open the application's log file.
                singleInstanceMutex = new Mutex(
                    initiallyOwned: true,
                    name: BuildSingleInstanceMutexName(),
                    createdNew: out bool createdNew);

                if (!createdNew) {
                    WriteStandardError(
                        "Thai OpenUtau is already running " +
                        "in the applicable user/session scope.");

                    return ExitSuccess;
                }

                ownsMutex = true;

                Encoding.RegisterProvider(
                    CodePagesEncodingProvider.Instance);

                InitLogging();
                loggingInitialized = true;

                LogStartupInformation();

                applicationStartupAttempted = true;

                Run(args);

                Log.Information(
                    "Desktop application lifetime ended.");
            } catch (Exception ex) {
                exitCode = ExitFailure;

                ReportFailure(
                    loggingInitialized,
                    ex,
                    "Thai OpenUtau terminated because of an error.");
            } finally {
                // Keep the mutex owned until other cleanup has completed.
                try {
                    if (applicationStartupAttempted &&
                        !OS.IsMacOS()) {
                        NetMQ.NetMQConfig.Cleanup(block: false);
                    }
                } catch (Exception ex) {
                    exitCode = ExitFailure;

                    ReportFailure(
                        loggingInitialized,
                        ex,
                        "NetMQ cleanup failed.");
                }

                // Closing the logger must not prevent mutex disposal.
                try {
                    if (loggingInitialized) {
                        Log.Information(
                            "Closing application resources. " +
                            "Exit code before final cleanup: {ExitCode}",
                            exitCode);
                    }

                    Log.CloseAndFlush();
                } catch (Exception ex) {
                    exitCode = ExitFailure;

                    WriteStandardError(
                        "Failed to close the logger.",
                        ex);
                }

                try {
                    if (ownsMutex &&
                        singleInstanceMutex != null) {
                        singleInstanceMutex.ReleaseMutex();
                        ownsMutex = false;
                    }
                } catch (Exception ex) {
                    exitCode = ExitFailure;

                    WriteStandardError(
                        "Failed to release the single-instance mutex.",
                        ex);
                } finally {
                    try {
                        singleInstanceMutex?.Dispose();
                    } catch (Exception ex) {
                        exitCode = ExitFailure;

                        WriteStandardError(
                            "Failed to dispose the single-instance mutex.",
                            ex);
                    }
                }
            }

            // Normal return lets the runtime perform normal shutdown.
            // Do not force Environment.Exit(0), which would also erase
            // the distinction between successful and failed execution.
            return exitCode;
        }

        /// <summary>
        /// Application builder is also used by the Avalonia designer.
        /// Logging and single-instance initialization must remain outside it.
        /// </summary>
        public static AppBuilder BuildAvaloniaApp() {
            FontManagerOptions fontOptions = CreateFontOptions();

            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI()
                .With(fontOptions)
                .With(new X11PlatformOptions {
                    EnableIme = true,
                });
        }

        public static void Run(string[] args) {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(
                    args,
                    ShutdownMode.OnMainWindowClose);
        }

        public static void InitLogging() {
            string logFilePath = PathManager.Inst.LogFilePath;
            string? logDirectory = Path.GetDirectoryName(logFilePath);

            if (!string.IsNullOrWhiteSpace(logDirectory)) {
                Directory.CreateDirectory(logDirectory);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Debug()
                .WriteTo.Logger(configuration => configuration
                    .MinimumLevel.Information()
                    .WriteTo.File(
                        logFilePath,
                        rollingInterval: RollingInterval.Day,
                        encoding: Encoding.UTF8))
                .WriteTo.Logger(configuration => configuration
                    .MinimumLevel.ControlledBy(
                        DebugViewModel.Sink.Inst.LevelSwitch)
                    .WriteTo.Sink(DebugViewModel.Sink.Inst))
                .CreateLogger();

            // Avoid duplicate registration if this method is invoked
            // again by a diagnostic or test harness.
            AppDomain.CurrentDomain.UnhandledException -=
                OnUnhandledException;

            AppDomain.CurrentDomain.UnhandledException +=
                OnUnhandledException;

            Log.Information("Logging initialized.");
        }

        private static string BuildSingleInstanceMutexName() {
            // Use a safe, fixed-length identifier instead of placing
            // arbitrary account-name characters into an OS object name.
            //
            // This is an application coordination mechanism,
            // not a security boundary.
            string identity =
                Environment.UserDomainName +
                "\\" +
                Environment.UserName;

            byte[] identityBytes = Encoding.UTF8.GetBytes(identity);
            byte[] hash = SHA256.HashData(identityBytes);

            return MutexPrefix + Convert.ToHexString(hash);
        }

        private static void LogStartupInformation() {
            Assembly? entryAssembly = Assembly.GetEntryAssembly();

            Log.Information(
                "Thai OpenUtau version: {Version}",
                entryAssembly?.GetName().Version?.ToString()
                    ?? "unknown");

            Log.Information(
                "OS: {OSDescription}; OS architecture: {OSArchitecture}; " +
                "process architecture: {ProcessArchitecture}",
                RuntimeInformation.OSDescription,
                RuntimeInformation.OSArchitecture,
                RuntimeInformation.ProcessArchitecture);

            Log.Information(
                "Runtime: {Framework}; runtime identifier: {RuntimeIdentifier}",
                RuntimeInformation.FrameworkDescription,
                RuntimeInformation.RuntimeIdentifier);

            Log.Information(
                "Data path: {DataPath}",
                PathManager.Inst.DataPath);

            Log.Information(
                "Cache path: {CachePath}",
                PathManager.Inst.CachePath);

            Log.Information(
                "Default encoding: {Encoding}",
                Encoding.GetEncoding(0).WebName);
        }

        private static FontManagerOptions CreateFontOptions() {
            var options = new FontManagerOptions();

            if (OS.IsWindows()) {
                // Use a single family name for DefaultFamilyName.
                // Additional candidates belong in FontFallbacks.
                options.DefaultFamilyName = "Leelawadee UI";

                options.FontFallbacks = new[] {
                    CreateFontFallback("Leelawadee UI"),
                    CreateFontFallback("Noto Sans Thai"),
                    CreateFontFallback("Segoe UI"),
                };
            } else if (OS.IsMacOS()) {
                options.DefaultFamilyName = "Hiragino Sans";

                options.FontFallbacks = new[] {
                    CreateFontFallback("Thonburi"),
                    CreateFontFallback("Noto Sans Thai"),
                    CreateFontFallback("Helvetica Neue"),
                };
            } else if (OS.IsLinux()) {
                // This remains a bounded, optional startup step.
                // If lookup fails, leave the default family unspecified
                // and let Avalonia perform its normal font selection.
                string? systemFont = TryGetLinuxDefaultFont();

                if (!string.IsNullOrWhiteSpace(systemFont)) {
                    options.DefaultFamilyName = systemFont;
                }

                options.FontFallbacks = new[] {
                    CreateFontFallback("Noto Sans Thai"),
                    CreateFontFallback("Noto Sans"),
                    CreateFontFallback("DejaVu Sans"),
                };
            }

            return options;
        }

        private static FontFallback CreateFontFallback(
            string familyName) {
            return new FontFallback {
                FontFamily = new FontFamily(familyName),
            };
        }

        private static string? TryGetLinuxDefaultFont() {
            try {
                // All awaits in the helper use ConfigureAwait(false).
                // This method is synchronous because Avalonia's builder
                // is synchronous and may also run under the designer.
                return GetLinuxDefaultFontAsync()
                    .GetAwaiter()
                    .GetResult();
            } catch (Exception ex) {
                Log.Warning(
                    ex,
                    "Linux font detection failed. " +
                    "Using Avalonia's normal font selection.");

                return null;
            }
        }

        private static async Task<string?> GetLinuxDefaultFontAsync() {
            using var timeout = new CancellationTokenSource(
                FontLookupTimeoutMilliseconds);

            using var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "fc-match",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
            };

            process.StartInfo.ArgumentList.Add("-f");
            process.StartInfo.ArgumentList.Add("%{family}");

            if (!process.Start()) {
                Log.Warning(
                    "Could not start fc-match. " +
                    "Using Avalonia's normal font selection.");

                return null;
            }

            // Drain both pipes concurrently before waiting for exit.
            Task<string> outputTask =
                process.StandardOutput.ReadToEndAsync(
                    timeout.Token);

            Task<string> errorTask =
                process.StandardError.ReadToEndAsync(
                    timeout.Token);

            try {
                Task exitTask =
                    process.WaitForExitAsync(timeout.Token);

                // Observe all three tasks together.
                await Task.WhenAll(
                    outputTask,
                    errorTask,
                    exitTask).ConfigureAwait(false);

                string output =
                    await outputTask.ConfigureAwait(false);

                string error =
                    await errorTask.ConfigureAwait(false);

                if (process.ExitCode != 0) {
                    Log.Warning(
                        "fc-match exited with code {ExitCode}. " +
                        "Standard error: {StandardError}",
                        process.ExitCode,
                        error.Trim());

                    return null;
                }

                string[] families = output.Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries);

                foreach (string family in families) {
                    string trimmedFamily = family.Trim();

                    if (!string.IsNullOrWhiteSpace(trimmedFamily)) {
                        Log.Debug(
                            "Detected Linux default font: {FontFamily}",
                            trimmedFamily);

                        return trimmedFamily;
                    }
                }

                Log.Warning(
                    "fc-match returned no usable font family.");

                return null;
            } catch (OperationCanceledException)
                when (timeout.IsCancellationRequested) {
                TryStopProcess(process);

                Log.Warning(
                    "fc-match exceeded the {TimeoutMilliseconds} ms " +
                    "lookup timeout. Using normal font selection.",
                    FontLookupTimeoutMilliseconds);

                return null;
            } catch {
                TryStopProcess(process);
                throw;
            }
        }

        private static void TryStopProcess(Process process) {
            try {
                if (process.HasExited) {
                    return;
                }

                process.Kill(entireProcessTree: true);

                // Best effort only: never wait indefinitely during startup.
                if (!process.WaitForExit(500)) {
                    Log.Warning(
                        "fc-match did not exit within the " +
                        "post-termination wait.");
                }
            } catch (InvalidOperationException) {
                // The process may have exited between inspection and kill.
            } catch (Exception ex) {
                Log.Warning(
                    ex,
                    "Could not terminate the font lookup process.");
            }
        }

        private static void OnUnhandledException(
            object? sender,
            UnhandledExceptionEventArgs args) {
            try {
                if (args.ExceptionObject is Exception exception) {
                    Log.Fatal(
                        exception,
                        "Unhandled exception. IsTerminating: {IsTerminating}",
                        args.IsTerminating);
                } else {
                    Log.Fatal(
                        "Unhandled non-Exception object. " +
                        "IsTerminating: {IsTerminating}; value: {Value}",
                        args.IsTerminating,
                        args.ExceptionObject);
                }
            } catch (Exception loggingException) {
                WriteStandardError(
                    "Could not log an unhandled exception.",
                    loggingException);
            } finally {
                if (args.IsTerminating) {
                    try {
                        Log.CloseAndFlush();
                    } catch (Exception flushException) {
                        WriteStandardError(
                            "Could not flush logs during termination.",
                            flushException);
                    }
                }
            }
        }

        private static void ReportFailure(
            bool loggingInitialized,
            Exception exception,
            string message) {
            // stderr remains useful if initialization failed or the
            // configured sinks cannot write. GUI launches may not show it.
            WriteStandardError(message, exception);

            if (!loggingInitialized) {
                return;
            }

            try {
                Log.Error(
                    exception,
                    "{FailureMessage}",
                    message);
            } catch (Exception loggingException) {
                WriteStandardError(
                    "An additional logging failure occurred.",
                    loggingException);
            }
        }

        private static void WriteStandardError(
            string message,
            Exception? exception = null) {
            try {
                Console.Error.WriteLine(message);

                if (exception != null) {
                    Console.Error.WriteLine(exception);
                }
            } catch {
                // Last-resort reporting must not cause another failure.
                // A GUI process may have no writable stderr stream.
            }
        }
    }
}