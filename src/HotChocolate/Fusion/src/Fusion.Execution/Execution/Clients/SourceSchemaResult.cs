using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Represents the result returned by a source schema after executing a GraphQL request. Provides
/// access to the <c>data</c>, <c>errors</c>, and <c>extensions</c> sections of the response.
/// </summary>
public sealed class SourceSchemaResult : IDisposable
{
    private static ReadOnlySpan<byte> DataProperty => "data"u8;
    private static ReadOnlySpan<byte> ErrorsProperty => "errors"u8;
    private static ReadOnlySpan<byte> ExtensionsProperty => "extensions"u8;
    private readonly SourceResultDocument _document;
    private readonly IDisposable? _documentOwner;
    private SourceSchemaErrors? _errors;
    private bool _errorsParsed;

    /// <summary>
    /// Creates a new <see cref="SourceSchemaResult"/> that holds the given document and releases
    /// its hold when this result is disposed.
    /// </summary>
    /// <param name="path">The path in the Fusion result where this result will be merged.</param>
    /// <param name="document">The raw response document from the source schema.</param>
    /// <param name="documentOwner">
    /// The owner of <paramref name="document"/> when it is shared with other results, or
    /// <c>null</c> when this result is the only one that reads it.
    /// </param>
    /// <param name="lookupData">
    /// The pre-resolved element of the root field that carries the lookup data, or <c>null</c>
    /// when it is resolved from <see cref="Data"/>.
    /// </param>
    /// <param name="errors">The errors that apply to this result, or <c>null</c> if there are none.</param>
    /// <param name="final">Whether this is the final message in a streaming response.</param>
    /// <param name="additionalPaths">Any additional paths where this result should also be merged.</param>
    public SourceSchemaResult(
        CompactPath path,
        SourceResultDocument document,
        SourceResultDocumentOwner? documentOwner = null,
        SourceResultElement? lookupData = null,
        SourceSchemaErrors? errors = null,
        FinalMessage final = FinalMessage.Undefined,
        CompactPathSegment additionalPaths = default)
        : this(
            path,
            document,
            final,
            documentOwner ?? (IDisposable)document,
            additionalPaths,
            lookupData,
            errors,
            // The errors of a shared document are routed per result, so a result that shares one
            // never reads its errors from the document.
            errorsParsed: errors is not null || documentOwner is not null)
    {
    }

    private SourceSchemaResult(
        CompactPath path,
        SourceResultDocument document,
        FinalMessage final,
        IDisposable? documentOwner,
        CompactPathSegment additionalPaths,
        SourceResultElement? lookupData,
        SourceSchemaErrors? errors,
        bool errorsParsed)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;
        _documentOwner = documentOwner;
        _errors = errors;
        _errorsParsed = errorsParsed;
        AdditionalPaths = additionalPaths;
        LookupData = lookupData;
        Path = path;
        Final = final;
    }

    /// <summary>
    /// Gets the primary path in the composite result into which this source schema result will be merged.
    /// </summary>
    public CompactPath Path { get; }

    /// <summary>
    /// Gets the additional paths in the composite result into which this source schema result will
    /// also be merged.
    /// </summary>
    public CompactPathSegment AdditionalPaths { get; }

    /// <summary>
    /// Gets the <c>data</c> element of the source schema response, or an empty element if the
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
    /// Gets the pre-resolved element of the root field that carries the lookup data, or <c>null</c>
    /// when it is resolved from <see cref="Data"/>. When set, it stands in for the first segment of
    /// the source path.
    /// </summary>
    public SourceResultElement? LookupData { get; }

    /// <summary>
    /// Gets the errors of the source schema response that apply to this result, or <c>null</c>
    /// when there are none.
    /// </summary>
    public SourceSchemaErrors? Errors
    {
        get
        {
            if (!_errorsParsed)
            {
                _errors = _document.Root.TryGetProperty(ErrorsProperty, out var errors)
                    ? SourceSchemaErrors.From(errors)
                    : null;
                _errorsParsed = true;
            }

            return _errors;
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the source schema response carries errors that apply to this result.
    /// </summary>
    public bool HasErrors => Errors is not null;

    /// <summary>
    /// Gets the <c>extensions</c> element of the source schema response, or an empty element if
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

    internal SourceSchemaResult WithPath(CompactPath path, CompactPathSegment additionalPaths)
        => new(
            path,
            _document,
            Final,
            documentOwner: null,
            additionalPaths,
            LookupData,
            _errors,
            _errorsParsed);

    /// <summary>
    /// Releases this result's hold on the underlying result document.
    /// </summary>
    public void Dispose() => _documentOwner?.Dispose();
}
