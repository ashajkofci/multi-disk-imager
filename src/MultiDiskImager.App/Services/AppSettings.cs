using System.Text.Json;

namespace MultiDiskImager.Services;

internal enum AppTheme
{
    System,
    Light,
    Dark
}

internal enum TitleExtra
{
    Nothing,
    Percent,
    CurrentSpeed,
    RemainingTime,
    ActiveDevice,
    ImageFileName
}

internal sealed record AppSettings
{
    public bool DisplayWriteWarnings { get; init; } = true;
    public bool EnableAnimations { get; init; } = true;
    public bool CheckForUpdatesOnStartup { get; init; } = true;
    public bool EnableSoundNotification { get; init; } = true;
    public bool AutoSelectSingleDevice { get; init; }
    public bool AutoCloseOnSuccess { get; init; }
    public bool ShowExternalHardDrives { get; init; }
    public bool OmitDrivesOverSize { get; init; }
    public double OmitDrivesThresholdGiB { get; init; } = 128;
    public bool AlwaysOnTop { get; init; }
    public AppTheme Theme { get; init; } = AppTheme.System;
    public TitleExtra TitleExtra { get; init; } = TitleExtra.Nothing;
    public string Language { get; init; } = "system";
    public string? LastFolderPath { get; init; }
    public string? UserSpecifiedFolder { get; init; }
    public bool UseUserSpecifiedFolder { get; init; }
    public IReadOnlyList<string> CustomPlaces { get; init; } = [];
}

internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;

    public SettingsStore()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _path = Path.Combine(root, "MultiDiskImager", "settings.json");
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new AppSettings();
            }

            await using var stream = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_path) ?? throw new InvalidOperationException("Settings path has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporary = _path + ".tmp";
        await using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
        {
            await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(temporary, _path, overwrite: true);
    }
}
