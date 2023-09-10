using System.Threading.Tasks;
using GreenDonut;
using System.Collections.Generic;
using System.Threading;

namespace HotChocolate.Diagnostics;

public partial class QueryInstrumentationTests
{
    public class CustomDataLoader : BatchDataLoader<string, string>
    {
        public CustomDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions? options = null)
            : base(batchScheduler, options)
        {
        }

        protected override Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            var dict = new Dictionary<string, string>();

            foreach (var key in keys)
            {
                dict.Add(key, key);
            }

            return Task.FromResult<IReadOnlyDictionary<string, string>>(dict);
        }
    }

}
