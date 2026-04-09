using Microsoft.Extensions.DependencyInjection;
using Mocha.Testing;
using Mocha.Transport.InMemory;

namespace Mocha.Testing.Tests;

public class DiagnosticOutputTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ToDiagnosticString_Should_ContainTimelineEntries_When_MessagesTracked()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<DiagnosticEventHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new DiagnosticEvent { Value = "DIAG-1" }, CancellationToken.None);
        await tracker.WaitForCompletionAsync(Timeout);

        // assert — diagnostic string should contain timeline-formatted entries
        var diagnostic = tracker.ToDiagnosticString();
        Assert.Contains("Dispatched", diagnostic);
        Assert.Contains("DiagnosticEvent", diagnostic);
        Assert.Contains("ConsumeCompleted", diagnostic);
        Assert.Contains("ms", diagnostic);
    }

    [Fact]
    public async Task WaitForCompletionAsync_Should_IncludePendingInfo_When_Timeout()
    {
        // arrange — handler that delays longer than the timeout
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<DiagnosticSlowHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new DiagnosticEvent { Value = "PENDING" }, CancellationToken.None);

        // assert — timeout exception should contain pending info
        var ex = await Assert.ThrowsAsync<MessageTrackingException>(
            () => tracker.WaitForCompletionAsync(TimeSpan.FromMilliseconds(100)));

        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(ex.DiagnosticOutput);

        // Should contain timeline and pending envelope info
        Assert.Contains("Timeline", ex.DiagnosticOutput);
        Assert.Contains("Pending", ex.DiagnosticOutput);
    }

    [Fact]
    public async Task MessageTrackingException_Should_ContainFullContext_When_AssertionFails()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<DiagnosticEventHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new DiagnosticEvent { Value = "CTX" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — requesting a type that doesn't exist should throw with full context
        var ex = Assert.Throws<MessageTrackingException>(
            () => result.ShouldHavePublished<DiagnosticMissingEvent>());

        // Exception message contains the type name that was expected
        Assert.Contains("DiagnosticMissingEvent", ex.Message);

        // Diagnostic output contains lists of what was actually tracked
        Assert.Contains("Dispatched", ex.DiagnosticOutput);
        Assert.Contains("Consumed", ex.DiagnosticOutput);
        Assert.Contains("Failed", ex.DiagnosticOutput);

        // Should show the actual messages that were dispatched
        Assert.Contains("DiagnosticEvent", ex.DiagnosticOutput);
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

public sealed class DiagnosticEvent
{
    public required string Value { get; init; }
}

public sealed class DiagnosticMissingEvent
{
    public required string Value { get; init; }
}

public sealed class DiagnosticEventHandler : IEventHandler<DiagnosticEvent>
{
    public ValueTask HandleAsync(DiagnosticEvent _, CancellationToken __) => default;
}

public sealed class DiagnosticSlowHandler : IEventHandler<DiagnosticEvent>
{
    public async ValueTask HandleAsync(DiagnosticEvent message, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
    }
}
