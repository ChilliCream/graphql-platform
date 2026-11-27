using HotChocolate.Execution;
using HotChocolate.Subscriptions.Diagnostics;
using HotChocolate.Tests;

namespace HotChocolate.Subscriptions;

public class DefaultTopicTests(ITestOutputHelper outputHelper)
{
    private const int Rounds = 200;
    private const int ConcurrentSubscribers = 32;

    [Fact]
    public async Task Unsubscribe_ForAsyncDisposableSession_DisposesAsync()
    {
        var sessionMock = new StubAsyncDisposableSession();
        var pubSub = new NoOpPubSub(sessionMock, new SubscriptionTestDiagnostics(outputHelper));

        var sourceStream = await pubSub.SubscribeAsync<string>("topic", TestContext.Current.CancellationToken);
        await sourceStream.DisposeAsync();

        SpinWait.SpinUntil(() => sessionMock.AsyncDisposableCalled, TimeSpan.FromSeconds(5));
        Assert.True(sessionMock.AsyncDisposableCalled);
    }

    [Fact]
    public async Task SubscribeAsync_Should_DeliverMessageToEverySubscriber_When_SubscribingConcurrently()
    {
        // arrange
        using var pubSub = new PublishingPubSub();
        var missed = 0;

        // act
        for (var round = 0; round < Rounds && missed == 0; round++)
        {
            var topic = $"topic-{round}";
            var first = await pubSub.SubscribeAsync<string>(topic, TestContext.Current.CancellationToken);
            ISourceStream<string>[] streams = [first, .. await SubscribeConcurrentlyAsync(pubSub, topic)];

            await pubSub.SendAsync(topic, "message", TestContext.Current.CancellationToken);

            missed += await CountMissedAsync(streams, "message");
            await DisposeAllAsync(streams);
        }

        // assert
        Assert.Equal(0, missed);
    }

    [Fact]
    public async Task SubscribeAsync_Should_DeliverMessageToEverySubscriber_When_SubscribingDuringDispatch()
    {
        // arrange
        using var pubSub = new PublishingPubSub();
        var missed = 0;

        // act
        for (var round = 0; round < Rounds && missed == 0; round++)
        {
            var topic = $"topic-{round}";
            var first = await pubSub.SubscribeAsync<string>(topic, TestContext.Current.CancellationToken);

            using var stopPublishing = new CancellationTokenSource();
            var publishing = PublishUntilCanceledAsync(pubSub, topic, stopPublishing.Token);
            ISourceStream<string>[] streams = [first, .. await SubscribeConcurrentlyAsync(pubSub, topic)];
            await stopPublishing.CancelAsync();
            await publishing;

            await pubSub.SendAsync(topic, "message", TestContext.Current.CancellationToken);

            missed += await CountMissedAsync(streams, "message");
            await DisposeAllAsync(streams);
        }

        // assert
        Assert.Equal(0, missed);
    }

    private static async Task<ISourceStream<string>[]> SubscribeConcurrentlyAsync(
        DefaultPubSub pubSub,
        string topic)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscribing = new Task<ISourceStream<string>>[ConcurrentSubscribers];

        for (var i = 0; i < subscribing.Length; i++)
        {
            subscribing[i] = Task.Run(async () =>
            {
                await start.Task;
                return await pubSub.SubscribeAsync<string>(topic);
            });
        }

        start.SetResult();
        return await Task.WhenAll(subscribing);
    }

    private static Task PublishUntilCanceledAsync(
        DefaultPubSub pubSub,
        string topic,
        CancellationToken cancellationToken)
        => Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await pubSub.SendAsync(topic, "noise", CancellationToken.None);
                await Task.Yield();
            }
        });

    private static async Task<int> CountMissedAsync(ISourceStream<string>[] streams, string expected)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var received = await Task.WhenAll(streams.Select(s => ReceiveAsync(s, expected, timeout.Token)));
        return received.Count(r => !r);
    }

    private static async Task<bool> ReceiveAsync(
        ISourceStream<string> stream,
        string expected,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in stream.ReadEventsAsync().WithCancellation(cancellationToken))
            {
                if (message == expected)
                {
                    return true;
                }
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static async Task DisposeAllAsync(ISourceStream<string>[] streams)
    {
        foreach (var stream in streams)
        {
            await stream.DisposeAsync();
        }
    }

    private sealed class PublishingPubSub()
        : DefaultPubSub(new SubscriptionOptions(), NoopSubscriptionDiagnosticEventsListener.Default)
    {
        protected override ValueTask OnSendAsync<TMessage>(
            string formattedTopic,
            TMessage message,
            CancellationToken cancellationToken = default)
        {
            if (TryGetTopic<TMessage>(formattedTopic, out var topic))
            {
                topic.Publish(message);
            }

            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnCompleteAsync(string formattedTopic)
            => ValueTask.CompletedTask;

        protected override DefaultTopic<TMessage> OnCreateTopic<TMessage>(
            string formattedTopic,
            int? bufferCapacity,
            TopicBufferFullMode? bufferFullMode)
            => new PublishingTopic<TMessage>(
                formattedTopic,
                bufferCapacity ?? 64,
                bufferFullMode ?? TopicBufferFullMode.DropOldest,
                DiagnosticEvents);

        private sealed class PublishingTopic<TMessage>(
            string name,
            int capacity,
            TopicBufferFullMode fullMode,
            ISubscriptionDiagnosticEvents diagnosticEvents)
            : DefaultTopic<TMessage>(name, capacity, fullMode, diagnosticEvents);
    }

    private sealed class NoOpPubSub(IAsyncDisposable session, ISubscriptionDiagnosticEvents diagnosticEvents)
        : DefaultPubSub(new SubscriptionOptions(), diagnosticEvents)
    {
        protected override ValueTask OnSendAsync<TMessage>(string formattedTopic, TMessage message, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnCompleteAsync(string formattedTopic)
        {
            return ValueTask.CompletedTask;
        }

        protected override DefaultTopic<TMessage> OnCreateTopic<TMessage>(string formattedTopic, int? bufferCapacity, TopicBufferFullMode? bufferFullMode)
        {
            return new AsyncDisposableTopic<TMessage>(
                formattedTopic,
                bufferCapacity ?? 1,
                bufferFullMode ?? TopicBufferFullMode.DropOldest,
                DiagnosticEvents,
                session);
        }

        private sealed class AsyncDisposableTopic<TMessage>(
            string name,
            int capacity,
            TopicBufferFullMode fullMode,
            ISubscriptionDiagnosticEvents diagnosticEvents,
            IAsyncDisposable session)
            : DefaultTopic<TMessage>(name, capacity, fullMode, diagnosticEvents)
        {
            protected override ValueTask<IAsyncDisposable> OnConnectAsync(CancellationToken cancellationToken)
            {
                return ValueTask.FromResult(session);
            }
        }
    }

    private class StubAsyncDisposableSession : IAsyncDisposable
    {
        public bool AsyncDisposableCalled { get; private set; }

        public ValueTask DisposeAsync()
        {
            AsyncDisposableCalled = true;
            return ValueTask.CompletedTask;
        }
    }
}
