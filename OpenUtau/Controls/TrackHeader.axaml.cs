using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;

namespace OpenUtau.App.Controls {
    public partial class TrackHeader : UserControl, IDisposable {
        public static readonly DirectProperty<TrackHeader, double> TrackHeightProperty =
            AvaloniaProperty.RegisterDirect<TrackHeader, double>(
                nameof(TrackHeight),
                o => o.TrackHeight,
                (o, v) => o.TrackHeight = v);
        public static readonly DirectProperty<TrackHeader, Point> OffsetProperty =
            AvaloniaProperty.RegisterDirect<TrackHeader, Point>(
                nameof(Offset),
                o => o.Offset,
                (o, v) => o.Offset = v);
        public static readonly DirectProperty<TrackHeader, int> TrackNoProperty =
            AvaloniaProperty.RegisterDirect<TrackHeader, int>(
                nameof(TrackNo),
                o => o.TrackNo,
                (o, v) => o.TrackNo = v);

        public double TrackHeight {
            get => trackHeight;
            set => SetAndRaise(TrackHeightProperty, ref trackHeight, value);
        }
        public Point Offset {
            get => offset;
            set => SetAndRaise(OffsetProperty, ref offset, value);
        }
        public int TrackNo {
            get => trackNo;
            set => SetAndRaise(TrackNoProperty, ref trackNo, value);
        }

        private double trackHeight;
        private Point offset;
        private int trackNo;
        private readonly KeyModifiers cmdKey =
            OS.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

        public TrackHeaderViewModel? ViewModel;

        private List<IDisposable> unbinds = new List<IDisposable>();

        private UTrack? track;
        private TrackHeaderCanvas? canvas;

        public TrackHeader() {
            InitializeComponent();
            Width = ViewConstants.TrackHeaderBaseWidth;
            SingersMenu.ContainerPrepared += OnSingersMenuContainerPrepared;
            PhonemizersMenu.ContainerPrepared += OnPhonemizersMenuContainerPrepared;
            SyncAvatarChromeSize();
        }

        void OnSingersMenuContainerPrepared(object? sender, ContainerPreparedEventArgs e) {
            if (e.Container is not MenuItem menuItem) {
                return;
            }
            switch (menuItem.DataContext) {
                case SingerMenuItemViewModel:
                    menuItem.Classes.Set("singerMenuItem", true);
                    break;
                case MenuSeparatorViewModel:
                    menuItem.Classes.Set("singerMenuSpacer", true);
                    break;
            }
        }

        void OnPhonemizersMenuContainerPrepared(object? sender, ContainerPreparedEventArgs e) {
            if (e.Container is not MenuItem menuItem) {
                return;
            }
            switch (menuItem.DataContext) {
                case PhonemizerMenuItemViewModel phonemizer:
                    menuItem.Header = phonemizer;
                    break;
                case PhonemizerMenuSeparatorViewModel:
                    menuItem.Classes.Set("phonemizerMenuSeparator", true);
                    break;
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            if (change.Property == TrackHeightProperty) {
                SyncAvatarChromeSize();
            }
            if (change.Property == OffsetProperty ||
                change.Property == TrackNoProperty ||
                change.Property == TrackHeightProperty) {
                SetPosition();
            }
        }

        internal void Bind(UTrack track, TrackHeaderCanvas canvas) {
            this.track = track;
            this.canvas = canvas;
            unbinds.Add(this.Bind(TrackHeightProperty, canvas.GetObservable(TrackHeaderCanvas.TrackHeightProperty)));
            unbinds.Add(this.Bind(HeightProperty, canvas.GetObservable(TrackHeaderCanvas.TrackHeightProperty)));
            unbinds.Add(this.Bind(OffsetProperty, canvas.WhenAnyValue(x => x.TrackOffset, trackOffset => new Point(0, -trackOffset * TrackHeight))));
            if (ViewModel != null) {
                unbinds.Add(ViewModel.WhenAnyValue(x => x.IsTrackSettingsVisible)
                    .Subscribe(_ => RefreshChipLayout()));
            }
            SetPosition();
        }

        internal void RefreshChipLayout() {
            if (ChipLayoutGrid is null || MuteChip is null || SoloChip is null || FxChip is null ||
                SettingsChip is null) {
                return;
            }

            if (SettingsInlineChip != null) {
                SettingsInlineChip.IsVisible = false;
            }
            if (SingerPhonemizerPanel != null) {
                SingerPhonemizerPanel.Margin = new Thickness(0);
            }

            SettingsChip.IsVisible = true;

            double chipStep = ViewConstants.TrackHeaderChipStep;
            double availableHeight = Math.Max(chipStep, TrackHeight - 4);
            int maxRows = Math.Max(1, (int)(availableHeight / chipStep));

            int overflowColumns = 0;
            if (maxRows >= 4) {
                // 4 rows in 1 column (default TrackHeight 84 >= 80)
                Grid.SetRow(MuteChip, 0);
                Grid.SetColumn(MuteChip, 0);
                Grid.SetRow(SoloChip, 1);
                Grid.SetColumn(SoloChip, 0);
                Grid.SetRow(FxChip, 2);
                Grid.SetColumn(FxChip, 0);
                Grid.SetRow(SettingsChip, 3);
                Grid.SetColumn(SettingsChip, 0);
                overflowColumns = 0;
            } else if (maxRows >= 2) {
                // 2 rows x 2 columns (height 40px - 79px)
                Grid.SetRow(MuteChip, 0);
                Grid.SetColumn(MuteChip, 0);
                Grid.SetRow(SoloChip, 0);
                Grid.SetColumn(SoloChip, 1);
                Grid.SetRow(FxChip, 1);
                Grid.SetColumn(FxChip, 0);
                Grid.SetRow(SettingsChip, 1);
                Grid.SetColumn(SettingsChip, 1);
                overflowColumns = 1;
            } else {
                // 1 row x 4 columns (height < 40px)
                Grid.SetRow(MuteChip, 0);
                Grid.SetColumn(MuteChip, 0);
                Grid.SetRow(SoloChip, 0);
                Grid.SetColumn(SoloChip, 1);
                Grid.SetRow(FxChip, 0);
                Grid.SetColumn(FxChip, 2);
                Grid.SetRow(SettingsChip, 0);
                Grid.SetColumn(SettingsChip, 3);
                overflowColumns = 3;
            }

            double extraWidth = overflowColumns * (chipStep + 4);
            Width = ViewConstants.TrackHeaderBaseWidth + extraWidth;
            canvas?.UpdateTrackHeaderWidths();
        }

        internal void RefreshLayout() {
            SyncAvatarChromeSize();
            RefreshChipLayout();
            InvalidateMeasure();
            InvalidateArrange();
        }

        void SyncAvatarChromeSize() {
            if (AvatarChrome is null) {
                return;
            }
            const double verticalInset = 2; // outer header margin top + bottom
            double cap = ViewConstants.TrackHeightMax - verticalInset;
            double h = TrackHeight;
            double available = h > 0 ? h - verticalInset : cap;
            double side = Math.Max(0, Math.Min(available, cap));
            AvatarChrome.Width = side;
            AvatarChrome.Height = side;
        }

        private void SetPosition() {
            Canvas.SetLeft(this, 0);
            Canvas.SetTop(this, Offset.Y + (track?.TrackNo ?? 0) * trackHeight);
            if (ViewModel != null) {
                ViewModel.IsSingerVisible = trackHeight >= ViewConstants.TrackHeightDelta * 3;
                ViewModel.IsPhonemizerVisible = trackHeight >= ViewConstants.TrackHeightDelta * 4;
            }
            RefreshChipLayout();
        }

        void HeaderPointerPressed(object? sender, PointerPressedEventArgs args) {
            if (args.Handled) {
                return;
            }
            SelectTrackFromPointer(args);
        }

        void SelectTrackFromPointer(PointerEventArgs args) {
            if (!args.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
                track == null ||
                canvas?.DataContext is not TracksViewModel tracksViewModel) {
                return;
            }
            if (args.KeyModifiers == KeyModifiers.Shift) {
                tracksViewModel.SelectTracksUntil(track);
            } else if (args.KeyModifiers == cmdKey) {
                tracksViewModel.ToggleSelectTrack(track);
            } else {
                tracksViewModel.SelectTrack(track);
            }
        }

        void TrackNoBadgePointerPressed(object? sender, PointerPressedEventArgs args) {
            SelectTrackFromPointer(args);
            if (track != null && canvas != null) {
                canvas.BeginTrackReorder(track, this, args);
            }
            args.Handled = true;
        }

        void TrackNameButtonClicked(object sender, RoutedEventArgs args) {
            ViewModel?.Rename();
            args.Handled = true;
        }

        void SingerButtonClicked(object sender, RoutedEventArgs args) {
            try {
                ViewModel?.RefreshSingers();
                SingersMenu.Open((Control)sender);
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(e));
            }
            args.Handled = true;
        }

        void SingerButtonContextRequested(object sender, ContextRequestedEventArgs args) {
            args.Handled = true;
        }

        void PhonemizerButtonClicked(object sender, RoutedEventArgs args) {
            ViewModel?.RefreshPhonemizers();
            PhonemizersMenu.Open((Control)sender);
            args.Handled = true;
        }

        void PhonemizerButtonContextRequested(object sender, ContextRequestedEventArgs args) {
            args.Handled = true;
        }

        void VolumeFaderPointerPressed(object sender, PointerPressedEventArgs args) {
            if (args.GetCurrentPoint((Visual?)sender).Properties.IsRightButtonPressed && ViewModel != null) {
                ViewModel.Volume = 0;
                args.Handled = true;
            }
        }

        void PanFaderPointerPressed(object sender, PointerPressedEventArgs args) {
            if (args.GetCurrentPoint((Visual?)sender).Properties.IsRightButtonPressed && ViewModel != null) {
                ViewModel.Pan = 0;
                args.Handled = true;
            }
        }

        void VolumeFaderContextRequested(object sender, ContextRequestedEventArgs args) {
            if (ViewModel != null) {
                ViewModel.Volume = 0;
            }
            args.Handled = true;
        }

        void PanFaderContextRequested(object sender, ContextRequestedEventArgs args) {
            if (ViewModel != null) {
                ViewModel.Pan = 0;
            }
            args.Handled = true;
        }

        void TrackSettingsButtonClicked(object sender, RoutedEventArgs args) {
            if (track == null) {
                return;
            }
            if (VisualRoot is Window window) {
                var dialog = new Views.TrackSettingsDialog(track);
                dialog.ShowDialog(window);
            }
        }

        void VolumePointerPressed(object sender, PointerPressedEventArgs args) {
            if (args.ClickCount == 2 && ViewModel != null) {
                ActivateVolumeOrPanTextBox(
                    VolumeTextBox, string.Format("{0:0.0}", ViewModel.Volume));
                args.Handled = true;
                args.Pointer.Capture(sender as IInputElement);
            }
        }
        void PanPointerPressed(object sender, PointerPressedEventArgs args) {
            if (args.ClickCount == 2 && ViewModel != null) {
                ActivateVolumeOrPanTextBox(
                    PanTextBox, string.Format("{0:0}", ViewModel.Pan));
                args.Handled = true;
            }
        }
        void VolumeOrPanTextBoxKeyDown(object sender, KeyEventArgs args) {
            if (args.Key == Key.Enter) {
                FinishVolumeOrPanInput(sender, true);
                args.Handled = true;
            } else if (args.Key == Key.Escape) {
                FinishVolumeOrPanInput(sender, false);
                args.Handled = true;
            }
        }
        void VolumeOrPanTextBoxLostFocus(object sender, RoutedEventArgs args) {
            FinishVolumeOrPanInput(sender, true);
            args.Handled = true;
        }
        void VolumeOrPanSliderValueChanged(object sender, RangeBaseValueChangedEventArgs args) {
            VolumeTextBox.IsVisible = false;
            VolumeTextBox.IsEnabled = false;
            PanTextBox.IsVisible = false;
            PanTextBox.IsEnabled = false;
        }

        void ActivateVolumeOrPanTextBox(TextBox textBox, string text) {
            textBox.Text = text;
            textBox.IsEnabled = true;
            textBox.IsVisible = true;
            textBox.Focus();
        }
        private void FinishVolumeOrPanInput(object sender, bool commit) {
            if (sender == VolumeTextBox) {
                if (!VolumeTextBox.IsVisible) {
                    return; // Avoid double commit.
                }
                VolumeTextBox.IsVisible = false;
                VolumeTextBox.IsEnabled = false;
                if (commit && double.TryParse(VolumeTextBox.Text, out var volume) && ViewModel != null) {
                    ViewModel.Volume = Math.Clamp(volume, VolumeSlider.Minimum, VolumeSlider.Maximum);
                }
            } else {
                if (!PanTextBox.IsVisible) {
                    return; // Avoid double commit.
                }
                PanTextBox.IsVisible = false;
                PanTextBox.IsEnabled = false;
                if (commit && double.TryParse(PanTextBox.Text, out var pan) && ViewModel != null) {
                    ViewModel.Pan = Math.Clamp(pan, PanSlider.Minimum, PanSlider.Maximum);
                }
            }
        }

        public void Dispose() {
            SingersMenu.ContainerPrepared -= OnSingersMenuContainerPrepared;
            unbinds.ForEach(u => u.Dispose());
            unbinds.Clear();
        }
    }
}
