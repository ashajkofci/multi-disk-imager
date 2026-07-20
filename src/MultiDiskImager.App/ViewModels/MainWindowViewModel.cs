using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Styling;
using MultiDiskImager.Core;
using MultiDiskImager.Platform;
using MultiDiskImager.Privileged;
using MultiDiskImager.Services;

namespace MultiDiskImager.ViewModels;

internal sealed class MainWindowViewModel : ObservableObject
{
    private readonly SettingsStore _settingsStore;
    private readonly CommandLineOptions _startupOptions;
    private readonly IBlockDeviceCatalog _catalog = PlatformServices.CreateCatalog();
    private CancellationTokenSource? _operationCancellation;
    private AppSettings _settings;
    private string _imagePath = string.Empty;
    private bool _verifyAfter;
    private bool _onlyAllocated;
    private bool _isBusy;
    private bool _isRefreshing;
    private string _status = "Ready";
    private string _checksum = string.Empty;
    private ChecksumAlgorithm _checksumAlgorithm = ChecksumAlgorithm.Sha256;
    private double _progress;
    private string _progressText = string.Empty;
    private IReadOnlyList<double> _speedSamples = [];
    private UpdateInfo? _availableUpdate;
    private ImagingProgress? _lastProgress;

    public MainWindowViewModel(SettingsStore settingsStore, AppSettings settings, CommandLineOptions startupOptions)
    {
        _settingsStore = settingsStore;
        _settings = settings;
        _startupOptions = startupOptions;
        _imagePath = startupOptions.ImagePath ?? string.Empty;
        _verifyAfter = startupOptions.Verify && (startupOptions.Read || startupOptions.Write);
        _onlyAllocated = startupOptions.OnlyAllocated;
    }

    public ObservableCollection<DeviceItemViewModel> Devices { get; } = [];
    public IReadOnlyList<ChecksumAlgorithm> ChecksumAlgorithms { get; } = Enum.GetValues<ChecksumAlgorithm>();
    public AppSettings Settings => _settings;

    public string ImagePath
    {
        get => _imagePath;
        set
        {
            if (SetProperty(ref _imagePath, value))
            {
                RaisePropertyChanged(nameof(WindowTitle));
            }
        }
    }

    public bool VerifyAfter
    {
        get => _verifyAfter;
        set => SetProperty(ref _verifyAfter, value);
    }

    public bool OnlyAllocated
    {
        get => _onlyAllocated;
        set => SetProperty(ref _onlyAllocated, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaisePropertyChanged(nameof(IsIdle));
            }
        }
    }

    public bool IsIdle => !IsBusy;

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set => SetProperty(ref _isRefreshing, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string Checksum
    {
        get => _checksum;
        private set => SetProperty(ref _checksum, value);
    }

    public ChecksumAlgorithm ChecksumAlgorithm
    {
        get => _checksumAlgorithm;
        set => SetProperty(ref _checksumAlgorithm, value);
    }

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public string ProgressText
    {
        get => _progressText;
        private set => SetProperty(ref _progressText, value);
    }

    public IReadOnlyList<double> SpeedSamples
    {
        get => _speedSamples;
        private set => SetProperty(ref _speedSamples, value);
    }

    public UpdateInfo? AvailableUpdate
    {
        get => _availableUpdate;
        private set
        {
            if (SetProperty(ref _availableUpdate, value))
            {
                RaisePropertyChanged(nameof(HasAvailableUpdate));
            }
        }
    }

    public bool HasAvailableUpdate => AvailableUpdate is not null;
    public string WindowTitle => _settings.TitleExtra switch
    {
        TitleExtra.Percent when _lastProgress is not null => $"{_lastProgress.Fraction:P0} — Multi Disk Imager",
        TitleExtra.CurrentSpeed when _lastProgress is not null => $"{ByteSize.Format((long)_lastProgress.BytesPerSecond)}/s — Multi Disk Imager",
        TitleExtra.RemainingTime when _lastProgress?.Remaining is { } remaining => $"{remaining:hh\\:mm\\:ss} — Multi Disk Imager",
        TitleExtra.ActiveDevice when SelectedDevices.FirstOrDefault() is { } device => $"{device.Id} — Multi Disk Imager",
        TitleExtra.ImageFileName when !string.IsNullOrWhiteSpace(ImagePath) => $"{Path.GetFileName(ImagePath)} — Multi Disk Imager",
        _ => "Multi Disk Imager"
    };
    public ImagingOperation? StartupOperation => _startupOptions.Read
        ? ImagingOperation.Read
        : _startupOptions.Write
            ? ImagingOperation.Write
            : _startupOptions.Verify && !_startupOptions.Read && !_startupOptions.Write
                ? ImagingOperation.Verify
                : null;
    public bool AutoStartRequested => _startupOptions.AutoStart;
    public IReadOnlyList<DeviceDescriptor> SelectedDevices => Devices.Where(item => item.IsSelected).Select(item => item.Device).ToArray();

    public async Task InitializeAsync()
    {
        await RefreshDevicesAsync().ConfigureAwait(false);
        SelectStartupDevices();
        if (_settings.CheckForUpdatesOnStartup)
        {
            _ = CheckForUpdatesAsync();
        }
    }

    public async Task RefreshDevicesAsync()
    {
        if (IsRefreshing || IsBusy)
        {
            return;
        }

        IsRefreshing = true;
        Status = "Refreshing devices…";
        var selected = Devices.Where(item => item.IsSelected).Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        try
        {
            var devices = await _catalog.GetDevicesAsync().ConfigureAwait(false);
            var threshold = _settings.OmitDrivesThresholdGiB * 1024 * 1024 * 1024;
            var visible = devices.Where(device => !device.IsSystem)
                .Where(device => device.IsRemovable || _settings.ShowExternalHardDrives && device.IsExternal)
                .Where(device => !_settings.OmitDrivesOverSize || device.Size <= threshold)
                .ToArray();

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Devices.Clear();
                foreach (var device in visible)
                {
                    Devices.Add(new DeviceItemViewModel(device) { IsSelected = selected.Contains(device.Id) });
                }

                if (_settings.AutoSelectSingleDevice && Devices.Count == 1)
                {
                    Devices[0].IsSelected = true;
                }
            });
            Status = visible.Length == 0
                ? OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? "No eligible removable devices found" : "This platform is not supported"
                : $"{visible.Length} device{(visible.Length == 1 ? string.Empty : "s")} available";
        }
        catch (Exception exception)
        {
            Status = $"Device refresh failed: {exception.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task<ImagingJobResult> RunAsync(ImagingOperation operation, bool allowCrop = false, long? byteCount = null)
    {
        Validate(operation);
        IsBusy = true;
        Progress = 0;
        _lastProgress = null;
        RaisePropertyChanged(nameof(WindowTitle));
        ProgressText = "Preparing…";
        Checksum = string.Empty;
        SpeedSamples = [];
        _operationCancellation = new CancellationTokenSource();
        var selected = SelectedDevices;

        try
        {
            var progress = new Progress<ImagingProgress>(UpdateProgress);
            var request = new HelperRequest(operation, ImagePath, selected, VerifyAfter, OnlyAllocated, allowCrop, byteCount);
            var result = await PrivilegedHelperClient.RunAsync(request, progress, _operationCancellation.Token).ConfigureAwait(false);
            Status = result.Canceled
                ? "Operation canceled; the target may contain incomplete data"
                : result.Success
                    ? $"{operation} completed successfully"
                    : result.PartialSuccess
                        ? $"{operation} completed on some devices"
                        : $"{operation} failed";
            return result;
        }
        catch (OperationCanceledException)
        {
            Status = "Operation canceled; the target may contain incomplete data";
            return new ImagingJobResult(operation, true, selected.Select(device => new DeviceOperationResult(device.Id, false, 0, "Canceled")).ToArray());
        }
        catch (Exception exception)
        {
            Status = $"{operation} failed: {exception.Message}";
            throw;
        }
        finally
        {
            _operationCancellation?.Dispose();
            _operationCancellation = null;
            IsBusy = false;
        }
    }

    public void Cancel() => _operationCancellation?.Cancel();

    public async Task CalculateChecksumAsync()
    {
        if (!File.Exists(ImagePath))
        {
            throw new FileNotFoundException("Select an existing image file first.", ImagePath);
        }

        IsBusy = true;
        _operationCancellation = new CancellationTokenSource();
        try
        {
            await using var stream = new FileStream(ImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var progress = new Progress<double>(value =>
            {
                Progress = value * 100;
                ProgressText = $"Checksum {value:P0}";
            });
            Checksum = await ChecksumService.ComputeAsync(stream, ChecksumAlgorithm, progress, _operationCancellation.Token).ConfigureAwait(false);
            Status = $"{ChecksumAlgorithm} checksum calculated";
        }
        finally
        {
            _operationCancellation?.Dispose();
            _operationCancellation = null;
            IsBusy = false;
        }
    }

    public async Task<bool> TailContainsDataAsync(long deviceSize)
    {
        await using var image = new FileStream(ImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await new ImagingEngine().TailContainsNonZeroAsync(image, deviceSize).ConfigureAwait(false);
    }

    public async Task ApplySettingsAsync(AppSettings settings)
    {
        _settings = settings;
        RaisePropertyChanged(nameof(Settings));
        RaisePropertyChanged(nameof(WindowTitle));
        await _settingsStore.SaveAsync(settings).ConfigureAwait(false);
        if (Application.Current is { } application)
        {
            application.RequestedThemeVariant = settings.Theme switch
            {
                AppTheme.Light => ThemeVariant.Light,
                AppTheme.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }

        await RefreshDevicesAsync().ConfigureAwait(false);
    }

    public async Task RememberImageFolderAsync(string imagePath)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(imagePath));
        if (string.IsNullOrWhiteSpace(folder) || string.Equals(folder, _settings.LastFolderPath, StringComparison.Ordinal))
        {
            return;
        }

        _settings = _settings with { LastFolderPath = folder };
        RaisePropertyChanged(nameof(Settings));
        await _settingsStore.SaveAsync(_settings).ConfigureAwait(false);
    }

    public async Task CheckForUpdatesAsync()
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            AvailableUpdate = await new UpdateService(httpClient).CheckAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }
    }

    public void OpenUpdatePage()
    {
        if (AvailableUpdate is not null)
        {
            Process.Start(new ProcessStartInfo(AvailableUpdate.ReleasePage.AbsoluteUri) { UseShellExecute = true });
        }
    }

    private void UpdateProgress(ImagingProgress value)
    {
        _lastProgress = value;
        RaisePropertyChanged(nameof(WindowTitle));
        Progress = value.Fraction * 100;
        var remaining = value.Remaining is { } duration && duration > TimeSpan.Zero ? $" • {duration:hh\\:mm\\:ss} remaining" : string.Empty;
        ProgressText = $"{value.Stage} • {value.Fraction:P0} • {ByteSize.Format(value.BytesProcessed)} / {ByteSize.Format(value.TotalBytes)} • {ByteSize.Format((long)value.BytesPerSecond)}/s{remaining}";
        var samples = SpeedSamples.ToList();
        samples.Add(value.BytesPerSecond);
        if (samples.Count > 90)
        {
            samples.RemoveAt(0);
        }

        SpeedSamples = samples;
    }

    private void Validate(ImagingOperation operation)
    {
        var selected = SelectedDevices;
        if (selected.Count == 0)
        {
            throw new InvalidOperationException("Select at least one device.");
        }

        if (selected.Any(device => device.IsSystem))
        {
            throw new InvalidOperationException("The system disk cannot be selected.");
        }

        if (operation == ImagingOperation.Read && selected.Count != 1)
        {
            throw new InvalidOperationException("Reading requires exactly one source device.");
        }

        if (operation != ImagingOperation.Wipe && string.IsNullOrWhiteSpace(ImagePath))
        {
            throw new InvalidOperationException("Select an image path.");
        }

        if (operation is ImagingOperation.Write or ImagingOperation.Verify && !File.Exists(ImagePath))
        {
            throw new FileNotFoundException("The selected image does not exist.", ImagePath);
        }

        if (operation is ImagingOperation.Write or ImagingOperation.Wipe && selected.Any(device => device.IsReadOnly))
        {
            throw new InvalidOperationException("One or more selected devices are read-only.");
        }
    }

    private void SelectStartupDevices()
    {
        foreach (var item in Devices)
        {
            item.IsSelected = _startupOptions.Devices.Any(id =>
                item.Id.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                item.Device.MountPoints.Any(mount => mount.TrimEnd(':').Equals(id.TrimEnd(':'), StringComparison.OrdinalIgnoreCase)));
        }
    }
}
