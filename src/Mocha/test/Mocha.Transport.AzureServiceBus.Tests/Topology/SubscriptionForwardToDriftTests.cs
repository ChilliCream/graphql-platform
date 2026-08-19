using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Topology;

/// <summary>
/// Covers <see cref="AzureServiceBusSubscription.ProvisionAsync"/> handling of a
/// <see cref="ServiceBusFailureReason.MessagingEntityAlreadyExists"/> race, where the broker
/// returns the pre-existing subscription's ForwardTo as an absolute URI rather than the bare
/// entity name that was originally configured.
/// </summary>
public class SubscriptionForwardToDriftTests
{
    private const string QueueName = "accounting";

    [Fact]
    public async Task ProvisionAsync_Should_Succeed_When_ExistingForwardToUriPathMatchesDestinationQueue()
    {
        // arrange - the broker reports the pre-existing subscription's ForwardTo as an absolute URI
        // whose path matches the destination queue this instance is racing to provision
        var (_, runtime) = CreateRuntime(existingForwardTo: $"sb://ns.servicebus.windows.net/{QueueName}");

        // act
        await runtime.StartAsync(CancellationToken.None);

        // assert - no exception was thrown provisioning the transport
        await runtime.Transports.Single().DisposeAsync();
    }

    [Fact]
    public async Task ProvisionAsync_Should_Throw_When_ExistingForwardToUriPathDiffersFromDestinationQueue()
    {
        // arrange - the broker reports the pre-existing subscription forwarding to a different queue
        const string driftedQueueName = "drifted-accounting";
        var (_, runtime) = CreateRuntime(existingForwardTo: $"sb://ns.servicebus.windows.net/{driftedQueueName}");

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => runtime.StartAsync(CancellationToken.None).AsTask());

        // assert
        Assert.Contains(QueueName, exception.Message);
        Assert.Contains(driftedQueueName, exception.Message);
    }

    private static (FakeServiceBusAdministrationClient Admin, MessagingRuntime Runtime) CreateRuntime(
        string existingForwardTo)
    {
        var client = new FakeServiceBusClient(_ => null);
        var admin = new FakeServiceBusAdministrationClient
        {
            CreateSubscriptionFailure = _ => new ServiceBusException(
                "subscription already exists",
                ServiceBusFailureReason.MessagingEntityAlreadyExists),
            ExistingSubscriptionForwardTo = existingForwardTo
        };
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(admin);
        var builder = services
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.DeclareTopic("orders");
                t.DeclareQueue(QueueName);
                t.DeclareSubscription("orders", QueueName);
            });
        var provider = builder.Services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        return (admin, runtime);
    }
}
