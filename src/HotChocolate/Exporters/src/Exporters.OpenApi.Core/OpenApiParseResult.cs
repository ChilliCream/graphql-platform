using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Exporters.OpenApi;

/// <summary>
/// Represents the result of parsing an OpenAPI document.
/// </summary>
public sealed class OpenApiParseResult
{
    private OpenApiParseResult(bool isValid, IOpenApiDocument? document, ImmutableArray<OpenApiParsingError> errors)
    {
        IsValid = isValid;
        Document = document;
        Errors = errors;
    }

    /// <summary>
    /// Gets a value indicating whether the parsing was successful.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Errors))]
    [MemberNotNullWhen(true, nameof(Document))]
    public bool IsValid { get; }

    /// <summary>
    /// Gets the parsed document, if parsing was successful.
    /// </summary>
    public IOpenApiDocument? Document { get; }

    /// <summary>
    /// Gets the parsing errors, if any.
    /// </summary>
    public ImmutableArray<OpenApiParsingError> Errors { get; }

    /// <summary>
    /// Creates a successful parse result.
    /// </summary>
    public static OpenApiParseResult Success(IOpenApiDocument document)
        => new(true, document, ImmutableArray<OpenApiParsingError>.Empty);

    /// <summary>
    /// Creates a parse result with errors.
    /// </summary>
    public static OpenApiParseResult Failure(params OpenApiParsingError[] errors)
        => new(false, null, [..errors]);

    /// <summary>
    /// Creates a parse result with errors.
    /// </summary>
    public static OpenApiParseResult Failure(IEnumerable<OpenApiParsingError> errors)
        => new(false, null, [..errors]);
}
