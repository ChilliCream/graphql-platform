using Microsoft.Extensions.DependencyInjection;
using Mocha.Testing;
using Mocha.Transport.InMemory;

namespace Mocha.Testing.Tests;

public class AssertionEdgeCaseTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ShouldHavePublished_Should_ReturnFirstMatch_When_MultipleMessagesOfSameType()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<AssertionEventHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new AssertionEvent { Label = "FIRST" }, CancellationToken.None);
        await bus.PublishAsync(new AssertionEvent { Label = "SECOND" }, CancellationToken.None);
        await bus.PublishAsync(new AssertionEvent { Label = "THIRD" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — ShouldHavePublished without predicate returns the first match
        var first = result.ShouldHavePublished<AssertionEvent>();
        Assert.Equal("FIRST", first.Label);
    }

    [Fact]
    public async Task ShouldHavePublished_Should_ThrowWithDiagnostic_When_PredicateMatchesNone()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<AssertionEventHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new AssertionEvent { Label = "AAA" }, CancellationToken.None);
        await bus.PublishAsync(new AssertionEvent { Label = "BBB" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — predicate for "MISSING" should fail with diagnostic info
        var ex = Assert.Throws<MessageTrackingException>(
            () => result.ShouldHavePublished<AssertionEvent>(m => m.Label == "MISSING"));

        Assert.Contains("AssertionEvent", ex.Message);
        Assert.Contains("Dispatched", ex.DiagnosticOutput);
    }

    [Fact]
    public async Task ShouldHaveConsumed_Should_NotFind_When_HandlerThrows()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<AssertionThrowingHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new AssertionEvent { Label = "FAIL" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — message should be in Failed, not in Consumed
        Assert.NotEmpty(result.Failed);
        Assert.Empty(result.Consumed);

        var failed = result.Failed[0];
        Assert.IsType<InvalidOperationException>(failed.Exception);
    }

    [Fact]
    public async Task ShouldHaveNoMessages_Should_Throw_When_MessagesExist()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<AssertionEventHandler>();
            b.AddEventHandler<AssertionSecondaryHandler>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act — publish 3 messages
        await bus.PublishAsync(new AssertionEvent { Label = "ONE" }, CancellationToken.None);
        await bus.PublishAsync(new AssertionEvent { Label = "TWO" }, CancellationToken.None);
        await bus.PublishAsync(new AssertionSecondaryEvent { Label = "THREE" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — result has messages, so ShouldHaveNoMessages should throw
        Assert.Throws<MessageTrackingException>(() => result.ShouldHaveNoMessages());
    }

    [Fact]
    public async Task ShouldHavePublished_Should_SeeOnlyDelta_When_CalledOnResult()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<AssertionEventHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        // step 1 — publish first message
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new AssertionEvent { Label = "STEP1" }, CancellationToken.None);
        }

        var step1 = await tracker.WaitForCompletionAsync(Timeout);

        // step 2 — publish second message
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new AssertionEvent { Label = "STEP2" }, CancellationToken.None);
        }

        var step2 = await tracker.WaitForCompletionAsync(Timeout);

        // assert — step2 (delta) should only see STEP2, not STEP1
        var deltaMsg = step2.ShouldHavePublished<AssertionEvent>();
        Assert.Equal("STEP2", deltaMsg.Label);

        // cumulative tracker should see both
        Assert.Equal(2, tracker.Dispatched.Count);

        // tracker (cumulative) ShouldHavePublished returns STEP1 (first match)
        var cumulativeMsg = tracker.ShouldHavePublished<AssertionEvent>();
        Assert.Equal("STEP1", cumulativeMsg.Label);
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

public sealed class AssertionEvent
{
    public required string Label { get; init; }
}

public sealed class AssertionSecondaryEvent
{
    public required string Label { get; init; }
}

public sealed class AssertionEventHandler : IEventHandler<AssertionEvent>
{
    public ValueTask HandleAsync(AssertionEvent _, CancellationToken __) => default;
}

public sealed class AssertionSecondaryHandler : IEventHandler<AssertionSecondaryEvent>
{
    public ValueTask HandleAsync(AssertionSecondaryEvent _, CancellationToken __) => default;
}

public sealed class AssertionThrowingHandler : IEventHandler<AssertionEvent>
{
    public ValueTask HandleAsync(AssertionEvent message, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Deliberate assertion test failure");
}
