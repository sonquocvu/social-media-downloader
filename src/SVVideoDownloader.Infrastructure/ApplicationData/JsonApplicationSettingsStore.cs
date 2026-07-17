using System.Text.Json;
using SVVideoDownloader.Core.Videos;
using SVVideoDownloader.Infrastructure.Diagnostics;

namespace SVVideoDownloader.Infrastructure.ApplicationData;

public sealed class JsonApplicationSettingsStore(
    string settingsFilePath,
    IDiagnosticLogger logger) : IApplicationSettingsStore, IDisposable
{
    private readonly string _settingsFilePath = Path.GetFullPath(settingsFilePath);
    private readonly IDiagnosticLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<ApplicationSettings> LoadAsync(
        ApplicationSettings defaults,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(
                    () => LoadCore(defaults),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            await _logger.LogAsync(
                    DiagnosticLogLevel.Warning,
                    $"Không thể đọc cài đặt; dùng giá trị mặc định. {exception.GetType().Name}",
                    cancellationToken)
                .ConfigureAwait(false);
            return defaults;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        ApplicationSettings settings,
        CancellationToken cancellationToken = default)
    {
        Validate(settings);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await Task.Run(
                    () => JsonStoreHelpers.WriteAtomic(_settingsFilePath, settings),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private ApplicationSettings LoadCore(ApplicationSettings defaults)
    {
        if (!File.Exists(_settingsFilePath))
        {
            return defaults;
        }

        var json = File.ReadAllText(_settingsFilePath);
        var settings = JsonSerializer.Deserialize<ApplicationSettings>(
            json,
            JsonStoreHelpers.SerializerOptions);
        return IsValid(settings) ? settings! : defaults;
    }

    private static void Validate(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!IsValid(settings))
        {
            throw new ArgumentException(null, nameof(settings));
        }
    }

    private static bool IsValid(ApplicationSettings? settings) =>
        settings is not null &&
        !string.IsNullOrWhiteSpace(settings.DownloadDirectory) &&
        Path.IsPathFullyQualified(settings.DownloadDirectory) &&
        Enum.IsDefined(settings.DefaultQuality) &&
        settings.DefaultQuality is QualityPreset.Best or
            QualityPreset.Video1080p or
            QualityPreset.Video720p or
            QualityPreset.Video480p or
            QualityPreset.AudioMp3;

    private static bool IsStorageException(Exception exception) =>
        exception is IOException or
            UnauthorizedAccessException or
            JsonException or
            NotSupportedException;
}
