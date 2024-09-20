using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.Caching.Memory;

namespace HotChocolate.PersistedOperations.FileSystem;

/// <summary>
/// An implementation of <see cref="IOperationDocumentStorage"/> that uses an in-memory cache.
/// </summary>
public class InMemoryOperationDocumentStorage : IOperationDocumentStorage
{
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    public InMemoryOperationDocumentStorage(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public ValueTask<IOperationDocument?> TryReadAsync(
        OperationDocumentId documentId,
        CancellationToken cancellationToken = default)
    {
        if (OperationDocumentId.IsNullOrEmpty(documentId))
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        return _cache.TryGetValue(documentId.Value, out OperationDocument? document)
            ? new ValueTask<IOperationDocument?>(document)
            : new ValueTask<IOperationDocument?>(default(IOperationDocument));
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(
        OperationDocumentId documentId,
        IOperationDocument document,
        CancellationToken cancellationToken = default)
    {
        if (OperationDocumentId.IsNullOrEmpty(documentId))
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        _cache.GetOrCreate<OperationDocument>(
            documentId.Value,
            _ =>
            {
                if (document is OperationDocument parsedDocument)
                {
                    return parsedDocument;
                }

                var documentNode = Utf8GraphQLParser.Parse(document.AsSpan());
                return new OperationDocument(documentNode);
            });

        return default;
    }
}
