using GreenDonut;

namespace HotChocolate.Diagnostics;

public partial class QueryInstrumentationTests
{
    public class CustomDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
        : BatchDataLoader<string, string>(batchScheduler, options)
    {
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
