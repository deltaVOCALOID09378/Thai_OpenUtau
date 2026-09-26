using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using OpenUtau.Classic;
using OpenUtau.Core;
using Serilog;

// ============================================================================
// Made And Checked By DELTA SYNTH & Gemini AI
// Original by Patiphat Wongyai (Delta)
// Version: 1.2 | Date: 2026-07-28
// Description: เริ่มส่วนประกอบแบบขนานโดยไม่บล็อก UI และจัดการข้อผิดพลาดอย่างเป็นลำดับ
// ============================================================================

namespace OpenUtau.App.Views
{
    public partial class SplashWindow : Window, IDisposable
    {
        public SplashWindow()
        {
            InitializeComponent();
            this.Cursor = new Cursor(StandardCursorType.AppStarting);
            this.Opened += SplashWindow_Opened;
            this.Closed += SplashWindow_Closed;
        }

        private readonly CompositeDisposable disposable = new();
        private bool started;
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            Opened -= SplashWindow_Opened;
            Closed -= SplashWindow_Closed;
            disposable.Dispose();
        }

        private async void SplashWindow_Opened(object? sender, EventArgs e)
        {
            if (Screens.Primary == null && Screens.ScreenCount == 0)
            {
                return;
            }
            if (started)
            {
                return;
            }
            started = true;
            Opened -= SplashWindow_Opened;
            await StartAsync();
        }

        private void SplashWindow_Closed(object? sender, EventArgs e)
        {
            Dispose();
        }

        private async Task StartAsync()
        {
            var mainThread = Thread.CurrentThread;
            var mainScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            try
            {
                Log.Information("Initializing OpenUtau.");
                var initTheme = App.InitializeThemeAsync();
                var initTools = Task.Run(() => ToolsManager.Inst.Initialize());
                var initSingers = Task.Run(() => SingerManager.Inst.Initialize());
                var initDocs = Task.Run(() => DocManager.Inst.Initialize(mainThread, mainScheduler));
                await Task.WhenAll(initTheme, initTools, initSingers, initDocs);
                DocManager.Inst.PostOnUIThread = action => Avalonia.Threading.Dispatcher.UIThread.Post(action);
                Log.Information("Initialized OpenUtau.");
                await Task.Run(InitAudio);

                if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    desktop.MainWindow = mainWindow;
                    mainWindow.InitProject();
                    LoadingWindow.InitializeLoadingWindow();
                    Close();
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to start OpenUtau.");
                await MessageBox.ShowError(this, exception, "Failed to Start OpenUtau");
                Close();
            }
        }

        private static void InitAudio()
        {
            Log.Information("Initializing audio.");
            if (!OS.IsWindows() || Core.Util.Preferences.Default.PreferPortAudio)
            {
                try
                {
                    PlaybackManager.Inst.AudioOutput = new Audio.MiniAudioOutput();
                }
                catch (Exception e1)
                {
                    Log.Error(e1, "Failed to init MiniAudio");
                }
            }
            else
            {
                try
                {
                    PlaybackManager.Inst.AudioOutput = new NAudioOutput();
                }
                catch (Exception e2)
                {
                    Log.Error(e2, "Failed to init NAudio");
                }
            }
            Log.Information("Initialized audio.");
        }
    }
}
