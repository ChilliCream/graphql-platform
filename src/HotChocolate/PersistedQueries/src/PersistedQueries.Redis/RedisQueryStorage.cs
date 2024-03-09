using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using StackExchange.Redis;

namespace HotChocolate.PersistedQueries.Redis;

/// <summary>
/// An implementation of <see cref="IReadStoredQueries"/>
/// and <see cref="IWriteStoredQueries"/> that
/// uses a redis database.
/// </summary>
public class RedisQueryStorage
    : IReadStoredQueries
        , IWriteStoredQueries
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
    public Task<OperationDocument?> TryReadQueryAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        return TryReadQueryInternalAsync(documentId);
    }

    private async Task<OperationDocument?> TryReadQueryInternalAsync(
        string queryId)
    {
        var buffer = (byte[]?)await _database.StringGetAsync(queryId).ConfigureAwait(false);
        return buffer is null ? null : new OperationDocument(Utf8GraphQLParser.Parse(buffer));
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

        return _queryExpiration.HasValue
            ? _database.StringSetAsync(queryId, query.AsSpan().ToArray(), _queryExpiration.Value)
            : _database.StringSetAsync(queryId, query.AsSpan().ToArray());
    }
}
