using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.Execution.Integration.DataLoader
{
    public class TestDataLoader : DataLoaderBase<string, string>, ITestDataLoader
    {
        public TestDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
            : base(batchScheduler, options)
        {
        }

        public List<IReadOnlyList<string>> Loads { get; } = new();

        protected override ValueTask FetchAsync(
            IReadOnlyList<string> keys,
            Memory<Result<string>> results,
            CancellationToken cancellationToken)
        {
            Loads.Add(keys.OrderBy(t => t).ToArray());

            Span<Result<string>> span = results.Span;

            for (var i = 0; i < keys.Count; i++)
            {
                span[i] = keys[i];
            }

            return default;
        }
    }
}
