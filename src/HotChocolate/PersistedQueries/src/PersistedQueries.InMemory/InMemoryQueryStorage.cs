using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.Caching.Memory;

namespace HotChocolate.PersistedQueries.FileSystem;

/// <summary>
/// An implementation of <see cref="IReadStoredQueries"/>
/// and <see cref="IWriteStoredQueries"/> that
/// uses the local file system.
/// </summary>
public class InMemoryQueryStorage
    : IReadStoredQueries
    , IWriteStoredQueries
{
    private static readonly Task<OperationDocument?> _null = Task.FromResult<OperationDocument?>(null);
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    public InMemoryQueryStorage(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public Task<OperationDocument?> TryReadQueryAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        if (_cache.TryGetValue(documentId, out Task<OperationDocument?>? queryDocumentTask))
        {
            return queryDocumentTask ?? _null;
        }

        return _null;
    }


    /// <inheritdoc />
    public Task WriteQueryAsync(
        string queryId,
        IQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queryId))
        {
            throw new ArgumentNullException(nameof(queryId));
        }

        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        _cache.GetOrCreate<Task<OperationDocument>>(queryId, _ =>
        {
            if (query is OperationDocument queryDocument)
            {
                return Task.FromResult(queryDocument);
            }

            var document = Utf8GraphQLParser.Parse(query.AsSpan());
            queryDocument = new OperationDocument(document);
            return Task.FromResult(queryDocument);
        });

        return Task.CompletedTask;
    }
}
