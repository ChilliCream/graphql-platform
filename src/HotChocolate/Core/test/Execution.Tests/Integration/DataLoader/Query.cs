using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Integration.DataLoader
{
    public class Query
    {
        public Task<string> GetWithDataLoader(
            string key,
            FieldNode fieldSelection,
            [DataLoader] TestDataLoader testDataLoader,
            CancellationToken cancellationToken)
        {
            return testDataLoader.LoadAsync(key, cancellationToken);
        }

        public Bar Bar => new Bar();

        public Task<string> GetWithDataLoader2(
            string key,
            FieldNode fieldSelection,
            [DataLoader("fooBar")] TestDataLoader testDataLoader,
            CancellationToken cancellationToken)
        {
            return testDataLoader.LoadAsync(key, cancellationToken);
        }

        public Task<string> GetDataLoaderWithInterface(
            string key,
            FieldNode fieldSelection,
            ITestDataLoader testDataLoader,
            CancellationToken cancellationToken)
        {
            return testDataLoader.LoadAsync(key, cancellationToken);
        }

        public async Task<string> GetWithStackedDataLoader(
            string key,
            FieldNode fieldSelection,
            [DataLoader("fooBar")] TestDataLoader testDataLoader,
            CancellationToken cancellationToken)
        {

            string s = await testDataLoader.LoadAsync(key + "a", cancellationToken);
            s += await testDataLoader.LoadAsync(key + "b", cancellationToken);
            s += await testDataLoader.LoadAsync(key + "c", cancellationToken);
            s += await testDataLoader.LoadAsync(key + "d", cancellationToken);
            s += await testDataLoader.LoadAsync(key + "e", cancellationToken);
            return s;
        }
    }

    public class Bar
    {
        public Task<string> GetWithDataLoader(
            string key,
            FieldNode fieldSelection,
            [DataLoader] TestDataLoader testDataLoader,
            CancellationToken cancellationToken)
        {
            return testDataLoader.LoadAsync(key, cancellationToken);
        }
    }
}
