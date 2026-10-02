using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using NATS.Client.Core;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public sealed record Heartbeat(int Sequence);

[Collection(JetStreamCollection.Name)]
public class ReconnectionTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task ConsumeLoop_Should_ResumeReceiving_When_TheConnectionIsReset()
    {
        // arrange
        // A dedicated connection, because this test deliberately resets it and doing that to the
        // shared fixture connection disturbs every other test's subscriptions.
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();

        await using var connection = new NatsConnection(new NatsOpts
        {
            Url = fixture.ConnectionString,
            SubPendingChannelFullMode = System.Threading.Channels.BoundedChannelFullMode.Wait
        });

        await connection.ConnectAsync();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<INatsConnection>(connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<HeartbeatHandler>()
            .AddNats(nats => nats.StreamName("e2e-reconnect"));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            var bus = host.Services.GetRequiredService<IMessageBus>();

            await bus.PublishAsync(new Heartbeat(1), cancellationToken);
            Assert.True(await recorder.WaitAsync(s_timeout), "The first heartbeat never arrived.");

            // act
            // NATS.Net owns reconnection, which is why this transport has no connection manager of
            // its own. Forcing a reconnect proves the consume loop survives it rather than silently
            // stopping and leaving the service alive but deaf.
            await connection.ReconnectAsync();

            await bus.PublishAsync(new Heartbeat(2), cancellationToken);

            // assert
            Assert.True(
                await recorder.WaitAsync(s_timeout),
                "Consumption did not resume after the connection was reset.");

            Assert.Equal(
                [1, 2],
                recorder.Messages.Cast<Heartbeat>().Select(h => h.Sequence).Order());
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    public sealed class HeartbeatHandler(MessageRecorder recorder) : IEventHandler<Heartbeat>
    {
        public ValueTask HandleAsync(Heartbeat message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
