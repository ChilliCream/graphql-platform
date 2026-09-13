using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

public sealed record LabelPrinted(string LabelId);

[Collection(JetStreamCollection.Name)]
public class DeclaredConsumerTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task DeclareConsumer_Should_KeepEndpointSubjects_When_ItNamesAnEndpointsDurable()
    {
        // arrange
        // Naming a durable an endpoint already derives must not discard the subjects that endpoint
        // contributes, which would leave the consumer with none and fail start-up.
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<LabelPrintedHandler>()
            .AddNats(nats =>
            {
                // A named endpoint so the durable name is predictable rather than derived from the
                // entry assembly name.
                nats.StreamName("declared-consumer")
                    .Endpoint("label-printing")
                    .Handler<LabelPrintedHandler>();

                nats.DeclareConsumer("label-printing")
                    .AckWait(TimeSpan.FromSeconds(20))
                    .AckProgressEvery(TimeSpan.FromSeconds(5));
            });

        using var host = builder.Build();

        // act
        await host.StartAsync(cancellationToken);

        try
        {
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(new LabelPrinted("L-1"), cancellationToken);

            // assert
            Assert.True(await recorder.WaitAsync(s_timeout), "The handler did not receive the event.");

            var consumer = Assert.Single(
                Topology(host).Consumers,
                c => c.Name == "label-printing");

            // The explicit acknowledgement settings survive, and the endpoint's subject is present.
            Assert.Equal(TimeSpan.FromSeconds(5), consumer.AckProgressInterval);
            Assert.Equal(
                ["mocha.transport.nats.tests.behaviors.label-printed"],
                consumer.FilterSubjects);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    private static NatsMessagingTopology Topology(IHost host)
        => (NatsMessagingTopology)host.Services
            .GetRequiredService<IMessagingRuntime>()
            .Transports.OfType<NatsMessagingTransport>()
            .Single()
            .Topology;

    public sealed class LabelPrintedHandler(MessageRecorder recorder) : IEventHandler<LabelPrinted>
    {
        public ValueTask HandleAsync(LabelPrinted message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
