using GreenDonut;
using HotChocolate.Fetching;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class DependencyInjectionTests
{
    [Fact]
    public async Task Extension_With_Constructor_Injection()
    {
        // this test ensures that we inject services into type instances without the need of
        // registering the type into the dependency container.
        var executor =
            await new ServiceCollection()
                .AddSingleton<SomeService>()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddType<ExtendQuery1>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        new
        {
            result1 = await executor
                .ExecuteAsync("{ hello }", TestContext.Current.CancellationToken)
                .ToJsonAsync(),
            result2 = await executor
                .ExecuteAsync("{ hello }", TestContext.Current.CancellationToken)
                .ToJsonAsync()
        }.MatchSnapshot();
    }

    [Fact]
    public async Task Extension_With_Scoped_Constructor_Injection()
    {
        IServiceProvider services =
            new ServiceCollection()
                .AddScoped<SomeService>()
                .AddScoped<ExtendQuery1>()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddType<ExtendQuery1>()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = new string[2];

        using (var scope = services.CreateScope())
        {
            result[0] = await executor
                .ExecuteAsync(
                    OperationRequestBuilder
                        .New()
                        .SetDocument("{ hello }")
                        .SetServices(scope.ServiceProvider)
                        .Build(),
                    TestContext.Current.CancellationToken)
                .ToJsonAsync();
        }

        using (var scope = services.CreateScope())
        {
            result[1] = await executor
                .ExecuteAsync(
                    OperationRequestBuilder
                        .New()
                        .SetDocument("{ hello }")
                        .SetServices(scope.ServiceProvider)
                        .Build(),
                    TestContext.Current.CancellationToken)
                .ToJsonAsync();
        }

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Type_With_Constructor_Injection()
    {
        // this test ensures that we inject services into type instances without the need of
        // registering the type into the dependency container.
        var executor =
            await new ServiceCollection()
                .AddSingleton<SomeService>()
                .AddGraphQL()
                .AddQueryType<Query2>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        new
        {
            result1 = await executor
                .ExecuteAsync("{ hello }", TestContext.Current.CancellationToken)
                .ToJsonAsync(),
            result2 = await executor
                .ExecuteAsync("{ hello }", TestContext.Current.CancellationToken)
                .ToJsonAsync()
        }.MatchSnapshot();
    }

    [Fact]
    public async Task Type_With_Scoped_Constructor_Injection()
    {
        IServiceProvider services =
            new ServiceCollection()
                .AddScoped<SomeService>()
                .AddScoped<Query2>()
                .AddGraphQL()
                .AddQueryType<Query2>()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = new string[2];

        using (var scope = services.CreateScope())
        {
            result[0] = await executor
                .ExecuteAsync(
                    OperationRequestBuilder
                        .New()
                        .SetDocument("{ hello }")
                        .SetServices(scope.ServiceProvider)
                        .Build(),
                    TestContext.Current.CancellationToken)
                .ToJsonAsync();
        }

        using (var scope = services.CreateScope())
        {
            result[1] = await executor
                .ExecuteAsync(
                    OperationRequestBuilder
                        .New()
                        .SetDocument("{ hello }")
                        .SetServices(scope.ServiceProvider)
                        .Build(),
                    TestContext.Current.CancellationToken)
                .ToJsonAsync();
        }

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Keyed_Services_Do_Not_Throw()
    {
        var services =
            new ServiceCollection()
                .AddKeyedScoped<Query1>("abc")
                .AddScoped<SomeService>()
                .AddScoped<Query2>()
                .AddGraphQL()
                .AddQueryType<Query2>()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        using var scope = services.CreateScope();

        await executor
            .ExecuteAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ hello }")
                    .SetServices(scope.ServiceProvider)
                    .Build(),
                TestContext.Current.CancellationToken)
            .ToJsonAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Keyed_DataLoader_Service_Is_Resolved()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddKeyedSingleton<KeyedService>("keyed")
                .AddGraphQL()
                .AddQueryType<KeyedDataLoaderQuery>()
                .AddDataLoader<KeyedDataLoader>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ value }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "value": "keyed"
              }
            }
            """);
    }

    [Fact]
    public async Task Custom_DataLoader_Factory_Can_Create_Keyed_DataLoader()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddKeyedSingleton<KeyedService>("keyed")
                .AddGraphQL()
                .AddQueryType<KeyedDataLoaderQuery>()
                .AddDataLoader<KeyedDataLoader>(serviceProvider =>
                {
                    var serviceInspector = serviceProvider.GetRequiredService<IServiceProviderIsService>();
                    var keyedServiceInspector =
                        serviceProvider.GetRequiredService<IServiceProviderIsKeyedService>();

                    Assert.Same(serviceInspector, keyedServiceInspector);
                    Assert.IsAssignableFrom<IServiceProviderIsKeyedService>(serviceInspector);
                    Assert.True(keyedServiceInspector.IsKeyedService(typeof(KeyedService), "keyed"));

                    return ActivatorUtilities.CreateInstance<KeyedDataLoader>(serviceProvider);
                })
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ value }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "value": "keyed"
              }
            }
            """);
    }

    [Fact]
    public async Task Unkeyed_DataLoader_Service_Is_Resolved_From_NonKeyed_Provider()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddScoped<SomeService>()
                .AddGraphQL()
                .AddQueryType<UnkeyedDataLoaderQuery>()
                .AddDataLoader<UnkeyedDataLoader>()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        using var scope = services.CreateScope();

        var serviceProvider = new NonKeyedServiceProvider(scope.ServiceProvider);
        var serviceInspector = serviceProvider.GetRequiredService<IServiceProviderIsService>();
        Assert.True(serviceInspector.IsService(typeof(IBatchDispatcher)));

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument("{ value }")
                .SetServices(serviceProvider)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "value": "Hello_0"
              }
            }
            """);
    }

    public class SomeService
    {
        private int _i;

        public int Count => _i;

        public string SayHello() => "Hello_" + _i++;
    }

    public class Query1;

    [ExtendObjectType(typeof(Query1))]
    public class ExtendQuery1
    {
        private readonly SomeService _service;

        public ExtendQuery1(SomeService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public string Hello() => _service.SayHello();
    }

    public class Query2
    {
        private readonly SomeService _service;

        public Query2(SomeService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public string Hello() => _service.SayHello();
    }

    public class KeyedService
    {
        public string Value => "keyed";
    }

    public class KeyedDataLoaderQuery
    {
        public async Task<string?> GetValueAsync(
            KeyedDataLoader dataLoader,
            CancellationToken cancellationToken)
            => await dataLoader.LoadAsync("key", cancellationToken);
    }

    public class KeyedDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options,
        [FromKeyedServices("keyed")] KeyedService service)
        : BatchDataLoader<string, string>(batchScheduler, options)
    {
        protected override Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            var results = new Dictionary<string, string>();

            foreach (var key in keys)
            {
                results[key] = service.Value;
            }

            return Task.FromResult<IReadOnlyDictionary<string, string>>(results);
        }
    }

    public class UnkeyedDataLoaderQuery
    {
        public async Task<string?> GetValueAsync(
            UnkeyedDataLoader dataLoader,
            CancellationToken cancellationToken)
            => await dataLoader.LoadAsync("key", cancellationToken);
    }

    public class UnkeyedDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options,
        SomeService service)
        : BatchDataLoader<string, string>(batchScheduler, options)
    {
        protected override Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            var results = new Dictionary<string, string>();

            foreach (var key in keys)
            {
                results[key] = service.SayHello();
            }

            return Task.FromResult<IReadOnlyDictionary<string, string>>(results);
        }
    }

    private sealed class NonKeyedServiceProvider(IServiceProvider innerServiceProvider)
        : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(IServiceProviderIsKeyedService)
                ? null
                : innerServiceProvider.GetService(serviceType);
    }
}
