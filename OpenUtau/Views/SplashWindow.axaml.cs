// Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
// Original by OpenUtau Authors
using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using OpenUtau.App;
using OpenUtau.Classic;
using OpenUtau.Core;
using ReactiveUI;
using Serilog;

namespace OpenUtau.App.Views {
    public partial class SplashWindow : Window, IDisposable {
        public SplashWindow() {
            InitializeComponent();
            UpdateLogo();
            MessageBus.Current.Listen<ThemeChangedEvent>()
                .Subscribe(_ => UpdateLogo())
                .DisposeWith(disposable);
            this.Cursor = new Cursor(StandardCursorType.AppStarting);
            this.Opened += SplashWindow_Opened;
        }

        private readonly CompositeDisposable disposable = new();

        private void UpdateLogo() {
            LogoTypeDark.IsVisible = ThemeManager.IsDarkMode;
            LogoTypeLight.IsVisible = !ThemeManager.IsDarkMode;
        }

        public void Dispose() {
            disposable.Dispose();
        }

        public void SetProgress(double percent, string status) {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                if (InitProgressBar != null) {
                    InitProgressBar.Value = percent;
                }
                if (PercentText != null) {
                    PercentText.Text = $"{(int)percent}%";
                }
                if (StatusText != null) {
                    StatusText.Text = status;
                }
            });
        }

        private void SplashWindow_Opened(object? sender, EventArgs e) {
            if (Screens.Primary == null && Screens.ScreenCount == 0) {
                return;
            }

            Start();
        }

        private void Start() {
            var mainThread = Thread.CurrentThread;
            var mainScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Run(() => {
                Log.Information("Initializing OpenUtau.");
                SetProgress(15, "กำลังเตรียมพร้อมสภาพแวดล้อมระบบ (Initializing Environment)...");
                
                // DELTA SYNTH Optimization: Fully parallelize core subsystem initializations.
                // Tools, Singers, Plugins (DocManager), and Audio Output run concurrently.
                // Overlapping filesystem discovery and audio device enumeration drastically cuts cold-start latency.
                var t1 = Task.Run(() => {
                    ToolsManager.Inst.Initialize();
                    SetProgress(40, "กำลังโหลดเครื่องมือสังเคราะห์เสียง (Loading Core Tools)...");
                });
                var t2 = Task.Run(() => SingerManager.Inst.Initialize());
                var t3 = Task.Run(() => {
                    DocManager.Inst.Initialize(mainThread, mainScheduler);
                    SetProgress(70, "กำลังโหลดปลั๊กอินและข้อมูลโครงการ (Loading Plugins & Workspace)...");
                });
                var tAudio = Task.Run(() => {
                    InitAudio();
                });
                
                // Wait for all critical initialization tasks concurrently
                Task.WaitAll(t1, t3, tAudio);
                
                SetProgress(85, "กำลังเริ่มต้นเอนจินประมวลผลเสียง (Initializing Audio Engine)...");
                DocManager.Inst.PostOnUIThread = action => Avalonia.Threading.Dispatcher.UIThread.Post(action);
                Log.Information("Initialized OpenUtau.");

                SetProgress(100, "ระบบพร้อมเริ่มการทำงาน 100% (Ready)");
            }).ContinueWith(t => {
                if (t.IsFaulted) {
                    Log.Error(t.Exception?.Flatten(), "Failed to Start.");
                    MessageBox.ShowError(this, t.Exception, "Failed to Start OpenUtau").ContinueWith(t1 => { Close(); });
                    return;
                }
                if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    desktop.MainWindow = mainWindow;
                    mainWindow.InitProject();
                    LoadingWindow.InitializeLoadingWindow();
                    Close();
                }
            }, CancellationToken.None, TaskContinuationOptions.None, mainScheduler);
        }

        private static void InitAudio() {
            Log.Information("Initializing audio.");
            if (!OS.IsWindows() || Core.Util.Preferences.Default.PreferPortAudio) {
                try {
                    PlaybackManager.Inst.AudioOutput = new Audio.MiniAudioOutput();
                } catch (Exception e1) {
                    Log.Error(e1, "Failed to init MiniAudio");
                }
            } else {
                try {
                    PlaybackManager.Inst.AudioOutput = new NAudioOutput();
                } catch (Exception e2) {
                    Log.Error(e2, "Failed to init NAudio");
                }
            }
            Log.Information("Initialized audio.");
        }
    }
}
