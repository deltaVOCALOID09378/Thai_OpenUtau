// ============================================================================
// Date         : 10/11/2026 (B.E. 2569 / A.D. 2026)
// File Name    : UpdaterDialog.axaml.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau/Views/UpdaterDialog.axaml.cs
// Description  : Update notification dialog that verifies the installed system version before prompting any download.
// ============================================================================
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using NetSparkleUpdater.Enums;
using OpenUtau.App.ViewModels;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.App.Views {
    public partial class UpdaterDialog : Window {
        public readonly UpdaterViewModel ViewModel;
        public UpdaterDialog() {
            InitializeComponent();
            DataContext = ViewModel = new UpdaterViewModel();
        }

        void OnClosing(object sender, WindowClosingEventArgs e) {
            ViewModel.OnClosing();
        }

        public static void CheckForUpdate(Action<Window> showDialog, Action closeApplication, TaskScheduler scheduler) {
            Task.Run(async () => {
                using var updater = await UpdaterViewModel.NewUpdaterAsync();
                if (updater == null) {
                    return false;
                }
                var info = await updater.CheckForUpdatesQuietly(true);
                if (info != null && info.Status == UpdateStatus.UpdateAvailable && info.Updates != null && info.Updates.Count > 0) {
                    var remoteVerStr = info.Updates[0].Version?.ToString() ?? string.Empty;
                    if (remoteVerStr == Preferences.Default.SkipUpdate) {
                        return false;
                    }
                    var installedVer = Assembly.GetEntryAssembly()?.GetName().Version;
                    if (installedVer != null &&
                        DowngradableFilter.TryParseNormalizedVersion(remoteVerStr, out var remoteVer) &&
                        DowngradableFilter.CompareVersions(installedVer, remoteVer) >= 0) {
                        return false;
                    }
                    return true;
                }
                return false;
            }).ContinueWith(t => {
                if (t.IsCompletedSuccessfully && t.Result) {
                    var dialog = new UpdaterDialog();
                    dialog.ViewModel.CloseApplication = closeApplication;
                    showDialog.Invoke(dialog);
                }
                if (t.IsFaulted) {
                    Log.Error(t.Exception, "Failed to check for update");
                }
            }, scheduler).ContinueWith((t2, _) => {
                if (t2.IsFaulted) {
                    Log.Error(t2.Exception, "Failed to check for update");
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}

