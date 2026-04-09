using Microsoft.Extensions.DependencyInjection;
using Mocha.Testing;
using Mocha.Transport.InMemory;

namespace Mocha.Testing.Tests;

public partial class SnapshotTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task MatchMarkdownSnapshot_Should_ProduceDeterministicOutput_When_SameFlowRunTwice()
    {
        // Run the same flow and check the snapshot structure is deterministic
        // (MessageIds are GUIDs so we normalize them before comparison)
        var snapshot1 = NormalizeMessageIds(await RunSnapshotFlow());
        var snapshot2 = NormalizeMessageIds(await RunSnapshotFlow());

        Assert.Equal(snapshot1, snapshot2);
    }

    [Fact]
    public async Task MatchMarkdownSnapshot_Should_SortByTypeName_When_MultipleMessageTypes()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
        {
            b.AddEventHandler<SnapshotZebraHandler>();
            b.AddEventHandler<SnapshotAlphaHandler>();
        });

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act — publish in reverse alphabetical order
        await bus.PublishAsync(new SnapshotZebraEvent { Name = "Z" }, CancellationToken.None);
        await bus.PublishAsync(new SnapshotAlphaEvent { Name = "A" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(Timeout);

        // assert — format the result and verify alphabetical ordering
        var formatted = FormatTrackedMessages(result);
        var alphaIndex = formatted.IndexOf("SnapshotAlphaEvent", StringComparison.Ordinal);
        var zebraIndex = formatted.IndexOf("SnapshotZebraEvent", StringComparison.Ordinal);
        Assert.True(alphaIndex < zebraIndex,
            $"Expected SnapshotAlphaEvent (at {alphaIndex}) before SnapshotZebraEvent (at {zebraIndex}) in sorted output");
    }

    [Fact]
    public void MatchMarkdownSnapshot_Should_HandleEmpty_When_NoMessages()
    {
        // arrange — result with no messages
        var result = new MessageTrackingResult([], [], [], completed: true, TimeSpan.Zero);

        // act — format the empty result
        var formatted = FormatTrackedMessages(result);

        // assert — should produce valid output with "(none)" sections
        Assert.Contains("Dispatched", formatted);
        Assert.Contains("(none)", formatted);
    }

    [Fact]
    public async Task MatchMarkdownSnapshot_Should_ShowDeltaOnly_When_UsedOnResult()
    {
        // arrange
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<SnapshotAlphaHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        // step 1
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new SnapshotAlphaEvent { Name = "STEP1" }, CancellationToken.None);
        }

        var step1 = await tracker.WaitForCompletionAsync(Timeout);

        // step 2
        using (var scope = provider.CreateScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new SnapshotAlphaEvent { Name = "STEP2" }, CancellationToken.None);
        }

        var step2 = await tracker.WaitForCompletionAsync(Timeout);

        // assert — result (delta) should only show step2's messages
        var deltaFormatted = FormatTrackedMessages(step2);
        var cumulativeFormatted = FormatTrackedMessages(tracker);

        // Delta should have 1 dispatched entry
        var deltaDispatchedCount = CountOccurrences(deltaFormatted, "SnapshotAlphaEvent");
        // Cumulative should have more
        var cumulativeDispatchedCount = CountOccurrences(cumulativeFormatted, "SnapshotAlphaEvent");

        Assert.True(cumulativeDispatchedCount > deltaDispatchedCount,
            $"Cumulative ({cumulativeDispatchedCount}) should have more SnapshotAlphaEvent entries than delta ({deltaDispatchedCount})");
    }

    // --- Helpers ---

    private static async Task<string> RunSnapshotFlow()
    {
        await using var provider = await CreateBusAsync(
            b => b.AddEventHandler<SnapshotAlphaHandler>());

        var tracker = provider.GetRequiredService<IMessageTracker>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await bus.PublishAsync(new SnapshotAlphaEvent { Name = "DETERMINISTIC" }, CancellationToken.None);
        var result = await tracker.WaitForCompletionAsync(TimeSpan.FromSeconds(10));

        return FormatTrackedMessages(result);
    }

    private static string FormatTrackedMessages(ITrackedMessages messages)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        TrackedMessagesSnapshotValueFormatter.Instance.Format(buffer, messages);
        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    [System.Text.RegularExpressions.GeneratedRegex(
        "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial System.Text.RegularExpressions.Regex GuidPattern();

    private static string NormalizeMessageIds(string text)
    {
        // Replace GUIDs with a stable placeholder so output is comparable across runs
        return GuidPattern().Replace(text, "<id>");
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
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

public sealed class SnapshotAlphaEvent
{
    public required string Name { get; init; }
}

public sealed class SnapshotZebraEvent
{
    public required string Name { get; init; }
}

public sealed class SnapshotAlphaHandler : IEventHandler<SnapshotAlphaEvent>
{
    public ValueTask HandleAsync(SnapshotAlphaEvent _, CancellationToken __) => default;
}

public sealed class SnapshotZebraHandler : IEventHandler<SnapshotZebraEvent>
{
    public ValueTask HandleAsync(SnapshotZebraEvent _, CancellationToken __) => default;
}
