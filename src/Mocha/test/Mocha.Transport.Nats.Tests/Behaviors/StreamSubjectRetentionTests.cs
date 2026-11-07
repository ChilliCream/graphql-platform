using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats.Tests.Behaviors;

public sealed record CrateSealed(string CrateId);

[Collection(JetStreamCollection.Name)]
public class StreamSubjectRetentionTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task ProvisionAsync_Should_KeepSubjectsItDoesNotKnowAbout_When_UpdatingAConventionStream()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();

        await fixture.JetStream.CreateOrUpdateStreamAsync(
            new StreamConfig
            {
                Name = "RETENTION_SERVICE",
                Subjects = ["a-peer.publishes.this"],
                Storage = StreamConfigStorage.Memory
            },
            cancellationToken);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<CrateSealedHandler>()
            .AddNats(nats => nats.StreamName("retention-service"));

        using var host = builder.Build();

        // act
        await host.StartAsync(cancellationToken);

        try
        {
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(new CrateSealed("C-1"), cancellationToken);

            Assert.True(await recorder.WaitAsync(s_timeout), "The handler did not receive the event.");

            // assert
            var stream = await fixture.JetStream.GetStreamAsync(
                "RETENTION_SERVICE",
                cancellationToken: cancellationToken);

            Assert.Equal(
                [
                    "a-peer.publishes.this",
                    "mocha.transport.nats.tests.behaviors.crate-sealed",
                    "mocha.transport.nats.tests.crate-sealed_error",
                    "mocha.transport.nats.tests.crate-sealed_skipped"
                ],
                (stream.Info.Config.Subjects ?? []).Order(StringComparer.Ordinal));

            // The server rejects an update that changes the owner's storage.
            Assert.Equal(StreamConfigStorage.Memory, stream.Info.Config.Storage);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    public sealed class CrateSealedHandler(MessageRecorder recorder) : IEventHandler<CrateSealed>
    {
        public ValueTask HandleAsync(CrateSealed message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
