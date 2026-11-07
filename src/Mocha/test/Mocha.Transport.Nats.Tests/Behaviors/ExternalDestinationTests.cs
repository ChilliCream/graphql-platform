using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats.Tests.Behaviors;

/// <summary>
/// Verifies address-based sends to subjects captured by a stream that another service owns.
/// </summary>
[Collection(JetStreamCollection.Name)]
public sealed class ExternalDestinationTests(JetStreamFixture fixture)
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task SendAsync_Should_DeliverToForeignStream_When_SubjectNotDeclaredUnderExplicitBinding()
    {
        // arrange
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
        var order = JsonSerializer.Deserialize<OrderCreated>(stored.Data!, s_json);
        Assert.Equal("EXTERNAL-1", order!.OrderId);
        Assert.Equal(["REPORTING"], await StreamNamesAsync(cancellationToken));
    }

    [Fact]
    public async Task SendAsync_Should_FailWithoutCreatingAStream_When_NoStreamCapturesTheSubject()
    {
        // arrange
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
        Assert.Equal(
            $"Publishing to subject '{subject}' failed. A JetStream publish waits for an "
            + "acknowledgement from the stream capturing the subject, so this also happens when "
            + "no stream captures it or the server has JetStream disabled.",
            Assert.IsType<InvalidOperationException>(exception).Message);
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
