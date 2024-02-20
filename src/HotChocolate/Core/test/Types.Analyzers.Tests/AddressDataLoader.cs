using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.Types;

public class AddressDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
    : BatchDataLoader<string, string>(batchScheduler, options)
{
    protected override Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyDictionary<string, string>>(keys.ToDictionary(t => t));
    }
}
