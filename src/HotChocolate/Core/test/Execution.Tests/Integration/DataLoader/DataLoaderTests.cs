using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CookieCrumble;
using GreenDonut;
using GreenDonut.DependencyInjection;
using HotChocolate.Fetching;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Tests.TestHelper;
using Snapshot = CookieCrumble.Snapshot;

#nullable enable

namespace HotChocolate.Execution.Integration.DataLoader;

public class DataLoaderTests
{
    [Fact]
    public async Task FetchOnceDataLoader()
    {
        var snapshot = new Snapshot();

        snapshot.Add(
            await ExpectValid(
                "{ fetchItem }",
                configure: b => b
                    .AddGraphQL()
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .AddResolver(
                        "Query",
                        "fetchItem",
                        async ctx => await ctx.FetchOnceAsync(_ => Task.FromResult("fooBar")))
            ));

        snapshot.MatchMarkdown();
    }

    [Fact]
    public async Task FetchSingleDataLoader()
    {
        var snapshot = new Snapshot();

        snapshot.Add(
            await ExpectValid(
                "{ fetchItem }",
                configure: b => b
                    .AddGraphQL()
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .AddResolver(
                        "Query",
                        "fetchItem",
                        async ctx => await ctx.CacheDataLoader<string, string>(
                                (key, _) => Task.FromResult(key))
                            .LoadAsync("fooBar"))
            ));

        snapshot.MatchMarkdown();
    }

    [LocalFact]
    public async Task FetchDataLoader()
    {
        var snapshot = new Snapshot();

        snapshot.Add(
            await ExpectValid(
                "{ fetchItem }",
                configure: b => b
                    .AddGraphQL()
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .AddResolver(
                        "Query",
                        "fetchItem",
                        async ctx => await ctx.BatchDataLoader<string, string>(
                                (keys, _) => Task.FromResult<IReadOnlyDictionary<string, string>>(
                                    keys.ToDictionary(t => t)))
                            .LoadAsync("fooBar"))
            ));

        snapshot.MatchMarkdown();
    }

    [Fact]
    public async Task FetchGroupDataLoader()
    {
        var snapshot = new Snapshot();

        snapshot.Add(
            await ExpectValid(
                "{ fetchItem }",
                configure: b => b
                    .AddGraphQL()
                    .AddDocumentFromString("type Query { fetchItem: String }")
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    .AddResolver(
                        "Query",
                        "fetchItem",
                        async ctx => await ctx.GroupDataLoader<string, string>(
                                (keys, _) => Task.FromResult(
                                    keys.ToLookup(t => t)))
                            .LoadAsync("fooBar"))
            ));

        snapshot.MatchMarkdown();
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
                    "Query",
                    "fetchItem",
                    async ctx => await ctx.GroupDataLoader<string, string>(
                            (keys, _) => Task.FromResult(
                                keys.ToLookup(t => t)))
                        .LoadAsync("fooBar"))
        );

        Assert.True(listener.ExecuteBatchTouched);
        Assert.True(listener.BatchResultsTouched);
    }

    [LocalFact]
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
                    "Query",
                    "fetchItem",
                    async ctx => await ctx.GroupDataLoader<string, string>(
                            (keys, _) => Task.FromResult(
                                keys.ToLookup(t => t)))
                        .LoadAsync("fooBar"))
        );

        Assert.True(listener1.ExecuteBatchTouched, "listener1.ExecuteBatchTouched");
        Assert.True(listener1.BatchResultsTouched, "listener1.BatchResultsTouched");
        Assert.True(listener2.ExecuteBatchTouched, "listener2.ExecuteBatchTouched");
        Assert.True(listener2.BatchResultsTouched, "listener2.BatchResultsTouched");
    }

    [Fact]
    public async Task ClassDataLoader()
    {
        var snapshot = new Snapshot();

        // arrange
        var executor = await CreateExecutorAsync(
            c => c
                .AddQueryType<Query>()
                .AddDataLoader<ITestDataLoader, TestDataLoader>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .UseRequest(
                    next => async context =>
                    {
                        await next(context);

                        var dataLoader =
                            context.Services
                                .GetRequiredService<IDataLoaderScope>()
                                .GetDataLoader<TestDataLoader>(_ => throw new Exception());

                        context.Result = OperationResultBuilder
                            .FromResult((IOperationResult)context.Result!)
                            .AddExtension("loads", dataLoader.Loads)
                            .Build();
                    })
                .UseDefaultPipeline());

        // act

        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.Create()
                    .SetDocument(
                        @"{
                            a: withDataLoader(key: ""a"")
                            b: withDataLoader(key: ""b"")
                            bar {
                                c: withDataLoader(key: ""c"")
                            }
                        }")
                    .Build()));
        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.Create()
                    .SetDocument(
                        @"{
                            a: withDataLoader(key: ""a"")
                        }")
                    .Build()));
        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.Create()
                    .SetDocument(
                        @"{
                            c: withDataLoader(key: ""c"")
                        }")
                    .Build()));

        // assert
        snapshot.MatchMarkdown();
    }

    [Fact]
    public async Task ClassDataLoader_Out_Off_GraphQL_Context_Not_Initialized()
    {
        // arrange
        var services = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddDataLoader<ITestDataLoader, TestDataLoader>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .UseRequest(
                next => async context =>
                {
                    await next(context);

                    var dataLoader =
                        context.Services
                            .GetRequiredService<IDataLoaderScope>()
                            .GetDataLoader<TestDataLoader>(_ => throw new Exception());

                    context.Result = OperationResultBuilder
                        .FromResult((IOperationResult)context.Result!)
                        .AddExtension("loads", dataLoader.Loads)
                        .Build();
                })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider();

        // act
        using var serviceScope = services.CreateScope();
        var dataLoader = serviceScope.ServiceProvider.GetRequiredService<ITestDataLoader>();
        var result = await dataLoader.LoadAsync("a");
        Assert.Equal("a", result);
    }

    [Fact]
    public async Task ClassDataLoader_Out_Off_GraphQL_Context()
    {
        // arrange
        var services = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddDataLoader<ITestDataLoader, TestDataLoader>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .UseRequest(
                next => async context =>
                {
                    await next(context);

                    var dataLoader =
                        context.Services
                            .GetRequiredService<IDataLoaderScope>()
                            .GetDataLoader<TestDataLoader>(_ => throw new Exception());

                    context.Result = OperationResultBuilder
                        .FromResult((IOperationResult)context.Result!)
                        .AddExtension("loads", dataLoader.Loads)
                        .Build();
                })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider();

        // act
        using var serviceScope = services.CreateScope();
        var dataLoaderScopeFactory = serviceScope.ServiceProvider.GetRequiredService<IDataLoaderScopeFactory>();
        dataLoaderScopeFactory.BeginScope();

        var dataLoader = serviceScope.ServiceProvider.GetRequiredService<ITestDataLoader>();
        var result = await dataLoader.LoadAsync("a");
        Assert.Equal("a", result);
    }

    [Fact]
    public async Task ClassDataLoader_Out_Off_GraphQL_Context_Just_Works()
    {
        // arrange
        var services = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddDataLoader<ITestDataLoader, TestDataLoader>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .UseRequest(
                next => async context =>
                {
                    await next(context);

                    var dataLoader =
                        context.Services
                            .GetRequiredService<IDataLoaderScope>()
                            .GetDataLoader<TestDataLoader>(_ => throw new Exception());

                    context.Result = OperationResultBuilder
                        .FromResult((IOperationResult)context.Result!)
                        .AddExtension("loads", dataLoader.Loads)
                        .Build();
                })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider();

        // act
        using var serviceScope = services.CreateScope();
        var dataLoader = serviceScope.ServiceProvider.GetRequiredService<ITestDataLoader>();
        var result = await dataLoader.LoadAsync("a");
        Assert.Equal("a", result);
    }

    [LocalFact]
    public async Task StackedDataLoader()
    {
        var snapshot = new Snapshot();

        // arrange
        var executor = await CreateExecutorAsync(
            c => c
                .AddQueryType<Query>()
                .AddDataLoader<ITestDataLoader, TestDataLoader>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true));

        // act
        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.Create()
                    .SetDocument(
                        @"{
                            a: withStackedDataLoader(key: ""a"")
                            b: withStackedDataLoader(key: ""b"")
                        }")
                    .Build()));

        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.Create()
                    .SetDocument(
                        @"{
                            a: withStackedDataLoader(key: ""a"")
                        }")
                    .Build()));

        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.Create()
                    .SetDocument(
                        @"{
                            c: withStackedDataLoader(key: ""c"")
                        }")
                    .Build()));

        // assert
        snapshot.MatchMarkdown();
    }

    [Fact]
    public async Task ClassDataLoader_Resolve_From_DependencyInjection()
    {
        var snapshot = new Snapshot();

        // arrange
        var executor = await CreateExecutorAsync(
            c => c
                .AddQueryType<Query>()
                .AddDataLoader<ITestDataLoader, TestDataLoader>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .UseRequest(
                    next => async context =>
                    {
                        await next(context);

                        var dataLoader = (TestDataLoader)context.Services.GetRequiredService<ITestDataLoader>();

                        context.Result = OperationResultBuilder
                            .FromResult(((IOperationResult)context.Result!))
                            .AddExtension("loads", dataLoader.Loads)
                            .Build();
                    })
                .UseDefaultPipeline());

        // act
        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.Create()
                    .SetDocument(
                        @"{
                            a: dataLoaderWithInterface(key: ""a"")
                            b: dataLoaderWithInterface(key: ""b"")
                        }")
                    .Build()));

        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.Create()
                    .SetDocument(
                        @"{
                            a: dataLoaderWithInterface(key: ""a"")
                        }")
                    .Build()));

        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.Create()
                    .SetDocument(
                        @"{
                            c: dataLoaderWithInterface(key: ""c"")
                        }")
                    .Build()));

        // assert
        snapshot.MatchMarkdown();
    }

    [LocalFact]
    public async Task NestedDataLoader()
    {
        var snapshot = new Snapshot();
        using var cts = new CancellationTokenSource(2000);

        snapshot.Add(
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType()
                .AddType<FooQueries>()
                .AddDataLoader<FooDataLoader>()
                .AddDataLoader<FooNestedDataLoader>()
                .ExecuteRequestAsync("query Foo { foo { id field } }", cancellationToken: cts.Token));

        snapshot.MatchMarkdown();
    }

    [Fact]
    public async Task Ensure_That_DataLoader_Dispatch_Correctly_When_Used_Serially()
    {
        var snapshot = new Snapshot();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType()
                .AddMutationType<SerialMutation>()
                .AddDataLoader<CustomDataLoader>()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildRequestExecutorAsync();

        snapshot.Add(
            await executor.ExecuteAsync(
                @"mutation {
                    a: doSomething(key: ""a"")
                    b: doSomething(key: ""b"")
                }"));

        snapshot.MatchMarkdown();
    }

    [Fact]
    public async Task DataLoader_Request_Ensures_That_There_Is_A_Single_Instance()
    {
        using var cts = new CancellationTokenSource(5000);
        var ct = cts.Token;
        var snapshot = new Snapshot();

        var executor =
            await new ServiceCollection()
                .AddScoped<CounterService>()
                .AddGraphQLServer()
                .AddQueryType<CounterQuery>()
                .AddDataLoader<CounterDataLoader>()
                .BuildRequestExecutorAsync(cancellationToken: ct);

        snapshot.Add(
            await executor.ExecuteAsync(
                """
                {
                    a: do
                    b: do
                    c: do
                    d: do
                    e: do
                }
                """,
                cancellationToken: ct));

        snapshot.MatchMarkdown();
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

        public override void BatchResults<TKey, TValue>(
            IReadOnlyList<TKey> keys,
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

    [SuppressMessage("ReSharper", "InconsistentNaming")]
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

        public static async Task<FooObject?> Get(
            IResolverContext context,
            string id,
            CancellationToken ct)
        {
            return new((await context.DataLoader<FooDataLoader>().LoadAsync(id, ct)).Field);
        }
    }

    public class FooDataLoader : BatchDataLoader<string, FooRecord>
    {
        private readonly FooNestedDataLoader _nestedDataLoader;

        public FooDataLoader(
            IBatchScheduler batchScheduler,
            FooNestedDataLoader nestedDataLoader,
            DataLoaderOptions options)
            : base(batchScheduler, options)
        {
            _nestedDataLoader = nestedDataLoader;
        }

        protected override async Task<IReadOnlyDictionary<string, FooRecord>> LoadBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            return (await _nestedDataLoader.LoadAsync(keys, cancellationToken))
                .ToImmutableDictionary(x => x.Field);
        }
    }

    public class FooNestedDataLoader : BatchDataLoader<string, FooRecord>
    {
        public FooNestedDataLoader(
            IBatchScheduler batchScheduler,
            DataLoaderOptions options)
            : base(batchScheduler, options) { }

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

    public class SerialMutation
    {
        [Serial]
        public async Task<string> DoSomethingAsync(
            CustomDataLoader dataLoader,
            string key,
            CancellationToken cancellationToken)
        {
            var value = await dataLoader.LoadAsync(key, cancellationToken);
            return value;
        }
    }

    public class CustomDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : BatchDataLoader<string, string>(batchScheduler, options)
    {
        protected override Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            var dict = new Dictionary<string, string>();

            foreach (var s in keys)
            {
                dict[s] = s + "_value";
            }

            return Task.FromResult<IReadOnlyDictionary<string, string>>(dict);
        }
    }

    public class CounterQuery
    {
        public Task<string> Do(CounterService service)
            => service.Do();
    }

    public class CounterService
    {
        private readonly CounterDataLoader _dataLoader;

        public CounterService(CounterDataLoader dataLoader)
        {
            _dataLoader = dataLoader;
        }

        public async Task<string> Do() => await _dataLoader.LoadAsync("abc");
    }

    public class CounterDataLoader : CacheDataLoader<string, string>
    {
        public static int Counter;

        public CounterDataLoader(DataLoaderOptions options) : base(options)
            => Interlocked.Increment(ref Counter);

        protected override Task<string> LoadSingleAsync(string key, CancellationToken cancellationToken)
            => Task.FromResult(key + Counter);
    }
}
