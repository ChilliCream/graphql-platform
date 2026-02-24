using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

public sealed class OperationCompilerSingleFlightTests
{
    [Fact]
    public async Task Concurrent_Same_Operation_Should_Be_Coalesced_To_One_Compilation()
    {
        // arrange
        const int requestCount = 8;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var compileCount = 0;
        var gate = new RequestGate(requestCount);

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => new CompileCountListener(() => Interlocked.Increment(ref compileCount)))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationCacheMiddleware,
                (_, next) => CreateGateMiddleware(next, gate),
                key: "Gate",
                allowMultiple: true)
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationResolverMiddleware,
                (_, next) => CreateSingleFlightLeaderDelayMiddleware(next, TimeSpan.FromMilliseconds(100)),
                key: "LeaderDelay",
                allowMultiple: true)
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
        Assert.All(results, r => Assert.Empty(Assert.IsType<OperationResult>(r).Errors));
        Assert.Equal(1, Volatile.Read(ref compileCount));
    }

    [Fact]
    public async Task Concurrent_Distinct_Operations_Should_Not_Be_Coalesced()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var compileCount = 0;
        var gate = new RequestGate(expectedRequests: 2);

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Field("foo").Resolve("bar");
                d.Field("baz").Resolve("qux");
            })
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => new CompileCountListener(() => Interlocked.Increment(ref compileCount)))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationCacheMiddleware,
                (_, next) => CreateGateMiddleware(next, gate),
                key: "Gate",
                allowMultiple: true)
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
              baz
            }
            """;

        // act
        var results = await Task.WhenAll(
            executor.ExecuteAsync(operationText1, cts.Token),
            executor.ExecuteAsync(operationText2, cts.Token));

        // assert
        Assert.All(results, r => Assert.Empty(Assert.IsType<OperationResult>(r).Errors));
        Assert.Equal(2, Volatile.Read(ref compileCount));
    }

    [Fact]
    public async Task Leader_Compilation_Failure_Should_Be_Observed_By_Followers()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var compileCount = 0;
        var gate = new RequestGate(expectedRequests: 2);

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => new CompileCountListener(() => Interlocked.Increment(ref compileCount)))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationCacheMiddleware,
                (_, next) => CreateGateMiddleware(next, gate),
                key: "Gate",
                allowMultiple: true)
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationResolverMiddleware,
                (_, next) => CreateSingleFlightLeaderDelayMiddleware(next, TimeSpan.FromMilliseconds(100)),
                key: "LeaderDelay",
                allowMultiple: true)
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationResolverMiddleware,
                (_, next) => CreateThrowingMiddleware(next),
                key: "Throwing",
                allowMultiple: true)
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
        Assert.All(results, r => Assert.NotEmpty(Assert.IsType<OperationResult>(r).Errors));
    }

    [Fact]
    public async Task Follower_Cancellation_Should_Not_Cancel_Leader_Compilation()
    {
        // arrange
        using var leaderCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var followerCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var compileCount = 0;
        var compileGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => new CompileCountListener(() => Interlocked.Increment(ref compileCount)))
            .InsertUseRequest(
                before: WellKnownRequestMiddleware.OperationResolverMiddleware,
                (_, next) => CreateBlockingMiddleware(next, compileGate),
                key: "Blocking",
                allowMultiple: true)
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

        // Give the leader time to enter the pipeline and register in-flight.
        await Task.Delay(50, leaderCts.Token);

        var followerTask = Task.Run(
            () => executor.ExecuteAsync(operationText, followerCts.Token),
            CancellationToken.None);

        // Wait for the follower to cancel.
        var followerCompletion = await Task.WhenAny(
            followerTask,
            Task.Delay(TimeSpan.FromSeconds(3), leaderCts.Token));

        // Release the leader.
        compileGate.TrySetResult();
        Assert.Same(followerTask, followerCompletion);

        var followerResult = await followerTask;
        var leaderResult = await leaderTask;

        // assert
        var followerErrors = Assert.IsType<OperationResult>(followerResult).Errors;
        Assert.NotEmpty(followerErrors);

        Assert.Empty(Assert.IsType<OperationResult>(leaderResult).Errors);
        Assert.Equal(1, Volatile.Read(ref compileCount));
    }

    private static RequestDelegate CreateGateMiddleware(
        RequestDelegate next,
        RequestGate gate)
        => async context =>
        {
            await gate.SignalAndWaitAsync(context.RequestAborted);
            await next(context);
        };

    private static RequestDelegate CreateSingleFlightLeaderDelayMiddleware(
        RequestDelegate next,
        TimeSpan delay)
        => async context =>
        {
            if (context.Features.Get<TaskCompletionSource<Operation>>() is not null)
            {
                await Task.Delay(delay, context.RequestAborted);
            }

            await next(context);
        };

    private static RequestDelegate CreateThrowingMiddleware(
        RequestDelegate next)
        => async context =>
        {
            // Only throw for the leader (the one that has the TCS set).
            if (context.Features.Get<TaskCompletionSource<Operation>>() is not null)
            {
                throw new InvalidOperationException("Simulated compilation failure.");
            }

            await next(context);
        };

    private static RequestDelegate CreateBlockingMiddleware(
        RequestDelegate next,
        TaskCompletionSource gate)
        => async context =>
        {
            // Only block the leader.
            if (context.Features.Get<TaskCompletionSource<Operation>>() is not null)
            {
                await gate.Task;
            }

            await next(context);
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

    private sealed class CompileCountListener(Action onCompile) : ExecutionDiagnosticEventListener
    {
        public override IDisposable CompileOperation(RequestContext context)
        {
            onCompile();
            return EmptyScope;
        }
    }
}
