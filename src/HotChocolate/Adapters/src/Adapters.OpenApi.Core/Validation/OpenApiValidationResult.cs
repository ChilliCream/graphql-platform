using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Adapters.OpenApi;

public sealed class OpenApiValidationResult
{
    private OpenApiValidationResult(bool isValid, ImmutableArray<OpenApiValidationError>? errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Errors))]
    public bool IsValid { get; }

    /// <summary>
    /// Gets the validation errors, if any.
    /// </summary>
    public ImmutableArray<OpenApiValidationError>? Errors { get; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static OpenApiValidationResult Success()
        => new(true, null);

    /// <summary>
    /// Creates a validation result with errors.
    /// </summary>
    public static OpenApiValidationResult Failure(params OpenApiValidationError[] errors)
        => new(false, [..errors]);

    /// <summary>
    /// Creates a validation result with errors.
    /// </summary>
    public static OpenApiValidationResult Failure(IEnumerable<OpenApiValidationError> errors)
        => new(false, [..errors]);
}
