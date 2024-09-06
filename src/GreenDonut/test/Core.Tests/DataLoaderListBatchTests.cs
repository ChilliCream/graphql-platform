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
        var result = await dataLoader.LoadRequiredAsync(
            Enumerable.Range(0, 5000).ToArray(),
            CancellationToken.None);

        // assert
        Assert.Equal(5000, result.Count);
        foreach (var items in result)
        {
            Assert.Equal(Enumerable.Range(0, 500).ToArray(), items.ToArray());
        }
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

    [Fact]
    public static async Task Ensure_No_Buffer_Leakage_When_Batch_Is_Overrun_Async()
    {
        using var cts = new CancellationTokenSource(5000);
        var services = new ServiceCollection()
            .AddDataLoader<TestDataLoader>()
            .BuildServiceProvider();

        await using (var scope1 = services.CreateAsyncScope())
        {
            var dataLoader = scope1.ServiceProvider.GetRequiredService<TestDataLoader>();
            await dataLoader.LoadRequiredAsync(
                Enumerable.Range(0, 5000).ToArray(),
                CancellationToken.None);
        }

        await using (var scope2 = services.CreateAsyncScope())
        {
            var dataLoader = scope2.ServiceProvider.GetRequiredService<TestDataLoader>();
            await dataLoader.LoadRequiredAsync(
                Enumerable.Range(0, 5000).ToArray(),
                CancellationToken.None);
        }
    }

    [Fact]
    public static async Task Ensure_No_Buffer_Leakage_With_Single_Calls_Async()
    {
        using var cts = new CancellationTokenSource(5000);
        var services = new ServiceCollection()
            .AddDataLoader<TestDataLoader>()
            .BuildServiceProvider();

        await using (var scope1 = services.CreateAsyncScope())
        {
            var dataLoader = scope1.ServiceProvider.GetRequiredService<TestDataLoader>();
            await dataLoader.LoadRequiredAsync(1, cts.Token);
        }

        await using (var scope2 = services.CreateAsyncScope())
        {
            var dataLoader = scope2.ServiceProvider.GetRequiredService<TestDataLoader>();
            await dataLoader.LoadRequiredAsync(2, cts.Token);
        }

        await using (var scope3 = services.CreateAsyncScope())
        {
            var dataLoader = scope3.ServiceProvider.GetRequiredService<TestDataLoader>();
            await dataLoader.LoadRequiredAsync(1, cts.Token);
        }
    }

    [Fact]
    public static async Task Ensure_No_Buffer_Leakage_With_Batch_Calls_Async()
    {
        using var cts = new CancellationTokenSource(5000);
        var services = new ServiceCollection()
            .AddDataLoader<TestDataLoader>()
            .BuildServiceProvider();

        await using (var scope1 = services.CreateAsyncScope())
        {
            var dataLoader = scope1.ServiceProvider.GetRequiredService<TestDataLoader>();
            await dataLoader.LoadRequiredAsync(
                Enumerable.Range(0, 1).ToArray(),
                CancellationToken.None);
        }

        await using (var scope2 = services.CreateAsyncScope())
        {
            var dataLoader = scope2.ServiceProvider.GetRequiredService<TestDataLoader>();
            await dataLoader.LoadRequiredAsync(
                Enumerable.Range(0, 1).ToArray(),
                CancellationToken.None);
        }
    }

    private sealed class TestDataLoader(
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
