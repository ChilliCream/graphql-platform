namespace Mocha.Tests.Scheduling;

public class MessageBusSchedulingTests
{
    [Fact]
    public async Task ScheduleSendAsync_Should_RecordMessage_When_Called()
    {
        // arrange
        var spy = new SpyMessageBus();
        var scheduledTime = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var message = new TestMessage("send-abs");

        // act
        var result = await spy.ScheduleSendAsync(message, scheduledTime, CancellationToken.None);

        // assert
        Assert.Single(spy.ScheduledSendMessages);
        var (sentMsg, sentTime) = spy.ScheduledSendMessages[0];
        Assert.Same(message, sentMsg);
        Assert.Equal(scheduledTime, sentTime);
        Assert.Equal(scheduledTime, result.ScheduledTime);
    }

    [Fact]
    public async Task SchedulePublishAsync_Should_RecordMessage_When_Called()
    {
        // arrange
        var spy = new SpyMessageBus();
        var scheduledTime = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var message = new TestMessage("pub-abs");

        // act
        var result = await spy.SchedulePublishAsync(message, scheduledTime, CancellationToken.None);

        // assert
        Assert.Single(spy.ScheduledPublishMessages);
        var (pubMsg, pubTime) = spy.ScheduledPublishMessages[0];
        Assert.Same(message, pubMsg);
        Assert.Equal(scheduledTime, pubTime);
        Assert.Equal(scheduledTime, result.ScheduledTime);
    }

    private sealed record TestMessage(string Payload);

    private sealed class SpyMessageBus : IMessageBus
    {
        public List<(object Message, DateTimeOffset ScheduledTime)> ScheduledSendMessages { get; } = [];
        public List<(object Message, DateTimeOffset ScheduledTime)> ScheduledPublishMessages { get; } = [];

        public ValueTask SendAsync<T>(T message, CancellationToken cancellationToken) where T : notnull =>
            ValueTask.CompletedTask;

        public ValueTask SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken) where T : notnull =>
            ValueTask.CompletedTask;

        public ValueTask PublishAsync<T>(T message, CancellationToken cancellationToken) where T : notnull =>
            ValueTask.CompletedTask;

        public ValueTask PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken) where T : notnull =>
            ValueTask.CompletedTask;

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

        public ValueTask<SchedulingResult> SchedulePublishAsync<T>(
            T message,
            DateTimeOffset scheduledTime,
            CancellationToken cancellationToken) where T : notnull
        {
            ScheduledPublishMessages.Add((message, scheduledTime));
            return ValueTask.FromResult(new SchedulingResult { ScheduledTime = scheduledTime });
        }

        public ValueTask<SchedulingResult> SchedulePublishAsync<T>(
            T message,
            DateTimeOffset scheduledTime,
            PublishOptions options,
            CancellationToken cancellationToken) where T : notnull
        {
            ScheduledPublishMessages.Add((message, scheduledTime));
            return ValueTask.FromResult(new SchedulingResult { ScheduledTime = scheduledTime });
        }

        public ValueTask<SchedulingResult> ScheduleSendAsync<T>(
            T message,
            DateTimeOffset scheduledTime,
            CancellationToken cancellationToken) where T : notnull
        {
            ScheduledSendMessages.Add((message, scheduledTime));
            return ValueTask.FromResult(new SchedulingResult { ScheduledTime = scheduledTime });
        }

        public ValueTask<SchedulingResult> ScheduleSendAsync<T>(
            T message,
            DateTimeOffset scheduledTime,
            SendOptions options,
            CancellationToken cancellationToken) where T : notnull
        {
            ScheduledSendMessages.Add((message, scheduledTime));
            return ValueTask.FromResult(new SchedulingResult { ScheduledTime = scheduledTime });
        }

        public ValueTask<bool> CancelScheduledMessageAsync(string token, CancellationToken cancellationToken) =>
            ValueTask.FromResult(false);
    }
}
