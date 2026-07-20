using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MultiDiskImager.Localization;

namespace MultiDiskImager.Views;

internal sealed partial class MessageDialog : Window
{
    public MessageDialog() => InitializeComponent();

    public MessageDialog(string title, string message, string? confirmText = null, bool cancelVisible = true)
    {
        InitializeComponent();
        Title = title;
        this.FindControl<TextBlock>("MessageText")!.Text = message;
        this.FindControl<Button>("ConfirmButton")!.Content = confirmText ?? Localizer.Get("Continue");
        this.FindControl<Button>("CancelButton")!.IsVisible = cancelVisible;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    private void OnConfirm(object? sender, RoutedEventArgs e) => Close(true);
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
