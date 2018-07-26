using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Integration.DataLoader
{
    public class Query
    {
        public async Task<IResolverResult<string>> GetWithDataLoader(
            string key,
            FieldNode fieldSelection,
            [DataLoader]TestDataLoader testDataLoader)
        {
            Result<string> result = await testDataLoader.LoadAsync(key);
            if (result.IsError)
            {
                throw new QueryException(
                    new FieldError(result.ErrorMessage, fieldSelection));
            }
            // return result.Value;
            return null;
        }
    }



    public class TestDataLoader
        : DataLoaderBase<string, string>
    {
        protected TestDataLoader()
            : base(new DataLoaderOptions<string>())
        {
        }

        public List<IReadOnlyList<string>> Loads { get; } =
            new List<IReadOnlyList<string>>();

        protected override Task<IReadOnlyList<Result<string>>> Fetch(
            IReadOnlyList<string> keys)
        {
            Loads.Add(keys);
            return Task.FromResult<IReadOnlyList<Result<string>>>(
                keys.Select(t => Result<string>.Resolve(t)).ToArray());
        }
    }
}
