using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Inbox;
using Mocha.Middlewares;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class InboxTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Inbox_Should_DeduplicateMessage_When_SameMessageIdPublishedTwice()
    {
        // arrange
        var inbox = new InMemoryMessageInbox();
        var recorder = new MessageRecorder();
        var fixedMessageId = Guid.NewGuid().ToString();
        await using var scope = await fixture.CreateScopeAsync();

        var services = new ServiceCollection();
        services.AddSingleton(fixture.Connection);
        services.AddSingleton(recorder);
        services.AddSingleton<IMessageInbox>(inbox);

        var builder = services
            .AddMessageBus()
            .AddEventHandler<InboxEventHandler>()
            .UseInboxCore();

        builder.ConfigureMessageBus(h =>
            h.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) =>
                        ctx =>
                        {
                            ctx.MessageId = fixedMessageId;
                            return next(ctx);
                        },
                    "ForceMessageId"),
                before: "Instrumentation"));

        await using var bus = await builder
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(new InboxEvent { Payload = "first" }, CancellationToken.None);

        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the first event within timeout");
        await WaitUntilAsync(() => !inbox.RecordedEnvelopes.IsEmpty, s_timeout);

        await messageBus.PublishAsync(new InboxEvent { Payload = "second" }, CancellationToken.None);

        // assert
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
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(recorder)
            .AddSingleton<IMessageInbox>(inbox)
            .AddMessageBus()
            .AddEventHandler<InboxEventHandler>()
            .UseInboxCore()
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(new InboxEvent { Payload = "msg-1" }, CancellationToken.None);
        await messageBus.PublishAsync(new InboxEvent { Payload = "msg-2" }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: 2),
            "Handler did not receive both events within timeout");

        var payloads = recorder.Messages.Cast<InboxEvent>().Select(e => e.Payload).OrderBy(p => p).ToList();

        Assert.Equal(["msg-1", "msg-2"], payloads);
    }

    [Fact]
    public async Task Inbox_Should_ProcessMessage_When_SkipInboxIsSet()
    {
        // arrange
        var inbox = new InMemoryMessageInbox();
        var recorder = new MessageRecorder();
        var fixedMessageId = Guid.NewGuid().ToString();
        await using var scope = await fixture.CreateScopeAsync();

        var services = new ServiceCollection();
        services.AddSingleton(fixture.Connection);
        services.AddSingleton(recorder);
        services.AddSingleton<IMessageInbox>(inbox);

        var builder = services
            .AddMessageBus()
            .AddEventHandler<InboxEventHandler>()
            .UseInboxCore();

        builder.ConfigureMessageBus(h =>
        {
            h.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) =>
                        ctx =>
                        {
                            ctx.MessageId = fixedMessageId;
                            return next(ctx);
                        },
                    "ForceMessageId"),
                before: "Instrumentation");

            h.UseConsume(
                new ConsumerMiddlewareConfiguration(
                    static (_, next) =>
                        ctx =>
                        {
                            var feature = ctx.Features.GetOrSet<InboxMiddlewareFeature>();
                            feature.SkipInbox = true;
                            return next(ctx);
                        },
                    "SkipInboxCheck"),
                before: "Inbox");
        });

        await using var bus = await builder
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(new InboxEvent { Payload = "skip-1" }, CancellationToken.None);
        await messageBus.PublishAsync(new InboxEvent { Payload = "skip-2" }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: 2),
            "Handler should receive both messages when SkipInbox is set");

        Assert.Equal(2, recorder.Messages.Count);
    }

    [Fact]
    public async Task Inbox_Should_ProcessMessage_When_MessageIdIsNull()
    {
        // arrange
        var inbox = new InMemoryMessageInbox();
        var recorder = new MessageRecorder();
        await using var scope = await fixture.CreateScopeAsync();

        var services = new ServiceCollection();
        services.AddSingleton(fixture.Connection);
        services.AddSingleton(recorder);
        services.AddSingleton<IMessageInbox>(inbox);

        var builder = services
            .AddMessageBus()
            .AddEventHandler<InboxEventHandler>()
            .UseInboxCore();

        builder.ConfigureMessageBus(h =>
            h.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) =>
                        ctx =>
                        {
                            ctx.MessageId = null;
                            return next(ctx);
                        },
                    "ClearMessageId"),
                before: "Instrumentation"));

        await using var bus = await builder
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(new InboxEvent { Payload = "null-id-1" }, CancellationToken.None);
        await messageBus.PublishAsync(new InboxEvent { Payload = "null-id-2" }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: 2),
            "Handler should receive both messages when MessageId is null");

        Assert.Equal(2, recorder.Messages.Count);
        Assert.Empty(inbox.RecordedEnvelopes);
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
            => ValueTask.FromResult(_processed.ContainsKey((messageId, consumerType)));

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

        public ValueTask<int> CleanupAsync(TimeSpan maxAge, CancellationToken cancellationToken)
            => ValueTask.FromResult(0);
    }
}
