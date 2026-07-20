using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MultiDiskImager.Localization;
using MultiDiskImager.Services;

namespace MultiDiskImager.Views;

internal sealed partial class AboutWindow : Window
{
    private static readonly Uri Website = new("https://www.bnovate.com");

    public AboutWindow()
    {
        InitializeComponent();
        this.FindControl<TextBlock>("VersionText")!.Text = $"Version {UpdateService.CurrentVersionText}";
        this.FindControl<TextBlock>("AuthorText")!.Text = $"{Localizer.Get("Author")}: Adrian Shajkofci";
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnWebsite(object? sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo(Website.AbsoluteUri) { UseShellExecute = true });

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
