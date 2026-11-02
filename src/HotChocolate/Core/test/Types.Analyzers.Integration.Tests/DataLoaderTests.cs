using System.Collections.Concurrent;
using System.Xml.Linq;
using GreenDonut;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class DataLoaderTests
{
    [Fact]
    public void GeneratedDataLoaders_Should_DocumentOnlyImplementation_When_XmlDocumentationIsGenerated()
    {
        // arrange
        var documentationPath = System.IO.Path.ChangeExtension(
            typeof(DataLoaderTests).Assembly.Location,
            ".xml");
        var document = XDocument.Load(documentationPath);
        var generatedTypeNames = new[]
        {
            "T:HotChocolate.Types.IValueByKeyDataLoader",
            "T:HotChocolate.Types.ValueByKeyDataLoader"
        };

        // act
        var documentation = document
            .Descendants("member")
            .Where(m => generatedTypeNames.Contains(m.Attribute("name")?.Value))
            .OrderBy(m => m.Attribute("name")?.Value, StringComparer.Ordinal)
            .Select(m =>
            {
                var summary = m.Element("summary")!;
                return $"""
                    {m.Attribute("name")!.Value}
                    Summary: {summary.Value.Trim()}
                    Cref: {summary.Element("see")!.Attribute("cref")!.Value}
                    """;
            });

        // assert
        string.Join(Environment.NewLine, documentation).MatchInlineSnapshot(
            """
            T:HotChocolate.Types.ValueByKeyDataLoader
            Summary: A DataLoader generated from .
            Cref: M:HotChocolate.Types.DataLoaders.GetValueByKeyAsync(System.Collections.Generic.IReadOnlyList{System.Int32},System.Threading.CancellationToken)
            """);
    }

    [Fact]
    public async Task DataLoader_Should_Split_Keys_Into_Batches_When_MaxBatchSize_Is_Set()
    {
        // arrange
        DataLoaders.RecordedBatchSizes.Clear();

        var dataLoader = new ValueByKeyDataLoader(
            new ServiceCollection().BuildServiceProvider(),
            AutoBatchScheduler.Default,
            new DataLoaderOptions());

        // act
        var result = await dataLoader.LoadAsync(
            new[] { 1, 2, 3, 4, 5 },
            TestContext.Current.CancellationToken);

        // assert
        // MaxBatchSize = 2 splits the five keys into batches of sizes 2, 2 and 1.
        Assert.Equal(new[] { 1, 2, 2 }, DataLoaders.RecordedBatchSizes.OrderBy(x => x));
        Assert.Equal(new[] { "1", "2", "3", "4", "5" }, result);
    }

    [Fact]
    public async Task GenericDataLoader_Should_Load_BatchValue_When_ClosedBatchInterfaceIsUsedDirectly()
    {
        // arrange
        await using var serviceProvider = new ServiceCollection()
            .AddIntegrationTestTypesCore()
            .BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var dataLoader = scope.ServiceProvider.GetRequiredService<IBatchDataLoader<int, string>>();

        // act
        var result = await dataLoader.LoadAsync(1, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("batch-1", result);
    }

    [Fact]
    public async Task GenericDataLoader_Should_Load_CacheValue_When_ClosedCacheInterfaceIsUsedDirectly()
    {
        // arrange
        await using var serviceProvider = new ServiceCollection()
            .AddIntegrationTestTypesCore()
            .BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var dataLoader = scope.ServiceProvider.GetRequiredService<ICacheDataLoader<int, string>>();

        // act
        var result = await dataLoader.LoadAsync(2, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("cache-2", result);
    }

    [Fact]
    public async Task GenericDataLoader_Should_Load_ArrayValue_When_BatchInterfaceUsesAnArrayValue()
    {
        // arrange
        await using var serviceProvider = new ServiceCollection()
            .AddIntegrationTestTypesCore()
            .BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var dataLoader = scope.ServiceProvider.GetRequiredService<IBatchDataLoader<int, string[]>>();

        // act
        var result = await dataLoader.LoadAsync(3, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(new[] { "array-3", "array-4" }, result);
    }

    [Fact]
    public async Task GenericDataLoader_Should_Resolve_UniqueInterface_When_OnlyOneLoaderUsesIt()
    {
        // arrange
        await using var serviceProvider = new ServiceCollection()
            .AddIntegrationTestTypesCore()
            .BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var dataLoader = scope.ServiceProvider.GetRequiredService<IUniqueValueDataLoader>();

        // act
        var result = await dataLoader.LoadAsync(4, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("unique-4", result);
    }

    [Fact]
    public async Task GenericDataLoader_Should_Resolve_ConcreteLoaders_When_MultipleLoadersShareAnInterface()
    {
        // arrange
        await using var serviceProvider = new ServiceCollection()
            .AddIntegrationTestTypesCore()
            .BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var first = services.GetRequiredService<FirstSharedValueDataLoader>();
        var second = services.GetRequiredService<SecondSharedValueDataLoader>();

        // act
        var results = await Task.WhenAll(
            first.LoadAsync(5, TestContext.Current.CancellationToken),
            second.LoadAsync(5, TestContext.Current.CancellationToken));

        // assert
        Assert.Null(services.GetService<ISharedValueDataLoader>());
        Assert.Equal(new[] { "first-5", "second-5" }, results);
    }
}

public static class DataLoaders
{
    public static readonly ConcurrentQueue<int> RecordedBatchSizes = new();

    [DataLoader(MaxBatchSize = 2)]
    public static Task<IReadOnlyDictionary<int, string>> GetValueByKeyAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        RecordedBatchSizes.Enqueue(keys.Count);
        IReadOnlyDictionary<int, string> result = keys.ToDictionary(k => k, k => k.ToString());
        return Task.FromResult(result);
    }
}

public interface IUniqueValueDataLoader : IBatchDataLoader<int, string>;

public interface ISharedValueDataLoader : IBatchDataLoader<int, string>;

public static class GenericDataLoaders
{
    [DataLoader<IBatchDataLoader<int, string>>]
    public static Task<IReadOnlyDictionary<int, string>> GetDirectBatchValueAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyDictionary<int, string>>(
            keys.ToDictionary(key => key, key => $"batch-{key}"));

    [DataLoader<ICacheDataLoader<int, string>>]
    public static Task<string> GetDirectCacheValueAsync(int key, CancellationToken cancellationToken)
        => Task.FromResult($"cache-{key}");

    [DataLoader<IBatchDataLoader<int, string[]>>]
    public static Task<IReadOnlyDictionary<int, string[]>> GetArrayValuesAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyDictionary<int, string[]>>(
            keys.ToDictionary(key => key, key => new[] { $"array-{key}", $"array-{key + 1}" }));

    [DataLoader<IUniqueValueDataLoader>]
    public static Task<IReadOnlyDictionary<int, string>> GetUniqueValueAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyDictionary<int, string>>(
            keys.ToDictionary(key => key, key => $"unique-{key}"));

    [DataLoader<ISharedValueDataLoader>]
    public static Task<IReadOnlyDictionary<int, string>> GetFirstSharedValueAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyDictionary<int, string>>(
            keys.ToDictionary(key => key, key => $"first-{key}"));

    [DataLoader<ISharedValueDataLoader>]
    public static Task<IReadOnlyDictionary<int, string>> GetSecondSharedValueAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyDictionary<int, string>>(
            keys.ToDictionary(key => key, key => $"second-{key}"));
}
