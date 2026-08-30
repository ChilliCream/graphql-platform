using GreenDonut;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class KeyedDataLoaderIntegrationTests
{
    [Fact]
    public async Task GeneratedDataLoader_Should_Resolve_ServiceAttribute_Keyed_Service()
    {
        // arrange
        var result = await ExecuteAsync(
            services => services.AddKeyedSingleton<KeyedDataLoaderService>(
                "service",
                static (_, _) => new("service")),
            "{ serviceKeyedValue }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "serviceKeyedValue": "service"
              }
            }
            """);
    }

    [Fact]
    public async Task GeneratedDataLoader_Should_Resolve_SourceDerived_ServiceAttribute_Keyed_Service()
    {
        // arrange
        var result = await ExecuteAsync(
            services => services.AddKeyedSingleton<KeyedDataLoaderService>(
                "MEMOIZED",
                static (_, _) => new("memoized")),
            "{ derivedServiceKeyedValue }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "derivedServiceKeyedValue": "memoized"
              }
            }
            """);
    }

    [Fact]
    public async Task GeneratedDataLoader_Should_Resolve_FromKeyedServices_Enum_Key()
    {
        // arrange
        var result = await ExecuteAsync(
            services => services.AddKeyedSingleton<KeyedDataLoaderService>(
                KeyedDataLoaderServiceKey.Enum,
                static (_, _) => new("enum")),
            "{ enumKeyedValue }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "enumKeyedValue": "enum"
              }
            }
            """);
    }

    [Fact]
    public async Task GeneratedDataLoader_Should_Resolve_Null_When_Nullable_Keyed_Service_Is_Absent()
    {
        // arrange
        var result = await ExecuteAsync(static _ => { }, "{ optionalKeyedValue }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "optionalKeyedValue": "absent"
              }
            }
            """);
    }

    [Fact]
    public async Task GeneratedDataLoader_Should_Resolve_Value_When_Nullable_Keyed_Service_Is_Present()
    {
        // arrange
        var result = await ExecuteAsync(
            services => services.AddKeyedSingleton<KeyedDataLoaderService>(
                "optional",
                static (_, _) => new("optional")),
            "{ optionalKeyedValue }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "optionalKeyedValue": "optional"
              }
            }
            """);
    }

    [Fact]
    public async Task GeneratedDataLoader_Should_Resolve_Keyed_Scoped_Service_When_Using_OriginalScope()
    {
        // arrange
        var result = await ExecuteAsync(
            services => services.AddKeyedScoped<KeyedDataLoaderService>(
                "original-scope",
                static (_, _) => new("original-scope")),
            "{ originalScopeKeyedValue }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "originalScopeKeyedValue": "original-scope"
              }
            }
            """);
    }

    [Fact]
    public async Task HandWrittenDataLoader_Should_Resolve_FromKeyedServices_Keyed_Service()
    {
        // arrange
        var result = await ExecuteAsync(
            services => services.AddKeyedSingleton<KeyedDataLoaderService>(
                "hand-written",
                static (_, _) => new("hand-written")),
            "{ handWrittenKeyedValue }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "handWrittenKeyedValue": "hand-written"
              }
            }
            """);
    }

    private static async Task<IExecutionResult> ExecuteAsync(
        Action<IServiceCollection> configure,
        string document)
    {
        var services = new ServiceCollection();
        configure(services);

        var executor = await services
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddDataLoader<HandWrittenKeyedDataLoader>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        return await executor.ExecuteAsync(document, TestContext.Current.CancellationToken);
    }
}

public static partial class Query
{
    public static Task<string?> GetServiceKeyedValueAsync(
        ServiceKeyedValueDataLoader loader,
        CancellationToken cancellationToken)
        => loader.LoadAsync(1, cancellationToken);

    public static Task<string?> GetDerivedServiceKeyedValueAsync(
        DerivedServiceKeyedValueDataLoader loader,
        CancellationToken cancellationToken)
        => loader.LoadAsync(1, cancellationToken);

    public static Task<string?> GetEnumKeyedValueAsync(
        EnumKeyedValueDataLoader loader,
        CancellationToken cancellationToken)
        => loader.LoadAsync(1, cancellationToken);

    public static Task<string?> GetOptionalKeyedValueAsync(
        OptionalKeyedValueDataLoader loader,
        CancellationToken cancellationToken)
        => loader.LoadAsync(1, cancellationToken);

    public static Task<string?> GetOriginalScopeKeyedValueAsync(
        OriginalScopeKeyedValueDataLoader loader,
        CancellationToken cancellationToken)
        => loader.LoadAsync(1, cancellationToken);

    public static Task<string?> GetHandWrittenKeyedValueAsync(
        HandWrittenKeyedDataLoader loader,
        CancellationToken cancellationToken)
        => loader.LoadAsync(1, cancellationToken);
}

public static class KeyedDataLoaders
{
    [DataLoader]
    public static Task<IReadOnlyDictionary<int, string>> GetServiceKeyedValueAsync(
        IReadOnlyList<int> keys,
        [Service("service")] KeyedDataLoaderService service,
        CancellationToken cancellationToken)
        => Task.FromResult(CreateValues(keys, service.Value));

    [DataLoader]
    public static Task<IReadOnlyDictionary<int, string>> GetDerivedServiceKeyedValueAsync(
        IReadOnlyList<int> keys,
        [Memoized] KeyedDataLoaderService service,
        CancellationToken cancellationToken)
        => Task.FromResult(CreateValues(keys, service.Value));

    [DataLoader]
    public static Task<IReadOnlyDictionary<int, string>> GetEnumKeyedValueAsync(
        IReadOnlyList<int> keys,
        [FromKeyedServices(KeyedDataLoaderServiceKey.Enum)] KeyedDataLoaderService service,
        CancellationToken cancellationToken)
        => Task.FromResult(CreateValues(keys, service.Value));

    [DataLoader]
    public static Task<IReadOnlyDictionary<int, string>> GetOptionalKeyedValueAsync(
        IReadOnlyList<int> keys,
        [Service("optional")] KeyedDataLoaderService? service,
        CancellationToken cancellationToken)
        => Task.FromResult(CreateValues(keys, service?.Value ?? "absent"));

    [DataLoader(ServiceScope = DataLoaderServiceScope.OriginalScope)]
    public static Task<IReadOnlyDictionary<int, string>> GetOriginalScopeKeyedValueAsync(
        IReadOnlyList<int> keys,
        [Service("original-scope")] KeyedDataLoaderService service,
        CancellationToken cancellationToken)
        => Task.FromResult(CreateValues(keys, service.Value));

    internal static IReadOnlyDictionary<int, string> CreateValues(
        IReadOnlyList<int> keys,
        string value)
        => keys.ToDictionary(key => key, _ => value);
}

public sealed class HandWrittenKeyedDataLoader(
    IBatchScheduler batchScheduler,
    DataLoaderOptions options,
    [FromKeyedServices("hand-written")] KeyedDataLoaderService service)
    : BatchDataLoader<int, string>(batchScheduler, options)
{
    protected override Task<IReadOnlyDictionary<int, string>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
        => Task.FromResult(KeyedDataLoaders.CreateValues(keys, service.Value));
}

public sealed record KeyedDataLoaderService(string Value);

public enum KeyedDataLoaderServiceKey
{
    Enum
}

public sealed class MemoizedAttribute() : ServiceAttribute("MEMOIZED");
