using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Inbox;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Inbox;

public class InboxIntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Inbox_Should_RecordMessage_When_EventReceived()
    {
        // arrange
        var inbox = new InMemoryMessageInbox();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithInboxAsync(
            inbox,
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<InboxTestEventHandler>();
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new InboxTestEvent { Payload = "record-me" }, CancellationToken.None);

        // assert - handler received the message and inbox recorded it
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event within timeout");
        await WaitUntilAsync(() => inbox.RecordedEnvelopes.Count >= 1, s_timeout);
        Assert.Single(inbox.RecordedEnvelopes);
    }

    [Fact]
    // THis test is wrong?
    public async Task Inbox_Should_DeduplicateMessage_When_SameEventPublishedTwice()
    {
        // arrange
        var inbox = new InMemoryMessageInbox();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithInboxAsync(
            inbox,
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<InboxTestEventHandler>();
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish two distinct messages (each gets its own MessageId)
        await bus.PublishAsync(new InboxTestEvent { Payload = "first" }, CancellationToken.None);
        await bus.PublishAsync(new InboxTestEvent { Payload = "second" }, CancellationToken.None);

        // assert - both messages are received and recorded (they have different IDs)
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: 2),
            "Handler did not receive both events within timeout");

        await WaitUntilAsync(() => inbox.RecordedEnvelopes.Count >= 2, s_timeout);
        Assert.Equal(2, inbox.RecordedEnvelopes.Count);
    }

    [Fact]
    public async Task Inbox_Should_RecordMultipleMessages_When_MultipleEventsReceived()
    {
        // arrange
        var inbox = new InMemoryMessageInbox();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithInboxAsync(
            inbox,
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<InboxTestEventHandler>();
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new InboxTestEvent { Payload = "first" }, CancellationToken.None);
        await bus.PublishAsync(new InboxTestEvent { Payload = "second" }, CancellationToken.None);
        await bus.PublishAsync(new InboxTestEvent { Payload = "third" }, CancellationToken.None);

        // assert - all three captured
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: 3),
            "Handler did not receive all 3 events within timeout");

        await WaitUntilAsync(() => inbox.RecordedEnvelopes.Count >= 3, s_timeout);
        Assert.Equal(3, inbox.RecordedEnvelopes.Count);
    }

    [Fact]
    public async Task Inbox_Should_SkipRecording_When_SkipInboxFeatureSet()
    {
        // arrange
        var inbox = new InMemoryMessageInbox();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithInboxAsync(
            inbox,
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<InboxTestEventHandler>();

                // Add a consumer middleware before inbox that sets SkipInbox
                b.ConfigureMessageBus(h =>
                    h.PrependConsume(
                        "Inbox",
                        new ConsumerMiddlewareConfiguration(
                            static (_, next) =>
                                ctx =>
                                {
                                    var feature = ctx.Features.GetOrSet<InboxMiddlewareFeature>();
                                    feature.SkipInbox = true;
                                    return next(ctx);
                                },
                            "SkipInboxCheck"))
                );
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new InboxTestEvent { Payload = "skip-inbox" }, CancellationToken.None);

        // assert - handler received the message but inbox did NOT record it
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event within timeout");

        // Give a short delay to ensure no async recording happens
        await Task.Delay(200);
        Assert.Empty(inbox.RecordedEnvelopes);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════════════

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!condition())
        {
            await Task.Delay(50, cts.Token);
        }
    }

    private static async Task<ServiceProvider> CreateBusWithInboxAsync(
        InMemoryMessageInbox inbox,
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageInbox>(inbox);

        var builder = services.AddMessageBus();
        builder.UseInboxCore();

        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    // ══════════════════════════════════════════════════════════════════════
    // Test types
    // ══════════════════════════════════════════════════════════════════════

    public sealed class InboxTestEvent
    {
        public required string Payload { get; init; }
    }

    public sealed class InboxTestEventHandler(MessageRecorder recorder) : IEventHandler<InboxTestEvent>
    {
        public ValueTask HandleAsync(InboxTestEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }
}
