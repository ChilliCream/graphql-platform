using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.Execution.Integration.DataLoader;

public class TestDataLoader : BatchDataLoader<string, string>, ITestDataLoader
{
    public TestDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
        : base(batchScheduler, options)
    {
    }

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