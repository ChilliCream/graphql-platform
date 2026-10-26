using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats.Tests.Behaviors;

/// <summary>
/// Covers address-based sends to subjects another service owns: the sending host binds explicitly,
/// never declares a stream for the subject, and still has to reach the stream that captures it.
/// </summary>
[Collection(JetStreamCollection.Name)]
public sealed class ExternalDestinationTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task SendAsync_Should_DeliverToForeignStream_When_SubjectNotDeclaredUnderExplicitBinding()
    {
        // arrange
        // The stream belongs to another service: it captures the subject and this host never
        // declares it.
        const string subject = "reporting.events";
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scope = await fixture.CreateScopeAsync();
        await fixture.JetStream.CreateStreamAsync(
            new StreamConfig("REPORTING", [subject]) { AllowDirect = true },
            cancellationToken);

        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddMessageBus()
            .AddNats(nats => nats.StreamName(scope.StreamName).BindExplicitly())
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.SendAsync(
            new OrderCreated { OrderId = "EXTERNAL-1" },
            new SendOptions { Endpoint = new Uri($"nats:s/{subject}") },
            cancellationToken);

        // assert
        var stream = await fixture.JetStream.GetStreamAsync("REPORTING", cancellationToken: cancellationToken);
        var stored = await stream.GetDirectAsync<byte[]>(
            new StreamMsgGetRequest { LastBySubj = subject },
            cancellationToken: cancellationToken);
        Assert.Contains("EXTERNAL-1", Encoding.UTF8.GetString(stored.Data!));
        Assert.Equal(["REPORTING"], await StreamNamesAsync(cancellationToken));
    }

    [Fact]
    public async Task SendAsync_Should_FailWithoutCreatingAStream_When_NoStreamCapturesTheSubject()
    {
        // arrange
        // Unlike RabbitMQ, where an unrouted send is dropped by the broker, JetStream refuses a
        // publish no stream captures, so the send fails rather than vanishing.
        const string subject = "reporting.missing";
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddMessageBus()
            .AddNats(nats => nats.StreamName(scope.StreamName).BindExplicitly())
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;
        var before = await StreamNamesAsync(cancellationToken);

        // act
        var exception = await Record.ExceptionAsync(
            async () => await messageBus.SendAsync(
                new OrderCreated { OrderId = "EXTERNAL-2" },
                new SendOptions { Endpoint = new Uri($"nats:s/{subject}") },
                cancellationToken));

        // assert
        Assert.NotNull(exception);
        Assert.Equal(before, await StreamNamesAsync(cancellationToken));
    }

    private async Task<string[]> StreamNamesAsync(CancellationToken cancellationToken)
    {
        var names = new List<string>();

        await foreach (var name in fixture.JetStream.ListStreamNamesAsync(cancellationToken: cancellationToken))
        {
            names.Add(name);
        }

        return [.. names.Order(StringComparer.Ordinal)];
    }
}
