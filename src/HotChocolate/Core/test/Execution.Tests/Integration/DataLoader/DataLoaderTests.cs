using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using GreenDonut;
using HotChocolate.DataLoader;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
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
            Snapshot.FullName();
            await ExpectValid(
                "{ fetchItem }",
                configure: b => b
                    .AddGraphQL()
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .AddResolver(
                        "Query", "fetchItem",
                        async ctx => await ctx.FetchOnceAsync(ct => Task.FromResult("fooBar")))
            )
            .MatchSnapshotAsync();
        }

        [Fact]
        public async Task FetchSingleDataLoader()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ fetchItem }",
                configure: b => b
                    .AddGraphQL()
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .AddResolver(
                        "Query", "fetchItem",
                        async ctx => await ctx.CacheDataLoader<string, string>(
                            (key, ct) => Task.FromResult(key))
                            .LoadAsync("fooBar"))
            )
            .MatchSnapshotAsync();
        }

        [Fact]
        public async Task FetchDataLoader()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ fetchItem }",
                configure: b => b
                    .AddGraphQL()
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .AddResolver(
                        "Query", "fetchItem",
                        async ctx => await ctx.BatchDataLoader<string, string>(
                            (keys, ct) => Task.FromResult<IReadOnlyDictionary<string, string>>(
                                keys.ToDictionary(t => t)))
                            .LoadAsync("fooBar"))
            )
            .MatchSnapshotAsync();
        }

        [Fact]
        public async Task FetchGroupDataLoader()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ fetchItem }",
                configure: b => b
                    .AddGraphQL()
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .AddResolver(
                        "Query", "fetchItem",
                        async ctx => await ctx.GroupDataLoader<string, string>(
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
            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddQueryType<Query>()
                .AddDataLoader<ITestDataLoader, TestDataLoader>()
                .UseRequest(next => async context =>
                {
                    await next(context);

                    var dataLoader =
                        context.Services
                            .GetRequiredService<IDataLoaderRegistry>()
                            .GetOrRegister<TestDataLoader>(() => throw new Exception());

                    context.Result = QueryResultBuilder
                        .FromResult((IQueryResult)context.Result)
                        .AddExtension("loads", dataLoader.Loads)
                        .Create();
                })
                .UseDefaultPipeline());

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: withDataLoader(key: ""a"")
                            b: withDataLoader(key: ""b"")
                            bar {
                                c: withDataLoader(key: ""c"")
                            }
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

            // assert
            results.MatchSnapshot();
        }

        [Fact]
        public async Task ClassDataLoaderWithKey()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddQueryType<Query>()
                .AddDataLoader<ITestDataLoader, TestDataLoader>()
                .UseRequest(next => async context =>
                {
                    await next(context);

                    var dataLoader =
                        context.Services
                            .GetRequiredService<IDataLoaderRegistry>()
                            .GetOrRegister<TestDataLoader>("fooBar", () => throw new Exception());

                    context.Result = QueryResultBuilder
                        .FromResult((IQueryResult)context.Result)
                        .AddExtension("loads", dataLoader.Loads)
                        .Create();
                })
                .UseDefaultPipeline());

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: withDataLoader2(key: ""a"")
                            b: withDataLoader2(key: ""b"")
                        }")
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: withDataLoader2(key: ""a"")
                        }")
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            c: withDataLoader2(key: ""c"")
                        }")
                    .Create()));

            // assert
            results.MatchSnapshot();
        }

        [Fact]
        public async Task StackedDataLoader()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddQueryType<Query>()
                .AddDataLoader<ITestDataLoader, TestDataLoader>());

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: withStackedDataLoader(key: ""a"")
                            b: withStackedDataLoader(key: ""b"")
                        }")
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: withStackedDataLoader(key: ""a"")
                        }")
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            c: withStackedDataLoader(key: ""c"")
                        }")
                    .Create()));

            // assert
            results.MatchSnapshot();
        }

        [Fact]
        public async Task ClassDataLoader_Resolve_From_DependencyInjection()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddQueryType<Query>()
                .AddDataLoader<ITestDataLoader, TestDataLoader>()
                .UseRequest(next => async context =>
                {
                    await next(context);

                    var dataLoader =
                        (TestDataLoader)context.Services.GetRequiredService<ITestDataLoader>();

                    context.Result = QueryResultBuilder
                        .FromResult((IQueryResult)context.Result)
                        .AddExtension("loads", dataLoader.Loads)
                        .Create();
                })
                .UseDefaultPipeline());

            // act
            var results = new List<IExecutionResult>();

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: dataLoaderWithInterface(key: ""a"")
                            b: dataLoaderWithInterface(key: ""b"")
                        }")
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            a: dataLoaderWithInterface(key: ""a"")
                        }")
                    .Create()));

            results.Add(await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            c: dataLoaderWithInterface(key: ""c"")
                        }")
                    .Create()));

            // assert
            results.MatchSnapshot();
        }
    }
}
