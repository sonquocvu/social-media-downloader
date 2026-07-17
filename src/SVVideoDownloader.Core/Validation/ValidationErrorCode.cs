namespace SVVideoDownloader.Core.Validation;

public enum ValidationErrorCode
{
    Required,
    MalformedUrl,
    HttpsRequired,
    CredentialsNotAllowed,
    UnsupportedHost,
    InvalidValue,
    ValueOutOfRange,
    RightsConfirmationRequired,
    InvalidStatusTransition,
    ProgressNotAllowed,
}
