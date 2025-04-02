using HotChocolate.Subscriptions.Diagnostics;
using HotChocolate.Tests;
using Moq;
using Xunit.Abstractions;

namespace HotChocolate.Subscriptions;

public class DefaultTopicTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task Unsubscribe_ForAsyncDisposableSession_DisposesAsync()
    {
        var sessionMock = new Mock<IAsyncAndSyncDisposable>();
        var pubSub = new NoOpPubSub(sessionMock.Object, new SubscriptionTestDiagnostics(outputHelper));

        var sourceStream = await pubSub.SubscribeAsync<string>("topic");
        await sourceStream.DisposeAsync();

        sessionMock.Verify(x => x.DisposeAsync(), Times.Once);
        sessionMock.Verify(x => x.Dispose(), Times.Never);
    }

    private sealed class NoOpPubSub(IAsyncAndSyncDisposable session, ISubscriptionDiagnosticEvents diagnosticEvents)
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
            IAsyncAndSyncDisposable session)
            : DefaultTopic<TMessage>(name, capacity, fullMode, diagnosticEvents)
        {
            protected override ValueTask<IDisposable> OnConnectAsync(CancellationToken cancellationToken)
            {
                return ValueTask.FromResult<IDisposable>(session);
            }
        }
    }

    public interface IAsyncAndSyncDisposable : IAsyncDisposable, IDisposable;
}
