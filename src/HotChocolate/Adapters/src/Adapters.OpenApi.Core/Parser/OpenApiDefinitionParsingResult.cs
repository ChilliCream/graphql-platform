using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Represents the result of parsing an OpenAPI document.
/// </summary>
public sealed class OpenApiDefinitionParsingResult
{
    private OpenApiDefinitionParsingResult(bool isValid, IOpenApiDefinition? definition, ImmutableArray<OpenApiDefinitionParsingError> errors)
    {
        IsValid = isValid;
        Definition = definition;
        Errors = errors;
    }

    /// <summary>
    /// Gets a value indicating whether the parsing was successful.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Errors))]
    [MemberNotNullWhen(true, nameof(Definition))]
    public bool IsValid { get; }

    /// <summary>
    /// Gets the parsed definition, if parsing was successful.
    /// </summary>
    public IOpenApiDefinition? Definition { get; }

    /// <summary>
    /// Gets the parsing errors, if any.
    /// </summary>
    public ImmutableArray<OpenApiDefinitionParsingError> Errors { get; }

    /// <summary>
    /// Creates a successful parse result.
    /// </summary>
    public static OpenApiDefinitionParsingResult Success(IOpenApiDefinition definition)
        => new(true, definition, []);

    /// <summary>
    /// Creates a parse result with errors.
    /// </summary>
    public static OpenApiDefinitionParsingResult Failure(params OpenApiDefinitionParsingError[] errors)
        => new(false, null, [.. errors]);

    /// <summary>
    /// Creates a parse result with errors.
    /// </summary>
    public static OpenApiDefinitionParsingResult Failure(IEnumerable<OpenApiDefinitionParsingError> errors)
        => new(false, null, [.. errors]);
}
