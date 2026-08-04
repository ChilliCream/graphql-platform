using HotChocolate.Subscriptions;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

public sealed class ConcurrencyGateMiddlewareTests
{
    [Fact]
    public async Task Gate_Should_Be_Held_Inside_Operation_Execution_But_Not_Outside_When_Query_Is_Executed()
    {
        // arrange
        // N=1 lets us observe slot state via a non-blocking probe acquire.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        ExecutionConcurrencyGate gate = null!;
        var beforeHeld = false;
        var insideHeld = false;
        var afterHeld = false;

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .ConfigureSchemaServices(s => s.AddSingleton(new ExecutionConcurrencyGate(maxConcurrentExecutions: 1)))
            .UseDefaultPipeline()
            .UseRequest(
                (_, next) => async context =>
                {
                    beforeHeld = await IsSlotHeldAsync(gate);
                    await next(context);
                    afterHeld = await IsSlotHeldAsync(gate);
                },
                key: "Outside",
                before: WellKnownRequestMiddleware.ConcurrencyGateMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => async context =>
                {
                    insideHeld = await IsSlotHeldAsync(gate);
                    await next(context);
                },
                key: "Inside",
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        gate = executor.Schema.Services.GetRequiredService<ExecutionConcurrencyGate>();

        // act
        var result = await executor.ExecuteAsync("{ foo }", cts.Token);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
        Assert.False(beforeHeld);
        Assert.True(insideHeld);
        Assert.False(afterHeld);
    }

    [Fact]
    public async Task Gate_Should_Be_Held_Inside_Operation_Execution_When_Mutation_Is_Executed()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        ExecutionConcurrencyGate gate = null!;
        var beforeHeld = false;
        var insideHeld = false;

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .AddMutationType(d => d.Field("setFoo").Resolve("baz"))
            .ConfigureSchemaServices(s => s.AddSingleton(new ExecutionConcurrencyGate(maxConcurrentExecutions: 1)))
            .UseDefaultPipeline()
            .UseRequest(
                (_, next) => async context =>
                {
                    beforeHeld = await IsSlotHeldAsync(gate);
                    await next(context);
                },
                key: "Outside",
                before: WellKnownRequestMiddleware.ConcurrencyGateMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => async context =>
                {
                    insideHeld = await IsSlotHeldAsync(gate);
                    await next(context);
                },
                key: "Inside",
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        gate = executor.Schema.Services.GetRequiredService<ExecutionConcurrencyGate>();

        // act
        var result = await executor.ExecuteAsync("mutation { setFoo }", cts.Token);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
        Assert.False(beforeHeld);
        Assert.True(insideHeld);
    }

    [Fact]
    public async Task Gate_Should_Be_Released_Between_Subscription_Handshake_And_Iteration()
    {
        // arrange
        // N=1; subscribe, then submit a query while events are (potentially) flowing.
        // The handshake must have released the slot by the time the IResponseStream is returned.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddInMemorySubscriptions()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .AddSubscriptionType<StringSubscription>()
            .ConfigureSchemaServices(s => s.AddSingleton(new ExecutionConcurrencyGate(maxConcurrentExecutions: 1)))
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var gate = executor.Schema.Services.GetRequiredService<ExecutionConcurrencyGate>();

        // act
        // subscribe; this goes through the pipeline including the gate
        var subscribe = await executor.ExecuteAsync("subscription { onMessage }", cts.Token);
        await using var stream = subscribe.ExpectResponseStream();

        // a subsequent query must be able to acquire the slot (i.e. handshake released it)
        var queryResult = await executor.ExecuteAsync("{ foo }", cts.Token);

        // assert
        Assert.Empty(queryResult.ExpectOperationResult().Errors);
        Assert.False(await IsSlotHeldAsync(gate));
    }

    [Fact]
    public async Task Query_Should_Wait_For_Available_Slot_When_Gate_Is_Saturated()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var first = new AsyncLatch();
        var second = new AsyncLatch();
        var third = new AsyncLatch();

        var counter = 0;
        var latches = new[] { first, second };

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Field("block").Type<StringType>().Resolve(async _ =>
                {
                    var index = Interlocked.Increment(ref counter) - 1;
                    if (index < latches.Length)
                    {
                        await latches[index].WaitAsync(cts.Token);
                    }

                    return "ok";
                });
                d.Field("instant").Resolve("ok");
            })
            .ConfigureSchemaServices(s => s.AddSingleton(new ExecutionConcurrencyGate(maxConcurrentExecutions: 2)))
            .UseDefaultPipeline()
            .UseRequest(
                (_, next) => async context =>
                {
                    if (context.Request.OperationName == "Third")
                    {
                        third.Signal();
                    }

                    await next(context);
                },
                key: "ThirdProbe",
                before: WellKnownRequestMiddleware.ConcurrencyGateMiddleware,
                allowMultiple: true)
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        // act
        // saturate with two slow queries
        var firstTask = Task.Run(
            () => executor.ExecuteAsync("query A { block }", cts.Token),
            CancellationToken.None);
        var secondTask = Task.Run(
            () => executor.ExecuteAsync("query B { block }", cts.Token),
            CancellationToken.None);

        var gate = executor.Schema.Services.GetRequiredService<ExecutionConcurrencyGate>();
        await WaitUntilAsync(async () => await IsSlotHeldAsync(gate), cts.Token);

        var thirdRequest = OperationRequestBuilder.New()
            .SetDocument("query Third { instant }")
            .SetOperationName("Third")
            .Build();
        var thirdTask = Task.Run(
            () => executor.ExecuteAsync(thirdRequest, cts.Token),
            CancellationToken.None);

        // third has entered the pipeline but is still waiting on the gate
        await third.WaitAsync(cts.Token);
        await Task.Delay(100, cts.Token);
        Assert.False(thirdTask.IsCompleted);

        // release one in-flight query — the third can now acquire a slot
        first.Signal();

        var thirdResult = await thirdTask;
        Assert.Empty(thirdResult.ExpectOperationResult().Errors);

        // drain the other in-flight query
        second.Signal();

        var results = await Task.WhenAll(firstTask, secondTask);
        Assert.All(results, r => Assert.Empty(r.ExpectOperationResult().Errors));
    }

    [Fact]
    public async Task Slot_Should_Be_Released_When_Resolver_Throws()
    {
        // arrange
        // N=1; a resolver throw must not leak the slot.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Field("throw").Type<StringType>().Resolve(_ => throw new InvalidOperationException("boom"));
                d.Field("ok").Resolve("ok");
            })
            .ConfigureSchemaServices(s => s.AddSingleton(new ExecutionConcurrencyGate(maxConcurrentExecutions: 1)))
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var gate = executor.Schema.Services.GetRequiredService<ExecutionConcurrencyGate>();

        // act
        var throwing = await executor.ExecuteAsync("{ throw }", cts.Token);
        var throwingResult = throwing.ExpectOperationResult();

        // assert
        Assert.NotEmpty(throwingResult.Errors);
        Assert.False(await IsSlotHeldAsync(gate));

        // slot is still usable — subsequent queries proceed without blocking
        var ok = await executor.ExecuteAsync("{ ok }", cts.Token);
        Assert.Empty(ok.ExpectOperationResult().Errors);
        Assert.False(await IsSlotHeldAsync(gate));
    }

    [Fact]
    public async Task Slot_Should_Be_Released_When_Pipeline_Middleware_Throws_Inside_Gate()
    {
        // arrange
        // N=1; force an exception from a middleware sitting inside the gate
        // and verify the slot is returned by the gate's finally block.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("ok").Resolve("ok"))
            .ConfigureSchemaServices(s => s.AddSingleton(new ExecutionConcurrencyGate(maxConcurrentExecutions: 1)))
            .UseDefaultPipeline()
            .UseRequest(
                (_, _) => _ => throw new InvalidOperationException("pipeline failure"),
                key: "Throwing",
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                allowMultiple: true)
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var gate = executor.Schema.Services.GetRequiredService<ExecutionConcurrencyGate>();

        // act
        var result = await executor.ExecuteAsync("{ ok }", cts.Token);

        // assert
        Assert.NotEmpty(result.ExpectOperationResult().Errors);
        Assert.False(await IsSlotHeldAsync(gate));
    }

    [Fact]
    public async Task Query_Should_Fail_With_Timeout_Error_When_Wait_Exceeds_ExecutionTimeout()
    {
        // arrange
        // N=1, ExecutionTimeout = 200ms. Saturate the gate with a slow query
        // and submit a second that must time out while waiting for the slot.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var hold = new AsyncLatch();

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Field("block").Type<StringType>().Resolve(async _ =>
                {
                    await hold.WaitAsync(cts.Token);
                    return "ok";
                });
                d.Field("instant").Resolve("ok");
            })
            .ConfigureSchemaServices(s => s.AddSingleton(new ExecutionConcurrencyGate(maxConcurrentExecutions: 1)))
            .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(200))
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var gate = executor.Schema.Services.GetRequiredService<ExecutionConcurrencyGate>();

        // saturate the gate
        var blocking = Task.Run(
            () => executor.ExecuteAsync("{ block }", cts.Token),
            CancellationToken.None);
        await WaitUntilAsync(async () => await IsSlotHeldAsync(gate), cts.Token);

        // act
        // second request must time out waiting on the gate
        var timedOut = await executor.ExecuteAsync("{ instant }", cts.Token);

        // assert
        // clean timeout error, and the slot is still held by the in-flight query (not leaked)
        var timedOutResult = timedOut.ExpectOperationResult();
        Assert.Collection(
            timedOutResult.Errors,
            e => Assert.Equal(ErrorCodes.Execution.Timeout, e.Code));
        Assert.True(await IsSlotHeldAsync(gate));

        // drain the blocking query, under the shared ExecutionTimeout its own execution
        // also times out (the same 200ms applies to whichever request is holding the slot).
        // The blocker's fate is not what this test is verifying; the point is that the
        // *waiting* request times out cleanly on the gate. Drain it so the slot is returned
        // and verify the gate is empty.
        hold.Signal();
        await blocking;
        await WaitUntilAsync(async () => !await IsSlotHeldAsync(gate), cts.Token);
        Assert.False(await IsSlotHeldAsync(gate));
    }

    [Fact]
    public async Task Gate_Should_Be_Disabled_When_MaxConcurrentExecutions_Is_Null()
    {
        // arrange
        // null limit disables the gate entirely; middleware is a pass-through.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .ConfigureSchemaServices(s => s.AddSingleton(new ExecutionConcurrencyGate(maxConcurrentExecutions: null)))
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var gate = executor.Schema.Services.GetRequiredService<ExecutionConcurrencyGate>();

        // act
        var result = await executor.ExecuteAsync("{ foo }", cts.Token);

        // assert
        Assert.False(gate.IsEnabled);
        Assert.Empty(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task Gate_Should_Be_Disabled_When_MaxConcurrentExecutions_Is_Zero()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .ConfigureSchemaServices(s => s.AddSingleton(new ExecutionConcurrencyGate(maxConcurrentExecutions: 0)))
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var gate = executor.Schema.Services.GetRequiredService<ExecutionConcurrencyGate>();

        // act
        var result = await executor.ExecuteAsync("{ foo }", cts.Token);

        // assert
        Assert.False(gate.IsEnabled);
        Assert.Empty(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task Subscription_Event_Should_Wait_For_Slot_When_Query_Is_In_Flight()
    {
        // arrange
        // N=1. Subscribe first (handshake releases its slot), then saturate the gate
        // with a blocking query. Send an event — it must not be processed until the query completes.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var queryGate = new AsyncLatch();
        var completionOrder = new List<string>();
        var orderLock = new object();

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddInMemorySubscriptions()
            .AddQueryType(d =>
            {
                d.Field("block").Type<StringType>().Resolve(async _ =>
                {
                    await queryGate.WaitAsync(cts.Token);
                    lock (orderLock)
                    {
                        completionOrder.Add("query");
                    }

                    return "ok";
                });
            })
            .AddSubscriptionType<StringSubscription>()
            .ConfigureSchemaServices(s => s.AddSingleton(new ExecutionConcurrencyGate(maxConcurrentExecutions: 1)))
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var gate = executor.Schema.Services.GetRequiredService<ExecutionConcurrencyGate>();
        // ITopicEventSender is registered in application services, not schema services,
        // so it must be resolved via the root service provider accessor.
        var sender = executor.Schema.Services.GetRootServiceProvider().GetRequiredService<ITopicEventSender>();

        // subscribe
        var subscribe = await executor.ExecuteAsync("subscription { onMessage }", cts.Token);
        await using var stream = subscribe.ExpectResponseStream();
        var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(cts.Token);

        // saturate the slot with the blocking query
        var blocking = Task.Run(
            () => executor.ExecuteAsync("{ block }", cts.Token),
            CancellationToken.None);
        await WaitUntilAsync(async () => await IsSlotHeldAsync(gate), cts.Token);

        // act
        // send an event; its execution must queue behind the query's slot
        await sender.SendAsync("OnMessage", "one", cts.Token);

        var moveNext = enumerator.MoveNextAsync().AsTask();
        var delay = Task.Delay(150, cts.Token);
        var won = await Task.WhenAny(moveNext, delay);
        Assert.Same(delay, won);

        // release the blocking query — its slot is freed, the event executes, and the stream yields
        queryGate.Signal();
        Assert.True(await moveNext);
        lock (orderLock)
        {
            completionOrder.Add("event");
        }

        // assert
        // query finished before the event could run
        await blocking;
        Assert.Equal(["query", "event"], completionOrder);
        Assert.False(await IsSlotHeldAsync(gate));

        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task Subscription_Stream_Should_Not_Be_Drained_Ahead_Of_In_Flight_Execution()
    {
        // arrange
        // fast source; one event held in execution. Assert that the source has yielded
        // at most one event ahead of the currently processing event (pull-based back-pressure).
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var yielded = 0;
        var processed = 0;
        var processingGate = new AsyncLatch();

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddInMemorySubscriptions()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .AddSubscriptionType<CountingSubscription>()
            .ConfigureSchemaServices(s => s.AddSingleton(new ExecutionConcurrencyGate(maxConcurrentExecutions: 10)))
            .UseDefaultPipeline()
            .Services
            .AddSingleton(new CountingSource(
                () => Interlocked.Increment(ref yielded),
                async ct =>
                {
                    Interlocked.Increment(ref processed);
                    await processingGate.WaitAsync(ct);
                }))
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        // act
        var subscribe = await executor.ExecuteAsync("subscription { onTick }", cts.Token);
        await using var stream = subscribe.ExpectResponseStream();
        var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(cts.Token);
        var moveNext = enumerator.MoveNextAsync().AsTask();

        await WaitUntilAsync(() => Volatile.Read(ref processed) >= 1, cts.Token);
        await Task.Delay(100, cts.Token);

        // assert
        // yielded <= processed + 1 (at most one event is pulled ahead of the in-flight execution)
        var yieldedSnapshot = Volatile.Read(ref yielded);
        var processedSnapshot = Volatile.Read(ref processed);
        Assert.Equal(1, processedSnapshot);
        Assert.True(
            yieldedSnapshot <= processedSnapshot + 1,
            $"Expected yielded <= processed + 1, but yielded={yieldedSnapshot} and processed={processedSnapshot}.");

        // unblock and observe the event surfacing
        processingGate.Signal();
        Assert.True(await moveNext);

        await enumerator.DisposeAsync();
    }

    /// <summary>
    /// Behavioral probe: try to enter the gate with a zero timeout. If the gate is fully held,
    /// the wait returns <c>false</c>; otherwise it acquires a slot we must return immediately.
    /// </summary>
    private static async Task<bool> IsSlotHeldAsync(ExecutionConcurrencyGate gate)
    {
        if (!gate.IsEnabled)
        {
            return false;
        }

        var semaphore = GetSemaphore(gate);
        if (await semaphore.WaitAsync(0).ConfigureAwait(false))
        {
            semaphore.Release();
            // We got a slot — so at least one was free, which means not *all* slots are held.
            // For N=1 this means the gate is free; for N>1 this just means it is not saturated.
            return false;
        }

        return true;
    }

    private static SemaphoreSlim GetSemaphore(ExecutionConcurrencyGate gate)
    {
        // The gate exposes WaitAsync/Release but does not expose the underlying primitive.
        // Tests need a non-blocking probe; the least invasive option is to read the private field.
        var field = typeof(ExecutionConcurrencyGate).GetField(
            "_semaphore",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (SemaphoreSlim)field!.GetValue(gate)!;
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, CancellationToken cancellationToken)
    {
        while (!predicate())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate, CancellationToken cancellationToken)
    {
        while (!await predicate().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }

    public sealed class StringSubscription
    {
        [Subscribe]
        public string OnMessage([EventMessage] string message) => message;
    }

    public sealed class CountingSubscription
    {
        public async IAsyncEnumerable<int> SubscribeToTicks(
            [Service] CountingSource source,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var counter = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                source.OnYield();
                yield return ++counter;
            }
        }

        [Subscribe(With = nameof(SubscribeToTicks))]
        public async Task<int> OnTick(
            [EventMessage] int tick,
            [Service] CountingSource source,
            CancellationToken cancellationToken)
        {
            await source.OnProcess(cancellationToken);
            return tick;
        }
    }

    public sealed class CountingSource(Action onYield, Func<CancellationToken, Task> onProcess)
    {
        public void OnYield() => onYield();

        public Task OnProcess(CancellationToken ct) => onProcess(ct);
    }

    private sealed class AsyncLatch
    {
        private readonly SemaphoreSlim _semaphore = new(0);

        public void Signal() => _semaphore.Release();

        public Task WaitAsync(CancellationToken cancellationToken)
            => _semaphore.WaitAsync(cancellationToken);
    }
}
