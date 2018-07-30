using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
using Xunit;

namespace HotChocolate.Integration.DataLoader
{


    public class DataLoaderTests
    {
        [Fact]
        public void RequestDataLoader()
        {
            // arrange
            ISchema schema = CreateSchema(ExecutionScope.Request);

            // act
            List<IExecutionResult> results = new List<IExecutionResult>();
            results.Add(schema.Execute(
                "{ withDataLoader(key: \"a\") withDataLoader(key: \"b\") }"));
            results.Add(schema.Execute("{ withDataLoader(key: \"c\") }"));
            results.Add(schema.Execute("{ loads }"));

            // assert
            Assert.Collection(results,
                t => Assert.False(t.Errors.Any()),
                t => Assert.False(t.Errors.Any()));
            Assert.Equal(Snapshot.Current(), Snapshot.New(results));
        }

        [Fact]
        public void GlobalDataLoader()
        {
            // arrange
            ISchema schema = CreateSchema(ExecutionScope.Request);

            // act
            List<IExecutionResult> results = new List<IExecutionResult>();
            results.Add(schema.Execute(
                "{ withDataLoader(key: \"a\") withDataLoader(key: \"b\") }"));
            results.Add(schema.Execute("{ withDataLoader(key: \"c\") }"));
            results.Add(schema.Execute("{ loads }"));

            // assert
            Assert.Collection(results,
                t => Assert.False(t.Errors.Any()),
                t => Assert.False(t.Errors.Any()));
            Assert.Equal(Snapshot.Current(), Snapshot.New(results));
        }

        private static ISchema CreateSchema(ExecutionScope scope)
        {
            return Schema.Create(c =>
            {
                c.RegisterDataLoader<TestDataLoader>(scope);
                c.RegisterQueryType<Query>();
            });
        }
    }

    public class Query
    {
        public Task<string> GetWithDataLoader(
            string key,
            FieldNode fieldSelection,
            [DataLoader]TestDataLoader testDataLoader)
        {
            return testDataLoader.LoadAsync(key);
        }

        public List<string> GetLoads([DataLoader]TestDataLoader testDataLoader)
        {
            List<string> list = new List<string>();

            foreach (IReadOnlyList<string> request in testDataLoader.Loads)
            {
                list.Add(string.Join(", ", request));
            }

            return list;
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
