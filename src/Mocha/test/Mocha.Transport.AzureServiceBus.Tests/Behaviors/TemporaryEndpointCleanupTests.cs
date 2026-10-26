using Azure.Messaging.ServiceBus.Administration;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

[Collection("AzureServiceBus")]
public sealed class TemporaryEndpointCleanupTests(AzureServiceBusFixture fixture)
{
    [Fact]
    public async Task StopAsync_Should_RemoveTemporaryEndpointResources_When_StoppedGracefully()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var context = fixture.CreateTestContext();
        var queueName = context.QueueName("temporary-orders");
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(context.ConnectionString);
                t.AdministrationConnectionString(context.AdminConnectionString);
                t.Endpoint(queueName).Handler<OrderCreatedHandler>().Temporary();
            })
            .BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var subscription = topology.Subscriptions.Single(s => s.Destination.Name == queueName);
        var administrationClient = new ServiceBusAdministrationClient(context.AdminConnectionString);
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var result = await TemporaryEndpointCleanupScenario.ExecuteAsync(
            messageBus,
            recorder,
            ct => InspectResourcesAsync(
                administrationClient,
                queueName,
                subscription.Source.Name,
                subscription.Name,
                ct),
            ct => transport.StopAsync(runtime, ct),
            Xunit.TestContext.Current.CancellationToken);

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

    private static async Task<TemporaryEndpointResourceState> InspectResourcesAsync(
        ServiceBusAdministrationClient administrationClient,
        string queueName,
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken)
    {
        var queueExists = await administrationClient.QueueExistsAsync(queueName, cancellationToken);
        var bindingExists = await administrationClient.SubscriptionExistsAsync(
            topicName,
            subscriptionName,
            cancellationToken);

        return new TemporaryEndpointResourceState(queueExists.Value, bindingExists.Value);
    }
}
