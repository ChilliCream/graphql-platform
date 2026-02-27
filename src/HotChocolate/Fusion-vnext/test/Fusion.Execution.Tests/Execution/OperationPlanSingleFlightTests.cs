using System.Collections.Concurrent;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
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
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var gate = new RequestGate(requestCount);

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
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
        Assert.Equal(1, listener.PlanStartCount(operationId));
    }

    [Fact]
    public async Task Concurrent_Distinct_Operations_Should_Not_Be_Coalesced()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var gate = new RequestGate(expectedRequests: 2);

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
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
        Assert.All(ids, id => Assert.Equal(1, listener.PlanStartCount(id)));
    }

    [Fact]
    public async Task Leader_Planning_Failure_Should_Be_Observed_By_Followers()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var leaderGate = new SingleFlightLeaderGate();
        var secondRequestObserver = new SecondRequestObserver();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
            .ModifyPlannerOptions(o => o.MaxPlanningTime = TimeSpan.FromTicks(1))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                (_, next) => CreateSecondRequestEnteredDownstreamMiddleware(next, secondRequestObserver))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationPlanMiddleware,
                (_, next) => CreateSingleFlightLeaderBlockMiddleware(next, leaderGate))
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
        var leaderTask = executor.ExecuteAsync(operationText, cts.Token);
        await leaderGate.WaitForEntryAsync(cts.Token);

        var followerTask = executor.ExecuteAsync(operationText, cts.Token);
        await secondRequestObserver.WaitForSecondRequestEnteredDownstreamAsync(cts.Token);
        leaderGate.Release();

        var results = await Task.WhenAll(
            leaderTask,
            followerTask);

        // assert
        Assert.All(results, t => Assert.NotEmpty(t.ExpectOperationResult().Errors));

        var operationId = Assert.Single(operationIds.Distinct());
        Assert.Equal(1, listener.PlanStartCount(operationId));
        Assert.Equal(1, listener.PlanErrorCount(operationId));
    }

    [Fact]
    public async Task Follower_Cancellation_Should_Not_Cancel_Leader_Planning()
    {
        // arrange
        using var leaderCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var followerCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var listener = new PlanningCountDiagnosticListener();
        var operationIds = new ConcurrentBag<string>();
        var blockingInterceptor = new BlockingPlannerInterceptor();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => listener)
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
        Assert.Equal(1, listener.PlanStartCount(operationId));
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

    private static RequestDelegate CreateSingleFlightLeaderBlockMiddleware(
        RequestDelegate next,
        SingleFlightLeaderGate gate)
        => async context =>
        {
            if (context.Features.Get<TaskCompletionSource<OperationPlan>>() is not null)
            {
                gate.SignalEntry();
                await gate.WaitForReleaseAsync(context.RequestAborted);
            }

            await next(context);
        };

    private static RequestDelegate CreateSecondRequestEnteredDownstreamMiddleware(
        RequestDelegate next,
        SecondRequestObserver observer)
        => async context =>
        {
            if (!observer.IsSecondRequest())
            {
                await next(context);
                return;
            }

            ValueTask execution;

            try
            {
                execution = next(context);
            }
            finally
            {
                observer.SignalSecondRequestEnteredDownstream();
            }

            await execution;
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

    private sealed class SingleFlightLeaderGate
    {
        private readonly TaskCompletionSource _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void SignalEntry()
            => _entered.TrySetResult();

        public void Release()
            => _release.TrySetResult();

        public ValueTask WaitForEntryAsync(CancellationToken cancellationToken)
            => new(_entered.Task.WaitAsync(cancellationToken));

        public ValueTask WaitForReleaseAsync(CancellationToken cancellationToken)
            => new(_release.Task.WaitAsync(cancellationToken));
    }

    private sealed class SecondRequestObserver
    {
        private readonly TaskCompletionSource _secondRequestEnteredDownstream =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _requestCount;

        public bool IsSecondRequest()
            => Interlocked.Increment(ref _requestCount) == 2;

        public void SignalSecondRequestEnteredDownstream()
            => _secondRequestEnteredDownstream.TrySetResult();

        public ValueTask WaitForSecondRequestEnteredDownstreamAsync(CancellationToken cancellationToken)
            => new(_secondRequestEnteredDownstream.Task.WaitAsync(cancellationToken));
    }

    private sealed class PlanningCountDiagnosticListener : FusionExecutionDiagnosticEventListener
    {
        private readonly ConcurrentDictionary<string, int> _planStarts = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, int> _planErrors = new(StringComparer.Ordinal);

        public override IDisposable PlanOperation(RequestContext context, string operationPlanId)
        {
            _planStarts.AddOrUpdate(operationPlanId, 1, static (_, count) => count + 1);
            return EmptyScope;
        }

        public override void PlanOperationError(
            RequestContext context,
            string operationId,
            Exception error)
        {
            _planErrors.AddOrUpdate(operationId, 1, static (_, count) => count + 1);
        }

        public int PlanStartCount(string operationId)
            => _planStarts.TryGetValue(operationId, out var count)
                ? count
                : 0;

        public int PlanErrorCount(string operationId)
            => _planErrors.TryGetValue(operationId, out var count)
                ? count
                : 0;
    }
}
