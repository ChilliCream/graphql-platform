using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Adapters.OpenApi;

public sealed class OpenApiDocumentValidationResult
{
    private OpenApiDocumentValidationResult(bool isValid, ImmutableArray<OpenApiDocumentValidationError>? errors)
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
    public ImmutableArray<OpenApiDocumentValidationError>? Errors { get; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static OpenApiDocumentValidationResult Success()
        => new(true, null);

    /// <summary>
    /// Creates a validation result with errors.
    /// </summary>
    public static OpenApiDocumentValidationResult Failure(params OpenApiDocumentValidationError[] errors)
        => new(false, [.. errors]);

    /// <summary>
    /// Creates a validation result with errors.
    /// </summary>
    public static OpenApiDocumentValidationResult Failure(IEnumerable<OpenApiDocumentValidationError> errors)
        => new(false, [.. errors]);
}
