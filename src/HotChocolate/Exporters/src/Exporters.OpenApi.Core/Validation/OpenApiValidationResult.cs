using System.Collections.Immutable;

namespace HotChocolate.Exporters.OpenApi.Validation;

/// <summary>
/// Represents the result of OpenAPI document validation.
/// </summary>
public sealed class OpenApiValidationResult
{
    private OpenApiValidationResult(bool isValid, ImmutableArray<OpenApiValidationError> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the validation errors, if any.
    /// </summary>
    public ImmutableArray<OpenApiValidationError> Errors { get; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static OpenApiValidationResult Success()
        => new(true, ImmutableArray<OpenApiValidationError>.Empty);

    /// <summary>
    /// Creates a validation result with errors.
    /// </summary>
    public static OpenApiValidationResult Failure(params OpenApiValidationError[] errors)
        => new(false, errors.ToImmutableArray());

    /// <summary>
    /// Creates a validation result with errors.
    /// </summary>
    public static OpenApiValidationResult Failure(IEnumerable<OpenApiValidationError> errors)
        => new(false, errors.ToImmutableArray());
}
