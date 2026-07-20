using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using MultiDiskImager.Core;
using MultiDiskImager.Platform;

namespace MultiDiskImager.Views;

internal sealed partial class DeviceInfoWindow : Window
{
    private readonly DeviceDescriptor _device;

    public DeviceInfoWindow(DeviceDescriptor device)
    {
        _device = device;
        InitializeComponent();
        Title = $"Device info — {device.Id}";
        this.FindControl<TextBlock>("ModelText")!.Text = device.Model;
        this.FindControl<TextBlock>("DetailsText")!.Text =
            $"{device.Id} • {ByteSize.Format(device.Size)} ({device.Size:N0} bytes) • {device.BusType} • " +
            $"{device.LogicalSectorSize}-byte sectors" +
            (string.IsNullOrWhiteSpace(device.Serial) ? string.Empty : $" • Serial {device.Serial}");
        Opened += OnOpened;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private async void OnOpened(object? sender, EventArgs e)
    {
        var holder = this.FindControl<StackPanel>("PartitionList")!;
        try
        {
            var layout = await PlatformPartitionInspector.InspectAsync(_device);
            holder.Children.Add(new TextBlock { Text = $"Partition scheme: {layout.Scheme}", FontWeight = Avalonia.Media.FontWeight.SemiBold });
            if (layout.Partitions.Count == 0)
            {
                holder.Children.Add(new TextBlock { Text = "No allocated partitions found.", Opacity = 0.7 });
            }

            foreach (var partition in layout.Partitions)
            {
                var fraction = layout.DiskSize <= 0 ? 0 : Math.Clamp((double)partition.Length / layout.DiskSize, 0, 1);
                var panel = new StackPanel { Spacing = 4 };
                panel.Children.Add(new TextBlock
                {
                    Text = $"Partition {partition.Number}: {partition.Name ?? partition.Type} — {ByteSize.Format(partition.Length)}",
                    FontWeight = Avalonia.Media.FontWeight.SemiBold
                });
                panel.Children.Add(new ProgressBar { Minimum = 0, Maximum = 1, Value = fraction, Height = 8, HorizontalAlignment = HorizontalAlignment.Stretch });
                holder.Children.Add(panel);
            }
        }
        catch (Exception exception)
        {
            holder.Children.Add(new TextBlock { Text = $"Unable to inspect partitions: {exception.Message}", TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}

