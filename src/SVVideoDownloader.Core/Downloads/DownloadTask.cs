using System;
using SVVideoDownloader.Core.Validation;

namespace SVVideoDownloader.Core.Downloads;

public sealed class DownloadTask
{
    private DownloadTask(Guid id, DownloadRequest request)
    {
        Id = id;
        Request = request;
        Status = DownloadStatus.Pending;
        Progress = DownloadProgress.Empty;
    }

    public Guid Id { get; }

    public DownloadRequest Request { get; }

    public DownloadStatus Status { get; private set; }

    public DownloadProgress Progress { get; private set; }

    public static ValidationResult<DownloadTask> Create(DownloadRequest? request) =>
        Create(Guid.NewGuid(), request);

    public static ValidationResult<DownloadTask> Create(Guid id, DownloadRequest? request)
    {
        if (id == Guid.Empty)
        {
            return ValidationResult<DownloadTask>.Failure(
                new ValidationError(ValidationErrorCode.Required, "TaskId"));
        }

        if (request is null)
        {
            return ValidationResult<DownloadTask>.Failure(
                new ValidationError(ValidationErrorCode.Required, "Request"));
        }

        return ValidationResult<DownloadTask>.Success(new DownloadTask(id, request));
    }

    public ValidationResult TransitionTo(DownloadStatus nextStatus)
    {
        if (!Enum.IsDefined(nextStatus) || !CanTransition(Status, nextStatus))
        {
            return ValidationResult.Failure(
                new ValidationError(ValidationErrorCode.InvalidStatusTransition, "Status"));
        }

        Status = nextStatus;
        return ValidationResult.Success();
    }

    public ValidationResult UpdateProgress(DownloadProgress? progress)
    {
        if (progress is null)
        {
            return ValidationResult.Failure(
                new ValidationError(ValidationErrorCode.Required, "Progress"));
        }

        if (Status is not DownloadStatus.Downloading and not DownloadStatus.Processing)
        {
            return ValidationResult.Failure(
                new ValidationError(ValidationErrorCode.ProgressNotAllowed, "Progress"));
        }

        Progress = progress;
        return ValidationResult.Success();
    }

    private static bool CanTransition(DownloadStatus current, DownloadStatus next) =>
        (current, next) switch
        {
            (DownloadStatus.Pending, DownloadStatus.Analyzing) => true,
            (DownloadStatus.Pending, DownloadStatus.Cancelled) => true,

            (DownloadStatus.Analyzing, DownloadStatus.Ready) => true,
            (DownloadStatus.Analyzing, DownloadStatus.Failed) => true,
            (DownloadStatus.Analyzing, DownloadStatus.Cancelled) => true,

            (DownloadStatus.Ready, DownloadStatus.Downloading) => true,
            (DownloadStatus.Ready, DownloadStatus.Failed) => true,
            (DownloadStatus.Ready, DownloadStatus.Cancelled) => true,

            (DownloadStatus.Downloading, DownloadStatus.Processing) => true,
            (DownloadStatus.Downloading, DownloadStatus.Completed) => true,
            (DownloadStatus.Downloading, DownloadStatus.Failed) => true,
            (DownloadStatus.Downloading, DownloadStatus.Cancelled) => true,

            (DownloadStatus.Processing, DownloadStatus.Completed) => true,
            (DownloadStatus.Processing, DownloadStatus.Failed) => true,
            (DownloadStatus.Processing, DownloadStatus.Cancelled) => true,

            _ => false,
        };
}
