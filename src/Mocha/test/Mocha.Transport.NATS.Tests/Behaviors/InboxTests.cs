using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Inbox;
using Mocha.Middlewares;
using Mocha.Transport.NATS.Tests.Helpers;
using NATS.Client.Core;

namespace Mocha.Transport.NATS.Tests.Behaviors;

[Collection("NATS")]
public class InboxTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly NatsFixture _fixture;

    public InboxTests(NatsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Inbox_Should_DeduplicateMessage_When_SameMessageIdPublishedTwice()
    {
        // arrange
        var inbox = new InMemoryMessageInbox();
        var recorder = new MessageRecorder();
        var fixedMessageId = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddSingleton(new NatsConnection(_fixture.CreateOptions()));
        services.AddSingleton(recorder);
        services.AddSingleton<IMessageInbox>(inbox);

        var builder = services
            .AddMessageBus()
            .AddEventHandler<InboxEventHandler>()
            .UseInboxCore();

        builder.ConfigureMessageBus(h =>
            h.PrependDispatch(new DispatchMiddlewareConfiguration(
                (_, next) =>
                    ctx =>
                    {
                        ctx.MessageId = fixedMessageId;
                        return next(ctx);
                    },
                "ForceMessageId")));

        await using var bus = await builder
            .AddNats()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish the same logical message twice (same MessageId forced)
        await messageBus.PublishAsync(new InboxEvent { Payload = "first" }, CancellationToken.None);

        // Wait for the first message to be fully processed and recorded in the inbox
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the first event within timeout");
        await WaitUntilAsync(() => inbox.RecordedEnvelopes.Count >= 1, s_timeout);

        await messageBus.PublishAsync(new InboxEvent { Payload = "second" }, CancellationToken.None);

        // assert - only the first message should be handled; the second is a duplicate
        Assert.False(
            await recorder.WaitAsync(TimeSpan.FromSeconds(3), expectedCount: 2),
            "Handler should NOT have received the duplicate message");

        Assert.Single(recorder.Messages);
    }

    [Fact]
    public async Task Inbox_Should_ProcessBothMessages_When_DifferentMessageIds()
    {
        // arrange
        var inbox = new InMemoryMessageInbox();
        var recorder = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(new NatsConnection(_fixture.CreateOptions()))
            .AddSingleton(recorder)
            .AddSingleton<IMessageInbox>(inbox)
            .AddMessageBus()
            .AddEventHandler<InboxEventHandler>()
            .UseInboxCore()
            .AddNats()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish two distinct messages (each gets its own auto-generated MessageId)
        await messageBus.PublishAsync(new InboxEvent { Payload = "msg-1" }, CancellationToken.None);
        await messageBus.PublishAsync(new InboxEvent { Payload = "msg-2" }, CancellationToken.None);

        // assert - both messages should be processed
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: 2),
            "Handler did not receive both events within timeout");

        Assert.Equal(2, recorder.Messages.Count);

        var payloads = recorder.Messages.Cast<InboxEvent>().Select(e => e.Payload).OrderBy(p => p).ToList();
        Assert.Equal(["msg-1", "msg-2"], payloads);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!condition())
        {
            await Task.Delay(50, cts.Token);
        }
    }

    public sealed class InboxEvent
    {
        public required string Payload { get; init; }
    }

    public sealed class InboxEventHandler(MessageRecorder recorder) : IEventHandler<InboxEvent>
    {
        public ValueTask HandleAsync(InboxEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    internal sealed class InMemoryMessageInbox : IMessageInbox
    {
        private readonly ConcurrentDictionary<(string MessageId, string ConsumerType), MessageEnvelope> _processed = new();

        public ConcurrentBag<MessageEnvelope> RecordedEnvelopes { get; } = [];

        public ValueTask<bool> ExistsAsync(
            string messageId,
            string consumerType,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(_processed.ContainsKey((messageId, consumerType)));
        }

        public ValueTask<bool> TryClaimAsync(
            MessageEnvelope envelope,
            string consumerType,
            CancellationToken cancellationToken)
        {
            if (envelope.MessageId is null)
            {
                return ValueTask.FromResult(false);
            }

            var claimed = _processed.TryAdd((envelope.MessageId, consumerType), envelope);
            if (claimed)
            {
                RecordedEnvelopes.Add(envelope);
            }

            return ValueTask.FromResult(claimed);
        }

        public ValueTask RecordAsync(
            MessageEnvelope envelope,
            string consumerType,
            CancellationToken cancellationToken)
        {
            if (envelope.MessageId is not null)
            {
                _processed.TryAdd((envelope.MessageId, consumerType), envelope);
            }

            RecordedEnvelopes.Add(envelope);
            return ValueTask.CompletedTask;
        }

        public ValueTask<int> CleanupAsync(
            TimeSpan maxAge,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(0);
        }
    }
}
