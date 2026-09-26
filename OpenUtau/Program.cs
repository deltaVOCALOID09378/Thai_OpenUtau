// ============================================================================
// Made And Checked By DELTA SYNTH & Gemini AI
// Original by Patiphat Wongyai (Delta)
// Version: 1.2.1 | Date: 2026-08-09
// Description: ปิดโปรแกรมอย่างเป็นระเบียบและส่งคืนรหัสข้อผิดพลาดที่ถูกต้อง
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.App
{
    public class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            InitLogging();

            string processName = Process.GetCurrentProcess().ProcessName;
            if (processName != "dotnet")
            {
                var exists = Process.GetProcessesByName(processName).Count() > 1;
                if (exists)
                {
                    Log.Information($"Process {processName} already open. Exiting.");
                    Log.CloseAndFlush();
                    return 0;
                }
            }

            Log.Information($"{Environment.OSVersion}");
            Log.Information($"{RuntimeInformation.OSDescription} " +
                $"{RuntimeInformation.OSArchitecture} " +
                $"{RuntimeInformation.ProcessArchitecture}");

            Log.Information($"Thai OpenUtau v{Assembly.GetEntryAssembly()?.GetName().Version} by DELTA SYNTH " +
                $"{RuntimeInformation.RuntimeIdentifier}");

            Log.Information($"Data path = {PathManager.Inst.DataPath}");
            Log.Information($"Cache path = {PathManager.Inst.CachePath}");
            Log.Information($"System encoding = {Encoding.GetEncoding(0)?.WebName ?? "null"}");

            int exitCode = 0;
            try
            {
                Run(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "A fatal error occurred during execution.");
                exitCode = 1;
            }
            finally
            {
                if (!OS.IsMacOS())
                {
                    NetMQ.NetMQConfig.Cleanup(/*block=*/false);
                }

                Log.Information($"Exited.");
                Log.CloseAndFlush();
            }

            return exitCode;
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            FontManagerOptions fontOptions = new();
            if (OS.IsLinux())
            {
                using Process process = Process.Start(new ProcessStartInfo("fc-match")
                {
                    ArgumentList = { "-f", "%{family}" },
                    RedirectStandardOutput = true
                })!;
                process.WaitForExit();

                string fontFamily = process.StandardOutput.ReadToEnd();
                if (!string.IsNullOrEmpty(fontFamily))
                {
                    string[] fontFamilies = fontFamily.Split(',');
                    fontOptions.DefaultFamilyName = fontFamilies[0];
                }
            }
            else if (OS.IsMacOS())
            {
                fontOptions.DefaultFamilyName = "Hiragino Sans, Segoe UI, San Francisco, Helvetica Neue";
            }

            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI()
                .With(fontOptions)
                .With(new X11PlatformOptions { EnableIme = true });
        }

        public static void Run(string[] args)
            => BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(
                    args, ShutdownMode.OnMainWindowClose);

        public static void InitLogging()
        {
            string logFilePath = PathManager.Inst.LogFilePath;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            }
            catch (Exception exception)
            {
                string fallbackLogsPath = Path.Combine(Path.GetTempPath(), "OpenUtau", "Logs");
                Directory.CreateDirectory(fallbackLogsPath);
                logFilePath = Path.Combine(fallbackLogsPath, "log.txt");
                Console.Error.WriteLine(
                    $"Unable to create the preferred log directory. Using {logFilePath}. {exception.Message}");
            }

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Debug()
                .WriteTo.Logger(lc => lc
                    .MinimumLevel.Information()
                    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, encoding: Encoding.UTF8))
                .WriteTo.Logger(lc => lc
                    .MinimumLevel.ControlledBy(DebugViewModel.Sink.Inst.LevelSwitch)
                    .WriteTo.Sink(DebugViewModel.Sink.Inst))
                .WriteTo.Logger(lc => lc
                    .MinimumLevel.Warning()
                    .WriteTo.Sink(ToastLogSink.Inst));

            if (string.Equals(
                Environment.GetEnvironmentVariable("OPENUTAU_CONSOLE_LOG"),
                "1",
                StringComparison.Ordinal))
            {
                loggerConfiguration.WriteTo.Console();
            }

            Log.Logger = loggerConfiguration.CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((sender, args) =>
            {
                if (args.ExceptionObject is Exception exception)
                {
                    Log.Error(exception, "Unhandled exception");
                }
                else
                {
                    Log.Error("Unhandled non-exception object: {ExceptionObject}", args.ExceptionObject);
                }
            });

            Log.Information("Logging initialized.");
        }
    }
}