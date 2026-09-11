using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Polymorphic.Contracts;
using Mocha.Transport.Nats.Tests.Fixtures;
using NATS.Client.JetStream.Models;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class PolymorphicFunnelTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Subject_Should_FunnelAWholeFamilyToOneDurable_When_TheConcreteSubjectsAreNamed()
    {
        // arrange
        // A publish resolves its subject from the concrete runtime type, so an interface-bound handler
        // has to name the concrete subjects; one durable then keeps the family ordered.
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new OrderedRecorder();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<LockerCommandHandler>()
            .AddNats(nats =>
            {
                nats.StreamName("polymorphic-funnel");

                nats.Endpoint("locker-commands")
                    .Handler<LockerCommandHandler>()
                    .Subject("mocha.polymorphic.contracts.delete-admin")
                    .Subject("mocha.polymorphic.contracts.create-admin")
                    // One durable orders delivery, but handling is parallel by default. Ordered
                    // processing across the family needs the concurrency pinned to one.
                    .MaxConcurrency(1);

                nats.DeclareStream("POLYMORPHIC_FUNNEL")
                    .Subject("mocha.polymorphic.contracts.>")
                    .Subject("mocha.transport.nats.tests.locker-commands_error")
                    .Subject("mocha.transport.nats.tests.locker-commands_skipped")
                    .Storage(StreamConfigStorage.Memory);
            });

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            var bus = host.Services.GetRequiredService<IMessageBus>();

            // act
            await bus.PublishAsync(new DeleteAdmin("L-1"), cancellationToken);
            await bus.PublishAsync(new CreateAdmin("L-2"), cancellationToken);

            // assert
            Assert.True(
                await recorder.WaitAsync(s_timeout, expectedCount: 2),
                $"Only {recorder.Received.Count} of 2 commands reached the handler.");

            // Both reach the one handler, typed as the interface, in publish order.
            Assert.Equal(
                ["DeleteAdmin:L-1", "CreateAdmin:L-2"],
                recorder.Received);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Records arrivals in order, which <see cref="MessageRecorder"/> cannot do because it collects
    /// into a bag.
    /// </summary>
    public sealed class OrderedRecorder
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        private readonly List<string> _received = [];
        private readonly object _sync = new();

        public IReadOnlyList<string> Received
        {
            get
            {
                lock (_sync)
                {
                    return [.. _received];
                }
            }
        }

        public void Record(ILockerCommand command)
        {
            lock (_sync)
            {
                _received.Add($"{command.GetType().Name}:{command.LockerId}");
            }

            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class LockerCommandHandler(OrderedRecorder recorder) : IEventHandler<ILockerCommand>
    {
        public ValueTask HandleAsync(ILockerCommand message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
