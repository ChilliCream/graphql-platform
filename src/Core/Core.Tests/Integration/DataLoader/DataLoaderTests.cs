using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
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
            IQueryExecuter executer =
                QueryExecutionBuilder.BuildDefault(schema);

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
            results.Snapshot();
        }

        [Fact]
        public async Task GlobalDataLoader()
        {
            // arrange
            ISchema schema = CreateSchema(ExecutionScope.Global);
            IQueryExecuter executer =
                QueryExecutionBuilder.BuildDefault(schema);

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

            // assert
            Assert.Collection(results,
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors));
            results.Snapshot();

            var keyLoads = new HashSet<string>();
            var loads = (IQueryExecutionResult)await executer
                .ExecuteAsync(new QueryRequest("{ loads }"));

            foreach (object o in (IEnumerable<object>)loads.Data["loads"])
            {
                string[] keys = o.ToString().Split(',');
                foreach (string key in keys)
                {
                    Assert.True(keyLoads.Add(key));
                }
            }
        }

        private static ISchema CreateSchema(ExecutionScope scope)
        {
            return Schema.Create(c =>
            {
                c.Options.ExecutionTimeout = TimeSpan.FromSeconds(30);
                c.Options.DeveloperMode = true;

                c.RegisterDataLoader<TestDataLoader>(scope);
                c.RegisterQueryType<Query>();
            });
        }
    }
}
