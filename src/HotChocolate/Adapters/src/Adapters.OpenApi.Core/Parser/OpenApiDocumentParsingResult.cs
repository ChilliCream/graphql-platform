using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Adapters.OpenApi;

/// <summary>
/// Represents the result of parsing an OpenAPI document.
/// </summary>
public sealed class OpenApiDocumentParsingResult
{
    private OpenApiDocumentParsingResult(bool isValid, IOpenApiDocument? document, ImmutableArray<OpenApiDocumentParsingError> errors)
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
    public ImmutableArray<OpenApiDocumentParsingError> Errors { get; }

    /// <summary>
    /// Creates a successful parse result.
    /// </summary>
    public static OpenApiDocumentParsingResult Success(IOpenApiDocument document)
        => new(true, document, []);

    /// <summary>
    /// Creates a parse result with errors.
    /// </summary>
    public static OpenApiDocumentParsingResult Failure(params OpenApiDocumentParsingError[] errors)
        => new(false, null, [.. errors]);

    /// <summary>
    /// Creates a parse result with errors.
    /// </summary>
    public static OpenApiDocumentParsingResult Failure(IEnumerable<OpenApiDocumentParsingError> errors)
        => new(false, null, [.. errors]);
}
