using Mocha;
using Mocha.Mediator;
using AzureEventHubTransport.Contracts.Events;
using AzureEventHubTransport.Contracts.Mediator;

namespace AzureEventHubTransport.OrderService;

/// <summary>
/// Background worker that simulates customers placing orders every few seconds.
/// Uses the mediator to create the order locally, then publishes to the bus
/// (which triggers the fulfillment saga).
/// </summary>
public sealed class OrderSimulatorWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderSimulatorWorker> logger) : BackgroundService
{
    private static readonly string[] Products =
    [
        "Mechanical Keyboard", "Wireless Mouse", "USB-C Hub",
        "Monitor Stand", "Webcam HD", "Noise-Cancelling Headphones",
        "Laptop Sleeve", "Desk Lamp", "Ergonomic Chair", "Standing Desk"
    ];

    private static readonly string[] Customers =
    [
        "alice@example.com", "bob@example.com", "carol@example.com",
        "dave@example.com", "eve@example.com"
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the bus a moment to finish starting up
        await Task.Delay(3000, stoppingToken);

        logger.LogInformation("Order simulator started - placing orders every 5 seconds");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                var product = Products[Random.Shared.Next(Products.Length)];
                var quantity = Random.Shared.Next(1, 6);
                var unitPrice = Math.Round(Random.Shared.Next(1999, 49999) / 100m, 2);
                var customer = Customers[Random.Shared.Next(Customers.Length)];

                // 1. Create order locally via mediator
                var result = await sender.SendAsync(
                    new CreateOrderCommand
                    {
                        ProductName = product,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        CustomerEmail = customer
                    },
                    stoppingToken);

                // 2. Publish event → kicks off the OrderFulfillmentSaga
                await messageBus.PublishAsync(
                    new OrderPlacedEvent
                    {
                        OrderId = result.OrderId,
                        ProductName = product,
                        Quantity = quantity,
                        TotalAmount = result.TotalAmount,
                        CustomerEmail = customer,
                        PlacedAt = DateTimeOffset.UtcNow
                    },
                    stoppingToken);

                logger.LogInformation(
                    "Simulated order {OrderId}: {Quantity}x {Product} for {Customer}",
                    result.OrderId, quantity, product, customer);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error placing simulated order");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}
