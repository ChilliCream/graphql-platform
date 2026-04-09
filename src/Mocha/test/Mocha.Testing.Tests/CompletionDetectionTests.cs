using Microsoft.Extensions.DependencyInjection;
using Mocha.Testing;
using Mocha.Transport.InMemory;

namespace Mocha.Testing.Tests;

public class CompletionDetectionTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task WaitForCompletionAsync_Should_CompleteGracefully_When_NoSubscribers()
    {
        // arrange — no handler registered for CompletionEvent
        await using var provider = await CreateBusAsync(_ => { });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act — publish to a type with no subscribers
        await bus.PublishAsync(new CompletionEvent { Tag = "EMPTY" }, CancellationToken.None);

        // assert — dispatched-only envelopes complete silently after a grace period
        var result = await tracker.WaitForCompletionAsync(Timeout);

        Assert.True(result.Completed);
        Assert.Single(result.Dispatched);
        Assert.Empty(result.Consumed);
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_TrackAllMessages_When_MultiplePublishesBeforeWait()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<CompletionEventHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act — publish 3 messages, then wait once
        await bus.PublishAsync(new CompletionEvent { Tag = "A" }, CancellationToken.None);
        await bus.PublishAsync(new CompletionEvent { Tag = "B" }, CancellationToken.None);
        await bus.PublishAsync(new CompletionEvent { Tag = "C" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — all 3 should be tracked
        Assert.Equal(3, result.Dispatched.Count);
        Assert.Equal(3, result.Consumed.Count);
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_ReturnExactDelta_When_RapidSequentialPublishes()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<CompletionEventHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        // step 1
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new CompletionEvent { Tag = "A" }, CancellationToken.None);
        }

        var step1 = await tracker.WaitForCompletionAsync(Timeout);

        // step 2
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new CompletionEvent { Tag = "B" }, CancellationToken.None);
        }

        var step2 = await tracker.WaitForCompletionAsync(Timeout);

        // step 3
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new CompletionEvent { Tag = "C" }, CancellationToken.None);
        }

        var step3 = await tracker.WaitForCompletionAsync(Timeout);

        // assert — each delta should have exactly 1 message
        Assert.Single(step1.Dispatched);
        Assert.Single(step1.Consumed);
        var tag1 = step1.ShouldHaveConsumed<CompletionEvent>();
        Assert.Equal("A", tag1.Tag);

        Assert.Single(step2.Dispatched);
        Assert.Single(step2.Consumed);
        var tag2 = step2.ShouldHaveConsumed<CompletionEvent>();
        Assert.Equal("B", tag2.Tag);

        Assert.Single(step3.Dispatched);
        Assert.Single(step3.Consumed);
        var tag3 = step3.ShouldHaveConsumed<CompletionEvent>();
        Assert.Equal("C", tag3.Tag);
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_TrackCascadingPublishes_When_HandlerPublishesMultiple()
    {
        // arrange — handler consumes CompletionEvent, publishes two secondary events
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<MultiPublishHandler>();
            b.AddEventHandler<CompletionSecondaryHandler>();
            b.AddEventHandler<CompletionTertiaryHandler>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new CompletionEvent { Tag = "MULTI" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — original + 2 cascading = 3 dispatched, 3 consumed
        Assert.Equal(3, result.Dispatched.Count);
        Assert.Equal(3, result.Consumed.Count);
        result.ShouldHaveConsumed<CompletionEvent>();
        result.ShouldHaveConsumed<CompletionSecondary>();
        result.ShouldHaveConsumed<CompletionTertiary>();
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_TrackDeepCascade_When_FourLevelsDeep()
    {
        // arrange — A→B→C→D chain
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<CascadeLevel1Handler>();
            b.AddEventHandler<CascadeLevel2Handler>();
            b.AddEventHandler<CascadeLevel3Handler>();
            b.AddEventHandler<CascadeLevel4Handler>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new CascadeLevel1 { Depth = 1 }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — all 4 levels were dispatched and consumed
        Assert.Equal(4, result.Dispatched.Count);
        Assert.Equal(4, result.Consumed.Count);
        result.ShouldHaveConsumed<CascadeLevel1>();
        result.ShouldHaveConsumed<CascadeLevel2>();
        result.ShouldHaveConsumed<CascadeLevel3>();
        result.ShouldHaveConsumed<CascadeLevel4>();
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_WaitForSlowHandler_When_HandlerTakesTime()
    {
        // arrange — handler takes 500ms
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<SlowCompletionHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new CompletionEvent { Tag = "SLOW" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — completion waited for the slow handler
        Assert.True(result.Completed);
        Assert.Single(result.Consumed);
        var consumed = result.ShouldHaveConsumed<CompletionEvent>();
        Assert.Equal("SLOW", consumed.Tag);
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

public sealed class CompletionEvent
{
    public required string Tag { get; init; }
}

public sealed class CompletionSecondary
{
    public required string Source { get; init; }
}

public sealed class CompletionTertiary
{
    public required string Source { get; init; }
}

public sealed class CompletionEventHandler : IEventHandler<CompletionEvent>
{
    public ValueTask HandleAsync(CompletionEvent _, CancellationToken __) => default;
}

public sealed class MultiPublishHandler(IMessageBus bus) : IEventHandler<CompletionEvent>
{
    public async ValueTask HandleAsync(CompletionEvent message, CancellationToken cancellationToken)
    {
        await bus.PublishAsync(
            new CompletionSecondary { Source = message.Tag },
            cancellationToken);
        await bus.PublishAsync(
            new CompletionTertiary { Source = message.Tag },
            cancellationToken);
    }
}

public sealed class CompletionSecondaryHandler : IEventHandler<CompletionSecondary>
{
    public ValueTask HandleAsync(CompletionSecondary _, CancellationToken __) => default;
}

public sealed class CompletionTertiaryHandler : IEventHandler<CompletionTertiary>
{
    public ValueTask HandleAsync(CompletionTertiary _, CancellationToken __) => default;
}

// Deep cascade types: Level1→Level2→Level3→Level4
public sealed class CascadeLevel1
{
    public required int Depth { get; init; }
}

public sealed class CascadeLevel2
{
    public required int Depth { get; init; }
}

public sealed class CascadeLevel3
{
    public required int Depth { get; init; }
}

public sealed class CascadeLevel4
{
    public required int Depth { get; init; }
}

public sealed class CascadeLevel1Handler(IMessageBus bus) : IEventHandler<CascadeLevel1>
{
    public async ValueTask HandleAsync(CascadeLevel1 message, CancellationToken cancellationToken)
    {
        await bus.PublishAsync(new CascadeLevel2 { Depth = 2 }, cancellationToken);
    }
}

public sealed class CascadeLevel2Handler(IMessageBus bus) : IEventHandler<CascadeLevel2>
{
    public async ValueTask HandleAsync(CascadeLevel2 message, CancellationToken cancellationToken)
    {
        await bus.PublishAsync(new CascadeLevel3 { Depth = 3 }, cancellationToken);
    }
}

public sealed class CascadeLevel3Handler(IMessageBus bus) : IEventHandler<CascadeLevel3>
{
    public async ValueTask HandleAsync(CascadeLevel3 message, CancellationToken cancellationToken)
    {
        await bus.PublishAsync(new CascadeLevel4 { Depth = 4 }, cancellationToken);
    }
}

public sealed class CascadeLevel4Handler : IEventHandler<CascadeLevel4>
{
    public ValueTask HandleAsync(CascadeLevel4 _, CancellationToken __) => default;
}

public sealed class SlowCompletionHandler : IEventHandler<CompletionEvent>
{
    public async ValueTask HandleAsync(CompletionEvent message, CancellationToken cancellationToken)
    {
        await Task.Delay(500, cancellationToken);
    }
}
