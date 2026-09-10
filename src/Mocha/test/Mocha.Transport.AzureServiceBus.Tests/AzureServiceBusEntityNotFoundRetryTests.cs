using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests;

/// <summary>
/// Covers dispatch recovery when Azure Service Bus reports <see cref="ServiceBusFailureReason.MessagingEntityNotFound"/>
/// for a cached sender: the sender is retired, the destination endpoint is reprovisioned, and the send
/// is retried exactly once against a freshly acquired sender.
/// </summary>
public class AzureServiceBusEntityNotFoundRetryTests
{
    [Fact]
    public async Task SendAsync_Should_RecreateSenderAndRetryOnce_When_EntityWasDeleted()
    {
        // arrange
        var (client, admin, bus) = CreateBus(
            senderIndex => senderIndex == 0
                ? new ServiceBusException("entity deleted", ServiceBusFailureReason.MessagingEntityNotFound)
                : null);
        await using var busScope = bus;

        using var scope = busScope.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.SendAsync(
            new ProcessPayment { OrderId = "ORD-1", Amount = 10m },
            CancellationToken.None);

        // assert
        var originalSender = client.CreatedSenders[0];
        var replacementSender = client.CreatedSenders[1];

        new
        {
            client.CreatedSenders.Count,
            Original = new
            {
                originalSender.EntityPath, originalSender.SendMessageCallCount, originalSender.IsClosed
            },
            Replacement = new
            {
                replacementSender.EntityPath, replacementSender.SendMessageCallCount, replacementSender.IsClosed
            },
            admin.CreateQueueCallCount
        }.MatchInlineSnapshot(
            """
            {
              "Count": 2,
              "Original": {
                "EntityPath": "payments",
                "SendMessageCallCount": 1,
                "IsClosed": true
              },
              "Replacement": {
                "EntityPath": "payments",
                "SendMessageCallCount": 1,
                "IsClosed": false
              },
              "CreateQueueCallCount": 2
            }
            """);
    }

    [Fact]
    public async Task SendAsync_Should_PropagateException_When_SecondMessagingEntityNotFoundOccurs()
    {
        // arrange
        var (client, admin, bus) = CreateBus(
            _ => new ServiceBusException("entity deleted", ServiceBusFailureReason.MessagingEntityNotFound));
        await using var busScope = bus;

        using var scope = busScope.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = await Assert.ThrowsAsync<ServiceBusException>(
            () => messageBus.SendAsync(
                new ProcessPayment { OrderId = "ORD-1", Amount = 10m },
                CancellationToken.None).AsTask());

        // assert
        Assert.Equal(ServiceBusFailureReason.MessagingEntityNotFound, exception.Reason);
        Assert.Equal(2, client.CreatedSenders.Count);
        Assert.All(client.CreatedSenders, sender => Assert.Equal(1, sender.SendMessageCallCount));
        Assert.Equal(2, admin.CreateQueueCallCount);
    }

    [Fact]
    public async Task ScheduleSendAsync_Should_RecreateSenderAndRetryOnce_When_EntityWasDeleted()
    {
        // arrange
        var (client, admin, bus) = CreateBus(
            senderIndex => senderIndex == 0
                ? new ServiceBusException("entity deleted", ServiceBusFailureReason.MessagingEntityNotFound)
                : null);
        await using var busScope = bus;

        using var scope = busScope.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var result = await messageBus.ScheduleSendAsync(
            new ProcessPayment { OrderId = "ORD-1", Amount = 10m },
            DateTimeOffset.UtcNow.AddMinutes(5),
            CancellationToken.None);

        // assert
        Assert.StartsWith("asb:v1:", result.Token, StringComparison.Ordinal);

        var originalSender = client.CreatedSenders[0];
        var replacementSender = client.CreatedSenders[1];

        new
        {
            client.CreatedSenders.Count,
            Original = new
            {
                originalSender.EntityPath, originalSender.ScheduleMessageCallCount, originalSender.IsClosed
            },
            Replacement = new
            {
                replacementSender.EntityPath, replacementSender.ScheduleMessageCallCount, replacementSender.IsClosed
            },
            admin.CreateQueueCallCount
        }.MatchInlineSnapshot(
            """
            {
              "Count": 2,
              "Original": {
                "EntityPath": "payments",
                "ScheduleMessageCallCount": 1,
                "IsClosed": true
              },
              "Replacement": {
                "EntityPath": "payments",
                "ScheduleMessageCallCount": 1,
                "IsClosed": false
              },
              "CreateQueueCallCount": 2
            }
            """);
    }

    [Fact]
    public async Task ScheduleSendAsync_Should_PropagateException_When_SecondMessagingEntityNotFoundOccurs()
    {
        // arrange
        var (client, admin, bus) = CreateBus(
            _ => new ServiceBusException("entity deleted", ServiceBusFailureReason.MessagingEntityNotFound));
        await using var busScope = bus;

        using var scope = busScope.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = await Assert.ThrowsAsync<ServiceBusException>(
            () => messageBus.ScheduleSendAsync(
                new ProcessPayment { OrderId = "ORD-1", Amount = 10m },
                DateTimeOffset.UtcNow.AddMinutes(5),
                CancellationToken.None).AsTask());

        // assert
        Assert.Equal(ServiceBusFailureReason.MessagingEntityNotFound, exception.Reason);
        Assert.Equal(2, client.CreatedSenders.Count);
        Assert.All(client.CreatedSenders, sender => Assert.Equal(1, sender.ScheduleMessageCallCount));
        Assert.Equal(2, admin.CreateQueueCallCount);
    }

    private static (FakeServiceBusClient Client, FakeServiceBusAdministrationClient Admin, TestBus Bus) CreateBus(
        Func<int, Exception?> failureAt)
    {
        var client = new FakeServiceBusClient(failureAt);
        var admin = new FakeServiceBusAdministrationClient();
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(admin);
        var builder = services
            .AddMessageBus()
            .AddAzureServiceBus(t => t.DispatchEndpoint("payments").ToQueue("payments").Send<ProcessPayment>());
        var provider = builder.Services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        return (client, admin, new TestBus(provider, runtime));
    }
}
