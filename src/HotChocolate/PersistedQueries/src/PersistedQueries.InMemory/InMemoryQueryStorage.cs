using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.Caching.Memory;

namespace HotChocolate.PersistedQueries.FileSystem
{
    /// <summary>
    /// An implementation of <see cref="IReadStoredQueries"/>
    /// and <see cref="IWriteStoredQueries"/> that
    /// uses the local file system.
    /// </summary>
    public class InMemoryQueryStorage
        : IReadStoredQueries
        , IWriteStoredQueries
    {
        private static readonly Task<QueryDocument?> _null = Task.FromResult<QueryDocument?>(null);
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public InMemoryQueryStorage(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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

            if (_cache.TryGetValue(queryId, out Task<QueryDocument?>? queryDocumentTask))
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


            _cache.GetOrCreate<Task<QueryDocument>>(queryId, item =>
            {
                if (query is QueryDocument queryDocument)
                {
                    return Task.FromResult(queryDocument);
                }
                else
                {
                    DocumentNode document = Utf8GraphQLParser.Parse(query.AsSpan());
                    queryDocument = new QueryDocument(document);
                    return Task.FromResult(queryDocument);
                }
            });

            return Task.CompletedTask;
        }
    }
}
