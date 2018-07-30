using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate;
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
        public async Task RequestDataLoader()
        {
            // arrange
            ISchema schema = CreateSchema(ExecutionScope.Request);
            QueryExecuter executer = new QueryExecuter(schema, 10);

            // act
            List<IExecutionResult> results = new List<IExecutionResult>();
            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader(key: ""a"")
                    b: withDataLoader(key: ""b"")
                }")));
            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader(key: ""a"")
                }")));
            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    c: withDataLoader(key: ""c"")
                }")));
            results.Add(await executer.ExecuteAsync(new QueryRequest(
                "{ loads }")));

            // assert
            Assert.Collection(results,
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors));
            Assert.Equal(Snapshot.Current(), Snapshot.New(results));
        }

        [Fact]
        public async Task GlobalDataLoader()
        {
            // arrange
            ISchema schema = CreateSchema(ExecutionScope.Global);
            QueryExecuter executer = new QueryExecuter(schema, 10);

            // act
            List<IExecutionResult> results = new List<IExecutionResult>();
            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader(key: ""a"")
                    b: withDataLoader(key: ""b"")
                }")));
            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    a: withDataLoader(key: ""a"")
                }")));
            results.Add(await executer.ExecuteAsync(new QueryRequest(
                @"{
                    c: withDataLoader(key: ""c"")
                }")));
            results.Add(await executer.ExecuteAsync(new QueryRequest(
                "{ loads }")));

            // assert
            Assert.Collection(results,
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors));
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
        public TestDataLoader()
            : base(new DataLoaderOptions<string>())
        {
        }

        public List<IReadOnlyList<string>> Loads { get; } =
            new List<IReadOnlyList<string>>();

        protected override Task<IReadOnlyList<Result<string>>> Fetch(
            IReadOnlyList<string> keys)
        {
            Loads.Add(keys.OrderBy(t => t).ToArray());
            return Task.FromResult<IReadOnlyList<Result<string>>>(
                keys.Select(t => Result<string>.Resolve(t)).ToArray());
        }
    }
}
