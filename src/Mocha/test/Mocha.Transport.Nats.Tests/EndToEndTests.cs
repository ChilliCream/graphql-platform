using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public sealed record OrderPlaced(Guid OrderId, string ProductName);

public sealed record StockChecked(Guid ItemId);

public sealed record TopologyProbe(Guid Id);

[Collection(JetStreamCollection.Name)]
public class EndToEndTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task PublishAsync_Should_ReachTheHandler_When_AnEventIsPublished()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();
        var published = new OrderPlaced(Guid.NewGuid(), "Mechanical Keyboard");

        using var host = BuildHost<OrderPlacedHandler>(
            "e2e-orders",
            services => services.AddSingleton(recorder));

        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(published, cancellationToken);

            // assert
            Assert.True(await recorder.WaitAsync(s_timeout), "The handler did not receive the event.");
            Assert.Equal(published, Assert.Single(recorder.Messages));
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task PublishAsync_Should_RedeliverUntilItSucceeds_When_TheHandlerFailsOnce()
    {
        // arrange
        // Redelivery has to be asked for. The default policy dead-letters a failing message rather
        // than returning it to the transport, so without the retry policy the handler is never called
        // again.
        var cancellationToken = TestContext.Current.CancellationToken;
        var counter = new InvocationCounter();
        var recorder = new MessageRecorder();

        using var host = BuildHost<StockCheckedHandler>(
            "e2e-stock",
            services => services.AddSingleton(counter).AddSingleton(recorder),
            bus => bus.AddResilience(policy => policy.Default().Retry(1).ThenRedeliver()));

        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(new StockChecked(Guid.NewGuid()), cancellationToken);

            // assert
            Assert.True(
                await recorder.WaitAsync(TimeSpan.FromSeconds(60)),
                "The handler never succeeded, so the message was not redelivered after failing.");

            Assert.True(
                counter.Count >= 2,
                $"Expected more than one delivery attempt, but there were {counter.Count}.");
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task StartAsync_Should_ProvisionAStreamAndConsumer_When_AHandlerIsRegistered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        using var host = BuildHost<TopologyProbeHandler>("e2e-topology");

        await host.StartAsync(cancellationToken);

        try
        {
            var topology = (NatsMessagingTopology)host.Services
                .GetRequiredService<IMessagingRuntime>()
                .Transports.OfType<NatsMessagingTransport>()
                .Single()
                .Topology;

            // act
            var stream = Assert.Single(topology.Streams);

            var provisioned = await fixture.JetStream.GetStreamAsync(
                stream.Name,
                cancellationToken: cancellationToken);

            // assert
            Assert.Equal(stream.Name, provisioned.Info.Config.Name);
            Assert.All(topology.Consumers, c => Assert.Equal(stream.Name, c.StreamName));

            // A reply inbox captured in a stream would persist every response, so core subjects must
            // stay out of it.
            Assert.Equal(
                [],
                topology.Subjects
                    .Where(s => s.IsCore && stream.Subjects.Contains(s.Subject))
                    .Select(s => s.Subject));
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    private IHost BuildHost<THandler>(
        string serviceName,
        Action<IServiceCollection>? configureServices = null,
        Action<IMessageBusHostBuilder>? configureBus = null)
        where THandler : class, IEventHandler
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton(fixture.Connection);
        configureServices?.Invoke(builder.Services);

        var bus = builder.Services.AddMessageBus().AddEventHandler<THandler>();

        configureBus?.Invoke(bus);

        bus.AddNats(nats => nats.StreamName(serviceName));

        return builder.Build();
    }

    public sealed class OrderPlacedHandler(MessageRecorder recorder) : IEventHandler<OrderPlaced>
    {
        public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class StockCheckedHandler(InvocationCounter counter, MessageRecorder recorder)
        : IEventHandler<StockChecked>
    {
        public ValueTask HandleAsync(StockChecked message, CancellationToken cancellationToken)
        {
            counter.Increment();

            if (counter.Count < 2)
            {
                throw new InvalidOperationException("Simulated transient failure.");
            }

            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class TopologyProbeHandler : IEventHandler<TopologyProbe>
    {
        public ValueTask HandleAsync(TopologyProbe message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
