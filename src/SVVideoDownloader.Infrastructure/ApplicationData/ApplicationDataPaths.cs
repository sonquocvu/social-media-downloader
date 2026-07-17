using System;
using System.IO;

namespace SVVideoDownloader.Infrastructure.ApplicationData;

public sealed record ApplicationDataPaths
{
    private ApplicationDataPaths(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        SettingsFilePath = Path.Combine(rootDirectory, "settings.json");
        HistoryFilePath = Path.Combine(rootDirectory, "history.json");
        LogsDirectory = Path.Combine(rootDirectory, "logs");
        LogFilePath = Path.Combine(LogsDirectory, "svvideodownloader.log");
        ToolsDirectory = Path.Combine(rootDirectory, "tools");
    }

    public string RootDirectory { get; }

    public string SettingsFilePath { get; }

    public string HistoryFilePath { get; }

    public string LogsDirectory { get; }

    public string LogFilePath { get; }

    public string ToolsDirectory { get; }

    public static ApplicationDataPaths CreateDefault()
    {
        var localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        return Create(Path.Combine(localApplicationData, "SVVideoDownloader"));
    }

    public static ApplicationDataPaths Create(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        if (!Path.IsPathFullyQualified(rootDirectory))
        {
            throw new ArgumentException(null, nameof(rootDirectory));
        }

        return new ApplicationDataPaths(Path.GetFullPath(rootDirectory));
    }
}
