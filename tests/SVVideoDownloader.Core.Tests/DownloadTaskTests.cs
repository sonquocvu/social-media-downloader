using SVVideoDownloader.Core.Downloads;
using SVVideoDownloader.Core.Validation;
using SVVideoDownloader.Core.Videos;

namespace SVVideoDownloader.Core.Tests;

public sealed class DownloadTaskTests
{
    public static TheoryData<DownloadStatus, DownloadStatus> AllowedTransitions => new()
    {
        { DownloadStatus.Pending, DownloadStatus.Analyzing },
        { DownloadStatus.Pending, DownloadStatus.Cancelled },
        { DownloadStatus.Analyzing, DownloadStatus.Ready },
        { DownloadStatus.Analyzing, DownloadStatus.Failed },
        { DownloadStatus.Analyzing, DownloadStatus.Cancelled },
        { DownloadStatus.Ready, DownloadStatus.Downloading },
        { DownloadStatus.Ready, DownloadStatus.Failed },
        { DownloadStatus.Ready, DownloadStatus.Cancelled },
        { DownloadStatus.Downloading, DownloadStatus.Processing },
        { DownloadStatus.Downloading, DownloadStatus.Completed },
        { DownloadStatus.Downloading, DownloadStatus.Failed },
        { DownloadStatus.Downloading, DownloadStatus.Cancelled },
        { DownloadStatus.Processing, DownloadStatus.Completed },
        { DownloadStatus.Processing, DownloadStatus.Failed },
        { DownloadStatus.Processing, DownloadStatus.Cancelled },
    };

    [Theory]
    [MemberData(nameof(AllowedTransitions))]
    public void TransitionToAllowsDefinedTransition(
        DownloadStatus current,
        DownloadStatus next)
    {
        var task = CreateTaskAt(current);

        var result = task.TransitionTo(next);

        Assert.True(result.IsSuccess);
        Assert.Equal(next, task.Status);
    }

    [Theory]
    [InlineData(DownloadStatus.Pending, DownloadStatus.Ready)]
    [InlineData(DownloadStatus.Pending, DownloadStatus.Completed)]
    [InlineData(DownloadStatus.Analyzing, DownloadStatus.Downloading)]
    [InlineData(DownloadStatus.Ready, DownloadStatus.Completed)]
    [InlineData(DownloadStatus.Downloading, DownloadStatus.Ready)]
    [InlineData(DownloadStatus.Processing, DownloadStatus.Downloading)]
    [InlineData(DownloadStatus.Completed, DownloadStatus.Analyzing)]
    [InlineData(DownloadStatus.Failed, DownloadStatus.Analyzing)]
    [InlineData(DownloadStatus.Cancelled, DownloadStatus.Analyzing)]
    public void TransitionToRejectsUndefinedTransition(
        DownloadStatus current,
        DownloadStatus next)
    {
        var task = CreateTaskAt(current);

        var result = task.TransitionTo(next);

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ValidationErrorCode.InvalidStatusTransition, error.Code);
        Assert.Equal(current, task.Status);
    }

    [Theory]
    [InlineData(DownloadStatus.Completed)]
    [InlineData(DownloadStatus.Failed)]
    [InlineData(DownloadStatus.Cancelled)]
    public void TerminalStatesRejectEveryNextState(DownloadStatus terminalStatus)
    {
        var task = CreateTaskAt(terminalStatus);

        foreach (var next in Enum.GetValues<DownloadStatus>())
        {
            var result = task.TransitionTo(next);

            Assert.False(result.IsSuccess);
            Assert.Equal(terminalStatus, task.Status);
        }
    }

    [Fact]
    public void TransitionToRejectsSameState()
    {
        var task = CreateTaskAt(DownloadStatus.Ready);

        var result = task.TransitionTo(DownloadStatus.Ready);

        Assert.False(result.IsSuccess);
        Assert.Equal(DownloadStatus.Ready, task.Status);
    }

    [Fact]
    public void TransitionToRejectsUnknownState()
    {
        var task = CreateTaskAt(DownloadStatus.Pending);

        var result = task.TransitionTo((DownloadStatus)999);

        Assert.False(result.IsSuccess);
        Assert.Equal(DownloadStatus.Pending, task.Status);
    }

    [Theory]
    [InlineData(DownloadStatus.Downloading)]
    [InlineData(DownloadStatus.Processing)]
    public void UpdateProgressIsAllowedOnlyForActiveTransferStates(DownloadStatus status)
    {
        var task = CreateTaskAt(status);
        var progress = Assert.IsType<DownloadProgress>(
            DownloadProgress.Create(50, 500, 1_000, 100).Value);

        var result = task.UpdateProgress(progress);

        Assert.True(result.IsSuccess);
        Assert.Same(progress, task.Progress);
    }

    [Fact]
    public void UpdateProgressRejectedOutsideActiveTransferDoesNotMutateTask()
    {
        var task = CreateTaskAt(DownloadStatus.Ready);
        var initialProgress = task.Progress;
        var progress = Assert.IsType<DownloadProgress>(
            DownloadProgress.Create(50, 500, 1_000, 100).Value);

        var result = task.UpdateProgress(progress);

        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationErrorCode.ProgressNotAllowed, Assert.Single(result.Errors).Code);
        Assert.Same(initialProgress, task.Progress);
    }

    private static DownloadTask CreateTaskAt(DownloadStatus targetStatus)
    {
        var task = Assert.IsType<DownloadTask>(DownloadTask.Create(CreateRequest()).Value);
        foreach (var status in PathTo(targetStatus))
        {
            Assert.True(task.TransitionTo(status).IsSuccess);
        }

        return task;
    }

    private static DownloadRequest CreateRequest()
    {
        var source = Assert.IsType<VideoSource>(VideoSource.Create("https://youtu.be/owned-video").Value);
        var options = Assert.IsType<DownloadOptions>(
            DownloadOptions.Create(QualityPreset.Best, "owned-video.mp4").Value);
        return Assert.IsType<DownloadRequest>(DownloadRequest.Create(source, options, true).Value);
    }

    private static IEnumerable<DownloadStatus> PathTo(DownloadStatus targetStatus) => targetStatus switch
    {
        DownloadStatus.Pending => Array.Empty<DownloadStatus>(),
        DownloadStatus.Analyzing => new[] { DownloadStatus.Analyzing },
        DownloadStatus.Ready => new[] { DownloadStatus.Analyzing, DownloadStatus.Ready },
        DownloadStatus.Downloading =>
            new[] { DownloadStatus.Analyzing, DownloadStatus.Ready, DownloadStatus.Downloading },
        DownloadStatus.Processing =>
            new[]
            {
                DownloadStatus.Analyzing,
                DownloadStatus.Ready,
                DownloadStatus.Downloading,
                DownloadStatus.Processing,
            },
        DownloadStatus.Completed =>
            new[]
            {
                DownloadStatus.Analyzing,
                DownloadStatus.Ready,
                DownloadStatus.Downloading,
                DownloadStatus.Completed,
            },
        DownloadStatus.Failed => new[] { DownloadStatus.Analyzing, DownloadStatus.Failed },
        DownloadStatus.Cancelled => new[] { DownloadStatus.Cancelled },
        _ => throw new ArgumentOutOfRangeException(nameof(targetStatus), targetStatus, null),
    };
}
