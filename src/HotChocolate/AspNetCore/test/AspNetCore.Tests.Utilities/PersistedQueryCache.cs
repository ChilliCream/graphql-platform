using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Tests.Utilities;

public class PersistedQueryCache
    : IReadStoredQueries
    , IWriteStoredQueries
{
    private readonly Dictionary<string, DocumentNode> _cache = new();

    public PersistedQueryCache()
    {
        _cache.Add(
            "60ddx_GGk4FDObSa6eK0sg",
            Utf8GraphQLParser.Parse(@"{ hero { name } }"));
        
        _cache.Add(
            "abc",
            Utf8GraphQLParser.Parse(@"query($if: Boolean) { hero { name @skip(if: $if) } }"));
    }

    public async Task<QueryDocument?> TryReadQueryAsync(
        string queryId,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(queryId, out var document))
        {
            return await Task.FromResult(new QueryDocument(document));
        }

        return null;
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
