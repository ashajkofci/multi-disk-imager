using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MultiDiskImager.Controls;

internal sealed class SpeedGraph : Control
{
    public static readonly StyledProperty<IReadOnlyList<double>> SamplesProperty =
        AvaloniaProperty.Register<SpeedGraph, IReadOnlyList<double>>(nameof(Samples), []);

    static SpeedGraph()
    {
        AffectsRender<SpeedGraph>(SamplesProperty, BoundsProperty);
    }

    public IReadOnlyList<double> Samples
    {
        get => GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = new Rect(Bounds.Size);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(20, 128, 128, 128)), null, bounds, 6, 6);
        if (Samples.Count < 2 || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var maximum = Math.Max(1, Samples.Max());
        var pen = new Pen(new SolidColorBrush(Color.Parse("#3B82F6")), 2);
        for (var index = 1; index < Samples.Count; index++)
        {
            var previous = new Point(
                (index - 1) * bounds.Width / Math.Max(1, Samples.Count - 1),
                bounds.Height - Samples[index - 1] / maximum * (bounds.Height - 8) - 4);
            var current = new Point(
                index * bounds.Width / Math.Max(1, Samples.Count - 1),
                bounds.Height - Samples[index] / maximum * (bounds.Height - 8) - 4);
            context.DrawLine(pen, previous, current);
        }
    }
}

