using HotChocolate.Subscriptions;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HotChocolate.Execution.Pipeline;

public sealed class SubscriptionEventTimeoutTests
{
    [Fact]
    public async Task Event_Should_Time_Out_When_Execution_Exceeds_Configured_Budget()
    {
        // arrange
        // the resolver blocks until cancelled, so the event times out once the budget has elapsed.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var time = new FakeTimeProvider();
        var started = new ResolverStarted();

        var executor = await new ServiceCollection()
            .AddSingleton(started)
            .AddGraphQL()
            .AddInMemorySubscriptions()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .AddSubscriptionType<BlockingSubscription>()
            .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(200))
            .ConfigureSchemaServices(s => s.AddSingleton<TimeProvider>(time))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var sender = executor.Schema.Services.GetRootServiceProvider()
            .GetRequiredService<ITopicEventSender>();

        var subscribe = await executor.ExecuteAsync("subscription { onMessage }", cts.Token);
        await using var stream = subscribe.ExpectResponseStream();
        var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(cts.Token);

        await sender.SendAsync("OnMessage", "one", cts.Token);
        var moveNext = enumerator.MoveNextAsync().AsTask();
        await started.Task.WaitAsync(cts.Token);

        // act
        time.Advance(TimeSpan.FromMilliseconds(200));

        // assert
        // the event timed out and the subscription tore down, so the stream ends.
        Assert.False(await moveNext.WaitAsync(cts.Token));

        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task Timer_Should_Be_Reset_Between_Events()
    {
        // arrange
        // more than the budget passes between the events, so an event only succeeds if its budget starts fresh.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var time = new FakeTimeProvider();

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddInMemorySubscriptions()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .AddSubscriptionType<EchoSubscription>()
            .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromSeconds(1))
            .ConfigureSchemaServices(s => s.AddSingleton<TimeProvider>(time))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var sender = executor.Schema.Services.GetRootServiceProvider()
            .GetRequiredService<ITopicEventSender>();

        var subscribe = await executor.ExecuteAsync("subscription { onMessage }", cts.Token);
        await using var stream = subscribe.ExpectResponseStream();
        var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(cts.Token);
        var messages = new List<string?>();

        // act
        for (var i = 0; i < 3; i++)
        {
            await sender.SendAsync("OnMessage", $"m{i}", cts.Token);
            Assert.True(await enumerator.MoveNextAsync());
            messages.Add(enumerator.Current.ToJson());
            time.Advance(TimeSpan.FromSeconds(2));
        }

        // assert
        messages.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "onMessage": "m0"
                  }
                }
                """,
                """
                {
                  "data": {
                    "onMessage": "m1"
                  }
                }
                """,
                """
                {
                  "data": {
                    "onMessage": "m2"
                  }
                }
                """
            ]);

        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task Event_Should_Not_Run_Callbacks_Of_A_Previous_Event_When_It_Times_Out()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var time = new FakeTimeProvider();
        var probe = new CallbackProbe();

        var executor = await new ServiceCollection()
            .AddSingleton(probe)
            .AddGraphQL()
            .AddInMemorySubscriptions()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .AddSubscriptionType<CallbackSubscription>()
            .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromSeconds(1))
            .ConfigureSchemaServices(s => s.AddSingleton<TimeProvider>(time))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var sender = executor.Schema.Services.GetRootServiceProvider()
            .GetRequiredService<ITopicEventSender>();

        var subscribe = await executor.ExecuteAsync("subscription { onMessage }", cts.Token);
        await using var stream = subscribe.ExpectResponseStream();
        var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(cts.Token);

        await sender.SendAsync("OnMessage", "register", cts.Token);
        Assert.True(await enumerator.MoveNextAsync());

        await sender.SendAsync("OnMessage", "block", cts.Token);
        var moveNext = enumerator.MoveNextAsync().AsTask();
        await probe.Blocked.Task.WaitAsync(cts.Token);

        // act
        time.Advance(TimeSpan.FromSeconds(1));

        // assert
        Assert.False(await moveNext.WaitAsync(cts.Token));
        Assert.False(probe.PreviousEventCallbackRan);

        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task Event_Should_Not_Run_Callbacks_Of_A_Previous_Event_When_The_Request_Is_Aborted()
    {
        // arrange
        // the system time provider resets the event cancellation between events.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var abortCts = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, abortCts.Token);
        var probe = new CallbackProbe();

        var executor = await new ServiceCollection()
            .AddSingleton(probe)
            .AddGraphQL()
            .AddInMemorySubscriptions()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .AddSubscriptionType<CallbackSubscription>()
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var sender = executor.Schema.Services.GetRootServiceProvider()
            .GetRequiredService<ITopicEventSender>();

        var subscribe = await executor.ExecuteAsync("subscription { onMessage }", linked.Token);
        await using var stream = subscribe.ExpectResponseStream();
        var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(linked.Token);

        await sender.SendAsync("OnMessage", "register", cts.Token);
        Assert.True(await enumerator.MoveNextAsync());

        await sender.SendAsync("OnMessage", "block", cts.Token);
        var moveNext = enumerator.MoveNextAsync().AsTask();
        await probe.Blocked.Task.WaitAsync(cts.Token);

        // act
        await abortCts.CancelAsync();

        // assert
        Assert.False(await moveNext.WaitAsync(cts.Token));
        Assert.False(probe.PreviousEventCallbackRan);

        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task Client_Abort_Should_Tear_Down_Current_Event_When_Timeout_Is_Configured()
    {
        // arrange
        // the resolver blocks until cancelled, so only the request abort can end the event.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var abortCts = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, abortCts.Token);
        var started = new ResolverStarted();

        var executor = await new ServiceCollection()
            .AddSingleton(started)
            .AddGraphQL()
            .AddInMemorySubscriptions()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .AddSubscriptionType<BlockingSubscription>()
            .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromSeconds(30))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var sender = executor.Schema.Services.GetRootServiceProvider()
            .GetRequiredService<ITopicEventSender>();

        var subscribe = await executor.ExecuteAsync("subscription { onMessage }", linked.Token);
        await using var stream = subscribe.ExpectResponseStream();
        var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(linked.Token);

        await sender.SendAsync("OnMessage", "one", cts.Token);

        var moveNext = enumerator.MoveNextAsync().AsTask();
        await started.Task.WaitAsync(cts.Token);

        // act
        await abortCts.CancelAsync();

        // assert
        // the abort cancelled the event and the subscription tore down, so the stream ends.
        Assert.False(await moveNext.WaitAsync(cts.Token));

        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task Subscription_Should_Run_End_To_End_With_Default_Execution_Timeout()
    {
        // arrange
        // Default configuration: ExecutionTimeout is 30 seconds. Events must flow
        // normally and no spurious cancellation must occur under normal timing.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddInMemorySubscriptions()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .AddSubscriptionType<EchoSubscription>()
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var sender = executor.Schema.Services.GetRootServiceProvider()
            .GetRequiredService<ITopicEventSender>();

        var subscribe = await executor.ExecuteAsync("subscription { onMessage }", cts.Token);
        await using var stream = subscribe.ExpectResponseStream();
        var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(cts.Token);

        // act
        await sender.SendAsync("OnMessage", "hello", cts.Token);
        var moved = await enumerator.MoveNextAsync();

        // assert
        Assert.True(moved);
        Assert.Empty(enumerator.Current.Errors);

        await enumerator.DisposeAsync();
    }

    public sealed class EchoSubscription
    {
        [Subscribe]
        public string OnMessage([EventMessage] string message) => message;
    }

    public sealed class BlockingSubscription
    {
        [Subscribe]
        public async Task<string> OnMessage(
            [EventMessage] string message,
            [Service] ResolverStarted started,
            CancellationToken cancellationToken)
        {
            started.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return message;
        }
    }

    public sealed class CallbackSubscription
    {
        [Subscribe]
        public async Task<string> OnMessage(
            [EventMessage] string message,
            [Service] CallbackProbe probe,
            CancellationToken cancellationToken)
        {
            if (message == "register")
            {
                // the registration is not disposed, so it outlives the event.
                cancellationToken.Register(() => probe.PreviousEventCallbackRan = true);
                return message;
            }

            probe.Blocked.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return message;
        }
    }

    public sealed class CallbackProbe
    {
        public TaskCompletionSource Blocked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool PreviousEventCallbackRan { get; set; }
    }

    public sealed class ResolverStarted() : TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
}
