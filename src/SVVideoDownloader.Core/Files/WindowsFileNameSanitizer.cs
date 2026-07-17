using System;
using System.Collections.Generic;
using System.Text;
using SVVideoDownloader.Core.Validation;

namespace SVVideoDownloader.Core.Files;

public static class WindowsFileNameSanitizer
{
    private const char ReplacementCharacter = '_';
    private const string InvalidCharacters = "<>:\"/\\|?*";

    private static readonly HashSet<string> ReservedNames = new(
        new[]
        {
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM1",
            "COM2",
            "COM3",
            "COM¹",
            "COM²",
            "COM³",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT¹",
            "LPT²",
            "LPT³",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9",
            "CONIN$",
            "CONOUT$",
        },
        StringComparer.OrdinalIgnoreCase);

    public static ValidationResult<string> Sanitize(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return ValidationResult<string>.Failure(
                new ValidationError(ValidationErrorCode.Required, "OutputFileName"));
        }

        var trimmed = candidate.Trim();
        var builder = new StringBuilder(trimmed.Length);

        foreach (var character in trimmed)
        {
            builder.Append(IsInvalid(character) ? ReplacementCharacter : character);
        }

        var sanitized = builder.ToString().TrimEnd(' ', '.');
        if (sanitized.Length == 0)
        {
            return ValidationResult<string>.Failure(
                new ValidationError(ValidationErrorCode.InvalidValue, "OutputFileName"));
        }

        if (IsReserved(sanitized))
        {
            sanitized = $"{ReplacementCharacter}{sanitized}";
        }

        return ValidationResult<string>.Success(sanitized);
    }

    private static bool IsInvalid(char character) =>
        character < ' ' || InvalidCharacters.IndexOf(character) >= 0;

    private static bool IsReserved(string fileName)
    {
        var firstDot = fileName.IndexOf('.');
        var baseName = firstDot >= 0 ? fileName[..firstDot] : fileName;
        return ReservedNames.Contains(baseName.TrimEnd(' ', '.'));
    }
}
