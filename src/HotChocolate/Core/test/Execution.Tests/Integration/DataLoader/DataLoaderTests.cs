using System.Collections.Immutable;
using GreenDonut;
using GreenDonut.DependencyInjection;
using HotChocolate.Fetching;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Tests.TestHelper;
using Snapshot = CookieCrumble.Snapshot;

namespace HotChocolate.Execution.Integration.DataLoader;

public class DataLoaderTests
{
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
                    (_, next) => async context =>
                    {
                        await next(context);

                        var dataLoader =
                            context.RequestServices
                                .GetRequiredService<IDataLoaderScope>()
                                .GetDataLoader<TestDataLoader>(_ => throw new Exception());

                        var result = context.Result.ExpectOperationResult();
                        result.Extensions = result.Extensions.SetItem("loads", dataLoader.Loads);
                    })
                .UseDefaultPipeline());

        // act

        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            a: withDataLoader(key: "a")
                            b: withDataLoader(key: "b")
                            bar {
                                c: withDataLoader(key: "c")
                            }
                        }
                        """)
                    .Build(),
                TestContext.Current.CancellationToken));
        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            a: withDataLoader(key: "a")
                        }
                        """)
                    .Build(),
                TestContext.Current.CancellationToken));
        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            c: withDataLoader(key: "c")
                        }
                        """)
                    .Build(),
                TestContext.Current.CancellationToken));

        // assert
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
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
                (_, next) => async context =>
                {
                    await next(context);

                    var dataLoader =
                        context.RequestServices
                            .GetRequiredService<IDataLoaderScope>()
                            .GetDataLoader<TestDataLoader>(_ => throw new Exception());

                    var result = context.Result.ExpectOperationResult();
                    result.Extensions = result.Extensions.SetItem("loads", dataLoader.Loads);
                })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider();

        // act
        using var serviceScope = services.CreateScope();
        var dataLoader = serviceScope.ServiceProvider.GetRequiredService<ITestDataLoader>();
        var result = await dataLoader.LoadAsync("a", TestContext.Current.CancellationToken);
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
                (_, next) => async context =>
                {
                    await next(context);

                    var dataLoader =
                        context.RequestServices
                            .GetRequiredService<IDataLoaderScope>()
                            .GetDataLoader<TestDataLoader>(_ => throw new Exception());

                    var result = context.Result.ExpectOperationResult();
                    result.Extensions = result.Extensions.SetItem("loads", dataLoader.Loads);
                })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider();

        // act
        using var serviceScope = services.CreateScope();
        var dataLoaderScopeFactory = serviceScope.ServiceProvider.GetRequiredService<IDataLoaderScopeFactory>();
        dataLoaderScopeFactory.BeginScope();

        var dataLoader = serviceScope.ServiceProvider.GetRequiredService<ITestDataLoader>();
        var result = await dataLoader.LoadAsync("a", TestContext.Current.CancellationToken);
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
                (_, next) => async context =>
                {
                    await next(context);

                    var dataLoader =
                        context.RequestServices
                            .GetRequiredService<IDataLoaderScope>()
                            .GetDataLoader<TestDataLoader>(_ => throw new Exception());

                    var result = context.Result.ExpectOperationResult();
                    result.Extensions = result.Extensions.SetItem("loads", dataLoader.Loads);
                })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider();

        // act
        using var serviceScope = services.CreateScope();
        var dataLoader = serviceScope.ServiceProvider.GetRequiredService<ITestDataLoader>();
        var result = await dataLoader.LoadAsync("a", TestContext.Current.CancellationToken);
        Assert.Equal("a", result);
    }

    [Fact]
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
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            a: withStackedDataLoader(key: "a")
                            b: withStackedDataLoader(key: "b")
                        }
                        """)
                    .Build(),
                TestContext.Current.CancellationToken));

        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            a: withStackedDataLoader(key: "a")
                        }
                        """)
                    .Build(),
                TestContext.Current.CancellationToken));

        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            c: withStackedDataLoader(key: "c")
                        }
                        """)
                    .Build(),
                TestContext.Current.CancellationToken));

        // assert
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
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
                    (_, next) => async context =>
                    {
                        await next(context);

                        var dataLoader = (TestDataLoader)context.RequestServices.GetRequiredService<ITestDataLoader>();

                        var result = context.Result.ExpectOperationResult();
                        result.Extensions = result.Extensions.SetItem("loads", dataLoader.Loads);
                    })
                .UseDefaultPipeline());

        // act
        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            a: dataLoaderWithInterface(key: "a")
                            b: dataLoaderWithInterface(key: "b")
                        }
                        """)
                    .Build(),
                TestContext.Current.CancellationToken));

        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            a: dataLoaderWithInterface(key: "a")
                        }
                        """)
                    .Build(),
                TestContext.Current.CancellationToken));

        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            c: dataLoaderWithInterface(key: "c")
                        }
                        """)
                    .Build(),
                TestContext.Current.CancellationToken));

        // assert
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ClassDataLoader_Resolve_From_DependencyInjection_Using_Factory()
    {
        var snapshot = new Snapshot();

        // arrange
        var executor = await CreateExecutorAsync(
            c => c
                .AddQueryType<Query>()
                .AddDataLoader<ITestDataLoader, TestDataLoader>(sp =>
                    new TestDataLoader(
                        sp.GetRequiredService<IBatchScheduler>(),
                        new DataLoaderOptions()))
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .UseRequest(
                    (_, next) => async context =>
                    {
                        await next(context);

                        var dataLoader = (TestDataLoader)context.RequestServices.GetRequiredService<ITestDataLoader>();

                        var result = context.Result.ExpectOperationResult();
                        result.Extensions = result.Extensions.SetItem("loads", dataLoader.Loads);
                    })
                .UseDefaultPipeline());

        // act
        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            a: dataLoaderWithInterface(key: "a")
                            b: dataLoaderWithInterface(key: "b")
                        }
                        """)
                    .Build(),
                TestContext.Current.CancellationToken));

        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            a: dataLoaderWithInterface(key: "a")
                        }
                        """)
                    .Build(),
                TestContext.Current.CancellationToken));

        snapshot.Add(
            await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            c: dataLoaderWithInterface(key: "c")
                        }
                        """)
                    .Build(),
                TestContext.Current.CancellationToken));

        // assert
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NestedDataLoader()
    {
        var snapshot = new Snapshot();

        snapshot.Add(
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType()
                .AddType<FooQueries>()
                .AddDataLoader<FooDataLoader>()
                .AddDataLoader<FooNestedDataLoader>()
                .ExecuteRequestAsync(
                    "query Foo { foo { id field } }",
                    cancellationToken: TestContext.Current.CancellationToken));

        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
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
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        snapshot.Add(
            await executor.ExecuteAsync(
                """
                mutation {
                    a: doSomething(key: "a")
                    b: doSomething(key: "b")
                }
                """,
                cancellationToken: TestContext.Current.CancellationToken));

        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
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

        await snapshot.MatchMarkdownAsync(ct);
    }

    [Fact]
    public async Task GetDataLoader_Should_NotDeadlock_When_ScopedServiceResolvesSameDataLoaderConcurrently()
    {
        // arrange
        var gates = new ConcurrentResolutionGates();
        var services = new ServiceCollection();
        services.AddScoped<LookupDependency>();
        services.AddScoped(sp =>
        {
            // Microsoft DI holds the scope monitor while it runs a scoped factory.
            gates.HandlerEntered.Set();
            gates.Wait(gates.LoaderFactoryEntered);
            return new LookupHandler(sp.GetRequiredService<LookupDataLoader>());
        });
        services
            .AddGraphQLServer()
            .AddQueryType<CounterQuery>()
            .AddDataLoader(sp =>
            {
                if (Interlocked.Increment(ref gates.LoaderFactoryCalls) == 1)
                {
                    gates.LoaderFactoryEntered.Set();
                    gates.Wait(gates.HandlerEntered);
                }

                return new LookupDataLoader(
                    sp.GetRequiredService<IBatchScheduler>(),
                    sp.GetRequiredService<LookupDependency>());
            });

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var dataLoaderScope = scope.ServiceProvider.GetRequiredService<IDataLoaderScope>();

        // act
        var direct = Task.Run(
            dataLoaderScope.GetDataLoader<LookupDataLoader>,
            TestContext.Current.CancellationToken);
        var viaHandler = Task.Run(
            scope.ServiceProvider.GetRequiredService<LookupHandler>,
            TestContext.Current.CancellationToken);
        var completed = Task.WhenAll(direct, viaHandler);
        var finished = await Task.WhenAny(
            completed,
            Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        // assert
        Assert.Same(completed, finished);
        var dataLoader = await direct;
        var handler = await viaHandler;
        Assert.Same(dataLoader, handler.DataLoader);

        scope.Dispose();
        await provider.DisposeAsync();
    }

    public class DataLoaderListener : DataLoaderDiagnosticEventListener
    {
        public bool ResolvedTaskFromCacheTouched;
        public bool ExecuteBatchTouched;
        public bool BatchResultsTouched;
        public bool BatchErrorTouched;
        public bool BatchItemErrorTouched;

        public override void ResolvedTaskFromCache(IDataLoader dataLoader, PromiseCacheKey cacheKey, Task task)
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
            ReadOnlySpan<Result<TValue?>> values)
            where TValue : default
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
    public class FooObject(string field)

    {
        public string id { get; } = field;

        public string field => id;

        public static async Task<FooObject?> Get(
            IResolverContext context,
            string id,
            CancellationToken ct)
            => new((await context.DataLoader<FooDataLoader>().LoadRequiredAsync(id, ct)).Field);
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
            return (await _nestedDataLoader.LoadRequiredAsync(keys, cancellationToken))
                .ToImmutableDictionary(t => t.Field);
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

    public class FooRecord(string field)
    {
        public string Field { get; } = field;
    }

    public class SerialMutation
    {
        [Serial]
        public async Task<string?> DoSomethingAsync(
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
        public Task<string?> Do(CounterService service)
            => service.Do();
    }

    public class CounterService(CounterDataLoader dataLoader)
    {
        public async Task<string?> Do() => await dataLoader.LoadAsync("abc");
    }

    public class CounterDataLoader : CacheDataLoader<string, string>
    {
        public static int Counter;

        public CounterDataLoader(DataLoaderOptions options) : base(options)
            => Interlocked.Increment(ref Counter);

        protected override Task<string> LoadSingleAsync(string key, CancellationToken cancellationToken)
            => Task.FromResult(key + Counter);
    }

    public class Entity
    {
        public int Id { get; set; }
    }

    private sealed class ConcurrentResolutionGates
    {
        public int LoaderFactoryCalls;

        public ManualResetEventSlim HandlerEntered { get; } = new(false);

        public ManualResetEventSlim LoaderFactoryEntered { get; } = new(false);

        public void Wait(ManualResetEventSlim gate)
        {
            if (!gate.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The other resolution path did not start.");
            }
        }
    }

    public sealed class LookupDependency;

    public sealed class LookupHandler(LookupDataLoader dataLoader)
    {
        public LookupDataLoader DataLoader { get; } = dataLoader;
    }

    public sealed class LookupDataLoader(IBatchScheduler batchScheduler, LookupDependency dependency)
        : BatchDataLoader<int, int>(batchScheduler, new DataLoaderOptions())
    {
        protected override Task<IReadOnlyDictionary<int, int>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            GC.KeepAlive(dependency);
            return Task.FromResult<IReadOnlyDictionary<int, int>>(keys.ToDictionary(k => k));
        }
    }
}
