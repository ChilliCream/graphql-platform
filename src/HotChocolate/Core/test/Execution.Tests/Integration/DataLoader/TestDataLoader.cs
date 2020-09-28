using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.Integration.DataLoader
{
    public class TestDataLoader
        : DataLoaderBase<string, string>
        , ITestDataLoader
    {
        public TestDataLoader(IBatchScheduler batchScheduler)
            : base(batchScheduler)
        {
        }

        public List<IReadOnlyList<string>> Loads { get; } =
            new List<IReadOnlyList<string>>();

        protected override ValueTask<IReadOnlyList<Result<string>>> FetchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            Loads.Add(keys.OrderBy(t => t).ToArray());
            return new ValueTask<IReadOnlyList<Result<string>>>(
                keys.Select(t => (Result<string>)t).ToArray());
        }
    }
}
