using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.Integration.DataLoader
{
    public class TestDataLoader
        : DataLoaderBase<string, string>
    {
        public TestDataLoader()
            : base(new DataLoaderOptions<string>())
        {
        }

        public List<IReadOnlyList<string>> Loads { get; } =
            new List<IReadOnlyList<string>>();

        protected override Task<IReadOnlyList<IResult<string>>> Fetch(
            IReadOnlyList<string> keys)
        {
            Loads.Add(keys.OrderBy(t => t).ToArray());
            return Task.FromResult<IReadOnlyList<IResult<string>>>(
                keys.Select(t => Result<string>.Resolve(t)).ToArray());
        }
    }
}
