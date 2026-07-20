using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MultiDiskImager.Views;

internal sealed partial class HelpWindow : Window
{
    public HelpWindow() => InitializeComponent();

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
