using System;
using System.IO;
using System.Text.Json;
using SVVideoDownloader.Core.Downloads;

namespace SVVideoDownloader.Infrastructure.YtDlp;

internal sealed class YtDlpOutputParser
{
    public const string LinePrefix = "SVVD_OUTPUT:";

    public bool TryParse(
        string line,
        string expectedOutputDirectory,
        out DownloadResult? result)
    {
        result = null;
        if (!line.StartsWith(LinePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(line[LinePrefix.Length..]);
            if (document.RootElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var outputPath = document.RootElement.GetString();
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return false;
            }

            var expectedRoot = Path.TrimEndingDirectorySeparator(
                Path.GetFullPath(expectedOutputDirectory));
            var fullOutputPath = Path.GetFullPath(outputPath);
            var actualDirectory = Path.GetDirectoryName(fullOutputPath);
            if (!string.Equals(
                    actualDirectory,
                    expectedRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var fileNameResult = DownloadResult.Create(Path.GetFileName(fullOutputPath));
            result = fileNameResult.Value;
            return fileNameResult.IsSuccess;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
