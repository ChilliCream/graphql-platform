using HotChocolate.Subscriptions;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

public sealed class SubscriptionEventTimeoutTests
{
    [Fact]
    public async Task Event_Should_Time_Out_When_Execution_Exceeds_Configured_Budget()
    {
        // arrange
        // A short per-event timeout. The resolver blocks until cancelled, so the event
        // must time out and the subscription must tear down.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddInMemorySubscriptions()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .AddSubscriptionType<BlockingSubscription>()
            .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(200))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var sender = executor.Schema.Services.GetRootServiceProvider()
            .GetRequiredService<ITopicEventSender>();

        var subscribe = await executor.ExecuteAsync("subscription { onMessage }", cts.Token);
        await using var stream = subscribe.ExpectResponseStream();
        var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(cts.Token);

        // act
        await sender.SendAsync("OnMessage", "one", cts.Token);

        // MoveNextAsync must complete (with false) once the event budget elapses and the
        // subscription tears down.
        var moveNext = enumerator.MoveNextAsync().AsTask();
        var completed = await Task.WhenAny(moveNext, Task.Delay(5000, cts.Token));

        // assert
        Assert.Same(moveNext, completed);
        Assert.False(await moveNext);

        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task Timer_Should_Be_Reset_Between_Events()
    {
        // arrange
        // Budget is short, but each event completes quickly. No event should time out
        // and the subscription must keep running across multiple fires.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddInMemorySubscriptions()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .AddSubscriptionType<EchoSubscription>()
            .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromSeconds(1))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        var sender = executor.Schema.Services.GetRootServiceProvider()
            .GetRequiredService<ITopicEventSender>();

        var subscribe = await executor.ExecuteAsync("subscription { onMessage }", cts.Token);
        await using var stream = subscribe.ExpectResponseStream();
        var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(cts.Token);

        // act
        // Fire several fast events, each separated by more than half the budget. If the
        // CTS were not reset between events the second or third fire would already have
        // a cancelled token.
        for (var i = 0; i < 3; i++)
        {
            await sender.SendAsync("OnMessage", $"m{i}", cts.Token);
            Assert.True(await enumerator.MoveNextAsync());
            Assert.Empty(enumerator.Current.Errors);
            await Task.Delay(600, cts.Token);
        }

        // assert
        // A fourth event still succeeds — the timer was reset on every prior event.
        await sender.SendAsync("OnMessage", "final", cts.Token);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Empty(enumerator.Current.Errors);

        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task Client_Abort_Should_Tear_Down_Current_Event_When_Timeout_Is_Configured()
    {
        // arrange
        // With a per-event timeout configured the shared CTS must still observe the
        // request-level abort. Start an event, abort the request, and confirm the
        // subscription tears down.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var abortCts = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, abortCts.Token);

        var executor = await new ServiceCollection()
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

        // Give the event loop a brief moment to enter the blocking resolver before aborting.
        await Task.Delay(100, cts.Token);

        // act
        // abort the request — the shared event CTS should cancel via the registration
        await abortCts.CancelAsync();

        // assert
        // MoveNext completes promptly (false = stream ended) once cancellation propagates.
        var completed = await Task.WhenAny(moveNext, Task.Delay(2000, cts.Token));
        Assert.Same(moveNext, completed);
        Assert.False(await moveNext);

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
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return message;
        }
    }
}
