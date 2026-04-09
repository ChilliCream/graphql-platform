using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Represents the result returned by a source schema after executing a GraphQL request.
/// Provides access to the <c>data</c>, <c>errors</c>, and <c>extensions</c> sections of the
/// response and manages the lifetime of the underlying result document.
/// </summary>
public sealed class SourceSchemaResult : IDisposable
{
    private static ReadOnlySpan<byte> DataProperty => "data"u8;
    private static ReadOnlySpan<byte> ErrorsProperty => "errors"u8;
    private static ReadOnlySpan<byte> ExtensionsProperty => "extensions"u8;
    private readonly SourceResultDocument _document;
    private readonly bool _ownsDocument;
    private bool _errorsParsed;

    /// <summary>
    /// Creates a new <see cref="SourceSchemaResult"/> that takes ownership of the given document
    /// and will dispose it when this result is disposed.
    /// </summary>
    /// <param name="path">The path in the Fusion result where this result will be merged.</param>
    /// <param name="document">The raw response document from the source schema.</param>
    /// <param name="final">Whether this is the final message in a streaming response.</param>
    /// <param name="additionalPaths">Any additional paths where this result should also be merged.</param>
    public SourceSchemaResult(
        CompactPath path,
        SourceResultDocument document,
        FinalMessage final = FinalMessage.Undefined,
        CompactPathSegment additionalPaths = default)
        : this(path, document, final, ownsDocument: true, additionalPaths)
    {
    }

    private SourceSchemaResult(
        CompactPath path,
        SourceResultDocument document,
        FinalMessage final,
        bool ownsDocument,
        CompactPathSegment additionalPaths)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;
        _ownsDocument = ownsDocument;
        AdditionalPaths = additionalPaths;
        Path = path;
        Final = final;
    }

    /// <summary>
    /// The primary path in the composite result into which this source schema result will be merged.
    /// </summary>
    public CompactPath Path { get; }

    /// <summary>
    /// Additional paths where this result should also be merged, used when a single source
    /// schema response satisfies multiple selection sets at different locations.
    /// </summary>
    public CompactPathSegment AdditionalPaths { get; }

    /// <summary>
    /// The <c>data</c> element of the source schema response, or an empty element if the
    /// response did not include a data property.
    /// </summary>
    public SourceResultElement Data
    {
        get
        {
            _document.Root.TryGetProperty(DataProperty, out var data);
            return data;
        }
    }

    /// <summary>
    /// The parsed errors from the source schema response, or <c>null</c> if there were none.
    /// Parsed lazily on first access.
    /// </summary>
    public SourceSchemaErrors? Errors
    {
        get
        {
            if (!_errorsParsed)
            {
                field = _document.Root.TryGetProperty(ErrorsProperty, out var errors)
                    ? SourceSchemaErrors.From(errors)
                    : null;
                _errorsParsed = true;
            }

            return field;
        }
    }

    internal SourceResultElement RawErrors
    {
        get
        {
            _document.Root.TryGetProperty(ErrorsProperty, out var errors);
            return errors;
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the source schema response contains an <c>errors</c> property.
    /// </summary>
    public bool HasErrors => _document.Root.TryGetProperty(ErrorsProperty, out _);

    /// <summary>
    /// The <c>extensions</c> element of the source schema response, or an empty element if
    /// the response did not include an extensions property.
    /// </summary>
    public SourceResultElement Extensions
    {
        get
        {
            _document.Root.TryGetProperty(ExtensionsProperty, out var extensions);
            return extensions;
        }
    }

    /// <summary>
    /// Indicates whether this result represents the final message in a streaming response.
    /// </summary>
    public FinalMessage Final { get; }

    /// <summary>
    /// Creates a copy of this result associated with a different path, without taking ownership
    /// of the underlying document. Used internally when the same result needs to be referenced
    /// at a different location in the composite result.
    /// </summary>
    internal SourceSchemaResult WithPath(CompactPath path)
        => new(path, _document, Final, ownsDocument: false, additionalPaths: default);

    internal SourceSchemaResult WithPath(CompactPath path, CompactPathSegment additionalPaths)
        => new(path, _document, Final, ownsDocument: false, additionalPaths);

    /// <summary>
    /// Disposes the underlying result document if this instance owns it.
    /// </summary>
    public void Dispose()
    {
        if (_ownsDocument)
        {
            _document.Dispose();
        }
    }
}
