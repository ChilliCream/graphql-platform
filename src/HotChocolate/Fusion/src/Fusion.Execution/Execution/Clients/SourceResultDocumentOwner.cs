using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Holds the response document that the results of a split response share. The document is freed
/// once every result has released its reference.
/// </summary>
public sealed class SourceResultDocumentOwner : IDisposable
{
    private readonly SourceResultDocument _document;
    private int _references;

    /// <summary>
    /// Initializes a new instance of <see cref="SourceResultDocumentOwner"/>.
    /// </summary>
    /// <param name="document">The response document that the results share.</param>
    /// <param name="references">The number of results that hold <paramref name="document"/>.</param>
    public SourceResultDocumentOwner(SourceResultDocument document, int references)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(references);

        _document = document;
        _references = references;
    }

    /// <summary>
    /// Releases one reference to the document and frees the document with the last reference.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Decrement(ref _references) == 0)
        {
            _document.Dispose();
        }
    }
}
