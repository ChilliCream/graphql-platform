using System.Collections.Concurrent;
using System.Text.Json;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class DeferBatchLifecycleTests
{
    private const string BatchTokenSkipReason =
        "The request token does not reach the streams and deferred resolvers of a variable batch, because the "
        + "timeout middleware disposes its linked token source when the result is an OperationResultBatch.";

    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task DeferTasks_Should_CompleteAndCleanUpOncePerItem_When_EveryStreamIsDisposedBeforeThePayload()
    {
        // arrange
        var probe = new BatchProbe(1, 2);
        var executor = await CreateExecutorAsync(probe);
        await using var result = await executor
            .ExecuteAsync(CreateBatchRequest(1, 2), TestContext.Current.CancellationToken)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        try
        {
            await probe.WaitForStartedAsync();
            await result.DisposeAsync();
        }
        finally
        {
            probe.ReleaseAll();
        }

        await probe.WaitForDeferTasksAsync(2);
        await probe.WaitForCleanupsAsync(4);

        // assert
        Assert.Equal(["cleanup:name:1", "cleanup:name:2", "cleanup:person:1", "cleanup:person:2"], probe.Events);
    }

    [Fact]
    public async Task ReadResultsAsync_Should_DeliverRemainingItems_When_OnlyTheFirstStreamIsDisposedEarly()
    {
        // arrange
        var probe = new BatchProbe(1, 2);
        var executor = await CreateExecutorAsync(probe);
        await using var result = await executor
            .ExecuteAsync(CreateBatchRequest(1, 2), TestContext.Current.CancellationToken)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var items = result.ExpectOperationResultBatch().Results;
        var remaining = ReadItemAsync(items[1], probe);

        // act
        string summary;

        try
        {
            await probe.WaitForStartedAsync(1);
            await items[0].DisposeAsync();
        }
        finally
        {
            probe.Release(1);
            summary = await remaining.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        }

        await probe.WaitForDeferTasksAsync(2);
        await result.DisposeAsync();
        await probe.WaitForCleanupsAsync(4);

        // assert
        summary.MatchInlineSnapshot("person.id=2; name=name-2; hasNext=True,False");
        Assert.Equal(["cleanup:name:1", "cleanup:name:2", "cleanup:person:1", "cleanup:person:2"], probe.Events);
    }

    [Fact]
    public async Task ReadResultsAsync_Should_DeliverRemainingItems_When_OnlyANonFirstStreamIsDisposedEarly()
    {
        // arrange
        var probe = new BatchProbe(1, 2);
        var executor = await CreateExecutorAsync(probe);
        await using var result = await executor
            .ExecuteAsync(CreateBatchRequest(1, 2), TestContext.Current.CancellationToken)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var items = result.ExpectOperationResultBatch().Results;
        var remaining = ReadItemAsync(items[0], probe);

        // act
        string summary;

        try
        {
            await probe.WaitForStartedAsync(2);
            await items[1].DisposeAsync();
        }
        finally
        {
            probe.Release(2);
            summary = await remaining.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        }

        await probe.WaitForDeferTasksAsync(2);
        await result.DisposeAsync();
        await probe.WaitForCleanupsAsync(4);

        // assert
        summary.MatchInlineSnapshot("person.id=1; name=name-1; hasNext=True,False");
        Assert.Equal(["cleanup:name:1", "cleanup:name:2", "cleanup:person:1", "cleanup:person:2"], probe.Events);
    }

    [Fact(Skip = BatchTokenSkipReason)]
    public async Task ReadResultsAsync_Should_Complete_When_RequestTokenIsAlreadyCancelled()
    {
        // arrange
        var probe = new BatchProbe(1, 2);
        var executor = await CreateExecutorAsync(probe);
        using var requestCancellation = new CancellationTokenSource();
        await using var result = await executor
            .ExecuteAsync(CreateBatchRequest(1, 2), requestCancellation.Token)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        int[] payloadCounts;

        // act
        try
        {
            await probe.WaitForStartedAsync();
            await requestCancellation.CancelAsync();
            payloadCounts = await Task
                .WhenAll(result.ExpectOperationResultBatch().Results.Select(CountPayloadsAsync))
                .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        }
        finally
        {
            probe.ReleaseAll();
        }

        await result.DisposeAsync();
        await probe.WaitForDeferTasksAsync(2);
        await probe.WaitForCleanupsAsync(4);

        // assert
        Assert.Equal([0, 0], payloadCounts);
        Assert.Equal(["cleanup:name:1", "cleanup:name:2", "cleanup:person:1", "cleanup:person:2"], probe.Events);
    }

    [Fact(Skip = BatchTokenSkipReason)]
    public async Task DeferredResolver_Should_ObserveCancellation_When_RequestIsCancelledWhileItRuns()
    {
        // arrange
        var probe = new BatchProbe(1, 2) { ObserveRequestToken = true };
        var executor = await CreateExecutorAsync(probe);
        using var requestCancellation = new CancellationTokenSource();
        await using var result = await executor
            .ExecuteAsync(CreateBatchRequest(1, 2), requestCancellation.Token)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        int[] cancelledItems;

        // act
        try
        {
            await probe.WaitForStartedAsync();
            await requestCancellation.CancelAsync();
            cancelledItems = await probe.WaitForObservedCancellationAsync();
        }
        finally
        {
            probe.ReleaseAll();
        }

        // assert
        Assert.Equal([1, 2], cancelledItems);
    }

    private static async Task<int> CountPayloadsAsync(IExecutionResult item)
    {
        var count = 0;

        await foreach (var payload in item.ExpectResponseStream().ReadResultsAsync())
        {
            await payload.DisposeAsync();
            count++;
        }

        return count;
    }

    private static async Task<string> ReadItemAsync(IExecutionResult item, BatchProbe probe)
    {
        int? personId = null;
        string? name = null;
        var hasNext = new List<bool>();

        await foreach (var payload in item.ExpectResponseStream().ReadResultsAsync())
        {
            await using var payloadCleanup = payload;
            using var document = JsonDocument.Parse(payload.ToJson());
            var root = document.RootElement;

            hasNext.Add(root.TryGetProperty("hasNext", out var hasNextValue) && hasNextValue.GetBoolean());

            if (personId is null && root.TryGetProperty("data", out var data))
            {
                personId = data.GetProperty("person").GetProperty("id").GetInt32();
                probe.Release(personId.Value);
            }

            if (root.TryGetProperty("incremental", out var incremental))
            {
                foreach (var entry in incremental.EnumerateArray())
                {
                    name = entry.GetProperty("data").GetProperty("name").GetString();
                }
            }
        }

        return $"person.id={personId}; name={name}; hasNext={string.Join(",", hasNext)}";
    }

    private static IOperationRequest CreateBatchRequest(params int[] ids)
        => OperationRequestBuilder
            .New()
            .SetDocument(
                """
                query ($id: Int!) {
                    person(id: $id) {
                        id
                        ... @defer {
                            name
                        }
                    }
                }
                """)
            .SetVariableValues(
                ids.Select(id => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { ["id"] = id })
                    .ToList())
            .Build();

    private static ValueTask<IRequestExecutor> CreateExecutorAsync(BatchProbe probe)
        => new ServiceCollection()
            .AddGraphQL()
            .AddDiagnosticEventListener(_ => new ProbeListener(probe))
            .AddQueryType(
                descriptor => descriptor
                    .Field("person")
                    .Argument("id", argument => argument.Type<NonNullType<IntType>>())
                    .Type<NonNullType<ObjectType<Person>>>()
                    .Use(_ => context =>
                    {
                        var id = context.ArgumentValue<int>("id");
                        context.RegisterForCleanup(() => probe.CleanUp($"person:{id}"), CleanAfter.Request);
                        context.Result = new Person(id);
                        return ValueTask.CompletedTask;
                    }))
            .AddObjectType<Person>(
                descriptor =>
                {
                    descriptor.Field(person => person.Id);
                    descriptor
                        .Field("name")
                        .Type<NonNullType<StringType>>()
                        .Use(_ => async context =>
                            context.Result = await probe.ResolveNameAsync(context.Parent<Person>().Id, context));
                })
            .ModifyOptions(options => options.EnableDefer = true)
            .BuildRequestExecutorAsync();

    public sealed class Person(int id)
    {
        public int Id => id;
    }

    private sealed class BatchProbe
    {
        private readonly Dictionary<int, TaskCompletionSource> _started;
        private readonly Dictionary<int, TaskCompletionSource> _gates;
        private readonly Dictionary<int, TaskCompletionSource> _cancelled;
        private readonly ConcurrentQueue<string> _events = new();
        private readonly SemaphoreSlim _cleanups = new(0);
        private readonly SemaphoreSlim _deferTasks = new(0);

        public BatchProbe(params int[] ids)
        {
            _started = CreateSignals(ids);
            _gates = CreateSignals(ids);
            _cancelled = CreateSignals(ids);
        }

        public bool ObserveRequestToken { get; init; }

        public string[] Events => [.. _events.Order()];

        public async Task<string> ResolveNameAsync(int id, IMiddlewareContext context)
        {
            context.RegisterForCleanup(() => CleanUp($"name:{id}"), CleanAfter.Request);
            _started[id].TrySetResult();

            try
            {
                if (ObserveRequestToken)
                {
                    await _gates[id].Task.WaitAsync(context.RequestAborted).ConfigureAwait(false);
                }
                else
                {
                    await _gates[id].Task.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                _cancelled[id].TrySetResult();
                throw;
            }

            return $"name-{id}";
        }

        public ValueTask CleanUp(string name)
        {
            _events.Enqueue($"cleanup:{name}");
            _cleanups.Release();
            return default;
        }

        public void CompleteDeferTask() => _deferTasks.Release();

        public void Release(int id) => _gates[id].TrySetResult();

        public void ReleaseAll()
        {
            foreach (var gate in _gates.Values)
            {
                gate.TrySetResult();
            }
        }

        public Task WaitForStartedAsync()
            => Task.WhenAll(_started.Values.Select(signal => signal.Task))
                .WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        public Task WaitForStartedAsync(int id)
            => _started[id].Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        public async Task<int[]> WaitForObservedCancellationAsync()
        {
            await Task.WhenAll(_cancelled.Values.Select(signal => signal.Task))
                .WaitAsync(s_timeout, TestContext.Current.CancellationToken);

            return [.. _cancelled.Keys.Order()];
        }

        public async Task WaitForDeferTasksAsync(int count)
        {
            for (var i = 0; i < count; i++)
            {
                await WaitAsync(_deferTasks);
            }
        }

        public async Task WaitForCleanupsAsync(int count)
        {
            for (var i = 0; i < count; i++)
            {
                await WaitAsync(_cleanups);
            }
        }

        private static async Task WaitAsync(SemaphoreSlim semaphore)
        {
            if (!await semaphore.WaitAsync(s_timeout, TestContext.Current.CancellationToken))
            {
                throw new TimeoutException();
            }
        }

        private static Dictionary<int, TaskCompletionSource> CreateSignals(int[] ids)
            => ids.ToDictionary(
                id => id,
                _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
    }

    private sealed class ProbeListener(BatchProbe probe) : ExecutionDiagnosticEventListener
    {
        public override IDisposable RunTask(IExecutionTask task)
            => task.IsDeferred && task.BranchId == BranchTracker.SystemBranchId
                ? new DeferTaskScope(probe)
                : EmptyScope;

        private sealed class DeferTaskScope(BatchProbe probe) : IDisposable
        {
            public void Dispose() => probe.CompleteDeferTask();
        }
    }
}
