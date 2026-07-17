using System.Text.Json;
using System.Text.Json.Serialization;

namespace SVVideoDownloader.Infrastructure.ApplicationData;

internal static class JsonStoreHelpers
{
    public static JsonSerializerOptions SerializerOptions { get; } = CreateOptions();

    public static void WriteAtomic<T>(string targetPath, T value)
    {
        var directory = Path.GetDirectoryName(targetPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            var json = JsonSerializer.Serialize(value, SerializerOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
