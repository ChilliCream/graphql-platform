using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using StackExchange.Redis;

namespace HotChocolate.PersistedQueries.FileSystem
{
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

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="database">The redis database instance.</param>
        public RedisQueryStorage(IDatabase database)
        {
            _database = database
                ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public Task<QueryDocument?> TryReadQueryAsync(
            string queryId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queryId))
            {
                throw new ArgumentNullException(nameof(queryId));
            }

            return TryReadQueryInternalAsync(queryId);
        }

        private async Task<QueryDocument?> TryReadQueryInternalAsync(
            string queryId)
        {
            var buffer = (byte[]?)await _database.StringGetAsync(queryId).ConfigureAwait(false);

            if (buffer is null)
            {
                return null;
            }

            return new QueryDocument(Utf8GraphQLParser.Parse(buffer));
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

            return _database.StringSetAsync(queryId, query.AsSpan().ToArray());
        }
    }
}
