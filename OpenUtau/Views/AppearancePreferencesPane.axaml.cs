using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OpenUtau.App;
using OpenUtau.App.Controls;
using OpenUtau.App.ViewModels;
using OpenUtau.Core.Util;
using ReactiveUI;

namespace OpenUtau.App.Views {
    public partial class AppearancePreferencesPane : UserControl {
        PreferencesViewModel ViewModel => (PreferencesViewModel)DataContext!;

        IDisposable? scrollStyleSubscription;
        int scrollStyleApplyGeneration;

        public AppearancePreferencesPane() {
            // ตั้ง DataContext ก่อนโหลด XAML เพื่อให้ binding ชี้ถูกตัวตั้งแต่ครั้งแรก
            // และไม่ไปรับ DataContext ของ parent (เช่น PianoRollViewModel)
            DataContext ??= new PreferencesViewModel();
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
            base.OnAttachedToVisualTree(e);

            ClosePanelButton.IsVisible = FindPianoRoll() != null;

            // subscribe ใหม่ทุกครั้งที่ attach และยกเลิกตอน detach
            scrollStyleSubscription?.Dispose();
            scrollStyleSubscription = MessageBus.Current
                .Listen<ScrollbarsStyleChangedEvent>()
                .Subscribe(_ => ScheduleApplyScrollStyle());

            ScheduleApplyScrollStyle();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
            // ยกเลิกงานที่ Post ค้างอยู่ และเลิกฟัง event
            scrollStyleApplyGeneration++;
            scrollStyleSubscription?.Dispose();
            scrollStyleSubscription = null;
            base.OnDetachedFromVisualTree(e);
        }

        void ScheduleApplyScrollStyle() {
            // event อาจมาจาก thread อื่น ห้ามแตะ visual tree นอก UI thread
            if (!Dispatcher.UIThread.CheckAccess()) {
                Dispatcher.UIThread.Post(ScheduleApplyScrollStyle);
                return;
            }
            if (!WorkspaceScrollbarHelper.IsInVisualTree(this)) {
                return;
            }
            int generation = ++scrollStyleApplyGeneration;
            Dispatcher.UIThread.Post(() => {
                if (generation != scrollStyleApplyGeneration
                    || !WorkspaceScrollbarHelper.IsInVisualTree(this)) {
                    return;
                }
                ApplyScrollStyle();
            }, DispatcherPriority.Loaded);
        }

        void ApplyScrollStyle() {
            WorkspaceScrollbarHelper.ApplyScrollViewer(
                ContentScroll, WorkspaceScrollbarHelper.UseClassicScrollbars);
        }

        PianoRoll? FindPianoRoll() => this.FindAncestorOfType<PianoRoll>();

        void OnCloseDockedPanel(object? sender, RoutedEventArgs e) {
            var pianoRoll = FindPianoRoll();
            if (pianoRoll?.ViewModel != null) {
                pianoRoll.ViewModel.ShowAppearancePanel = false;
            }
        }
    }
}