using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Adapters.OpenApi;

public sealed class OpenApiDefinitionValidationResult
{
    private OpenApiDefinitionValidationResult(bool isValid, ImmutableArray<OpenApiDefinitionValidationError>? errors)
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
    public ImmutableArray<OpenApiDefinitionValidationError>? Errors { get; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static OpenApiDefinitionValidationResult Success()
        => new(true, null);

    /// <summary>
    /// Creates a validation result with errors.
    /// </summary>
    public static OpenApiDefinitionValidationResult Failure(params OpenApiDefinitionValidationError[] errors)
        => new(false, [.. errors]);

    /// <summary>
    /// Creates a validation result with errors.
    /// </summary>
    public static OpenApiDefinitionValidationResult Failure(IEnumerable<OpenApiDefinitionValidationError> errors)
        => new(false, [.. errors]);
}
