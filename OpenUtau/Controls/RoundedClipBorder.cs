using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace OpenUtau.App.Controls {
    /// <summary>Clips children to <see cref="Border.CornerRadius"/> using an explicit geometry clip.</summary>
    public class RoundedClipBorder : Border {
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
            base.OnAttachedToVisualTree(e);
            UpdateClip();
        }

        protected override Size ArrangeOverride(Size finalSize) {
            var result = base.ArrangeOverride(finalSize);
            UpdateClip();
            return result;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            if (change.Property == CornerRadiusProperty || change.Property == BoundsProperty) {
                UpdateClip();
            }
        }

        void UpdateClip() {
            var size = Bounds.Size;
            if (size.Width <= 0 || size.Height <= 0) {
                Clip = null;
                return;
            }

            Clip = CreateRoundedRectGeometry(new Rect(size), CornerRadius);
            ClipToBounds = true;
        }

        static StreamGeometry CreateRoundedRectGeometry(Rect rect, CornerRadius radius) {
            var topLeft = Math.Min(radius.TopLeft, Math.Min(rect.Width, rect.Height) / 2);
            var topRight = Math.Min(radius.TopRight, Math.Min(rect.Width, rect.Height) / 2);
            var bottomRight = Math.Min(radius.BottomRight, Math.Min(rect.Width, rect.Height) / 2);
            var bottomLeft = Math.Min(radius.BottomLeft, Math.Min(rect.Width, rect.Height) / 2);

            var geometry = new StreamGeometry();
            using var context = geometry.Open();
            context.BeginFigure(new Point(rect.X + topLeft, rect.Y), true);

            context.LineTo(new Point(rect.Right - topRight, rect.Y));
            if (topRight > 0) {
                context.ArcTo(
                    new Point(rect.Right, rect.Y + topRight),
                    new Size(topRight, topRight),
                    0,
                    false,
                    SweepDirection.Clockwise);
            }

            context.LineTo(new Point(rect.Right, rect.Bottom - bottomRight));
            if (bottomRight > 0) {
                context.ArcTo(
                    new Point(rect.Right - bottomRight, rect.Bottom),
                    new Size(bottomRight, bottomRight),
                    0,
                    false,
                    SweepDirection.Clockwise);
            }

            context.LineTo(new Point(rect.X + bottomLeft, rect.Bottom));
            if (bottomLeft > 0) {
                context.ArcTo(
                    new Point(rect.X, rect.Bottom - bottomLeft),
                    new Size(bottomLeft, bottomLeft),
                    0,
                    false,
                    SweepDirection.Clockwise);
            }

            context.LineTo(new Point(rect.X, rect.Y + topLeft));
            if (topLeft > 0) {
                context.ArcTo(
                    new Point(rect.X + topLeft, rect.Y),
                    new Size(topLeft, topLeft),
                    0,
                    false,
                    SweepDirection.Clockwise);
            }

            context.EndFigure(true);
            return geometry;
        }
    }
}
