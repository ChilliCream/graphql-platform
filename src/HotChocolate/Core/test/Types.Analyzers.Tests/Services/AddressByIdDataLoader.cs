using GreenDonut;

namespace HotChocolate.Types;

public class AddressByIdDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
    : BatchDataLoader<int, AuthorAddress?>(batchScheduler, options)
{
    private readonly Dictionary<int, AuthorAddress> _addresses = new()
    {
        { 1, new AuthorAddress(1, 1, "Author 1", "Street 1", "City 1") },
        { 2, new AuthorAddress(2, 2, "Author 2", "Street 2", "City 2") },
        { 3, new AuthorAddress(3, 3, "Author 3", "Street 3", "City 3") }
    };

    protected override async Task<IReadOnlyDictionary<int, AuthorAddress?>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        return keys.ToDictionary(key => key, key => _addresses.TryGetValue(key, out var address) ? address : null);
    }
}
