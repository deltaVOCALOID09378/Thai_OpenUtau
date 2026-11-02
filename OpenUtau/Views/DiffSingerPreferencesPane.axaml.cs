/*
 * วัน/เดือน/ปี: 30/10/2569 (2026)
 * ชื่อไฟล์โค้ด: DiffSingerPreferencesPane.axaml.cs
 * เวอร์ชั่น: 1.2
 * เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Contributors ]
 * ตำแหน่งไฟล์: OpenUtau.App/Views/DiffSingerPreferencesPane.axaml.cs
 * รายละเอียดการแก้ไข: ปรับปรุงประสิทธิภาพความเสถียร (Zero-Defect) และประทับตรามาตรฐานองค์กร
 */

using System;
using System.Linq;
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
    public partial class DiffSingerPreferencesPane : UserControl {
        int scrollStyleApplyGeneration;

        public DiffSingerPreferencesPane() {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => {
                ClosePanelButton.IsVisible = IsHostedInPianoRollDock();
                ScheduleApplyScrollStyle();
            };
            DetachedFromVisualTree += (_, _) => {
                scrollStyleApplyGeneration++;
            };
            MessageBus.Current.Listen<ScrollbarsStyleChangedEvent>()
                .Subscribe(_ => ScheduleApplyScrollStyle());
        }

        void ScheduleApplyScrollStyle() {
            if (!WorkspaceScrollbarHelper.IsInVisualTree(this)) {
                return;
            }
            int generation = ++scrollStyleApplyGeneration;
            Dispatcher.UIThread.Post(() => {
                if (generation != scrollStyleApplyGeneration || !WorkspaceScrollbarHelper.IsInVisualTree(this)) {
                    return;
                }
                ApplyScrollStyle();
            }, DispatcherPriority.Loaded);
        }

        void ApplyScrollStyle() {
            if (!WorkspaceScrollbarHelper.IsInVisualTree(this)) {
                return;
            }
            WorkspaceScrollbarHelper.ApplyScrollViewer(ContentScroll, WorkspaceScrollbarHelper.UseClassicScrollbars);
        }

        bool IsHostedInPianoRollDock() {
            return this.GetVisualAncestors().OfType<PianoRoll>().Any();
        }

        void OnCloseDockedPanel(object? sender, RoutedEventArgs e) {
            var pianoRoll = this.GetVisualAncestors().OfType<PianoRoll>().FirstOrDefault();
            if (pianoRoll?.ViewModel != null) {
                pianoRoll.ViewModel.ShowDiffSingerPanel = false;
            }
        }
    }
}