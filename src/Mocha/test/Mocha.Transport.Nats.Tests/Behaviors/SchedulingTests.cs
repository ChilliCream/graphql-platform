using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class SchedulingTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_delay = TimeSpan.FromSeconds(4);

    [Fact]
    public async Task SchedulePublishAsync_Should_DeliverBoth_When_TwoMessagesTargetOneSubject()
    {
        // arrange
        // JetStream holds at most one schedule per subject, so every scheduled message needs its own
        // scheduling subject. Sharing one, as a subject derived only from the target does, makes the
        // second schedule replace the first and the first message is never delivered.
        var recorder = new MessageRecorder();
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<ScheduledReminderHandler>()
            .AddNats(nats => nats.StreamName(scope.StreamName).EnableScheduling())
            .BuildTestBusAsync();

        SkipUnlessSchedulesSupported(bus);

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;
        var due = DateTimeOffset.UtcNow.Add(s_delay);

        // act
        await messageBus.SchedulePublishAsync(new ScheduledReminder("first"), due, CancellationToken.None);
        await messageBus.SchedulePublishAsync(new ScheduledReminder("second"), due, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_delay + TimeSpan.FromSeconds(45), expectedCount: 2),
            $"Only {recorder.Messages.Count} of 2 scheduled messages were delivered.");

        var labels = recorder.Messages.Cast<ScheduledReminder>().Select(r => r.Label).OrderBy(l => l).ToList();

        Assert.Equal(["first", "second"], labels);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_StopDelivery_When_TheMessageIsNotYetDue()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<ScheduledReminderHandler>()
            .AddNats(nats => nats.StreamName(scope.StreamName).EnableScheduling())
            .BuildTestBusAsync();

        SkipUnlessSchedulesSupported(bus);

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;
        var due = DateTimeOffset.UtcNow.Add(s_delay);

        await messageBus.SchedulePublishAsync(new ScheduledReminder("kept"), due, CancellationToken.None);

        var doomed = await messageBus.SchedulePublishAsync(
            new ScheduledReminder("cancelled"),
            due,
            CancellationToken.None);

        // act
        var cancelled = await messageBus.CancelScheduledMessageAsync(
            TokenOf(doomed),
            CancellationToken.None);

        // assert
        Assert.True(cancelled, "Cancelling a message that is not yet due should report success.");

        Assert.True(
            await recorder.WaitAsync(s_delay + TimeSpan.FromSeconds(45)),
            "The message that was not cancelled should still have been delivered.");

        Assert.Equal(["kept"], recorder.Messages.Cast<ScheduledReminder>().Select(r => r.Label));
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_ReportNothingWithdrawn_When_TheTokenWasAlreadyCancelled()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<ScheduledReminderHandler>()
            .AddNats(nats => nats.StreamName(scope.StreamName).EnableScheduling())
            .BuildTestBusAsync();

        SkipUnlessSchedulesSupported(bus);

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        var scheduled = await messageBus.SchedulePublishAsync(
            new ScheduledReminder("cancelled twice"),
            DateTimeOffset.UtcNow.Add(s_delay),
            CancellationToken.None);

        var token = TokenOf(scheduled);

        await messageBus.CancelScheduledMessageAsync(token, CancellationToken.None);

        // act
        var cancelledAgain = await messageBus.CancelScheduledMessageAsync(token, CancellationToken.None);

        // assert
        Assert.False(cancelledAgain, "Cancelling an already cancelled message withdraws nothing.");
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_Should_ReportNothingWithdrawn_When_TheMessageWasReleased()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<ScheduledReminderHandler>()
            .AddNats(nats => nats.StreamName(scope.StreamName).EnableScheduling())
            .BuildTestBusAsync();

        SkipUnlessSchedulesSupported(bus);

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        var scheduled = await messageBus.SchedulePublishAsync(
            new ScheduledReminder("released"),
            DateTimeOffset.UtcNow.Add(s_delay),
            CancellationToken.None);

        Assert.True(
            await recorder.WaitAsync(s_delay + TimeSpan.FromSeconds(45)),
            "The scheduled message should have been delivered before cancellation is attempted.");

        // act
        var cancelled = await messageBus.CancelScheduledMessageAsync(
            TokenOf(scheduled),
            CancellationToken.None);

        // assert
        Assert.False(cancelled, "A message already released to its target has no schedule left to withdraw.");
    }

    private static string TokenOf(SchedulingResult result)
        => Assert.IsType<string>(result.Token);

    private static void SkipUnlessSchedulesSupported(TestBus bus)
    {
        var transport = bus.Runtime.Transports.OfType<NatsMessagingTransport>().Single();

        Assert.SkipUnless(
            transport.Capabilities.SupportsMessageSchedules,
            "Message schedules need NATS server 2.12 or later; this server reports "
            + $"{transport.Capabilities.Version?.ToString() ?? "an unknown version"}.");
    }

    public sealed record ScheduledReminder(string Label);

    public sealed class ScheduledReminderHandler(MessageRecorder recorder)
        : IEventHandler<ScheduledReminder>
    {
        public ValueTask HandleAsync(ScheduledReminder message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
