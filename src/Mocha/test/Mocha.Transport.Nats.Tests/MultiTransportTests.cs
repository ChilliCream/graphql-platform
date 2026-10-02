using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.InMemory;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public sealed record RoutedEvent(Guid Id);

public sealed record ClaimedEvent(Guid Id);

[Collection(JetStreamCollection.Name)]
public class MultiTransportTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task PublishAsync_Should_RouteThroughNats_When_RegisteredAlongsideInMemory()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();
        var published = new RoutedEvent(Guid.NewGuid());

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<RoutedEventHandler>()
            .AddNats(nats => nats.StreamName("e2e-routing").IsDefaultTransport())
            .AddInMemory();

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            var nats = host.Services.GetRequiredService<IMessagingRuntime>()
                .Transports.OfType<NatsMessagingTransport>()
                .Single();

            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(published, cancellationToken);

            Assert.True(
                await recorder.WaitAsync(TimeSpan.FromSeconds(30)),
                "The handler did not receive the event.");

            // assert
            // The default transport should own the route, so the message must have gone through
            // JetStream rather than quietly falling back to the in-process transport.
            var stream = Assert.Single(((NatsMessagingTopology)nats.Topology).Streams);

            var info = await fixture.JetStream.GetStreamAsync(
                stream.Name,
                cancellationToken: cancellationToken);

            Assert.Equal(published, Assert.Single(recorder.Messages));
            Assert.True(info.Info.State.Messages > 0, "The event never reached the NATS stream.");
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task IsDefaultTransport_Should_NotClaimHandlers_When_AnotherTransportIsRegisteredFirst()
    {
        // arrange
        // IsDefaultTransport only picks the fallback for an unrouted address. Convention-bound
        // handlers are claimed by whichever transport discovers them first, which is the one
        // registered first, so registering NATS last leaves it with nothing to publish and no stream.
        var cancellationToken = TestContext.Current.CancellationToken;

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<ClaimedEventHandler>()
            .AddInMemory()
            .AddNats(nats => nats.StreamName("e2e-claim-order").IsDefaultTransport());

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            var nats = host.Services
                .GetRequiredService<IMessagingRuntime>()
                .Transports.OfType<NatsMessagingTransport>()
                .Single();

            // assert
            Assert.True(nats.IsDefaultTransport);
            Assert.Empty(((NatsMessagingTopology)nats.Topology).Streams);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    public sealed class RoutedEventHandler(MessageRecorder recorder) : IEventHandler<RoutedEvent>
    {
        public ValueTask HandleAsync(RoutedEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ClaimedEventHandler : IEventHandler<ClaimedEvent>
    {
        public ValueTask HandleAsync(ClaimedEvent message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
