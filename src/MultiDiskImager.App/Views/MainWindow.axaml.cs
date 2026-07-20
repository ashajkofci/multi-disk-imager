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
            Title = "Select raw disk image",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedFolder,
            FileTypeFilter =
            [
                new FilePickerFileType("Raw disk image") { Patterns = ["*.img", "*.raw", "*.bin"] },
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
            await ShowErrorAsync("Checksum failed", exception.Message);
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
                    Title = "Save raw disk image",
                    SuggestedStartLocation = await GetSuggestedFolderAsync(),
                    SuggestedFileName = "disk.img",
                    DefaultExtension = "img",
                    FileTypeChoices = [new FilePickerFileType("Raw disk image") { Patterns = ["*.img"] }]
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
                throw new InvalidOperationException("Select at least one device.");
            }

            var allowCrop = false;
            long? byteCount = null;
            if (operation == ImagingOperation.Read && File.Exists(ViewModel.ImagePath))
            {
                var overwrite = await ConfirmAsync("Replace image?", $"{Path.GetFileName(ViewModel.ImagePath)} already exists and will be replaced.", "Replace");
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
                    var message = $"The image is {ByteSize.Format(imageSize - smallest)} larger than {selected.OrderBy(device => device.Size).First().Id}. " +
                                  $"The discarded tail {(dataFound ? "contains non-zero data" : "contains only zeroes")}. Write only the first {ByteSize.Format(smallest)}?";
                    if (!await ConfirmAsync("Image is too large", message, "Crop and write"))
                    {
                        return;
                    }

                    allowCrop = true;
                    byteCount = smallest;
                }
            }

            if (operation == ImagingOperation.Wipe || operation == ImagingOperation.Write && ViewModel.Settings.DisplayWriteWarnings)
            {
                var action = operation == ImagingOperation.Wipe ? "remove partition and filesystem metadata from" : "overwrite";
                var devices = string.Join(Environment.NewLine, selected.Select(device => $"• {device.Model} — {ByteSize.Format(device.Size)} ({device.Id})"));
                if (!await ConfirmAsync(
                        operation == ImagingOperation.Wipe ? "Confirm quick wipe" : "Confirm write",
                        $"This will {action} the following device(s):{Environment.NewLine}{Environment.NewLine}{devices}{Environment.NewLine}{Environment.NewLine}This cannot be undone. Verify the model and size before continuing.",
                        operation == ImagingOperation.Wipe ? "Quick wipe" : "Write image"))
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
                $"{device.DeviceId}: {(device.Success ? "Success" : device.Error ?? "Failed")}" +
                (device.FirstMismatchOffset is { } offset ? $" (first mismatch at byte {offset:N0})" : string.Empty)));
            await new MessageDialog(result.Success ? "Operation complete" : "Operation results", details, "Close", cancelVisible: false).ShowDialog<bool>(this);
            if (result.Success && ViewModel.Settings.AutoCloseOnSuccess)
            {
                _allowClose = true;
                Close();
            }
        }
        catch (Exception exception)
        {
            await ShowErrorAsync($"{operation} failed", exception.Message);
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

    private async void OnAbout(object? sender, RoutedEventArgs e)
    {
        await new MessageDialog(
            Localizer.Get("AboutTitle"),
            $"bNovate Multi Disk Imager {Services.UpdateService.CurrentVersionText}{Environment.NewLine}{Environment.NewLine}" +
            $"Copyright © 2026 bNovate Technologies SA{Environment.NewLine}" +
            $"{Localizer.Get("Author")}: Adrian Shajkofci{Environment.NewLine}" +
            $"https://www.bnovate.com{Environment.NewLine}{Environment.NewLine}" +
            Localizer.Get("AboutDescription"),
            Localizer.Get("Close"),
            cancelVisible: false).ShowDialog<bool>(this);
    }

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
        if (await ConfirmAsync("Cancel active operation?", "Stopping now may leave the target with incomplete or corrupt data.", "Cancel operation"))
        {
            ViewModel.Cancel();
            _allowClose = true;
            Close();
        }
    }

    private Task<bool> ConfirmAsync(string title, string message, string confirmText) =>
        new MessageDialog(title, message, confirmText).ShowDialog<bool>(this);

    private Task<bool> ShowErrorAsync(string title, string message) =>
        new MessageDialog(title, message, "Close", cancelVisible: false).ShowDialog<bool>(this);

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
