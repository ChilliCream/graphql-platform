using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;
using NATS.Client.JetStream;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public sealed class TemporaryEndpointCleanupTests(JetStreamFixture fixture)
{
    private const string EndpointName = "temporary-orders";

    [Fact]
    public async Task StopAsync_Should_RemoveTemporaryEndpointResources_When_StoppedGracefully()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var recorder = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.Endpoint(EndpointName).Handler<OrderCreatedHandler>().Temporary();
            })
            .BuildTestBusAsync();

        var transport = bus.Runtime.Transports.OfType<NatsMessagingTransport>().Single();
        var consumer = Assert.Single(bus.Topology.Consumers, c => c.Name == EndpointName);
        var subject = Assert.Single(consumer.FilterSubjects);
        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        // NATS has no auto-delete on disconnect; the consumer's inactive threshold is what removes
        // it once no instance is reading, so the scenario's wait covers that threshold.
        var result = await TemporaryEndpointCleanupScenario.ExecuteAsync(
            messageBus,
            recorder,
            ct => InspectResourcesAsync(consumer.StreamName!, subject, ct),
            ct => transport.StopAsync(bus.Runtime, ct),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "MessageDelivered": true,
              "BeforeStop": {
                "QueueExists": true,
                "BindingExists": true
              },
              "AfterStop": {
                "QueueExists": false,
                "BindingExists": false
              }
            }
            """);
    }

    [Fact]
    public async Task Temporary_Should_SetAnInactiveThreshold_When_TheEndpointIsTemporary()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.Endpoint(EndpointName).Handler<OrderCreatedHandler>().Temporary();
            })
            .BuildTestBusAsync();

        // act
        var consumer = Assert.Single(bus.Topology.Consumers, c => c.Name == EndpointName);
        var info = await fixture.JetStream.GetConsumerAsync(
            consumer.StreamName!,
            consumer.Name,
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(consumer.IsTemporary);
        Assert.Equal(NatsConsumer.DefaultTemporaryInactiveThreshold, info.Info.Config.InactiveThreshold);
    }

    [Fact]
    public async Task Temporary_Should_Throw_When_TheConsumerIsDeclaredAsPersistent()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var builder = new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.DeclareConsumer(EndpointName);
                nats.Endpoint(EndpointName).Handler<OrderCreatedHandler>().Temporary();
            });

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(builder.BuildTestBusAsync);

        // assert
        Assert.Contains("Temporary()", exception.Message);
    }

    private async Task<TemporaryEndpointResourceState> InspectResourcesAsync(
        string streamName,
        string subject,
        CancellationToken cancellationToken)
    {
        try
        {
            var consumer = await fixture.JetStream.GetConsumerAsync(
                streamName,
                EndpointName,
                cancellationToken);
            var filters = consumer.Info.Config.FilterSubjects
                ?? (consumer.Info.Config.FilterSubject is { } single ? [single] : []);

            return new TemporaryEndpointResourceState(true, filters.Contains(subject));
        }
        catch (NatsJSApiException exception) when (exception.Error.Code == 404)
        {
            return new TemporaryEndpointResourceState(false, false);
        }
    }
}
