using System;
using System.Collections.Generic;
using System.IO;
using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Videos;
using SVVideoDownloader.Infrastructure.ExternalTools;
using SVVideoDownloader.Infrastructure.Processes;

namespace SVVideoDownloader.Infrastructure.YtDlp;

internal static class YtDlpCommandBuilder
{
    private const string ProgressTemplate =
        "download:" + YtDlpProgressParser.LinePrefix + "%(progress)j";
    private const string Mp4FormatSort =
        "vcodec:h264,lang,quality,res,fps,hdr:12,acodec:aac";

    public static ProcessRequest BuildMetadataRequest(
        ExternalToolOptions options,
        VideoSource source)
    {
        var arguments = new[]
        {
            "--ignore-config",
            "--dump-single-json",
            "--skip-download",
            "--no-playlist",
            "--no-warnings",
            "--no-colors",
            "--",
            source.Uri.AbsoluteUri,
        };

        return new ProcessRequest(
            options.YtDlpPath,
            arguments,
            options.MetadataTimeout);
    }

    public static ProcessRequest BuildDownloadRequest(
        ExternalToolOptions options,
        DownloadRequest request)
    {
        var arguments = new List<string>
        {
            "--ignore-config",
            "--no-playlist",
            "--newline",
            "--progress",
            "--no-colors",
            "--progress-template",
            ProgressTemplate,
            "--print",
            "after_move:" + YtDlpOutputParser.LinePrefix + "%(filepath)j",
            "--ffmpeg-location",
            Path.GetDirectoryName(options.FfmpegPath)!,
            "--format",
            GetFormatSelector(request.Options.QualityPreset),
        };

        if (request.Options.QualityPreset == QualityPreset.AudioMp3)
        {
            arguments.Add("--extract-audio");
            arguments.Add("--audio-format");
            arguments.Add("mp3");
            arguments.Add("--audio-quality");
            arguments.Add("0");
        }
        else
        {
            arguments.Add("--format-sort");
            arguments.Add(Mp4FormatSort);
            arguments.Add("--recode-video");
            arguments.Add("mp4");
        }

        arguments.Add("--output");
        arguments.Add(BuildOutputTemplate(options.OutputDirectory, request.Options.OutputFileName));
        arguments.Add("--");
        arguments.Add(request.Source.Uri.AbsoluteUri);

        return new ProcessRequest(
            options.YtDlpPath,
            arguments,
            options.DownloadTimeout,
            maximumCapturedCharacters: 512 * 1024);
    }

    private static string GetFormatSelector(QualityPreset preset) => preset switch
    {
        QualityPreset.Best => "bestvideo*+bestaudio/best",
        QualityPreset.Video1080p =>
            "bestvideo*[height<=1080]+bestaudio/best[height<=1080]",
        QualityPreset.Video720p =>
            "bestvideo*[height<=720]+bestaudio/best[height<=720]",
        QualityPreset.Video480p =>
            "bestvideo*[height<=480]+bestaudio/best[height<=480]",
        QualityPreset.AudioMp3 => "bestaudio/best",
        _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null),
    };

    private static string BuildOutputTemplate(string outputDirectory, string outputFileName)
    {
        var fileName = Path.GetFileName(outputFileName);
        var stem = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = fileName.Trim('.');
        }

        var literalPath = Path.Combine(outputDirectory, stem);
        var escapedLiteralPath = literalPath.Replace("%", "%%", StringComparison.Ordinal);
        return $"{escapedLiteralPath}.%(ext)s";
    }
}
