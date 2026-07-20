using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using MultiDiskImager.Core;
using MultiDiskImager.Localization;
using MultiDiskImager.Services;
using MultiDiskImager.ViewModels;
using MultiDiskImager.Views;

namespace MultiDiskImager;

public sealed partial class App : Application
{
    internal static CommandLineOptions StartupOptions { get; set; } = CommandLineOptions.Parse([]);

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsStore = new SettingsStore();
            var settings = settingsStore.LoadAsync().GetAwaiter().GetResult();
            Localizer.Configure(settings.Language);
            RequestedThemeVariant = settings.Theme switch
            {
                AppTheme.Light => ThemeVariant.Light,
                AppTheme.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };

            var viewModel = new MainWindowViewModel(settingsStore, settings, StartupOptions);
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
