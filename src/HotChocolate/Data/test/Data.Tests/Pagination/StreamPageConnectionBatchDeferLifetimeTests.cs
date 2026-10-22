#if NET9_0_OR_GREATER
using System.Collections.Concurrent;
using CookieCrumble.Resources;
using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Data.Pagination.StreamPageConnectionLifetimeTests;

namespace HotChocolate.Data.Pagination;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamPageConnectionBatchDeferLifetimeTests(PostgreSqlResource resource)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Dispose_Should_ReleaseEachPageOnce_When_BatchStreamsAreDisposedBeforeTheDeferredPayloadArrives()
    {
        // arrange
        await using var probe = await Probe.CreatePostgresAsync(
            resource.GetConnectionString($"db_{Guid.NewGuid():N}"),
            authorCount: 1,
            itemsPerAuthor: 5);
        var tracker = new LifetimeTracker();
        var listener = new DeferTaskListener();
        var executor = await CreateExecutorAsync(probe, tracker, listener);
        probe.NodesGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = OperationRequestBuilder
            .New()
            .SetDocument(
                """
                query ($count: Int!) {
                    __typename
                    ... @defer {
                        items(first: $count) {
                            pageInfo {
                                startCursor
                            }
                        }
                    }
                }
                """)
            .SetVariableValues(
                new List<IReadOnlyDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["count"] = 2 },
                    new Dictionary<string, object?> { ["count"] = 3 }
                })
            .Build();

        // act
        var result = await executor
            .ExecuteAsync(request, TestContext.Current.CancellationToken)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        try
        {
            foreach (var item in result.ExpectOperationResultBatch().Results)
            {
                await foreach (var payload in item.ExpectResponseStream().ReadResultsAsync())
                {
                    await payload.DisposeAsync();
                    break;
                }
            }

            await tracker.WaitForCreatedAsync(2);
            await result.DisposeAsync();
        }
        finally
        {
            probe.NodesGate.TrySetResult();
        }

        await listener.WaitForDeferredTasksAsync(2);
        await tracker.WaitForReleasedAsync(2);

        // assert
        Assert.Equal([1, 1], tracker.DisposeCounts);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(
        Probe probe,
        LifetimeTracker tracker,
        DeferTaskListener listener)
        => await new ServiceCollection()
            .AddSingleton(probe)
            .AddSingleton(tracker)
            .AddGraphQL()
            .AddDiagnosticEventListener(_ => listener)
            .AddQueryType<BatchQueryType>()
            .AddType<StreamPageConnectionLifetimeTests.ItemConnectionType>()
            .AddPagingArguments()
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    private sealed class DeferTaskListener : ExecutionDiagnosticEventListener
    {
        private readonly SemaphoreSlim _completed = new(0);

        public override IDisposable RunTask(IExecutionTask task)
            => task.IsDeferred && task.BranchId == -1
                ? new DeferTaskScope(_completed)
                : EmptyScope;

        public async Task WaitForDeferredTasksAsync(int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (!await _completed.WaitAsync(s_timeout, TestContext.Current.CancellationToken))
                {
                    throw new TimeoutException();
                }
            }
        }

        private sealed class DeferTaskScope(SemaphoreSlim completed) : IDisposable
        {
            public void Dispose() => completed.Release();
        }
    }

    public sealed class BatchQuery
    {
        [UseConnection]
        public async Task<StreamPageConnection<Item>> GetItems(
            PagingArguments arguments,
            [Service] Probe probe,
            [Service] LifetimeTracker tracker,
            CancellationToken cancellationToken)
        {
            var context = probe.CreateContext();
            var page = await context.Items
                .OrderBy(t => t.Id)
                .ToStreamPageAsync(arguments, tracker.Create(context), cancellationToken);

            return page;
        }
    }

    public sealed class BatchQueryType : ObjectType<BatchQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<BatchQuery> descriptor)
            => descriptor
                .Field(t => t.GetItems(default!, default!, default!, default))
                .AddPagingArguments()
                .Type<NonNullType<StreamPageConnectionLifetimeTests.ItemConnectionType>>();
    }

    public sealed class LifetimeTracker
    {
        private readonly ConcurrentDictionary<int, int> _disposeCounts = new();
        private readonly SemaphoreSlim _created = new(0);
        private readonly SemaphoreSlim _released = new(0);
        private int _nextId;

        public int[] DisposeCounts => [.. _disposeCounts.OrderBy(t => t.Key).Select(t => t.Value)];

        public IAsyncDisposable Create(PagingContext context)
        {
            var id = Interlocked.Increment(ref _nextId);
            _disposeCounts[id] = 0;
            _created.Release();
            return new Lifetime(this, id, context);
        }

        public Task WaitForCreatedAsync(int count) => WaitAsync(_created, count);

        public Task WaitForReleasedAsync(int count) => WaitAsync(_released, count);

        private static async Task WaitAsync(SemaphoreSlim semaphore, int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (!await semaphore.WaitAsync(s_timeout, TestContext.Current.CancellationToken))
                {
                    throw new TimeoutException();
                }
            }
        }

        private sealed class Lifetime(LifetimeTracker tracker, int id, PagingContext context) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                tracker._disposeCounts.AddOrUpdate(id, 1, (_, count) => count + 1);
                tracker._released.Release();
                await context.DisposeAsync();
            }
        }
    }
}
#endif
