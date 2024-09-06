using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GreenDonut;

public static class DataLoaderListBatchTests
{
    [Fact]
    public static async Task Overflow_InternalBatch_Async()
    {
        // arrange
        using var cts = new CancellationTokenSource(5000);
        var services = new ServiceCollection()
            .AddDataLoader<TestDataLoader>()
            .BuildServiceProvider();
        var dataLoader = services.GetRequiredService<TestDataLoader>();

        // act
        var result = await dataLoader.LoadAsync(
            Enumerable.Range(0, 5000).ToArray(),
            CancellationToken.None);

        // assert
        Assert.Equal(5000, result.Count);
    }

    [Fact]
    public static async Task Ensure_Multiple_Large_Batches_Can_Be_Enqueued_Concurrently_Async()
    {
        // arrange
        using var cts = new CancellationTokenSource(5000);
        var ct = cts.Token;
        var services = new ServiceCollection()
            .AddDataLoader<TestDataLoader>()
            .BuildServiceProvider();
        var dataLoader = services.GetRequiredService<TestDataLoader>();

        // act
        List<Task> tasks = new();
        foreach (var _ in Enumerable.Range(0, 10))
        {
            tasks.Add(
                Task.Run(
                    async () =>
                    {
                        var result = await dataLoader.LoadAsync(Enumerable.Range(0, 5000).ToArray(), ct);

                        // assert
                        Assert.Equal(5000, result.Count);
                    },
                    ct));
        }

        await Task.WhenAll(tasks);
    }


    public sealed class TestDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : BatchDataLoader<int, int[]>(batchScheduler, options)
    {
        protected override async Task<IReadOnlyDictionary<int, int[]>> LoadBatchAsync(
            IReadOnlyList<int> runNumbers,
            CancellationToken cancellationToken)
        {
            await Task.Delay(300, cancellationToken).ConfigureAwait(false);

            return runNumbers
                .Select(t => (t, Enumerable.Range(0, 500)))
                .ToDictionary(t => t.Item1, t => t.Item2.ToArray());
        }
    }
}
