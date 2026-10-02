using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public sealed record ReminderDue(Guid Id);

[Collection(JetStreamCollection.Name)]
public class SchedulingIntegrationTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_delay = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task SchedulePublishAsync_Should_HoldTheMessage_When_ItIsNotYetDue()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var clock = new DeliveryClock();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(clock);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<ReminderDueHandler>()
            .AddNats(nats => nats.StreamName("e2e-reminders").EnableScheduling());

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            var transport = host.Services
                .GetRequiredService<IMessagingRuntime>()
                .Transports.OfType<NatsMessagingTransport>()
                .Single();

            Assert.SkipUnless(
                transport.Capabilities.SupportsMessageSchedules,
                "Message schedules need NATS server 2.12 or later; this server reports "
                + $"{transport.Capabilities.Version?.ToString() ?? "an unknown version"}.");

            var stream = Assert.Single(((NatsMessagingTopology)transport.Topology).Streams);

            // The server refuses a schedule whose target is the subject it arrived on, so enabling
            // scheduling has to add a scheduling namespace alongside each real subject. It is captured
            // as a filter rather than one subject, because each scheduled message gets its own subject
            // within it.
            Assert.Contains(
                stream.Subjects,
                s => s.EndsWith($".{NatsScheduling.SchedulingSuffix}.>", StringComparison.Ordinal));

            clock.Start();

            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .SchedulePublishAsync(
                    new ReminderDue(Guid.NewGuid()),
                    DateTimeOffset.UtcNow.Add(s_delay),
                    cancellationToken);

            var elapsed = await clock.WaitAsync(s_delay + TimeSpan.FromSeconds(45));

            // assert
            Assert.True(
                elapsed >= s_delay - TimeSpan.FromSeconds(1),
                $"The scheduled message arrived after {elapsed}, before its {s_delay} delay.");
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Records how long after the clock started a message was delivered.
    /// </summary>
    public sealed class DeliveryClock
    {
        private readonly Stopwatch _stopwatch = new();
        private readonly TaskCompletionSource<TimeSpan> _delivered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Start() => _stopwatch.Restart();

        public void Record() => _delivered.TrySetResult(_stopwatch.Elapsed);

        public Task<TimeSpan> WaitAsync(TimeSpan timeout) => _delivered.Task.WaitAsync(timeout);
    }

    public sealed class ReminderDueHandler(DeliveryClock clock) : IEventHandler<ReminderDue>
    {
        public ValueTask HandleAsync(ReminderDue message, CancellationToken cancellationToken)
        {
            clock.Record();
            return ValueTask.CompletedTask;
        }
    }
}
