using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;

// ============================================================================
// Made And Checked By DELTA SYNTH & Gemini AI
// Original by Patiphat Wongyai (Delta)
// Version: 1.2 | Date: 2026-07-28
// Description: แก้การแสดงหน้าต่างโหลดให้ใช้เวลาหน่วงจริง ยกเลิกได้ และไม่ทิ้งงานค้าง
// ============================================================================

namespace OpenUtau.App.Views
{
    public partial class LoadingWindow : Window
    {
        private static LoadingWindow? loadingDialog;
        private static bool isCurrentlyLoading = false;
        private static CancellationTokenSource? globalLoadingCancellationTokenSource;

        // Used to schedule tasks on the UI thread
        public static TaskFactory uiTaskFactory = new TaskFactory(CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.FromCurrentSynchronizationContext()
        );

        public LoadingWindow()
        {
            InitializeComponent();
        }

        public static void InitializeLoadingWindow()
        {
            loadingDialog = new LoadingWindow()
            {
                Title = "Loading"
            };
            loadingDialog.Text.Text = "Loading...";
        }

        private static void ShowLoadingWindow(Window parent)
        {
            if (!isCurrentlyLoading)
            {
                return;
            }

            if (loadingDialog == null)
            {
                InitializeLoadingWindow();
            }

            loadingDialog?.ShowDialog(parent);
        }

        private static void CloseLoadingWindow()
        {
            if (loadingDialog != null)
            {
                loadingDialog.Close();

                //Recreate loading dialog to make sure it is initialized before being shown
                loadingDialog = null;
                InitializeLoadingWindow();
            }
        }

        public static bool IsLoading()
        {
            return isCurrentlyLoading;
        }

        /// <summary>
        /// Returns a task that shows a loading popup for a task after a time delay (Default 250ms)
        /// </summary>
        public static async Task LoadForAsyncTask(Task loadingTask, Window parentWindow, int timeBeforeLoadingPopup = 250)
        {
            bool ownsLoadingState = !isCurrentlyLoading;
            using var cts = new CancellationTokenSource();
            Task loadingPopupTask = Task.CompletedTask;
            if (ownsLoadingState)
            {
                isCurrentlyLoading = true;
                loadingPopupTask = ShowLoadingAfterTime(
                    cts.Token,
                    parentWindow,
                    timeBeforeLoadingPopup);
            }

            try
            {
                await loadingTask;
            }
            finally
            {
                if (ownsLoadingState)
                {
                    cts.Cancel();
                    await loadingPopupTask;
                    CloseLoadingWindow();
                    isCurrentlyLoading = false;
                }
            }
        }

        /// <summary>
        /// Returns a task that shows a loading popup for a task after a time delay (Default 250ms)
        /// </summary>
        public static async Task<T> LoadForAsyncTask<T>(Task<T> loadingTask, Window parentWindow, int timeBeforeLoadingPopup = 250)
        {
            bool ownsLoadingState = !isCurrentlyLoading;
            using var cts = new CancellationTokenSource();
            Task loadingPopupTask = Task.CompletedTask;
            if (ownsLoadingState)
            {
                isCurrentlyLoading = true;
                loadingPopupTask = ShowLoadingAfterTime(
                    cts.Token,
                    parentWindow,
                    timeBeforeLoadingPopup);
            }

            try
            {
                return await loadingTask;
            }
            finally
            {
                if (ownsLoadingState)
                {
                    cts.Cancel();
                    await loadingPopupTask;
                    CloseLoadingWindow();
                    isCurrentlyLoading = false;
                }
            }
        }

        private static async Task ShowLoadingAfterTime(CancellationToken ct, Window parentWindow, int milisDelay)
        {
            // Must have at least 1ms of delay opening loading window to avoid strange race conditions with UI thread (?)
            try
            {
                await Task.Delay(Math.Max(milisDelay, 1), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }

            if (ct.IsCancellationRequested)
            {
                return;
            }

            await uiTaskFactory.StartNew(() => ShowLoadingWindow(parentWindow));
        }

        public static Task RunAsyncOnUIThread(Action func)
        {
            return uiTaskFactory.StartNew(func);
        }

        public static void BeginLoadingImmediate(Window parentWindow)
        {
            BeginLoading(parentWindow, 0);
        }

        public static void BeginLoading(Window parentWindow, int milisDelay = 250)
        {
            if (isCurrentlyLoading)
            {
                return;
            }

            isCurrentlyLoading = true;
            if (milisDelay == 0)
            {
                ShowLoadingWindow(parentWindow);
                return;
            }

            globalLoadingCancellationTokenSource = new CancellationTokenSource();
            _ = ShowLoadingAfterTime(
                globalLoadingCancellationTokenSource.Token,
                parentWindow,
                milisDelay);
        }

        public static void EndLoading()
        {
            if (!isCurrentlyLoading)
            {
                return;
            }
            isCurrentlyLoading = false;

            globalLoadingCancellationTokenSource?.Cancel();
            globalLoadingCancellationTokenSource?.Dispose();
            globalLoadingCancellationTokenSource = null;
            CloseLoadingWindow();
        }
    }
}
