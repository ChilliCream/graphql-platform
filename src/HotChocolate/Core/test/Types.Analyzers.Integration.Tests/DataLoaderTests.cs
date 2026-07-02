using System.Collections.Concurrent;
using GreenDonut;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class DataLoaderTests
{
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
