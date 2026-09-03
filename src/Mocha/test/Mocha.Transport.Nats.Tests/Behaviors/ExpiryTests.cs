using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Expiry.Contracts;
using Mocha.Transport.Nats.Tests.Fixtures;
using NATS.Client.JetStream.Models;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class ExpiryTests(JetStreamFixture fixture)
{
    private const string StreamName = "EXPIRY_OWNED";

    [Fact]
    public async Task PublishAsync_Should_ExpireTheMessage_When_AnExpirationTimeIsGiven()
    {
        // arrange
        // Nothing consumes this stream, so the message stays put until the server expires it. The
        // transport's job is to turn ExpirationTime into the Nats-TTL header; the server's job is to
        // honour it.
        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.SkipUnless(
            NatsServerCapabilities
                .FromServerVersion(fixture.Connection.ServerInfo?.Version)
                .SupportsMessageTtl,
            "Per-message TTL needs NATS server 2.11 or later.");

        await fixture.JetStream.CreateOrUpdateStreamAsync(
            new StreamConfig
            {
                Name = StreamName,
                Subjects = ["mocha.expiry.contracts.>"],
                Storage = StreamConfigStorage.Memory,
                AllowMsgTTL = true
            },
            cancellationToken);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services
            .AddMessageBus()
            .AddNats(nats => nats.StreamName("expiry-publisher").AutoProvision(false));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(
                    new PerishableNotice("N-1"),
                    new PublishOptions { ExpirationTime = DateTimeOffset.UtcNow.AddSeconds(1) },
                    cancellationToken);

            // assert
            Assert.Equal(1, await MessageCountAsync(cancellationToken));

            Assert.True(
                await WaitForEmptyAsync(TimeSpan.FromSeconds(30), cancellationToken),
                "The message outlived its expiration time, so Nats-TTL was not applied.");
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    private async Task<long> MessageCountAsync(CancellationToken cancellationToken)
    {
        var stream = await fixture.JetStream.GetStreamAsync(
            StreamName,
            cancellationToken: cancellationToken);

        return stream.Info.State.Messages;
    }

    private async Task<bool> WaitForEmptyAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await MessageCountAsync(cancellationToken) == 0)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        return false;
    }
}
