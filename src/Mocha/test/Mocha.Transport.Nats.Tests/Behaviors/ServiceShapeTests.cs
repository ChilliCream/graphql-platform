using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Shape.Contracts;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class ServiceShapeTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task DeclareStream_Should_AbsorbConventionSubjects_When_ItSharesTheDerivedName()
    {
        // arrange
        // A declared stream under the name the service name derives, so the convention stream for the
        // remaining handlers collides with it and its subjects have to be folded in rather than lost.
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<ShapeCommandHandler>()
            .AddEventHandler<ShapeBookingHandler>()
            .AddEventHandler<ShapePoolHandler>()
            .AddEventHandler<ShapeTakenHandler>()
            .AddConcurrencyLimiter(options => options.MaxConcurrency = 1)
            .AddNats(nats =>
            {
                nats.StreamName("shape-service").AutoProvision(true);

                nats.Endpoint("shape-commands")
                    .Handler<ShapeCommandHandler>()
                    .Subject("mocha.shape.contracts.remove-thing")
                    .MaxConcurrency(1);

                nats.DeclareStream("SHAPE_SERVICE")
                    .Subject("mocha.shape.contracts.>")
                    .Subject("mocha.transport.nats.tests.shape-commands_error")
                    .Subject("mocha.transport.nats.tests.shape-commands_skipped")
                    .AutoProvision(true);
            });

        using var host = builder.Build();

        // act
        await host.StartAsync(cancellationToken);

        try
        {
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(new RemoveThing("T-1"), cancellationToken);

            // assert
            Assert.True(
                await recorder.WaitAsync(TimeSpan.FromSeconds(30)),
                "The funnelled command never reached its handler.");

            var topology = (NatsMessagingTopology)host.Services
                .GetRequiredService<IMessagingRuntime>()
                .Transports.OfType<NatsMessagingTransport>()
                .Single()
                .Topology;

            // One stream, every consumer bound to it, and the declared subjects kept alongside the
            // fault subjects the other handlers contribute.
            Snapshot.Create()
                .Add(Describe(topology))
                .MatchMarkdown();
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    private static string Describe(NatsMessagingTopology topology)
    {
        var streams = topology.Streams
            .OrderBy(s => s.Name, StringComparer.Ordinal)
            .Select(s => $"stream {s.Name} ({s.Origin}) captures "
                + $"[{string.Join(", ", s.Subjects.Order(StringComparer.Ordinal))}]");

        var consumers = topology.Consumers
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .Select(c => $"consumer {c.Name} reads {c.StreamName}");

        return string.Join("\n", streams.Concat(consumers));
    }

    public sealed class ShapeCommandHandler(MessageRecorder recorder) : IEventHandler<IShapeCommand>
    {
        public ValueTask HandleAsync(IShapeCommand message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ShapeBookingHandler : IEventHandler<ThingBooked>
    {
        public ValueTask HandleAsync(ThingBooked message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    public sealed class ShapePoolHandler : IEventHandler<ThingPooled>
    {
        public ValueTask HandleAsync(ThingPooled message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    public sealed class ShapeTakenHandler : IEventHandler<ThingTaken>
    {
        public ValueTask HandleAsync(ThingTaken message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
