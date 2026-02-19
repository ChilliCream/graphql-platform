using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanSingleFlightTests : FusionTestBase
{
    [Fact]
    public async Task Concurrent_Same_Operation_Should_Be_Coalesced_To_One_Planning_Run()
    {
        // arrange
        const int requestCount = 8;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var listener = new PlannerEventListener();
        var operationIds = new ConcurrentBag<string>();
        var gate = new RequestGate(requestCount);

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                (_, next) => CreateGateMiddleware(next, gate))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                (_, next) => CreateSingleFlightLeaderDelayMiddleware(next, TimeSpan.FromMilliseconds(100)))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                (_, _) => CreatePlanCaptureMiddleware())
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        // act
        const string operationText =
            """
            query SameOpCoalesce {
              foo
            }
            """;
        var results = await Task.WhenAll(
            Enumerable.Range(0, requestCount)
                .Select(_ => executor.ExecuteAsync(operationText, cts.Token)));

        // assert
        Assert.All(results, t => Assert.Empty(t.ExpectOperationResult().Errors));

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(1, listener.Count(PlannerEventSource.PlanStartEventId, operationId));
    }

    [Fact]
    public async Task Concurrent_Distinct_Operations_Should_Not_Be_Coalesced()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var listener = new PlannerEventListener();
        var operationIds = new ConcurrentBag<string>();
        var gate = new RequestGate(expectedRequests: 2);

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                (_, next) => CreateGateMiddleware(next, gate))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                (_, _) => CreatePlanCaptureMiddleware())
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        const string operationText1 =
            """
            query DistinctOpOne {
              foo
            }
            """;
        const string operationText2 =
            """
            query DistinctOpTwo {
              __typename
            }
            """;

        // act
        var results = await Task.WhenAll(
            executor.ExecuteAsync(operationText1, cts.Token),
            executor.ExecuteAsync(operationText2, cts.Token));

        // assert
        Assert.All(results, t => Assert.Empty(t.ExpectOperationResult().Errors));

        var ids = operationIds.Distinct().ToArray();
        Assert.Equal(2, ids.Length);
        Assert.All(ids, id => Assert.Equal(1, listener.Count(PlannerEventSource.PlanStartEventId, id)));
    }

    [Fact]
    public async Task Leader_Planning_Failure_Should_Be_Observed_By_Followers()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var listener = new PlannerEventListener();
        var operationIds = new ConcurrentBag<string>();
        var gate = new RequestGate(expectedRequests: 2);

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .ModifyPlannerOptions(o => o.MaxPlanningTime = TimeSpan.FromTicks(1))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                (_, next) => CreateGateMiddleware(next, gate))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                (_, next) => CreateSingleFlightLeaderDelayMiddleware(next, TimeSpan.FromMilliseconds(100)))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds))
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        const string operationText =
            """
            query FailureCoalesce {
              foo
            }
            """;

        // act
        var results = await Task.WhenAll(
            executor.ExecuteAsync(operationText, cts.Token),
            executor.ExecuteAsync(operationText, cts.Token));

        // assert
        Assert.All(results, t => Assert.NotEmpty(t.ExpectOperationResult().Errors));

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(1, listener.Count(PlannerEventSource.PlanStartEventId, operationId));
        Assert.Equal(1, listener.Count(PlannerEventSource.PlanErrorEventId, operationId));
    }

    [Fact]
    public async Task Follower_Cancellation_Should_Not_Cancel_Leader_Planning()
    {
        // arrange
        using var leaderCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var followerCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        using var listener = new PlannerEventListener();
        var operationIds = new ConcurrentBag<string>();
        var blockingInterceptor = new BlockingPlannerInterceptor();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddOperationPlannerInterceptor(_ => blockingInterceptor)
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                (_, next) => CreateOperationIdCaptureMiddleware(next, operationIds))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                (_, _) => CreatePlanCaptureMiddleware())
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: leaderCts.Token);

        const string operationText =
            """
            query CancelFollowerOnly {
              foo
            }
            """;

        // act
        var leaderTask = Task.Run(
                () => executor.ExecuteAsync(operationText, leaderCts.Token),
                CancellationToken.None);
        Assert.True(blockingInterceptor.WaitForEntry(TimeSpan.FromSeconds(5)));

        var followerTask = Task.Run(
                () => executor.ExecuteAsync(operationText, followerCts.Token),
                CancellationToken.None);
        var followerCompletion = await Task.WhenAny(
            followerTask,
            Task.Delay(TimeSpan.FromSeconds(2), leaderCts.Token));
        blockingInterceptor.Release();
        Assert.Same(followerTask, followerCompletion);

        var followerResult = await followerTask;
        var leaderResult = await leaderTask;

        // assert
        var followerErrors = followerResult.ExpectOperationResult().Errors;
        Assert.NotEmpty(followerErrors);
        Assert.Contains(
            followerErrors,
            e => e.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(leaderResult.ExpectOperationResult().Errors);

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(1, listener.Count(PlannerEventSource.PlanStartEventId, operationId));
    }

    private static RequestDelegate CreateGateMiddleware(
        RequestDelegate next,
        RequestGate gate)
        => async context =>
        {
            await gate.SignalAndWaitAsync(context.RequestAborted);
            await next(context);
        };

    private static RequestDelegate CreateOperationIdCaptureMiddleware(
        RequestDelegate next,
        ConcurrentBag<string> operationIds)
        => async context =>
        {
            operationIds.Add(context.GetOperationId());
            await next(context);
        };

    private static RequestDelegate CreateSingleFlightLeaderDelayMiddleware(
        RequestDelegate next,
        TimeSpan delay)
        => async context =>
        {
            if (context.Features.Get<TaskCompletionSource<OperationPlan>>() is not null)
            {
                await Task.Delay(delay, context.RequestAborted);
            }

            await next(context);
        };

    private static RequestDelegate CreatePlanCaptureMiddleware()
        => context =>
        {
            context.Result =
                new OperationResult(
                    ImmutableOrderedDictionary<string, object?>.Empty.Add("operationPlan", context.GetOperationPlan()));
            return ValueTask.CompletedTask;
        };

    private sealed class RequestGate(int expectedRequests)
    {
        private readonly TaskCompletionSource _allArrived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrived;

        public ValueTask SignalAndWaitAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _arrived) >= expectedRequests)
            {
                _allArrived.TrySetResult();
            }

            return new ValueTask(_allArrived.Task.WaitAsync(cancellationToken));
        }
    }

    private sealed class BlockingPlannerInterceptor : IOperationPlannerInterceptor
    {
        private readonly ManualResetEventSlim _entered = new(false);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool WaitForEntry(TimeSpan timeout)
            => _entered.Wait(timeout);

        public void Release()
            => _release.TrySetResult();

        public void OnAfterPlanCompleted(
            OperationDocumentInfo operationDocumentInfo,
            OperationPlan operationPlan)
        {
            _entered.Set();
            _release.Task.GetAwaiter().GetResult();
        }
    }

    private sealed class PlannerEventListener : EventListener
    {
        private readonly ConcurrentQueue<CapturedEvent> _events = [];

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.Equals(PlannerEventSource.EventSourceName, StringComparison.Ordinal))
            {
                EnableEvents(eventSource, EventLevel.Informational, EventKeywords.All);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!eventData.EventSource.Name.Equals(PlannerEventSource.EventSourceName, StringComparison.Ordinal))
            {
                return;
            }

            _events.Enqueue(
                new CapturedEvent(
                    eventData.EventId,
                    eventData.Payload is null
                        ? []
                        : [.. eventData.Payload]));
        }

        public int Count(int eventId, string operationId)
            => _events.Count(t => t.EventId == eventId && t.HasOperationId(operationId));
    }

    private sealed record CapturedEvent(
        int EventId,
        IReadOnlyList<object?> Payload)
    {
        public bool HasOperationId(string operationId)
            => Payload.Count > 0
                && Payload[0] is string payloadOperationId
                && payloadOperationId.Equals(operationId, StringComparison.Ordinal);
    }
}
