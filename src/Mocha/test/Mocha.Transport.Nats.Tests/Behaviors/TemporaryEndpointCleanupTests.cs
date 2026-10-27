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
        // An inactive threshold longer than the scenario's wait, so only the deletion on stop can pass.
        await using var scope = await fixture.CreateScopeAsync();
        var recorder = new MessageRecorder();
        await using var bus = await BuildBusAsync(
            scope,
            recorder,
            endpoint => endpoint.Temporary(TimeSpan.FromMinutes(5)));

        var transport = bus.Runtime.Transports.OfType<NatsMessagingTransport>().Single();
        var consumer = Assert.Single(bus.Topology.Consumers, c => c.Name == EndpointName);
        var subject = Assert.Single(consumer.FilterSubjects);
        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
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
    public async Task Temporary_Should_SetTheDefaultInactiveThreshold_When_NoneIsGiven()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await BuildBusAsync(scope, new MessageRecorder(), endpoint => endpoint.Temporary());

        // act
        var threshold = await GetServerInactiveThresholdAsync(bus);

        // assert
        Assert.Equal(NatsConsumer.DefaultTemporaryInactiveThreshold, threshold);
    }

    [Fact]
    public async Task Temporary_Should_SetTheGivenInactiveThreshold_When_OneIsGiven()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await BuildBusAsync(
            scope,
            new MessageRecorder(),
            endpoint => endpoint.Temporary(TimeSpan.FromMinutes(2)));

        // act
        var threshold = await GetServerInactiveThresholdAsync(bus);

        // assert
        Assert.Equal(TimeSpan.FromMinutes(2), threshold);
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
        Assert.Equal(
            $"Endpoint '{EndpointName}' is marked Temporary(), but consumer '{EndpointName}' was already "
            + "declared without an inactive threshold. Remove the consumer declaration or Temporary() "
            + "from the endpoint.",
            exception.Message);
    }

    [Fact]
    public async Task Temporary_Should_Throw_When_TheInactiveThresholdIsNotPositive()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();

        // act
        var exception = await Record.ExceptionAsync(
            () => BuildBusAsync(scope, new MessageRecorder(), endpoint => endpoint.Temporary(TimeSpan.Zero)));

        // assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    private async Task<TestBus> BuildBusAsync(
        JetStreamScope scope,
        MessageRecorder recorder,
        Action<INatsReceiveEndpointDescriptor> configureEndpoint)
        => await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                configureEndpoint(nats.Endpoint(EndpointName).Handler<OrderCreatedHandler>());
            })
            .BuildTestBusAsync();

    private async Task<TimeSpan?> GetServerInactiveThresholdAsync(TestBus bus)
    {
        var consumer = Assert.Single(bus.Topology.Consumers, c => c.Name == EndpointName);
        var info = await fixture.JetStream.GetConsumerAsync(
            consumer.StreamName!,
            consumer.Name,
            TestContext.Current.CancellationToken);

        return info.Info.Config.InactiveThreshold;
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
