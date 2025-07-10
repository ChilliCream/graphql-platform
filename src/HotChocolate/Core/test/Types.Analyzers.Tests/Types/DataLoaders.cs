using GreenDonut;
using GreenDonut.Data;

namespace HotChocolate.Types;

[DataLoaderGroup("Group1DataLoader", "Group2DataLoader")]
public static class DataLoaders
{
    [DataLoader(Lookups = [nameof(CreateLookupKey)])]
    public static async Task<IDictionary<int, string>> GetSomeInfoById(
        IReadOnlyList<int> keys)
        => await Task.FromResult(keys.ToDictionary(k => k, k => k + " - some info"));

    public static int CreateLookupKey(string key)
        => 0;

    public static int CreateLookupKey(Guid key)
        => 0;

    [DataLoader]
    public static Task<ILookup<int, string>> GetSomeInfoGroupedById(
        IReadOnlyList<int> keys)
        => null!;

    [DataLoaderGroup("Group3DataLoader", "Group2DataLoader")]
    [DataLoader]
    public static Task<string> GetSomeInfoCacheById(
        int key)
        => null!;

    [DataLoader]
    public static Task<string> GetSomeInfoCacheWithServiceById(
        int key,
        ChapterRepository repository,
        CancellationToken ct)
        => null!;

    [DataLoader]
    public static async Task<IDictionary<int, string>> GetSomeInfoWithServiceById(
        IReadOnlyList<int> keys,
        ChapterRepository repository,
        CancellationToken ct)
        => await Task.FromResult(keys.ToDictionary(k => k, k => k + " - some info"));

    [DataLoader]
    public static async Task<IDictionary<int, string>> GetSomeInfoWithServiceAndRequiredStateById(
        IReadOnlyList<int> keys,
        ChapterRepository repository,
        [DataLoaderState("key")] string state,
        CancellationToken ct)
        => await Task.FromResult(keys.ToDictionary(k => k, k => k + " - some info"));

    [DataLoader]
    public static async Task<IDictionary<int, string>> GetSomeInfoWithServiceAndStateById(
        IReadOnlyList<int> keys,
        ChapterRepository repository,
        [DataLoaderState("key")] string? state,
        CancellationToken ct)
        => await Task.FromResult(keys.ToDictionary(k => k, k => k + " - some info"));

    [DataLoader]
    public static async Task<IDictionary<int, string>> GetSomeInfoWithServiceAndStateWithDefaultById(
        IReadOnlyList<int> keys,
        ChapterRepository repository,
        [DataLoaderState("key")] string state = "123",
        CancellationToken ct = default)
        => await Task.FromResult(keys.ToDictionary(k => k, k => k + " - some info"));

    [DataLoader]
    public static async Task<IDictionary<int, Author>> GetAuthorById(
        IReadOnlyList<int> keys,
        IQueryable<Author> query,
        ISelectorBuilder selector,
        CancellationToken ct)
        => await Task.FromResult(query.Select(t => t.Id, selector).ToDictionary(t => t.Id));

    [DataLoader]
    public static async Task<IDictionary<int, Author>> GetAuthorWithPagingById(
        IReadOnlyList<int> keys,
        IQueryable<Author> query,
        PagingArguments paging,
        CancellationToken ct)
        => await Task.FromResult(query.ToDictionary(t => t.Id));
}
