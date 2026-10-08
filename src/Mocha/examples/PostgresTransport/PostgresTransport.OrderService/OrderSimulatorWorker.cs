using Mocha;
using PostgresTransport.Contracts.Events;

namespace PostgresTransport.OrderService;

/// <summary>
/// Background worker that simulates customers placing orders every few seconds.
/// </summary>
public sealed class OrderSimulatorWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderSimulatorWorker> logger) : BackgroundService
{
    private static readonly string[] s_products =
    [
        "Mechanical Keyboard", "Wireless Mouse", "USB-C Hub",
        "Monitor Stand", "Webcam HD", "Noise-Cancelling Headphones",
        "Laptop Sleeve", "Desk Lamp", "Ergonomic Chair", "Standing Desk"
    ];

    private static readonly string[] s_customers =
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
                var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                var orderId = Guid.NewGuid();
                var product = s_products[Random.Shared.Next(s_products.Length)];
                var quantity = Random.Shared.Next(1, 6);
                var unitPrice = Math.Round(Random.Shared.Next(1999, 49999) / 100m, 2);
                var customer = s_customers[Random.Shared.Next(s_customers.Length)];

                var orderEvent = new OrderPlacedEvent
                {
                    OrderId = orderId,
                    ProductName = product,
                    Quantity = quantity,
                    TotalAmount = unitPrice * quantity,
                    CustomerEmail = customer,
                    PlacedAt = DateTimeOffset.UtcNow
                };

                await messageBus.PublishAsync(orderEvent, stoppingToken);

                logger.LogInformation(
                    "Simulated order {OrderId}: {Quantity}x {Product} for {Customer}",
                    orderId, quantity, product, customer);
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
