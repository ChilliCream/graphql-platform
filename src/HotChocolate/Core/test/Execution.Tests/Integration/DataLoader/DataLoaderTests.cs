using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Integration.DataLoader
{
    public class DataLoaderTests
    {
        [Fact]
        public async Task FetchOnceDataLoader()
        {
            await ExpectValid(
                "{ fetchItem }",
                configure: b => b
                    .AddGraphQL()
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .AddResolver(
                        "Query", "fetchItem",
                        ctx => ctx.FetchOnceAsync(ct => Task.FromResult("fooBar")))
            )
            .MatchSnapshotAsync();
        }

        [Fact]
        public async Task FetchSingleDataLoader()
        {
            await ExpectValid(
                "{ fetchItem }",
                configure: b => b
                    .AddGraphQL()
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .AddResolver(
                        "Query", "fetchItem",
                        ctx => ctx.CacheDataLoader<string, string>(
                            (key, ct) => Task.FromResult(key))
                            .LoadAsync("fooBar"))
            )
            .MatchSnapshotAsync();
        }

        [Fact]
        public async Task FetchDataLoader()
        {
            await ExpectValid(
                "{ fetchItem }",
                configure: b => b
                    .AddGraphQL()
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .AddResolver(
                        "Query", "fetchItem",
                        ctx => ctx.BatchDataLoader<string, string>(
                            (keys, ct) => Task.FromResult<IReadOnlyDictionary<string, string>>(
                                keys.ToDictionary(t => t)))
                            .LoadAsync("fooBar"))
            )
            .MatchSnapshotAsync();
        }

        [Fact]
        public async Task FetchGroupDataLoader()
        {
            await ExpectValid(
                "{ fetchItem }",
                configure: b => b
                    .AddGraphQL()
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .AddResolver(
                        "Query", "fetchItem",
                        ctx => ctx.GroupDataLoader<string, string>(
                            (keys, ct) => Task.FromResult(
                                keys.ToLookup(t => t)))
                            .LoadAsync("fooBar"))
            )
            .MatchSnapshotAsync();
        }

        [Fact]
        public async Task ClassDataLoader()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync(c => c.AddQueryType<Query>());

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: withDataLoader(key: ""a"")
                            b: withDataLoader(key: ""b"")
                        }")
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: withDataLoader(key: ""a"")
                        }")
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            c: withDataLoader(key: ""c"")
                        }")
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ loads }")
                    .Create()));

            // assert
            Assert.Collection(results,
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors),
                t => Assert.Null(t.Errors));
            results.MatchSnapshot();
        }

        /*
                [Fact]
                public async Task ClassDataLoaderWithKey()
                {
                    // arrange
                    IServiceProvider serviceProvider = new ServiceCollection()
                        .AddDataLoaderRegistry()
                        .BuildServiceProvider();

                    var schema = Schema.Create(c => c.RegisterQueryType<Query>());

                    IQueryExecutor executor = schema.MakeExecutable();
                    IServiceScope scope = serviceProvider.CreateScope();

                    // act
                    var results = new List<IExecutionResult>();

                    results.Add(await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"{
                                    a: withDataLoader2(key: ""a"")
                                    b: withDataLoader2(key: ""b"")
                                }")
                            .SetServices(scope.ServiceProvider)
                            .Create()));

                    results.Add(await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"{
                                    a: withDataLoader2(key: ""a"")
                                }")
                            .SetServices(scope.ServiceProvider)
                            .Create()));

                    results.Add(await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"{
                                    c: withDataLoader2(key: ""c"")
                                }")
                            .SetServices(scope.ServiceProvider)
                            .Create()));

                    results.Add(await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery("{ loads loads2 }")
                            .SetServices(scope.ServiceProvider)
                            .Create()));

                    // assert
                    Assert.Collection(results,
                        t => Assert.Null(t.Errors),
                        t => Assert.Null(t.Errors),
                        t => Assert.Null(t.Errors),
                        t => Assert.Null(t.Errors));
                    results.MatchSnapshot();
                }

                [Fact]
                public async Task StackedDataLoader()
                {
                    // arrange
                    IServiceProvider serviceProvider = new ServiceCollection()
                        .AddDataLoaderRegistry()
                        .BuildServiceProvider();

                    var schema = Schema.Create(c => c.RegisterQueryType<Query>());

                    IQueryExecutor executor = schema.MakeExecutable();
                    IServiceScope scope = serviceProvider.CreateScope();

                    // act
                    var results = new List<IExecutionResult>();

                    results.Add(await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"{
                                    a: withStackedDataLoader(key: ""a"")
                                    b: withStackedDataLoader(key: ""b"")
                                }")
                            .SetServices(scope.ServiceProvider)
                            .Create()));

                    results.Add(await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"{
                                    a: withStackedDataLoader(key: ""a"")
                                }")
                            .SetServices(scope.ServiceProvider)
                            .Create()));

                    results.Add(await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"{
                                    c: withStackedDataLoader(key: ""c"")
                                }")
                            .SetServices(scope.ServiceProvider)
                            .Create()));

                    // assert
                    Assert.Collection(results,
                        t => Assert.Null(t.Errors),
                        t => Assert.Null(t.Errors),
                        t => Assert.Null(t.Errors));
                    results.MatchSnapshot();
                }

                [Fact]
                public async Task ClassDataLoader_Resolve_From_DependencyInjection()
                {
                    // arrange
                    IServiceProvider serviceProvider = new ServiceCollection()
                        .AddDataLoader<ITestDataLoader, TestDataLoader>()
                        .BuildServiceProvider();

                    var schema = Schema.Create(c => c.RegisterQueryType<Query>());

                    IQueryExecutor executor = schema.MakeExecutable();
                    IServiceScope scope = serviceProvider.CreateScope();

                    // act
                    var results = new List<IExecutionResult>();

                    results.Add(await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"{
                                    a: dataLoaderWithInterface(key: ""a"")
                                    b: dataLoaderWithInterface(key: ""b"")
                                }")
                            .SetServices(scope.ServiceProvider)
                            .Create()));

                    results.Add(await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"{
                                    a: dataLoaderWithInterface(key: ""a"")
                                }")
                            .SetServices(scope.ServiceProvider)
                            .Create()));

                    results.Add(await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"{
                                    c: dataLoaderWithInterface(key: ""c"")
                                }")
                            .SetServices(scope.ServiceProvider)
                            .Create()));

                    results.Add(await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery("{ loads loads2 loads3 }")
                            .SetServices(scope.ServiceProvider)
                            .Create()));

                    // assert
                    Assert.Collection(results,
                        t => Assert.Null(t.Errors),
                        t => Assert.Null(t.Errors),
                        t => Assert.Null(t.Errors),
                        t => Assert.Null(t.Errors));
                    results.MatchSnapshot();
                }
                */
    }
}
