using System;
using System.Collections.Generic;
using System.Text.Json;
using SVVideoDownloader.Core.Media;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Infrastructure.YtDlp;

internal sealed class YtDlpMetadataParser
{
    public MediaOperationResult<VideoInfo> Parse(VideoSource source, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return InvalidResponse();
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || IsPlaylist(root))
            {
                return InvalidResponse();
            }

            var title = GetString(root, "title");
            var author = GetString(root, "uploader") ?? GetString(root, "channel");
            var duration = GetDuration(root);
            var formats = GetFormats(root);
            var infoResult = VideoInfo.Create(source, title, author, duration, formats);

            return infoResult.IsSuccess
                ? MediaOperationResult<VideoInfo>.Success(infoResult.Value!)
                : InvalidResponse();
        }
        catch (JsonException)
        {
            return InvalidResponse();
        }
        catch (OverflowException)
        {
            return InvalidResponse();
        }
    }

    private static bool IsPlaylist(JsonElement root) =>
        GetString(root, "_type") is { } type &&
        string.Equals(type, "playlist", StringComparison.OrdinalIgnoreCase);

    private static TimeSpan? GetDuration(JsonElement root)
    {
        if (!root.TryGetProperty("duration", out var durationElement) ||
            durationElement.ValueKind != JsonValueKind.Number ||
            !durationElement.TryGetDouble(out var seconds) ||
            !double.IsFinite(seconds))
        {
            return null;
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private static IReadOnlyList<VideoFormat> GetFormats(JsonElement root)
    {
        var formats = new List<VideoFormat>();
        if (!root.TryGetProperty("formats", out var formatsElement) ||
            formatsElement.ValueKind != JsonValueKind.Array)
        {
            return formats;
        }

        foreach (var element in formatsElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var hasVideo = HasCodec(element, "vcodec");
            var hasAudio = HasCodec(element, "acodec");
            var size = GetNullableInt64(element, "filesize") ??
                GetNullableInt64(element, "filesize_approx");
            var formatResult = VideoFormat.Create(
                GetString(element, "format_id"),
                GetString(element, "ext"),
                hasVideo,
                hasAudio,
                GetNullableInt32(element, "width"),
                GetNullableInt32(element, "height"),
                size);

            if (formatResult.IsSuccess)
            {
                formats.Add(formatResult.Value!);
            }
        }

        return formats;
    }

    private static bool HasCodec(JsonElement element, string propertyName)
    {
        var codec = GetString(element, propertyName);
        return !string.IsNullOrWhiteSpace(codec) &&
            !string.Equals(codec, "none", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static int? GetNullableInt32(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Number ||
            !property.TryGetInt32(out var value))
        {
            return null;
        }

        return value;
    }

    private static long? GetNullableInt64(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Number ||
            !property.TryGetInt64(out var value))
        {
            return null;
        }

        return value;
    }

    private static MediaOperationResult<VideoInfo> InvalidResponse() =>
        MediaOperationResult<VideoInfo>.Failure(
            new MediaOperationError(
                MediaErrorCategory.InvalidResponse,
                MediaComponent.MetadataExtractor));
}
