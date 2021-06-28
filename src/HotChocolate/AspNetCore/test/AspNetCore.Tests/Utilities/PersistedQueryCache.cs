using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Utilities
{
    public class PersistedQueryCache
        : IReadStoredQueries
        , IWriteStoredQueries
    {
        private readonly Dictionary<string, DocumentNode> _cache = new();

        public PersistedQueryCache()
        {
            _cache.Add(
                "60ddx/GGk4FDObSa6eK0sg==",
                Utf8GraphQLParser.Parse(@"{ hero { name } }"));
        }

        public Task<QueryDocument> TryReadQueryAsync(
            string queryId,
            CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(queryId, out DocumentNode document))
            {
                return Task.FromResult(new QueryDocument(document));
            }

            return Task.FromResult<QueryDocument>(null);
        }

        public Task WriteQueryAsync(
            string queryId,
            IQuery query,
            CancellationToken cancellationToken = default)
        {
            _cache[queryId] = Utf8GraphQLParser.Parse(query.AsSpan());
            return Task.CompletedTask;
        }
    }
}
