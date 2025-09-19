using HotChocolate.Subscriptions.Diagnostics;
using HotChocolate.Tests;
using Xunit.Abstractions;

namespace HotChocolate.Subscriptions;

public class DefaultTopicTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task Unsubscribe_ForAsyncDisposableSession_DisposesAsync()
    {
        var sessionMock = new StubAsyncDisposableSession();
        var pubSub = new NoOpPubSub(sessionMock, new SubscriptionTestDiagnostics(outputHelper));

        var sourceStream = await pubSub.SubscribeAsync<string>("topic");
        await sourceStream.DisposeAsync();

        SpinWait.SpinUntil(() => sessionMock.AsyncDisposableCalled, TimeSpan.FromSeconds(5));
        Assert.False(sessionMock.DisposableCalled);
    }

    [Fact]
    public async Task Unsubscribe_ForSyncDisposableSession_DisposesSync()
    {
        var sessionMock = new StubDisposableSession();
        var pubSub = new NoOpPubSub(sessionMock, new SubscriptionTestDiagnostics(outputHelper));

        var sourceStream = await pubSub.SubscribeAsync<string>("topic");
        await sourceStream.DisposeAsync();

        SpinWait.SpinUntil(() => sessionMock.DisposableCalled, TimeSpan.FromSeconds(5));
    }

    private sealed class NoOpPubSub(IDisposable session, ISubscriptionDiagnosticEvents diagnosticEvents)
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
            IDisposable session)
            : DefaultTopic<TMessage>(name, capacity, fullMode, diagnosticEvents)
        {
            protected override ValueTask<IDisposable> OnConnectAsync(CancellationToken cancellationToken)
            {
                return ValueTask.FromResult(session);
            }
        }
    }

    private class StubDisposableSession : IDisposable
    {
        public bool DisposableCalled { get; private set; }

        public void Dispose()
        {
            DisposableCalled = true;
        }
    }

    private class StubAsyncDisposableSession : StubDisposableSession, IAsyncDisposable
    {
        public bool AsyncDisposableCalled { get; private set; }

        public ValueTask DisposeAsync()
        {
            AsyncDisposableCalled = true;
            return ValueTask.CompletedTask;
        }
    }
}
