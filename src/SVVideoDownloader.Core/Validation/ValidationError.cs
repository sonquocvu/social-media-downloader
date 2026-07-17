namespace SVVideoDownloader.Core.Validation;

public sealed record ValidationError(ValidationErrorCode Code, string Field);
