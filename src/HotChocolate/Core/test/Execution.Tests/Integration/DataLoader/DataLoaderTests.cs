﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Fetching;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

#nullable enable

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
                        .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
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
                        .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
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
                        .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
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
                        .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
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
        public async Task AddSingleDiagnosticEventListener()
        {
            var listener = new DataLoaderListener();

            await ExpectValid(
                "{ fetchItem }",
                b => b
                    .AddGraphQL()
                    .AddDiagnosticEventListener(_ => listener)
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .AddResolver(
                        "Query", "fetchItem",
                        async ctx => await ctx.GroupDataLoader<string, string>(
                                (keys, _) => Task.FromResult(
                                    keys.ToLookup(t => t)))
                            .LoadAsync("fooBar"))
            );

            Assert.True(listener.ExecuteBatchTouched);
            Assert.True(listener.BatchResultsTouched);
        }

        [Fact]
        public async Task AddMultipleDiagnosticEventListener()
        {
            var listener1 = new DataLoaderListener();
            var listener2 = new DataLoaderListener();

            await ExpectValid(
                "{ fetchItem }",
                b => b
                    .AddGraphQL()
                    .AddDiagnosticEventListener(_ => listener1)
                    .AddDiagnosticEventListener(_ => listener2)
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .AddResolver(
                        "Query", "fetchItem",
                        async ctx => await ctx.GroupDataLoader<string, string>(
                                (keys, _) => Task.FromResult(
                                    keys.ToLookup(t => t)))
                            .LoadAsync("fooBar"))
            );

            Assert.True(listener1.ExecuteBatchTouched);
            Assert.True(listener1.BatchResultsTouched);
            Assert.True(listener2.ExecuteBatchTouched);
            Assert.True(listener2.BatchResultsTouched);
        }

        [Fact]
        public async Task ClassDataLoader()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddQueryType<Query>()
                .AddDataLoader<ITestDataLoader, TestDataLoader>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .UseRequest(next => async context =>
                {
                    await next(context);

                    TestDataLoader dataLoader =
                        context.Services
                            .GetRequiredService<IDataLoaderRegistry>()
                            .GetOrRegister<TestDataLoader>(() => throw new Exception());

                    context.Result = QueryResultBuilder
                        .FromResult(((IQueryResult)context.Result!))
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
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .UseRequest(next => async context =>
                {
                    await next(context);

                    TestDataLoader dataLoader =
                        context.Services
                            .GetRequiredService<IDataLoaderRegistry>()
                            .GetOrRegister<TestDataLoader>("fooBar", () => throw new Exception());

                    context.Result = QueryResultBuilder
                        .FromResult(((IQueryResult)context.Result!))
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
                .AddDataLoader<ITestDataLoader, TestDataLoader>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true));

            // act
            var results = new List<IExecutionResult>();

            results.Add(
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            a: withStackedDataLoader(key: ""a"")
                            b: withStackedDataLoader(key: ""b"")
                        }")
                        .Create()));

            results.Add(
                await executor.ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(
                            @"{
                            a: withStackedDataLoader(key: ""a"")
                        }")
                        .Create()));

            results.Add(
                await executor.ExecuteAsync(
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
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .UseRequest(next => async context =>
                {
                    await next(context);

                    var dataLoader =
                        (TestDataLoader)context.Services.GetRequiredService<ITestDataLoader>();

                    context.Result = QueryResultBuilder
                        .FromResult(((IQueryResult)context.Result!))
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

        [Fact]
        public async Task NestedDataLoader()
        {
            using var cts = new CancellationTokenSource(500);

            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType()
                .AddType<FooQueries>()
                .AddDataLoader<FooDataLoader>()
                .AddDataLoader<FooNestedDataLoader>()
                .ExecuteRequestAsync("query Foo { foo { id field } }", cancellationToken: cts.Token)
                .MatchSnapshotAsync();
        }

        public class DataLoaderListener : DataLoaderDiagnosticEventListener
        {
            public bool ResolvedTaskFromCacheTouched;
            public bool ExecuteBatchTouched;
            public bool BatchResultsTouched;
            public bool BatchErrorTouched;
            public bool BatchItemErrorTouched;

            public override void ResolvedTaskFromCache(IDataLoader dataLoader, TaskCacheKey cacheKey, Task task)
            {
                ResolvedTaskFromCacheTouched = true;
            }

            public override IDisposable ExecuteBatch<TKey>(IDataLoader dataLoader, IReadOnlyList<TKey> keys)
            {
                ExecuteBatchTouched = true;
                return base.ExecuteBatch(dataLoader, keys);
            }

            public override void BatchResults<TKey, TValue>(IReadOnlyList<TKey> keys,
                ReadOnlySpan<Result<TValue>> values)
            {
                BatchResultsTouched = true;
            }

            public override void BatchError<TKey>(IReadOnlyList<TKey> keys, Exception error)
            {
                BatchErrorTouched = true;
            }

            public override void BatchItemError<TKey>(TKey key, Exception error)
            {
                BatchItemErrorTouched = true;
            }
        }

        [ExtendObjectType("Query")]
        public class FooQueries
        {
            public async Task<FooObject?> GetFoo(IResolverContext context, CancellationToken ct) =>
                await FooObject.Get(context, "hello", ct);
        }

        [GraphQLName("Foo")]
        [Node]
        public class FooObject
        {
            public FooObject(string field)
            {
                id = field;
            }

            public string id { get; }
            public string field => id;

            public static async Task<FooObject?> Get(IResolverContext context, string id,
                CancellationToken ct) =>
                new((await context.DataLoader<FooDataLoader>().LoadAsync(id, ct)).Field);
        }

        public class FooDataLoader : BatchDataLoader<string, FooRecord>
        {
            private readonly FooNestedDataLoader _nestedDataLoader;

            public FooDataLoader(
                IBatchScheduler batchScheduler,
                FooNestedDataLoader nestedDataLoader,
                DataLoaderOptions? options = null)
                : base(batchScheduler, options)
            {
                _nestedDataLoader = nestedDataLoader;
            }

            protected override async Task<IReadOnlyDictionary<string, FooRecord>> LoadBatchAsync(
                IReadOnlyList<string> keys,
                CancellationToken cancellationToken)
                => (await _nestedDataLoader.LoadAsync(keys, cancellationToken))
                    .ToImmutableDictionary(x => x.Field);
        }

        public class FooNestedDataLoader : BatchDataLoader<string, FooRecord>
        {
            public FooNestedDataLoader(
                IBatchScheduler batchScheduler,
                DataLoaderOptions? options = null)
                : base(batchScheduler, options)
            {
            }

            protected override async Task<IReadOnlyDictionary<string, FooRecord>> LoadBatchAsync(
                IReadOnlyList<string> keys,
                CancellationToken cancellationToken)
            {
                await Task.Delay(1, cancellationToken);
                return keys.ToImmutableDictionary(key => key, key => new FooRecord(key));
            }
        }

        public class FooRecord
        {
            public FooRecord(string field)
            {
                Field = field;
            }

            public string Field { get; }
        }
    }
}
