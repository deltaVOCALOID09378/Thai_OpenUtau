using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    /// <summary>
    /// Draws the planned pitch-follow camera-center trajectory over the piano roll.
    /// </summary>
    class PitchFollowPathCanvas : Control {
        public static readonly StyledProperty<bool> ShowPathProperty =
            AvaloniaProperty.Register<PitchFollowPathCanvas, bool>(nameof(ShowPath));
        public static readonly DirectProperty<PitchFollowPathCanvas, double> TickWidthProperty =
            AvaloniaProperty.RegisterDirect<PitchFollowPathCanvas, double>(
                nameof(TickWidth),
                o => o.TickWidth,
                (o, v) => o.TickWidth = v);
        public static readonly DirectProperty<PitchFollowPathCanvas, double> TickOffsetProperty =
            AvaloniaProperty.RegisterDirect<PitchFollowPathCanvas, double>(
                nameof(TickOffset),
                o => o.TickOffset,
                (o, v) => o.TickOffset = v);
        public static readonly DirectProperty<PitchFollowPathCanvas, double> TrackHeightProperty =
            AvaloniaProperty.RegisterDirect<PitchFollowPathCanvas, double>(
                nameof(TrackHeight),
                o => o.TrackHeight,
                (o, v) => o.TrackHeight = v);
        public static readonly DirectProperty<PitchFollowPathCanvas, double> TrackOffsetProperty =
            AvaloniaProperty.RegisterDirect<PitchFollowPathCanvas, double>(
                nameof(TrackOffset),
                o => o.TrackOffset,
                (o, v) => o.TrackOffset = v);
        public static readonly DirectProperty<PitchFollowPathCanvas, double> ViewportTracksProperty =
            AvaloniaProperty.RegisterDirect<PitchFollowPathCanvas, double>(
                nameof(ViewportTracks),
                o => o.ViewportTracks,
                (o, v) => o.ViewportTracks = v);
        public static readonly DirectProperty<PitchFollowPathCanvas, NotesViewModel?> NotesViewModelProperty =
            AvaloniaProperty.RegisterDirect<PitchFollowPathCanvas, NotesViewModel?>(
                nameof(NotesViewModel),
                o => o.NotesViewModel,
                (o, v) => o.NotesViewModel = v);

        public bool ShowPath {
            get => GetValue(ShowPathProperty);
            set => SetValue(ShowPathProperty, value);
        }
        public double TickWidth {
            get => tickWidth;
            private set => SetAndRaise(TickWidthProperty, ref tickWidth, value);
        }
        public double TickOffset {
            get => tickOffset;
            private set => SetAndRaise(TickOffsetProperty, ref tickOffset, value);
        }
        public double TrackHeight {
            get => trackHeight;
            private set => SetAndRaise(TrackHeightProperty, ref trackHeight, value);
        }
        public double TrackOffset {
            get => trackOffset;
            private set => SetAndRaise(TrackOffsetProperty, ref trackOffset, value);
        }
        public double ViewportTracks {
            get => viewportTracks;
            private set => SetAndRaise(ViewportTracksProperty, ref viewportTracks, value);
        }
        public NotesViewModel? NotesViewModel {
            get => notesViewModel;
            set => SetAndRaise(NotesViewModelProperty, ref notesViewModel, value);
        }

        double tickWidth;
        double tickOffset;
        double trackHeight;
        double trackOffset;
        double viewportTracks;
        NotesViewModel? notesViewModel;
        IDisposable? previewSubscription;
        IDisposable? playPosSubscription;

        static PitchFollowPathCanvas() {
            AffectsRender<PitchFollowPathCanvas>(
                ShowPathProperty,
                TickWidthProperty,
                TickOffsetProperty,
                TrackHeightProperty,
                TrackOffsetProperty,
                ViewportTracksProperty,
                NotesViewModelProperty);
        }

        public PitchFollowPathCanvas() {
            ClipToBounds = true;
            IsHitTestVisible = false;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
            base.OnAttachedToVisualTree(e);
            previewSubscription = MessageBus.Current.Listen<PitchFollowPathPreviewChangedEvent>()
                .Subscribe(_ => InvalidateVisual());
            playPosSubscription = MessageBus.Current.Listen<SetPlayPosTickNotification>()
                .Subscribe(_ => {
                    if (ShowPath) {
                        InvalidateVisual();
                    }
                });
            MessageBus.Current.Listen<PlaybackPitchFollowSettingsChangedEvent>()
                .Subscribe(_ => {
                    if (ShowPath) {
                        InvalidateVisual();
                    }
                });
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
            base.OnDetachedFromVisualTree(e);
            previewSubscription?.Dispose();
            previewSubscription = null;
            playPosSubscription?.Dispose();
            playPosSubscription = null;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            if (change.Property == ShowPathProperty && ShowPath) {
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context) {
            if (!ShowPath || notesViewModel == null || !notesViewModel.PitchFollowPathIsBuilt) {
                return;
            }

            var samples = notesViewModel.PitchFollowPathSamples;
            if (samples.Count < 2 || TickWidth <= 0 || TrackHeight <= 0) {
                return;
            }

            double verticalPosition = Preferences.Default.PlaybackPitchFollowVerticalPosition;
            var pathPen = ThemeManager.AccentPen2Thickness2;
            var markerPen = ThemeManager.AccentPen2;
            var markerFill = ThemeManager.AccentBrush2Semi;

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open()) {
                bool started = false;
                foreach (var sample in samples) {
                    var point = ToCanvasPoint(sample.Tick, sample.TrackOffset, verticalPosition);
                    if (!started) {
                        ctx.BeginFigure(point, false);
                        started = true;
                    } else {
                        ctx.LineTo(point);
                    }
                }
            }
            context.DrawGeometry(null, pathPen, geometry);

            if (notesViewModel.Part != null) {
                int localTick = DocManager.Inst.playPosTick - notesViewModel.Part.position;
                bool playing = PlaybackManager.Inst.PlayingMaster || PlaybackManager.Inst.StartingToPlay;
                double pathOffset = notesViewModel.GetPitchFollowCameraOffset(localTick, playing);
                var playPoint = ToCanvasPoint(localTick, pathOffset, verticalPosition);
                if (playPoint.X >= -8 && playPoint.X <= Bounds.Width + 8) {
                    context.DrawEllipse(markerFill, markerPen, playPoint, 5, 5);
                    context.DrawLine(markerPen, new Point(playPoint.X, 0), new Point(playPoint.X, Bounds.Height));
                }
            }
        }

        Point ToCanvasPoint(double localTick, double pathTrackOffset, double verticalPosition) {
            double x = (localTick - TickOffset) * TickWidth;
            double y = (pathTrackOffset + ViewportTracks * verticalPosition - TrackOffset) * TrackHeight;
            return new Point(x, y);
        }
    }
}
