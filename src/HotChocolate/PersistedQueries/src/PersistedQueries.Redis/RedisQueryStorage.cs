using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using StackExchange.Redis;

namespace HotChocolate.PersistedQueries.Redis;

/// <summary>
/// An implementation of <see cref="IOperationDocumentStorage"/> that uses Redis as a storage.
/// </summary>
public class RedisQueryStorage : IOperationDocumentStorage
{
    private readonly IDatabase _database;
    private readonly TimeSpan? _queryExpiration;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="database">The redis database instance.</param>
    /// <param name="queryExpiration">
    /// A timespan after that a query will be removed from the cache.
    /// </param>
    public RedisQueryStorage(IDatabase database, TimeSpan? queryExpiration = null)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _queryExpiration = queryExpiration;
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

        return TryReadInternalAsync(documentId);
    }
    
    private async ValueTask<IOperationDocument?> TryReadInternalAsync(OperationDocumentId documentId)
    {
        var buffer = (byte[]?)await _database.StringGetAsync(documentId.Value).ConfigureAwait(false);
        return buffer is null ? null : new OperationDocument(Utf8GraphQLParser.Parse(buffer));
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
        
        return SaveInternalAsync(documentId, document);
    }

    private async ValueTask SaveInternalAsync(
        OperationDocumentId documentId,
        IOperationDocument document)
    {
        var promise = _queryExpiration.HasValue
            ? _database.StringSetAsync(documentId.Value, document.ToArray(), _queryExpiration.Value)
            : _database.StringSetAsync(documentId.Value, document.ToArray());
        await promise.ConfigureAwait(false);
    }
}
