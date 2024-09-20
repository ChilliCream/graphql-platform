using GreenDonut;

namespace HotChocolate.Execution.Integration.DataLoader;

public class TestDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
    : BatchDataLoader<string, string>(batchScheduler, options), ITestDataLoader
{
    public List<IReadOnlyList<string>> Loads { get; } = [];

    protected override async Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        Loads.Add(keys.OrderBy(t => t).ToArray());

        var dict = new Dictionary<string, string>();

        for (var i = 0; i < keys.Count; i++)
        {
            dict.Add( keys[i], keys[i]);
        }

        await Task.Delay(1, cancellationToken);
        return dict;
    }
}
