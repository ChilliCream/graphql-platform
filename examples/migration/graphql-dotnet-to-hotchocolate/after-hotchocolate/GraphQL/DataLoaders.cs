using AfterHotChocolate.Models;
using AfterHotChocolate.Services;

namespace AfterHotChocolate.GraphQL;

// Batch DataLoader that resolves authors by id, fixing the N+1 problem when
// selecting Book.author across many books (one batched lookup per request).
public static class DataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Author>> AuthorByIdAsync(
        IReadOnlyList<int> keys,
        BookDataStore store,
        CancellationToken cancellationToken)
        => await Task.FromResult(store.GetAuthorsByIds(keys).ToDictionary(a => a.Id));
}
