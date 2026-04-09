using Microsoft.Extensions.DependencyInjection;
using Mocha.Testing;
using Mocha.Transport.InMemory;

namespace Mocha.Testing.Tests;

public class FanOutTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task WaitForCompletionAsync_Should_TrackBothHandlers_When_TwoSubscribers()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<FanOutHandlerA>();
            b.AddEventHandler<FanOutHandlerB>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new FanOutEvent { Value = "TWO" }, CancellationToken.None);

        // Wait for both handlers — fan-out envelopes arrive staggered,
        // so loop until we see the expected count or timeout
        await WaitForConsumedCount<FanOutEvent>(tracker, 2);

        // assert — cumulative tracker should see both consumed
        var consumed = tracker.Consumed.Where(m => m.Message is FanOutEvent).ToList();
        Assert.Equal(2, consumed.Count);
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_TrackAllHandlers_When_ThreeSubscribers()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<FanOutHandlerA>();
            b.AddEventHandler<FanOutHandlerB>();
            b.AddEventHandler<FanOutHandlerC>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new FanOutEvent { Value = "THREE" }, CancellationToken.None);

        // Wait for all three handlers
        await WaitForConsumedCount<FanOutEvent>(tracker, 3);

        // assert — cumulative tracker should see all three consumed
        var consumed = tracker.Consumed.Where(m => m.Message is FanOutEvent).ToList();
        Assert.Equal(3, consumed.Count);
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_TrackCascade_When_FanOutHandlerPublishes()
    {
        // arrange — FanOutCascadingHandler publishes a secondary event, FanOutHandlerA is a no-op
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<FanOutCascadingHandler>();
            b.AddEventHandler<FanOutHandlerA>();
            b.AddEventHandler<FanOutSecondaryHandler>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new FanOutEvent { Value = "CASCADE" }, CancellationToken.None);

        // Wait for fan-out (2 handlers for FanOutEvent) + cascade (1 FanOutSecondary)
        await WaitForConsumedCount<FanOutEvent>(tracker, 2);
        await WaitForConsumedCount<FanOutSecondary>(tracker, 1);

        // assert — FanOutEvent consumed by both handlers, plus FanOutSecondary consumed
        var fanOutConsumed = tracker.Consumed.Where(m => m.Message is FanOutEvent).ToList();
        Assert.Equal(2, fanOutConsumed.Count);

        var secondaryConsumed = tracker.Consumed.Where(m => m.Message is FanOutSecondary).ToList();
        Assert.Single(secondaryConsumed);
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_TrackSeparateConsumptions_When_FanOut()
    {
        // arrange — two handlers for the same event
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<FanOutHandlerA>();
            b.AddEventHandler<FanOutHandlerB>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new FanOutEvent { Value = "KEYS" }, CancellationToken.None);

        // Wait for both handlers
        await WaitForConsumedCount<FanOutEvent>(tracker, 2);

        // assert — fan-out means each handler receives and consumes independently
        // The consumed list should have 2 entries (one per handler) even though
        // only 1 message was published
        var consumed = tracker.Consumed.Where(m => m.Message is FanOutEvent).ToList();
        Assert.Equal(2, consumed.Count);
    }

    // --- Helpers ---

    private static async Task WaitForConsumedCount<T>(IMessageTracker tracker, int expected)
    {
        // Fan-out envelopes arrive staggered — wait multiple times until we see the
        // expected count of consumed messages, or the timeout expires.
        var deadline = DateTime.UtcNow.Add(Timeout);
        while (DateTime.UtcNow < deadline)
        {
            await tracker.WaitForCompletionAsync(Timeout);
            var count = tracker.Consumed.Count(m => m.Message is T);
            if (count >= expected)
            {
                return;
            }
        }
    }

    private static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();
        services.AddMessageTracking();

        var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        await ((MessagingRuntime)runtime).StartAsync(CancellationToken.None);
        return provider;
    }
}

// --- Test message and handler types ---

public sealed class FanOutEvent
{
    public required string Value { get; init; }
}

public sealed class FanOutSecondary
{
    public required string Source { get; init; }
}

public sealed class FanOutHandlerA : IEventHandler<FanOutEvent>
{
    public ValueTask HandleAsync(FanOutEvent _, CancellationToken __) => default;
}

public sealed class FanOutHandlerB : IEventHandler<FanOutEvent>
{
    public ValueTask HandleAsync(FanOutEvent _, CancellationToken __) => default;
}

public sealed class FanOutHandlerC : IEventHandler<FanOutEvent>
{
    public ValueTask HandleAsync(FanOutEvent _, CancellationToken __) => default;
}

public sealed class FanOutCascadingHandler(IMessageBus bus) : IEventHandler<FanOutEvent>
{
    public async ValueTask HandleAsync(FanOutEvent message, CancellationToken cancellationToken)
    {
        await bus.PublishAsync(
            new FanOutSecondary { Source = message.Value },
            cancellationToken);
    }
}

public sealed class FanOutSecondaryHandler : IEventHandler<FanOutSecondary>
{
    public ValueTask HandleAsync(FanOutSecondary _, CancellationToken __) => default;
}
