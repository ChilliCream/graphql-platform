using Microsoft.Extensions.DependencyInjection;
using Mocha.Testing;
using Mocha.Transport.InMemory;

namespace Mocha.Testing.Tests;

public class ParallelIsolationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task WaitForCompletionAsync_Should_NotLeak_When_TwoProvidersRunSimultaneously()
    {
        // arrange — two completely independent service providers
        await using var provider1 = await CreateBusAsync(
            b => b.AddEventHandler<IsolationHandler1>());
        await using var provider2 = await CreateBusAsync(
            b => b.AddEventHandler<IsolationHandler2>());

        var tracker1 = provider1.GetRequiredService<IMessageTracker>();
        var tracker2 = provider2.GetRequiredService<IMessageTracker>();

        // act — publish different message types to each provider
        using (var scope = provider1.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new IsolationEvent1 { Tag = "P1" }, CancellationToken.None);
        }

        using (var scope = provider2.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new IsolationEvent2 { Tag = "P2" }, CancellationToken.None);
        }

        var result1 = await tracker1.WaitForCompletionAsync(Timeout);
        var result2 = await tracker2.WaitForCompletionAsync(Timeout);

        // assert — tracker1 only sees IsolationEvent1, tracker2 only sees IsolationEvent2
        Assert.Single(result1.Consumed);
        Assert.IsType<IsolationEvent1>(result1.Consumed[0].Message);

        Assert.Single(result2.Consumed);
        Assert.IsType<IsolationEvent2>(result2.Consumed[0].Message);

        // No cross-contamination
        Assert.DoesNotContain(result1.Consumed, m => m.Message is IsolationEvent2);
        Assert.DoesNotContain(result2.Consumed, m => m.Message is IsolationEvent1);
    }

    [Fact]
    public async Task Tracker_Should_StopTracking_When_ProviderDisposed()
    {
        // arrange
        IMessageTracker tracker;
        var provider = await CreateBusAsync(
            b => b.AddEventHandler<IsolationHandler1>());

        tracker = provider.GetRequiredService<IMessageTracker>();

        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new IsolationEvent1 { Tag = "BEFORE" }, CancellationToken.None);
        }

        var result = await tracker.WaitForCompletionAsync(Timeout);
        Assert.Single(result.Consumed);

        // act — dispose the provider
        await provider.DisposeAsync();

        // assert — tracker still has the messages from before disposal
        Assert.Single(tracker.Consumed);
        Assert.Single(tracker.Dispatched);
    }

    // --- Helpers ---

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

public sealed class IsolationEvent1
{
    public required string Tag { get; init; }
}

public sealed class IsolationEvent2
{
    public required string Tag { get; init; }
}

public sealed class IsolationHandler1 : IEventHandler<IsolationEvent1>
{
    public ValueTask HandleAsync(IsolationEvent1 _, CancellationToken __) => default;
}

public sealed class IsolationHandler2 : IEventHandler<IsolationEvent2>
{
    public ValueTask HandleAsync(IsolationEvent2 _, CancellationToken __) => default;
}
