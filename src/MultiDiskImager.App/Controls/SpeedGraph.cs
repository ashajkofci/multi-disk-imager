using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MultiDiskImager.Controls;

internal sealed class SpeedGraph : Control
{
    public static readonly StyledProperty<IReadOnlyDictionary<string, IReadOnlyList<double>>> SeriesProperty =
        AvaloniaProperty.Register<SpeedGraph, IReadOnlyDictionary<string, IReadOnlyList<double>>>(nameof(Series), new Dictionary<string, IReadOnlyList<double>>());

    static SpeedGraph()
    {
        AffectsRender<SpeedGraph>(SeriesProperty, BoundsProperty);
    }

    public IReadOnlyDictionary<string, IReadOnlyList<double>> Series
    {
        get => GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = new Rect(Bounds.Size);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(20, 128, 128, 128)), null, bounds, 6, 6);
        if (Series.Count == 0 || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var maximum = Math.Max(1, Series.Values.SelectMany(samples => samples).Where(double.IsFinite).DefaultIfEmpty().Max());
        var colors = new[] { "#3B82F6", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6", "#06B6D4" };
        var seriesIndex = 0;
        foreach (var samples in Series.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value))
        {
            var pen = new Pen(new SolidColorBrush(Color.Parse(colors[seriesIndex++ % colors.Length])), 2);
            Point? previous = null;
            for (var index = 0; index < samples.Count; index++)
            {
                if (!double.IsFinite(samples[index]))
                {
                    continue;
                }

                var current = new Point(index * bounds.Width / Math.Max(1, samples.Count - 1), bounds.Height - samples[index] / maximum * (bounds.Height - 8) - 4);
                if (previous is { } point)
                {
                    context.DrawLine(pen, point, current);
                }

                previous = current;
            }
        }
    }
}
