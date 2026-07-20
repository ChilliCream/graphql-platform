using System.Collections.Concurrent;
using GreenDonut;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Integration.DataLoader;

/// <summary>
/// Temporary experiment for GitHub discussion #9793 (DataLoader batch fragmentation).
/// These tests are measurement instruments, not regression tests: they record the
/// number of keys per FetchAsync call and print the per-run batch sizes. They do not
/// assert on batch counts and always pass. This file is not meant to be committed.
/// </summary>
public class DataLoaderBatchAggregationExperimentTests(ITestOutputHelper output)
{
    private const int ItemCount = 24;
    private const int RunCount = 15;
    private const int ExtendedRunCount = 30;

    [Fact]
    public async Task BatchAggregationExperiment_Staggered_Delay_Before_LoadAsync()
    {
        await RunExperimentAsync(
            "child",
            "staggered: await Task.Delay(1 + (id % 4)) before LoadAsync",
            RunCount);
    }

    [Fact]
    public async Task BatchAggregationExperiment_Control_No_Delay_Before_LoadAsync()
    {
        await RunExperimentAsync(
            "childFast",
            "control: LoadAsync called immediately",
            RunCount);
    }

    [Fact]
    public async Task BatchAggregationExperiment_YieldStaggered_Before_LoadAsync()
    {
        await RunExperimentAsync(
            "childYield",
            "yield staggered: (1 + (id % 4)) x await Task.Yield() before LoadAsync",
            ExtendedRunCount);
    }

    [Fact]
    public async Task BatchAggregationExperiment_TwoLevelChain_No_Delay()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            c => c
                .AddQueryType<ChainQuery>()
                .AddDataLoader<ChainDataLoaderA>()
                .AddDataLoader<ChainDataLoaderB>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true));

        const string document = "{ items { child { grand } } }";
        var report = new List<string>();

        // act
        for (var run = 1; run <= ExtendedRunCount; run++)
        {
            ChainDataLoaderA.BatchSizes.Clear();
            ChainDataLoaderB.BatchSizes.Clear();

            var result = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(document)
                    .Build(),
                TestContext.Current.CancellationToken);

            var errorCount = result.ExpectOperationResult().Errors?.Count ?? 0;
            var sizesA = ChainDataLoaderA.BatchSizes.ToArray();
            var sizesB = ChainDataLoaderB.BatchSizes.ToArray();

            report.Add(
                $"run {run,2}: "
                + $"A: fetches={sizesA.Length,2} sizes=[{string.Join(", ", sizesA)}] "
                + $"B: fetches={sizesB.Length,2} sizes=[{string.Join(", ", sizesB)}] "
                + $"errors={errorCount}");
        }

        // assert
        // Measurement instrument: the printed numbers are the deliverable.
        WriteLine(
            "Experiment: two-level chain: child -> LoaderA, grand -> LoaderB, no delays "
            + $"| items={ItemCount} runs={ExtendedRunCount}");

        foreach (var line in report)
        {
            WriteLine(line);
        }
    }

    [Fact]
    public async Task BatchAggregationExperiment_ConcurrentLoad_YieldStaggered_Slow_Fetch()
    {
        // arrange
        const int concurrentRequests = 16;
        const int iterationCount = 10;

        var executor = await CreateExecutorAsync(
            c => c
                .AddQueryType<ExperimentQuery>()
                .AddDataLoader<ExperimentDataLoader>()
                .AddDataLoader<SlowFetchExperimentDataLoader>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true));

        const string document = "{ items { childYieldSlow } }";
        var report = new List<string>();

        // act
        for (var iteration = 1; iteration <= iterationCount; iteration++)
        {
            SlowFetchExperimentDataLoader.BatchSizes.Clear();

            var requests = Enumerable.Range(0, concurrentRequests)
                .Select(_ => executor.ExecuteAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(document)
                        .Build(),
                    TestContext.Current.CancellationToken))
                .ToArray();

            var results = await Task.WhenAll(requests);

            var errorCount =
                results.Sum(r => r.ExpectOperationResult().Errors?.Count ?? 0);
            var sizes = SlowFetchExperimentDataLoader.BatchSizes
                .OrderByDescending(size => size)
                .ToArray();

            report.Add(
                $"iter {iteration,2}: totalFetches={sizes.Length,2} "
                + $"ideal={concurrentRequests} "
                + $"sizes=[{string.Join(", ", sizes)}] errors={errorCount}");
        }

        // assert
        // Measurement instrument: the printed numbers are the deliverable.
        WriteLine(
            "Experiment: concurrent load: 16 parallel requests, yield-staggered "
            + "resolver, fetch awaits Task.Delay(2) "
            + $"| items={ItemCount} iterations={iterationCount}");

        foreach (var line in report)
        {
            WriteLine(line);
        }
    }

    private async Task RunExperimentAsync(string fieldName, string label, int runCount)
    {
        // arrange
        var executor = await CreateExecutorAsync(
            c => c
                .AddQueryType<ExperimentQuery>()
                .AddDataLoader<ExperimentDataLoader>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true));

        var document = $"{{ items {{ {fieldName} }} }}";
        var report = new List<string>();

        // act
        for (var run = 1; run <= runCount; run++)
        {
            ExperimentDataLoader.BatchSizes.Clear();

            var result = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(document)
                    .Build(),
                TestContext.Current.CancellationToken);

            var errorCount = result.ExpectOperationResult().Errors?.Count ?? 0;
            var sizes = ExperimentDataLoader.BatchSizes.ToArray();

            report.Add(
                $"run {run,2}: fetches={sizes.Length,2} "
                + $"sizes=[{string.Join(", ", sizes)}] errors={errorCount}");
        }

        // assert
        // Measurement instrument: the printed numbers are the deliverable.
        WriteLine($"Experiment: {label} | items={ItemCount} runs={runCount}");

        foreach (var line in report)
        {
            WriteLine(line);
        }
    }

    private void WriteLine(string line)
    {
        output.WriteLine(line);
        Console.WriteLine(line);
    }

    public class ExperimentQuery
    {
        public IEnumerable<ExperimentItem> GetItems()
            => Enumerable.Range(1, ItemCount).Select(i => new ExperimentItem(i));
    }

    public class ExperimentItem(int id)
    {
        public int Id { get; } = id;

        public async Task<string?> GetChild(
            ExperimentDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            await Task.Delay(1 + (Id % 4), cancellationToken);
            return await dataLoader.LoadAsync(Id, cancellationToken);
        }

        public Task<string?> GetChildFast(
            ExperimentDataLoader dataLoader,
            CancellationToken cancellationToken)
            => dataLoader.LoadAsync(Id, cancellationToken);

        public async Task<string?> GetChildYield(
            ExperimentDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            // stagger arrivals via thread-pool queue hops instead of timers
            for (var i = 0; i <= Id % 4; i++)
            {
                await Task.Yield();
            }

            return await dataLoader.LoadAsync(Id, cancellationToken);
        }

        public async Task<string?> GetChildYieldSlow(
            SlowFetchExperimentDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            // stagger arrivals via thread-pool queue hops instead of timers
            for (var i = 0; i <= Id % 4; i++)
            {
                await Task.Yield();
            }

            return await dataLoader.LoadAsync(Id, cancellationToken);
        }
    }

    public class ExperimentDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : BatchDataLoader<int, string>(batchScheduler, options)
    {
        public static readonly ConcurrentQueue<int> BatchSizes = new();

        protected override Task<IReadOnlyDictionary<int, string>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            BatchSizes.Enqueue(keys.Count);

            return Task.FromResult<IReadOnlyDictionary<int, string>>(
                keys.ToDictionary(key => key, key => $"Item {key}"));
        }
    }

    public class SlowFetchExperimentDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : BatchDataLoader<int, string>(batchScheduler, options)
    {
        public static readonly ConcurrentQueue<int> BatchSizes = new();

        protected override async Task<IReadOnlyDictionary<int, string>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            BatchSizes.Enqueue(keys.Count);

            // simulate database latency
            await Task.Delay(2, cancellationToken);

            return keys.ToDictionary(key => key, key => $"Item {key}");
        }
    }

    public class ChainQuery
    {
        public IEnumerable<ChainItem> GetItems()
            => Enumerable.Range(1, ItemCount).Select(i => new ChainItem(i));
    }

    public class ChainItem(int id)
    {
        public int Id { get; } = id;

        public async Task<ChainChild> GetChild(
            ChainDataLoaderA dataLoader,
            CancellationToken cancellationToken)
        {
            var childId = await dataLoader.LoadAsync(Id, cancellationToken);
            return new ChainChild(childId);
        }
    }

    public class ChainChild(int childId)
    {
        public int ChildId { get; } = childId;

        public async Task<string?> GetGrand(
            ChainDataLoaderB dataLoader,
            CancellationToken cancellationToken)
            => await dataLoader.LoadAsync(ChildId, cancellationToken);
    }

    public class ChainDataLoaderA(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : BatchDataLoader<int, int>(batchScheduler, options)
    {
        public static readonly ConcurrentQueue<int> BatchSizes = new();

        protected override Task<IReadOnlyDictionary<int, int>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            BatchSizes.Enqueue(keys.Count);

            return Task.FromResult<IReadOnlyDictionary<int, int>>(
                keys.ToDictionary(key => key, key => key * 100));
        }
    }

    public class ChainDataLoaderB(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : BatchDataLoader<int, string>(batchScheduler, options)
    {
        public static readonly ConcurrentQueue<int> BatchSizes = new();

        protected override Task<IReadOnlyDictionary<int, string>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            BatchSizes.Enqueue(keys.Count);

            return Task.FromResult<IReadOnlyDictionary<int, string>>(
                keys.ToDictionary(key => key, key => $"v{key}"));
        }
    }
}
