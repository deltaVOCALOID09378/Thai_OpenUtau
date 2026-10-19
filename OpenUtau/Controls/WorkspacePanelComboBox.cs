using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace OpenUtau.App.Controls {
  /// <summary>Workspace panel ComboBox; merges popup chrome and clips list hover to rounded corners.</summary>
  public class WorkspacePanelComboBox : ComboBox {
    const string OpenAboveClass = "openAbove";
    const double JoinOverlap = 1;
    const double InnerRadius = 5;

    protected override Type StyleKeyOverride => typeof(ComboBox);

    Border? background;
    Border? popupBorder;
    RoundedClipBorder? popupContentClip;
    ContentControl? selectionPresenter;
    PathIcon? dropDownGlyph;
    bool? lastOpensAbove;

    public WorkspacePanelComboBox() {
      DropDownOpened += OnDropDownOpened;
      DropDownClosed += OnDropDownClosed;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
      base.OnApplyTemplate(e);

      if (popupBorder != null) {
        popupBorder.LayoutUpdated -= OnPopupBorderLayoutUpdated;
      }

      background = e.NameScope.Find<Border>("Background");
      popupBorder = e.NameScope.Find<Border>("PopupBorder");
      selectionPresenter = e.NameScope.Find<ContentControl>("ContentPresenter");
      dropDownGlyph = e.NameScope.Find<PathIcon>("DropDownGlyph");
      EnsurePopupClipHost();
      SyncEnabledChrome();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
      base.OnPropertyChanged(change);
      if (change.Property == IsEnabledProperty) {
        SyncEnabledChrome();
      }
    }

    void EnsurePopupClipHost() {
      popupContentClip = popupBorder?.Child as RoundedClipBorder;
      if (popupContentClip != null || popupBorder?.Child is not ScrollViewer scrollViewer) {
        return;
      }

      var clipHost = new RoundedClipBorder {
        Background = Brushes.Transparent,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
      };
      popupBorder.Child = null;
      clipHost.Child = scrollViewer;
      popupBorder.Child = clipHost;
      popupContentClip = clipHost;
    }

    void OnDropDownOpened(object? sender, EventArgs e) {
      lastOpensAbove = null;
      if (popupBorder != null) {
        popupBorder.LayoutUpdated += OnPopupBorderLayoutUpdated;
      }
      Dispatcher.UIThread.Post(ApplyOpenChrome, DispatcherPriority.Loaded);
    }

    void OnPopupBorderLayoutUpdated(object? sender, EventArgs e) {
      if (!IsDropDownOpen) {
        if (popupBorder != null) {
          popupBorder.LayoutUpdated -= OnPopupBorderLayoutUpdated;
        }
        return;
      }

      ApplyOpenChrome();
    }

    void OnDropDownClosed(object? sender, EventArgs e) {
      if (popupBorder != null) {
        popupBorder.LayoutUpdated -= OnPopupBorderLayoutUpdated;
      }
      lastOpensAbove = null;
      ClearOpenChrome();
    }

    void ApplyOpenChrome() {
      if (!IsDropDownOpen || background == null) {
        return;
      }

      var opensAbove = IsPopupAboveTarget();
      var reliable = popupBorder != null && popupBorder.Bounds.Height >= 2;
      if (reliable && lastOpensAbove == opensAbove) {
        return;
      }

      if (reliable) {
        lastOpensAbove = opensAbove;
      }

      background.Classes.Set(OpenAboveClass, opensAbove);
      popupBorder?.Classes.Set(OpenAboveClass, opensAbove);

      if (opensAbove) {
        popupBorder?.SetCurrentValue(Border.MarginProperty, new Thickness(0, 0, 0, -JoinOverlap));
        popupContentClip?.SetCurrentValue(Border.CornerRadiusProperty, new CornerRadius(InnerRadius, InnerRadius, 0, 0));
      } else {
        popupBorder?.SetCurrentValue(Border.MarginProperty, new Thickness(0, -JoinOverlap, 0, 0));
        popupContentClip?.SetCurrentValue(Border.CornerRadiusProperty, new CornerRadius(0, 0, InnerRadius, InnerRadius));
      }
    }

    bool IsPopupAboveTarget() {
      if (background == null || popupBorder == null) {
        return false;
      }

      var targetTop = background.PointToScreen(new Point(0, 0)).Y;
      var targetBottom = background.PointToScreen(new Point(0, background.Bounds.Height)).Y;
      var surfaceTop = popupBorder.PointToScreen(new Point(0, 0)).Y;
      var surfaceBottom = popupBorder.PointToScreen(new Point(0, Math.Max(popupBorder.Bounds.Height, 1))).Y;

      if (surfaceBottom <= targetTop + 3) {
        return true;
      }

      if (surfaceTop >= targetBottom - 3) {
        return false;
      }

      return surfaceTop + popupBorder.Bounds.Height * 0.5 < targetTop + background.Bounds.Height * 0.5;
    }

    void ClearOpenChrome() {
      background?.Classes.Remove(OpenAboveClass);
      popupBorder?.Classes.Remove(OpenAboveClass);
      popupBorder?.ClearValue(Border.MarginProperty);
      popupContentClip?.ClearValue(Border.CornerRadiusProperty);
    }

    void SyncEnabledChrome() {
      if (background == null && selectionPresenter == null && dropDownGlyph == null) {
        return;
      }

      var theme = ActualThemeVariant ?? ThemeVariant.Default;
      if (!IsEnabled) {
        var disabledForeground = GetThemeBrush("TextControlForegroundDisabled", theme);
        var disabledBorder = GetThemeBrush("TextControlBorderBrushDisabled", theme, "TextControlForegroundDisabled");
        SetValue(ForegroundProperty, disabledForeground);
        SetValue(BorderBrushProperty, disabledBorder);
        SetValue(OpacityProperty, 1d);
        background?.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        background?.SetValue(Border.BorderBrushProperty, disabledBorder);
        selectionPresenter?.SetValue(ContentControl.ForegroundProperty, disabledForeground);
        dropDownGlyph?.SetValue(PathIcon.ForegroundProperty, disabledForeground);
        return;
      }

      ClearValue(ForegroundProperty);
      ClearValue(BorderBrushProperty);
      ClearValue(OpacityProperty);
      background?.ClearValue(Border.BackgroundProperty);
      background?.ClearValue(Border.BorderBrushProperty);
      selectionPresenter?.ClearValue(ContentControl.ForegroundProperty);
      dropDownGlyph?.ClearValue(PathIcon.ForegroundProperty);
    }

    static IBrush GetThemeBrush(string key, ThemeVariant theme, string? fallbackKey = null) {
      if (Application.Current?.TryFindResource(key, theme, out var resource) == true
          && resource is IBrush brush) {
        return brush;
      }

      if (fallbackKey != null
          && Application.Current?.TryFindResource(fallbackKey, theme, out resource) == true
          && resource is IBrush fallback) {
        return fallback;
      }

      return Brushes.Gray;
    }
  }
}
