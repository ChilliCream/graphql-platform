using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Outbox;

public class OutboxIntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    // ──────────────────────────────────────────────────────────────────────
    // Test 1: Outbox captures a published message
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Outbox_Should_CaptureMessage_When_EventPublished()
    {
        // arrange
        var outbox = new InMemoryMessageOutbox();
        await using var provider = await CreateBusWithOutboxAsync(outbox, _ => { });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OutboxTestEvent { Payload = "capture-me" }, CancellationToken.None);

        // assert - message captured by outbox, not delivered to transport
        await WaitUntilAsync(() => !outbox.Envelopes.IsEmpty, s_timeout);
        Assert.Single(outbox.Envelopes);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Test 2: Outbox captures multiple published messages
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Outbox_Should_CaptureMultipleMessages_When_MultipleEventsPublished()
    {
        // arrange
        var outbox = new InMemoryMessageOutbox();
        await using var provider = await CreateBusWithOutboxAsync(outbox, _ => { });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OutboxTestEvent { Payload = "first" }, CancellationToken.None);
        await bus.PublishAsync(new OutboxTestEvent { Payload = "second" }, CancellationToken.None);
        await bus.PublishAsync(new OutboxTestEvent { Payload = "third" }, CancellationToken.None);

        // assert - all three captured
        await WaitUntilAsync(() => outbox.Envelopes.Count >= 3, s_timeout);
        Assert.Equal(3, outbox.Envelopes.Count);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Test 3: Outbox skipped when SkipOutbox feature is set
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Outbox_Should_SkipCapture_When_SkipOutboxFeatureSet()
    {
        // arrange
        var outbox = new InMemoryMessageOutbox();
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithOutboxAsync(
            outbox,
            b =>
            {
                b.Services.AddSingleton(recorder);
                b.AddEventHandler<OutboxTestEventHandler>();
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish with SkipOutbox header set via PublishOptions
        // The SkipOutbox is set via dispatch middleware feature, which we configure
        // by prepending a middleware that sets it before the outbox middleware runs
        await bus.PublishAsync(
            new OutboxTestEvent { Payload = "skip-me" },
            new PublishOptions { Headers = new Dictionary<string, object?> { ["x-skip-outbox"] = "true" } },
            CancellationToken.None);

        // Also publish a normal one that should go to outbox
        await bus.PublishAsync(new OutboxTestEvent { Payload = "capture-me" }, CancellationToken.None);

        // assert - only one message captured (the one without skip), the skipped one
        // was delivered to handler
        Assert.True(await recorder.WaitAsync(s_timeout), "Skipped message should have been delivered to handler");
        await WaitUntilAsync(() => !outbox.Envelopes.IsEmpty, s_timeout);

        Assert.Single(outbox.Envelopes);
        Assert.Single(recorder.Messages);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Test 4: Outbox signal is invoked when message persisted
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Outbox_Should_SignalWorker_When_MessagePersisted()
    {
        // arrange
        var signal = new TestOutboxSignal();
        var outbox = new InMemoryMessageOutbox(signal);
        await using var provider = await CreateBusWithOutboxAsync(
            outbox,
            b =>
            {
                // Replace the default signal with our test signal
                b.Services.AddSingleton<IOutboxSignal>(signal);
            });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OutboxTestEvent { Payload = "signal-test" }, CancellationToken.None);

        // assert - signal was set after persist
        await WaitUntilAsync(() => signal.SignalCount > 0, s_timeout);
        Assert.True(signal.SignalCount >= 1, "Signal should have been set at least once");
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

    private static async Task<ServiceProvider> CreateBusWithOutboxAsync(
        InMemoryMessageOutbox outbox,
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageOutbox>(outbox);

        var builder = services.AddMessageBus();
        builder.UseOutboxCore();

        // Add a middleware before outbox that checks for the skip header
        builder.ConfigureMessageBus(h =>
            h.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    static (_, next) =>
                        ctx =>
                        {
                            if (ctx.Headers.TryGetValue("x-skip-outbox", out var val) && val is "true")
                            {
                                ctx.SkipOutbox();
                            }
                            return next(ctx);
                        },
                    "SkipOutboxCheck"),
                before: "Outbox")
        );

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

    public sealed class OutboxTestEvent
    {
        public required string Payload { get; init; }
    }

    public sealed class OutboxTestEventHandler(MessageRecorder recorder) : IEventHandler<OutboxTestEvent>
    {
        public ValueTask HandleAsync(OutboxTestEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    /// <summary>
    /// In-memory outbox that stores envelopes for test assertions.
    /// Optionally signals an <see cref="IOutboxSignal"/> on persist,
    /// mirroring what a production outbox implementation would do.
    /// </summary>
    public sealed class InMemoryMessageOutbox(IOutboxSignal? signal = null) : IMessageOutbox
    {
        public ConcurrentBag<MessageEnvelope> Envelopes { get; } = [];

        public ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            Envelopes.Add(envelope);
            signal?.Set();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Test signal that records how many times it was set.
    /// </summary>
    public sealed class TestOutboxSignal : IOutboxSignal
    {
        private int _signalCount;

        public int SignalCount => _signalCount;

        public void Set() => Interlocked.Increment(ref _signalCount);

        public Task WaitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
