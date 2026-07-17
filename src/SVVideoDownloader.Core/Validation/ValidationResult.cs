using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SVVideoDownloader.Core.Validation;

public sealed class ValidationResult
{
    private ValidationResult(IReadOnlyList<ValidationError> errors)
    {
        Errors = errors;
    }

    public bool IsSuccess => Errors.Count == 0;

    public IReadOnlyList<ValidationError> Errors { get; }

    public static ValidationResult Success() => new(Array.Empty<ValidationError>());

    public static ValidationResult Failure(params ValidationError[] errors) =>
        new(ToReadOnly(errors));

    private static ReadOnlyCollection<ValidationError> ToReadOnly(
        IEnumerable<ValidationError> errors) =>
        Array.AsReadOnly(errors.ToArray());
}

public sealed class ValidationResult<T>
    where T : class
{
    private ValidationResult(T? value, IReadOnlyList<ValidationError> errors)
    {
        Value = value;
        Errors = errors;
    }

    public bool IsSuccess => Errors.Count == 0;

    public T? Value { get; }

    public IReadOnlyList<ValidationError> Errors { get; }

    public static ValidationResult<T> Success(T value) =>
        new(value ?? throw new ArgumentNullException(nameof(value)), Array.Empty<ValidationError>());

    public static ValidationResult<T> Failure(params ValidationError[] errors) =>
        Failure((IEnumerable<ValidationError>)errors);

    public static ValidationResult<T> Failure(IEnumerable<ValidationError> errors) =>
        new(null, Array.AsReadOnly(errors.ToArray()));
}
