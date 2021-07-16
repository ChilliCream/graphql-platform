using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Fetching;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Integration.DataLoader
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
                        async ctx => await ctx.FetchOnceAsync(_ => Task.FromResult("fooBar")))
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
                            (key, _) => Task.FromResult(key))
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
                            (keys, _) => Task.FromResult<IReadOnlyDictionary<string, string>>(
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
                            (keys, _) => Task.FromResult(
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

                    TestDataLoader dataLoader =
                        context.Services
                            .GetRequiredService<IDataLoaderRegistry>()
                            .GetOrRegister<TestDataLoader>(() => throw new Exception());

                    context.Result = QueryResultBuilder
                        .FromResult(((IQueryResult)context.Result)!)
                        .AddExtension("loads", dataLoader.Loads)
                        .Create();
                })
                .UseDefaultPipeline());

            // act
            var results = new List<IExecutionResult>
            {
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            a: withDataLoader(key: ""a"")
                            b: withDataLoader(key: ""b"")
                            bar {
                                c: withDataLoader(key: ""c"")
                            }
                        }")
                        .Create()),
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            a: withDataLoader(key: ""a"")
                        }")
                        .Create()),
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            c: withDataLoader(key: ""c"")
                        }")
                        .Create())
            };

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

                    TestDataLoader dataLoader =
                        context.Services
                            .GetRequiredService<IDataLoaderRegistry>()
                            .GetOrRegister<TestDataLoader>("fooBar", () => throw new Exception());

                    context.Result = QueryResultBuilder
                        .FromResult(((IQueryResult)context.Result)!)
                        .AddExtension("loads", dataLoader.Loads)
                        .Create();
                })
                .UseDefaultPipeline());

            // act
            var results = new List<IExecutionResult>
            {
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            a: withDataLoader2(key: ""a"")
                            b: withDataLoader2(key: ""b"")
                        }")
                        .Create()),
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            a: withDataLoader2(key: ""a"")
                        }")
                        .Create()),
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            c: withDataLoader2(key: ""c"")
                        }")
                        .Create())
            };

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
            var results = new List<IExecutionResult>
            {
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            a: withStackedDataLoader(key: ""a"")
                            b: withStackedDataLoader(key: ""b"")
                        }")
                        .Create()),

                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            a: withStackedDataLoader(key: ""a"")
                        }")
                        .Create()),

                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            c: withStackedDataLoader(key: ""c"")
                        }")
                        .Create())
            };

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
                        .FromResult(((IQueryResult)context.Result)!)
                        .AddExtension("loads", dataLoader.Loads)
                        .Create();
                })
                .UseDefaultPipeline());

            // act
            var results = new List<IExecutionResult>
            {
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            a: dataLoaderWithInterface(key: ""a"")
                            b: dataLoaderWithInterface(key: ""b"")
                        }")
                        .Create()),

                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            a: dataLoaderWithInterface(key: ""a"")
                        }")
                        .Create()),

                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            c: dataLoaderWithInterface(key: ""c"")
                        }")
                        .Create())
            };

            // assert
            results.MatchSnapshot();
        }
    }
}
