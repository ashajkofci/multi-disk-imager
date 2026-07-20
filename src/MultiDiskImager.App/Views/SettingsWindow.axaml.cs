using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MultiDiskImager.Services;

namespace MultiDiskImager.Views;

internal sealed partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        Check("WriteWarnings", settings.DisplayWriteWarnings);
        Check("Animations", settings.EnableAnimations);
        Check("UpdateChecks", settings.CheckForUpdatesOnStartup);
        Check("SoundNotification", settings.EnableSoundNotification);
        Check("AutoSelect", settings.AutoSelectSingleDevice);
        Check("AutoClose", settings.AutoCloseOnSuccess);
        Check("AlwaysOnTop", settings.AlwaysOnTop);
        Check("UseCustomFolder", settings.UseUserSpecifiedFolder);
        Check("ShowExternalHardDrives", settings.ShowExternalHardDrives);
        Check("LimitSize", settings.OmitDrivesOverSize);
        this.FindControl<NumericUpDown>("LimitGiB")!.Value = (decimal)settings.OmitDrivesThresholdGiB;
        this.FindControl<ComboBox>("ThemeChoice")!.SelectedIndex = (int)settings.Theme;
        this.FindControl<ComboBox>("TitleExtra")!.SelectedIndex = (int)settings.TitleExtra;
        this.FindControl<TextBox>("CustomFolder")!.Text = settings.UserSpecifiedFolder;
        this.FindControl<TextBox>("CustomPlaces")!.Text = string.Join(Environment.NewLine, settings.CustomPlaces);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    private void Check(string name, bool value) => this.FindControl<CheckBox>(name)!.IsChecked = value;
    private bool Checked(string name) => this.FindControl<CheckBox>(name)!.IsChecked == true;

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        var result = _settings with
        {
            DisplayWriteWarnings = Checked("WriteWarnings"),
            EnableAnimations = Checked("Animations"),
            CheckForUpdatesOnStartup = Checked("UpdateChecks"),
            EnableSoundNotification = Checked("SoundNotification"),
            AutoSelectSingleDevice = Checked("AutoSelect"),
            AutoCloseOnSuccess = Checked("AutoClose"),
            AlwaysOnTop = Checked("AlwaysOnTop"),
            UseUserSpecifiedFolder = Checked("UseCustomFolder"),
            UserSpecifiedFolder = this.FindControl<TextBox>("CustomFolder")!.Text?.Trim(),
            CustomPlaces = (this.FindControl<TextBox>("CustomPlaces")!.Text ?? string.Empty)
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            ShowExternalHardDrives = Checked("ShowExternalHardDrives"),
            OmitDrivesOverSize = Checked("LimitSize"),
            OmitDrivesThresholdGiB = (double)(this.FindControl<NumericUpDown>("LimitGiB")!.Value ?? 128),
            Theme = (AppTheme)Math.Max(0, this.FindControl<ComboBox>("ThemeChoice")!.SelectedIndex),
            TitleExtra = (TitleExtra)Math.Max(0, this.FindControl<ComboBox>("TitleExtra")!.SelectedIndex)
        };
        Close(result);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);

    private async void OnBrowseFolder(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = "Select default image folder",
            AllowMultiple = false
        });
        var folder = folders.FirstOrDefault();
        if (folder is not null)
        {
            this.FindControl<TextBox>("CustomFolder")!.Text = folder.TryGetLocalPath() ?? folder.Path.LocalPath;
            Check("UseCustomFolder", true);
        }
    }
}
