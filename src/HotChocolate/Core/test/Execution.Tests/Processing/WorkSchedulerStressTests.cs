using System.Collections.Concurrent;
using System.Diagnostics;
using GreenDonut;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Processing;

// Opt-in stress test for the completion bookkeeping of the WorkScheduler. It only runs when
// HC_STRESS_TESTS=1 is set; HC_STRESS_SECONDS overrides the default duration of 20 seconds.
// It lives in its own non-parallel collection because it observes process-wide unobserved task
// exceptions, which concurrently running tests could otherwise contribute to.
[Collection(WorkSchedulerStressCollection.Name)]
public class WorkSchedulerStressTests(ITestOutputHelper output)
{
    private const string EnableVariable = "HC_STRESS_TESTS";
    private const string SecondsVariable = "HC_STRESS_SECONDS";
    private const int DefaultSeconds = 20;
    private const int Concurrency = 64;
    private static readonly TimeSpan s_requestTimeout = TimeSpan.FromSeconds(30);

    private const string Query =
        """
        {
          items(count: 16) {
            id
            name
            related { id name }
            child { id name related { id } }
          }
        }
        """;

    [Fact]
    public async Task Execute_Should_NotProduceUnobservedExceptions_When_RequestsRunConcurrently()
    {
        // arrange
        if (Environment.GetEnvironmentVariable(EnableVariable) != "1")
        {
            Assert.Skip($"Set {EnableVariable}=1 to run the WorkScheduler stress test.");
        }

        var duration = TimeSpan.FromSeconds(
            int.TryParse(Environment.GetEnvironmentVariable(SecondsVariable), out var seconds) && seconds > 0
                ? seconds
                : DefaultSeconds);
        var cancellationToken = TestContext.Current.CancellationToken;

        var unobserved = new ConcurrentQueue<string>();
        var failures = new ConcurrentQueue<string>();

        void OnUnobservedException(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            foreach (var exception in args.Exception.InnerExceptions)
            {
                unobserved.Enqueue(exception.ToString());
            }

            args.SetObserved();
        }

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<StressQuery>()
            .AddDataLoader<StressRelatedDataLoader>()
            .BuildRequestExecutorAsync(cancellationToken: cancellationToken);

        long requests = 0;
        TaskScheduler.UnobservedTaskException += OnUnobservedException;

        try
        {
            // act
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < duration)
            {
                var round = new Task[Concurrency];

                for (var i = 0; i < Concurrency; i++)
                {
                    round[i] = Task.Run(
                        () => ExecuteRequestAsync(executor, failures, cancellationToken),
                        cancellationToken);
                }

                await Task.WhenAll(round);
                requests += Concurrency;

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobservedException;
        }

        // assert
        output.WriteLine($"requests={requests} failures={failures.Count} unobserved={unobserved.Count}");

        Assert.True(requests > 0);
        Assert.Empty(failures);
        Assert.Empty(unobserved);
    }

    private static async Task ExecuteRequestAsync(
        IRequestExecutor executor,
        ConcurrentQueue<string> failures,
        CancellationToken cancellationToken)
    {
        try
        {
            using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestCts.CancelAfter(s_requestTimeout);

            // The extra WaitAsync bounds a request that stops observing its cancellation token.
            await using var result = await executor
                .ExecuteAsync(Query, requestCts.Token)
                .WaitAsync(s_requestTimeout + TimeSpan.FromSeconds(5), cancellationToken);

            if (result.ExpectOperationResult().Errors is { Count: > 0 } errors)
            {
                failures.Enqueue(string.Join(" | ", errors.Select(e => e.Message)));
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            failures.Enqueue(ex.ToString());
        }
    }

    public sealed class StressQuery
    {
        public IEnumerable<StressItem> GetItems(int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new StressItem(i);
            }
        }
    }

    public sealed class StressItem(int id)
    {
        public int Id => id;

        public async Task<string> GetNameAsync()
        {
            await Task.Yield();
            return "item-" + id;
        }

        public async Task<StressItem> GetChildAsync()
        {
            await Task.Yield();
            return new StressItem(id * 1000);
        }

        public async Task<StressItem> GetRelatedAsync(
            StressRelatedDataLoader dataLoader,
            CancellationToken cancellationToken)
            => (await dataLoader.LoadAsync(id, cancellationToken))!;
    }

    public sealed class StressRelatedDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
        : BatchDataLoader<int, StressItem>(batchScheduler, options)
    {
        protected override async Task<IReadOnlyDictionary<int, StressItem>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            await Task.Yield();
            return keys.ToDictionary(k => k, k => new StressItem(k + 100000));
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class WorkSchedulerStressCollection
{
    public const string Name = "WorkSchedulerStress";
}
