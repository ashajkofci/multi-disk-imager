using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MultiDiskImager.Core;
using MultiDiskImager.Localization;
using MultiDiskImager.ViewModels;

namespace MultiDiskImager.Views;

internal sealed partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    private async void OnOpened(object? sender, EventArgs e)
    {
        Topmost = ViewModel.Settings.AlwaysOnTop;
        await ViewModel.InitializeAsync();
        if (ViewModel.AutoStartRequested && ViewModel.StartupOperation is { } operation)
        {
            await RunOperationAsync(operation);
        }
    }

    private async void OnBrowse(object? sender, RoutedEventArgs e)
    {
        var suggestedFolder = await GetSuggestedFolderAsync();
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Localizer.Get("RawImage"),
            AllowMultiple = false,
            SuggestedStartLocation = suggestedFolder,
            FileTypeFilter =
            [
                new FilePickerFileType(Localizer.Get("RawImage")) { Patterns = ["*.img", "*.raw", "*.bin"] },
                FilePickerFileTypes.All
            ]
        });
        var file = files.FirstOrDefault();
        if (file is not null)
        {
            ViewModel.ImagePath = file.TryGetLocalPath() ?? file.Path.LocalPath;
            await ViewModel.RememberImageFolderAsync(ViewModel.ImagePath);
        }
    }

    private async void OnRefresh(object? sender, RoutedEventArgs e) => await ViewModel.RefreshDevicesAsync();
    private void OnSelectAll(object? sender, RoutedEventArgs e) => ViewModel.SelectAllDevices();
    private async void OnRead(object? sender, RoutedEventArgs e) => await RunOperationAsync(ImagingOperation.Read);
    private async void OnWrite(object? sender, RoutedEventArgs e) => await RunOperationAsync(ImagingOperation.Write);
    private async void OnVerify(object? sender, RoutedEventArgs e) => await RunOperationAsync(ImagingOperation.Verify);
    private async void OnWipe(object? sender, RoutedEventArgs e) => await RunOperationAsync(ImagingOperation.Wipe);

    private void OnCancel(object? sender, RoutedEventArgs e) => ViewModel.Cancel();

    private async void OnChecksum(object? sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.CalculateChecksumAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(Localizer.Get("ChecksumFailed"), exception.Message);
        }
    }

    private async Task RunOperationAsync(ImagingOperation operation)
    {
        try
        {
            if (operation == ImagingOperation.Read && string.IsNullOrWhiteSpace(ViewModel.ImagePath))
            {
                var output = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = Localizer.Get("RawImage"),
                    SuggestedStartLocation = await GetSuggestedFolderAsync(),
                    SuggestedFileName = "disk.img",
                    DefaultExtension = "img",
                    FileTypeChoices = [new FilePickerFileType(Localizer.Get("RawImage")) { Patterns = ["*.img"] }]
                });
                if (output is null)
                {
                    return;
                }

                ViewModel.ImagePath = output.TryGetLocalPath() ?? output.Path.LocalPath;
                await ViewModel.RememberImageFolderAsync(ViewModel.ImagePath);
            }

            var selected = ViewModel.SelectedDevices;
            if (selected.Count == 0)
            {
                throw new InvalidOperationException(Localizer.Get("SelectAtLeastOneDevice"));
            }

            var allowCrop = false;
            long? byteCount = null;
            if (operation == ImagingOperation.Read && File.Exists(ViewModel.ImagePath))
            {
                var overwrite = await ConfirmAsync(Localizer.Get("ReplaceTitle"), $"{Path.GetFileName(ViewModel.ImagePath)} {Localizer.Get("AlreadyExists")}", Localizer.Get("Replace"));
                if (!overwrite)
                {
                    return;
                }
            }

            if (operation == ImagingOperation.Write)
            {
                var imageSize = new FileInfo(ViewModel.ImagePath).Length;
                var smallest = selected.Min(device => device.Size);
                if (imageSize > smallest)
                {
                    var dataFound = await ViewModel.TailContainsDataAsync(smallest);
                    var message = $"{Localizer.Get("ImageTooLarge")}: +{ByteSize.Format(imageSize - smallest)} — {selected.OrderBy(device => device.Size).First().Id}. " +
                                  $"{Localizer.Get("CropAndWrite")}: {ByteSize.Format(smallest)}?" +
                                  (dataFound ? $" {Localizer.Get("StopWarning")}" : string.Empty);
                    if (!await ConfirmAsync(Localizer.Get("ImageTooLarge"), message, Localizer.Get("CropAndWrite")))
                    {
                        return;
                    }

                    allowCrop = true;
                    byteCount = smallest;
                }
            }

            if (operation == ImagingOperation.Wipe || operation == ImagingOperation.Write && ViewModel.Settings.DisplayWriteWarnings)
            {
                var devices = string.Join(Environment.NewLine, selected.Select(device => $"• {device.Model} — {ByteSize.Format(device.Size)} ({device.Id})"));
                if (!await ConfirmAsync(
                        operation == ImagingOperation.Wipe ? Localizer.Get("QuickWipe") : Localizer.Get("WriteImage"),
                        $"{devices}{Environment.NewLine}{Environment.NewLine}{Localizer.Get("StopWarning")}",
                        operation == ImagingOperation.Wipe ? Localizer.Get("QuickWipe") : Localizer.Get("WriteImage")))
                {
                    return;
                }
            }

            var result = await ViewModel.RunAsync(operation, allowCrop, byteCount);
            if (result.Success && ViewModel.Settings.EnableSoundNotification)
            {
                Services.NotificationService.Notify();
            }
            var details = string.Join(Environment.NewLine, result.Devices.Select(device =>
                $"{device.DeviceId}: {(device.Success ? Localizer.Get("Success") : device.Error ?? Localizer.Get("Failed"))}" +
                (device.FirstMismatchOffset is { } offset ? $" ({Localizer.Get("Failed")}: {offset:N0} {Localizer.Get("BytesLabel")})" : string.Empty)));
            await new MessageDialog(result.Success ? Localizer.Get("OperationComplete") : Localizer.Get("OperationResults"), details, Localizer.Get("Close"), cancelVisible: false).ShowDialog<bool>(this);
            if (result.Success && ViewModel.Settings.AutoCloseOnSuccess)
            {
                _allowClose = true;
                Close();
            }
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(Localizer.Get("Failed"), exception.Message);
        }
    }

    private async void OnSettings(object? sender, RoutedEventArgs e)
    {
        var settings = await new SettingsWindow(ViewModel.Settings).ShowDialog<Services.AppSettings?>(this);
        if (settings is not null)
        {
            await ViewModel.ApplySettingsAsync(settings);
            Topmost = settings.AlwaysOnTop;
        }
    }

    private async void OnHelp(object? sender, RoutedEventArgs e) => await new HelpWindow().ShowDialog(this);

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            e.Handled = true;
            await new HelpWindow().ShowDialog(this);
        }
    }

    private async void OnAbout(object? sender, RoutedEventArgs e) => await new AboutWindow().ShowDialog(this);

    private void OnOpenUpdate(object? sender, RoutedEventArgs e) => ViewModel.OpenUpdatePage();

    private async void OnDeviceInfo(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DeviceItemViewModel item })
        {
            await new DeviceInfoWindow(item.Device).ShowDialog(this);
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var file = e.DataTransfer.TryGetFiles()?.FirstOrDefault();
        if (file is not null)
        {
            ViewModel.ImagePath = file.TryGetLocalPath() ?? file.Path.LocalPath;
        }
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || !ViewModel.IsBusy)
        {
            return;
        }

        e.Cancel = true;
        if (await ConfirmAsync(Localizer.Get("CancelActiveOperation"), Localizer.Get("StopWarning"), Localizer.Get("Cancel")))
        {
            ViewModel.Cancel();
            _allowClose = true;
            Close();
        }
    }

    private Task<bool> ConfirmAsync(string title, string message, string confirmText) =>
        new MessageDialog(title, message, confirmText).ShowDialog<bool>(this);

    private Task<bool> ShowErrorAsync(string title, string message) =>
        new MessageDialog(title, message, Localizer.Get("Close"), cancelVisible: false).ShowDialog<bool>(this);

    private async Task<IStorageFolder?> GetSuggestedFolderAsync()
    {
        var settings = ViewModel.Settings;
        var candidates = settings.UseUserSpecifiedFolder
            ? new[] { settings.UserSpecifiedFolder }.Concat(settings.CustomPlaces).Append(settings.LastFolderPath)
            : new[] { settings.LastFolderPath }.Concat(settings.CustomPlaces).Append(settings.UserSpecifiedFolder);
        var path = candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate) && Directory.Exists(candidate));
        return path is null ? null : await StorageProvider.TryGetFolderFromPathAsync(new Uri(Path.GetFullPath(path)));
    }
}
