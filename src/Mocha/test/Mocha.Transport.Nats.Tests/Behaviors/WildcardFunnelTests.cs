using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Wildcard.Commands;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class WildcardFunnelTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task Subject_Should_FunnelAWholeFamily_When_GivenAWildcardFilter()
    {
        // arrange
        // Naming each concrete subject means editing the endpoint whenever a command is added. One
        // wildcard covering the family's namespace does the same job and stays correct as it grows,
        // and it is still a single durable, so the family stays ordered relative to itself.
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<WildcardCommandHandler>()
            .AddNats(nats =>
            {
                nats.StreamName("wildcard-service");

                // No stream declaration: a subject the endpoint filters has to be captured, so the
                // convention stream claims the wildcard on its own.
                nats.Endpoint("wildcard-commands")
                    .Handler<WildcardCommandHandler>()
                    .Subject("mocha.wildcard.commands.>")
                    .MaxConcurrency(1);
            });

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            var bus = host.Services.GetRequiredService<IMessageBus>();

            // act
            await bus.PublishAsync(new DeleteThing("D-1"), cancellationToken);
            await bus.PublishAsync(new CreateThing("C-1"), cancellationToken);
            await bus.PublishAsync(new RenameThing("R-1"), cancellationToken);

            // assert
            Assert.True(
                await recorder.WaitAsync(TimeSpan.FromSeconds(30), expectedCount: 3),
                $"Only {recorder.Messages.Count} of 3 commands reached the handler.");

            // The interface subject the transport derives is covered by the wildcard, so it has to be
            // collapsed away: JetStream rejects a consumer whose filter subjects overlap.
            var consumer = Assert.Single(
                ((NatsMessagingTopology)host.Services
                    .GetRequiredService<IMessagingRuntime>()
                    .Transports.OfType<NatsMessagingTransport>()
                    .Single()
                    .Topology).Consumers,
                c => c.Name == "wildcard-commands");

            Assert.Equal(["mocha.wildcard.commands.>"], consumer.FilterSubjects);

            // And the stream captures it, which is what makes the publish land at all.
            Assert.Contains(
                "mocha.wildcard.commands.>",
                ((NatsMessagingTopology)host.Services
                    .GetRequiredService<IMessagingRuntime>()
                    .Transports.OfType<NatsMessagingTransport>()
                    .Single()
                    .Topology).Streams.Single().Subjects);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    public sealed class WildcardCommandHandler(MessageRecorder recorder)
        : IEventHandler<IWildcardCommand>
    {
        public ValueTask HandleAsync(IWildcardCommand message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
