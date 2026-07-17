using System;

namespace SVVideoDownloader.Core.Media;

public sealed class MediaOperationResult
{
    private MediaOperationResult(MediaOperationError? error)
    {
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public MediaOperationError? Error { get; }

    public static MediaOperationResult Success() => new(null);

    public static MediaOperationResult Failure(MediaOperationError error) =>
        new(error ?? throw new ArgumentNullException(nameof(error)));
}

public sealed class MediaOperationResult<T>
    where T : class
{
    private MediaOperationResult(T? value, MediaOperationError? error)
    {
        Value = value;
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public T? Value { get; }

    public MediaOperationError? Error { get; }

    public static MediaOperationResult<T> Success(T value) =>
        new(value ?? throw new ArgumentNullException(nameof(value)), null);

    public static MediaOperationResult<T> Failure(MediaOperationError error) =>
        new(null, error ?? throw new ArgumentNullException(nameof(error)));
}
