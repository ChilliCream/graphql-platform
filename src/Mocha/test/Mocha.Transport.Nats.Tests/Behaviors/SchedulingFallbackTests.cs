using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;

namespace Mocha.Transport.Nats.Tests.Behaviors;

/// <summary>
/// Verifies which store takes a scheduled send when a fallback scheduled message store is registered.
/// </summary>
[Collection(JetStreamCollection.Name)]
public sealed class SchedulingFallbackTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task SchedulePublishAsync_Should_UseTheFallbackStore_When_SchedulingIsDisabled()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var fallback = new RecordingScheduledMessageStore();
        await using var bus = await BuildBusAsync(scope, fallback, enableScheduling: false);
        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        var result = await messageBus.SchedulePublishAsync(
            new OrderCreated { OrderId = "SCHEDULED-1" },
            DateTimeOffset.UtcNow.AddDays(30),
            TestContext.Current.CancellationToken);

        // assert
        Assert.StartsWith(RecordingScheduledMessageStore.TokenPrefix, result.Token, StringComparison.Ordinal);
        Assert.Single(fallback.Persisted);
    }

    [Fact]
    public async Task SchedulePublishAsync_Should_UseJetStream_When_SchedulingIsEnabled()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var fallback = new RecordingScheduledMessageStore();
        await using var bus = await BuildBusAsync(scope, fallback, enableScheduling: true);
        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        var result = await messageBus.SchedulePublishAsync(
            new OrderCreated { OrderId = "SCHEDULED-2" },
            DateTimeOffset.UtcNow.AddDays(30),
            TestContext.Current.CancellationToken);

        // assert
        Assert.StartsWith(NatsScheduledMessageStore.TokenPrefix, result.Token, StringComparison.Ordinal);
        Assert.Empty(fallback.Persisted);
    }

    private async Task<TestBus> BuildBusAsync(
        JetStreamScope scope,
        RecordingScheduledMessageStore fallback,
        bool enableScheduling)
        => await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(new MessageRecorder())
            .AddRecordingSchedulingFallback(fallback)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(nats => nats.StreamName(scope.StreamName).EnableScheduling(enableScheduling))
            .BuildTestBusAsync();
}
