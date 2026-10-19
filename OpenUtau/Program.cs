// Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
// Original by OpenUtau Authors
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.App {
    public class Program {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) {
            Environment.SetEnvironmentVariable("DOTNET_ROLL_FORWARD", "LatestMajor");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitLogging();
            string processName = Process.GetCurrentProcess().ProcessName;
            Mutex? singleInstanceMutex = null;
            if (processName != "dotnet") {
                try {
                    singleInstanceMutex = new Mutex(true, $"ThaiOpenUtau_SingleInstance_{Environment.UserName}", out bool createdNew);
                    if (!createdNew) {
                        Log.Information($"Another instance of {processName} is already running. Exiting.");
                        return;
                    }
                } catch (Exception ex) {
                    Log.Warning(ex, "Single instance check fallback.");
                }
            }
            Log.Information($"{Environment.OSVersion}");
            Log.Information($"{RuntimeInformation.OSDescription} " +
                $"{RuntimeInformation.OSArchitecture} " +
                $"{RuntimeInformation.ProcessArchitecture}");
            Log.Information($"OpenUtau v{Assembly.GetEntryAssembly()?.GetName().Version} " +
                $"{RuntimeInformation.RuntimeIdentifier}");
            Log.Information($"Data path = {PathManager.Inst.DataPath}");
            Log.Information($"Cache path = {PathManager.Inst.CachePath}");
            Log.Information($"System encoding = {Encoding.GetEncoding(0)?.WebName ?? "null"}");
            try {
                Run(args);
                Log.Information($"Exiting.");
            } finally {
                if (singleInstanceMutex != null) {
                    try {
                        singleInstanceMutex.ReleaseMutex();
                        singleInstanceMutex.Dispose();
                    } catch { }
                }
                if (!OS.IsMacOS()) {
                    NetMQ.NetMQConfig.Cleanup(/*block=*/false);
                    // Cleanup() hangs on macOS https://github.com/zeromq/netmq/issues/1018
                }
            }
            Log.Information($"Exited.");
            Environment.Exit(0);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp() {
            FontManagerOptions fontOptions = new();
            if (OS.IsLinux()) {
                using Process process = Process.Start(new ProcessStartInfo("fc-match")
                {
                    ArgumentList = { "-f", "%{family}" },
                    RedirectStandardOutput = true
                })!;
                process.WaitForExit();

                string fontFamily = process.StandardOutput.ReadToEnd();
                if (!string.IsNullOrEmpty(fontFamily)) {
                    string [] fontFamilies = fontFamily.Split(',');
                    fontOptions.DefaultFamilyName = fontFamilies[0];
                }
            } else if (OS.IsMacOS()) {
                //To avoid text display corruption, specify Hiragino Sans font first.
                //Due to the specification of AvaloniaUI, this only affects when the language is set to Japanese.
                fontOptions.DefaultFamilyName = "Hiragino Sans, Segoe UI, San Francisco, Helvetica Neue";
            } else if (OS.IsWindows()) {
                // DELTA SYNTH: Set Leelawadee UI as the leading default font on Windows
                fontOptions.DefaultFamilyName = "Leelawadee UI, Segoe UI, sans-serif";
                fontOptions.FontFallbacks = [
                    new FontFallback { FontFamily = new FontFamily("Leelawadee UI") },
                    new FontFallback { FontFamily = new FontFamily("Noto Sans Thai") },
                    new FontFallback { FontFamily = new FontFamily("Segoe UI") },
                ];
            }
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI()
                .With(fontOptions)
                .With(new X11PlatformOptions {EnableIme = true});
        }

        public static void Run(string[] args)
            => BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(
                    args, ShutdownMode.OnMainWindowClose);

        public static void InitLogging() {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Debug()
                .WriteTo.Logger(lc => lc
                    .MinimumLevel.Information()
                    .WriteTo.File(PathManager.Inst.LogFilePath, rollingInterval: RollingInterval.Day, encoding: Encoding.UTF8))
                .WriteTo.Logger(lc => lc
                    .MinimumLevel.ControlledBy(DebugViewModel.Sink.Inst.LevelSwitch)
                    .WriteTo.Sink(DebugViewModel.Sink.Inst))
                .CreateLogger();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((sender, args) => {
                Log.Error((Exception)args.ExceptionObject, "Unhandled exception");
            });
            Log.Information("Logging initialized.");
        }
    }
}
