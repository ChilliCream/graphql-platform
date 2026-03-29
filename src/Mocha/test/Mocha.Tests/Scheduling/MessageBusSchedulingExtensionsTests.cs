using Microsoft.Extensions.Time.Testing;

namespace Mocha.Tests.Scheduling;

public class MessageBusSchedulingExtensionsTests
{
    [Fact]
    public async Task ScheduleSendAsync_WithAbsoluteTime_Should_DelegateToSendAsync_When_Called()
    {
        // arrange
        var spy = new SpyMessageBus();
        var scheduledTime = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var message = new TestMessage("send-abs");

        // act
        await spy.ScheduleSendAsync(message, scheduledTime);

        // assert
        Assert.Single(spy.SentMessages);
        var (sentMsg, sentOptions) = spy.SentMessages[0];
        Assert.Same(message, sentMsg);
        Assert.Equal(scheduledTime, sentOptions.ScheduledTime);
    }

    [Fact]
    public async Task SchedulePublishAsync_WithAbsoluteTime_Should_DelegateToPublishAsync_When_Called()
    {
        // arrange
        var spy = new SpyMessageBus();
        var scheduledTime = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var message = new TestMessage("pub-abs");

        // act
        await spy.SchedulePublishAsync(message, scheduledTime);

        // assert
        Assert.Single(spy.PublishedMessages);
        var (pubMsg, pubOptions) = spy.PublishedMessages[0];
        Assert.Same(message, pubMsg);
        Assert.Equal(scheduledTime, pubOptions.ScheduledTime);
    }

    private sealed record TestMessage(string Payload);

    private sealed class SpyMessageBus : IMessageBus
    {
        public List<(object Message, SendOptions Options)> SentMessages { get; } = [];
        public List<(object Message, PublishOptions Options)> PublishedMessages { get; } = [];

        public ValueTask SendAsync(object message, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask SendAsync(object message, SendOptions options, CancellationToken cancellationToken)
        {
            SentMessages.Add((message, options));
            return ValueTask.CompletedTask;
        }

        public ValueTask PublishAsync<T>(T message, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken)
        {
            PublishedMessages.Add((message!, options));
            return ValueTask.CompletedTask;
        }

        public ValueTask<TResponse> RequestAsync<TResponse>(
            IEventRequest<TResponse> message,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<TResponse> RequestAsync<TResponse>(
            IEventRequest<TResponse> message,
            SendOptions options,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask RequestAsync(object message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask RequestAsync(object message, SendOptions options, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask ReplyAsync<TResponse>(
            TResponse response,
            ReplyOptions options,
            CancellationToken cancellationToken) where TResponse : notnull =>
            throw new NotSupportedException();
    }
}
