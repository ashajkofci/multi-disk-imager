using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Styling;
using MultiDiskImager.Core;
using MultiDiskImager.Localization;
using MultiDiskImager.Platform;
using MultiDiskImager.Privileged;
using MultiDiskImager.Services;

namespace MultiDiskImager.ViewModels;

internal sealed class MainWindowViewModel : ObservableObject
{
    private const string ProductName = "bNovate Multi Disk Imager";
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
    private string _status = Localizer.Get("Ready");
    private string _checksum = string.Empty;
    private ChecksumAlgorithm _checksumAlgorithm = ChecksumAlgorithm.Sha256;
    private double _progress;
    private string _progressText = string.Empty;
    private IReadOnlyDictionary<string, IReadOnlyList<double>> _speedSeries = new Dictionary<string, IReadOnlyList<double>>();
    private readonly Dictionary<string, double> _deviceFractions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ImagingProgress> _latestDeviceProgress = new(StringComparer.Ordinal);
    private double _overallFraction;
    private ImagingOperation? _progressOperation;
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
    public ObservableCollection<DeviceProgressViewModel> DeviceProgress { get; } = [];
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

    public IReadOnlyDictionary<string, IReadOnlyList<double>> SpeedSeries
    {
        get => _speedSeries;
        private set => SetProperty(ref _speedSeries, value);
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
        TitleExtra.Percent when _lastProgress is not null => $"{_overallFraction:P0} — {ProductName}",
        TitleExtra.CurrentSpeed when _lastProgress is not null => $"{ByteSize.Format((long)_lastProgress.BytesPerSecond)}/s — {ProductName}",
        TitleExtra.RemainingTime when _lastProgress?.Remaining is { } remaining => $"{remaining:hh\\:mm\\:ss} — {ProductName}",
        TitleExtra.ActiveDevice when SelectedDevices.FirstOrDefault() is { } device => $"{device.Id} — {ProductName}",
        TitleExtra.ImageFileName when !string.IsNullOrWhiteSpace(ImagePath) => $"{Path.GetFileName(ImagePath)} — {ProductName}",
        _ => ProductName
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

    public void SelectAllDevices()
    {
        if (IsBusy)
        {
            return;
        }

        foreach (var device in Devices)
        {
            device.IsSelected = true;
        }
    }

    public async Task InitializeAsync()
    {
        await RefreshDevicesAsync();
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
        Status = Localizer.Get("RefreshingDevices");
        var selected = Devices.Where(item => item.IsSelected).Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        try
        {
            var devices = await _catalog.GetDevicesAsync();
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
                ? OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() ? Localizer.Get("NoEligibleDevices") : Localizer.Get("UnsupportedPlatform")
                : Localizer.Format("DevicesAvailable", visible.Length);
        }
        catch (Exception exception)
        {
            Status = $"{Localizer.Get("DeviceRefreshFailed")}: {exception.Message}";
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
        ProgressText = Localizer.Get("Preparing");
        Checksum = string.Empty;
        var selected = SelectedDevices;
        DeviceProgress.Clear();
        _deviceFractions.Clear();
        _latestDeviceProgress.Clear();
        _overallFraction = 0;
        _progressOperation = operation;
        foreach (var device in selected)
        {
            _deviceFractions[device.Id] = 0;
            DeviceProgress.Add(new DeviceProgressViewModel(device.Id)
            {
                Percentage = 0,
                OperationName = OperationName(operation),
                Details = "0%"
            });
        }
        SpeedSeries = new Dictionary<string, IReadOnlyList<double>>();
        _operationCancellation = new CancellationTokenSource();

        try
        {
            var progress = new Progress<ImagingProgress>(UpdateProgress);
            var request = new HelperRequest(operation, ImagePath, selected, VerifyAfter, OnlyAllocated, allowCrop, byteCount);
            var result = await PrivilegedHelperClient.RunAsync(request, progress, _operationCancellation.Token);
            Status = result.Canceled
                ? Localizer.Get("OperationCanceledWarning")
                : result.Success
                    ? Localizer.Get("CompletedSuccessfully")
                    : result.PartialSuccess
                        ? Localizer.Get("PartialCompletion")
                        : Localizer.Get("Failed");
            return result;
        }
        catch (OperationCanceledException)
        {
            Status = Localizer.Get("OperationCanceledWarning");
            return new ImagingJobResult(operation, true, selected.Select(device => new DeviceOperationResult(device.Id, false, 0, "Canceled")).ToArray());
        }
        catch (Exception exception)
        {
            Status = $"{Localizer.Get("Failed")}: {exception.Message}";
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
            throw new FileNotFoundException(Localizer.Get("SelectExistingImage"), ImagePath);
        }

        IsBusy = true;
        _operationCancellation = new CancellationTokenSource();
        try
        {
            var imageSource = DiskImageSource.Open(ImagePath);
            await using var stream = imageSource.OpenRead();
            var progress = new Progress<double>(value =>
            {
                Progress = value * 100;
                ProgressText = $"{Localizer.Get("Checksum")} {value:P0}";
            });
            Checksum = await ChecksumService.ComputeAsync(stream, ChecksumAlgorithm, progress, _operationCancellation.Token, imageSource.Length);
            Status = $"{ChecksumAlgorithm}: {Localizer.Get("Success")}";
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
        var imageSource = DiskImageSource.Open(ImagePath);
        await using var image = imageSource.OpenRead();
        return await new ImagingEngine().TailContainsNonZeroAsync(image, deviceSize);
    }

    public long GetImageSize() => DiskImageSource.Open(ImagePath).Length;

    public async Task ApplySettingsAsync(AppSettings settings)
    {
        _settings = settings;
        RaisePropertyChanged(nameof(Settings));
        RaisePropertyChanged(nameof(WindowTitle));
        await _settingsStore.SaveAsync(settings);
        if (Application.Current is { } application)
        {
            application.RequestedThemeVariant = settings.Theme switch
            {
                AppTheme.Light => ThemeVariant.Light,
                AppTheme.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }

        await RefreshDevicesAsync();
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
        await _settingsStore.SaveAsync(_settings);
    }

    public async Task CheckForUpdatesAsync()
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            AvailableUpdate = await new UpdateService(httpClient).CheckAsync();
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
        if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateProgress(value));
            return;
        }

        if (_progressOperation != value.Operation)
        {
            _progressOperation = value.Operation;
            _latestDeviceProgress.Clear();
            SpeedSeries = new Dictionary<string, IReadOnlyList<double>>();
            foreach (var deviceId in _deviceFractions.Keys.ToArray())
            {
                _deviceFractions[deviceId] = 0;
            }
        }

        var targetIds = value.DeviceId is null && _deviceFractions.Count > 0
            ? _deviceFractions.Keys.ToArray()
            : [value.DeviceId ?? "Operation"];
        foreach (var deviceId in targetIds)
        {
            _deviceFractions[deviceId] = Math.Max(_deviceFractions.GetValueOrDefault(deviceId), value.Fraction);
            _latestDeviceProgress[deviceId] = value;
        }

        _overallFraction = _deviceFractions.Count == 0 ? value.Fraction : _deviceFractions.Values.Min();
        var totalSpeed = value.DeviceId is null
            ? value.Fraction < 1 ? value.BytesPerSecond : 0
            : _latestDeviceProgress.Values.Where(item => item.Fraction < 1).Sum(item => item.BytesPerSecond);
        var overallRemaining = value.DeviceId is null
            ? value.Remaining ?? TimeSpan.Zero
            : _latestDeviceProgress.Values.Select(item => item.Remaining).Where(item => item.HasValue).Select(item => item!.Value).DefaultIfEmpty().Max();
        var overallBytes = (long)Math.Round(_overallFraction * value.TotalBytes);
        _lastProgress = value with { BytesProcessed = overallBytes, BytesPerSecond = totalSpeed, Remaining = overallRemaining };
        RaisePropertyChanged(nameof(WindowTitle));
        Progress = _overallFraction * 100;
        var remaining = overallRemaining > TimeSpan.Zero ? $" • {overallRemaining:hh\\:mm\\:ss} {Localizer.Get("RemainingLabel")}" : string.Empty;
        var operationName = OperationName(value.Operation);
        var overallStage = _overallFraction >= 1 ? $"{operationName} — {Localizer.Get("CompleteStage")}" : value.Stage;
        ProgressText = value.TotalBytes <= 0
            ? overallStage
            : $"{overallStage} • {_overallFraction:P0} • {ByteSize.Format(overallBytes)} / {ByteSize.Format(value.TotalBytes)} • {ByteSize.Format((long)totalSpeed)}/s{remaining}";

        var updated = SpeedSeries.ToDictionary(pair => pair.Key, pair => pair.Value.ToList(), StringComparer.Ordinal);
        foreach (var deviceId in targetIds)
        {
            var item = DeviceProgress.FirstOrDefault(candidate => candidate.DeviceId == deviceId);
            if (item is null)
            {
                item = new DeviceProgressViewModel(deviceId);
                DeviceProgress.Add(item);
            }
            item.OperationName = operationName;
            item.Percentage = value.Fraction * 100;
            item.Details = value.BytesProcessed == 0 && value.BytesPerSecond == 0 && value.Stage != "Complete"
                ? value.Stage
                : $"{value.Fraction:P0} • {ByteSize.Format((long)value.BytesPerSecond)}/s";

            var samples = updated.TryGetValue(deviceId, out var existing)
                ? existing
                : Enumerable.Repeat(double.NaN, 101).ToList();
            samples[Math.Clamp((int)Math.Round(value.Fraction * 100), 0, 100)] = value.Fraction < 1 ? value.BytesPerSecond : 0;

            updated[deviceId] = samples;
        }
        SpeedSeries = updated.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<double>)pair.Value, StringComparer.Ordinal);
    }

    private static string OperationName(ImagingOperation operation) => operation switch
    {
        ImagingOperation.Read => Localizer.Get("OperationRead"),
        ImagingOperation.Write => Localizer.Get("OperationWrite"),
        ImagingOperation.Verify => Localizer.Get("OperationVerify"),
        ImagingOperation.Wipe => Localizer.Get("QuickWipe"),
        _ => operation.ToString()
    };

    private void Validate(ImagingOperation operation)
    {
        var selected = SelectedDevices;
        if (selected.Count == 0)
        {
            throw new InvalidOperationException(Localizer.Get("SelectAtLeastOneDevice"));
        }

        if (selected.Any(device => device.IsSystem))
        {
            throw new InvalidOperationException(Localizer.Get("SystemDiskProtected"));
        }

        if (operation == ImagingOperation.Read && selected.Count != 1)
        {
            throw new InvalidOperationException(Localizer.Get("ReadRequiresOne"));
        }

        if (operation != ImagingOperation.Wipe && string.IsNullOrWhiteSpace(ImagePath))
        {
            throw new InvalidOperationException(Localizer.Get("SelectImagePath"));
        }

        if (operation is ImagingOperation.Write or ImagingOperation.Verify && !File.Exists(ImagePath))
        {
            throw new FileNotFoundException(Localizer.Get("ImageDoesNotExist"), ImagePath);
        }

        if (operation is ImagingOperation.Write or ImagingOperation.Wipe && selected.Any(device => device.IsReadOnly))
        {
            throw new InvalidOperationException(Localizer.Get("ReadOnlySelected"));
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
