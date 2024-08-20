using GreenDonut;

namespace HotChocolate.Types;

public static class DataLoaders
{
    [DataLoader(Lookups = [nameof(CreateLookupKey)])]
    public static async Task<IDictionary<int, string>> GetSomeInfoById(
        IReadOnlyList<int> keys)
        => await Task.FromResult(keys.ToDictionary(k => k, k => k + " - some info"));

    public static int CreateLookupKey(string key)
        => default!;

    public static int CreateLookupKey(Guid key)
        => default!;
}
