using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.Types;

public class AddressDataLoader : BatchDataLoader<string, string>
{
    public AddressDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
    }

    protected override Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyDictionary<string, string>>(keys.ToDictionary(t => t));
    }
}
