using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using NATS.Client.JetStream.Models;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public sealed record PreProvisioned(Guid Id);

[Collection(JetStreamCollection.Name)]
public class ProvisioningOverrideTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task DeclareStream_Should_RemoveTheStartUpOrderDependency_When_TheStreamIsDeclared()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<PreProvisionedHandler>()
            .AddNats(nats => nats
                .StreamName("e2e-declared")
                // Narrow subjects on purpose: JetStream rejects a stream whose subjects overlap an
                // existing one, so a wildcard here would collide with every other test's stream.
                .DeclareStream("E2E_DECLARED")
                    .Subject("mocha.transport.nats.tests.pre-provisioned")
                    .Subject("mocha.transport.nats.tests.pre-provisioned_error")
                    .Subject("mocha.transport.nats.tests.pre-provisioned_skipped")
                    .Storage(StreamConfigStorage.Memory));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            var topology = (NatsMessagingTopology)host.Services
                .GetRequiredService<IMessagingRuntime>()
                .Transports.OfType<NatsMessagingTransport>()
                .Single()
                .Topology;

            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(new PreProvisioned(Guid.NewGuid()), cancellationToken);

            // assert
            // The declared subjects cover everything routing produces, so no convention stream is
            // needed alongside it.
            var stream = Assert.Single(topology.Streams);

            Assert.Equal("E2E_DECLARED", stream.Name);
            Assert.All(topology.Consumers, c => Assert.Equal("E2E_DECLARED", c.StreamName));

            Assert.True(
                await recorder.WaitAsync(TimeSpan.FromSeconds(30)),
                "The handler did not receive the event.");
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task AutoProvision_Should_TouchNothing_When_Disabled()
    {
        // arrange
        // Stands in for infrastructure managed by ops rather than by the application.
        var cancellationToken = TestContext.Current.CancellationToken;

        await fixture.JetStream.CreateOrUpdateStreamAsync(
            new StreamConfig
            {
                Name = "E2E_EXTERNAL",
                Subjects = ["external-service.>"],
                Storage = StreamConfigStorage.Memory
            },
            cancellationToken);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services
            .AddMessageBus()
            .AddNats(nats => nats.StreamName("e2e-external").AutoProvision(false));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            var transport = host.Services
                .GetRequiredService<IMessagingRuntime>()
                .Transports.OfType<NatsMessagingTransport>()
                .Single();

            var external = await fixture.JetStream.GetStreamAsync(
                "E2E_EXTERNAL",
                cancellationToken: cancellationToken);

            // assert
            // With no handlers there is nothing to publish, and auto-provisioning is off, so the
            // transport must not have touched the server's topology.
            Assert.Empty(((NatsMessagingTopology)transport.Topology).Streams);
            Assert.Equal(["external-service.>"], external.Info.Config.Subjects);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    public sealed class PreProvisionedHandler(MessageRecorder recorder) : IEventHandler<PreProvisioned>
    {
        public ValueTask HandleAsync(PreProvisioned message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
