using AotExample.Contracts.Events;
using AotExample.OrderService.Commands;
using AotExample.OrderService.Queries;
using Mocha;
using Mocha.Mediator;

namespace AotExample.OrderService;

public sealed class OrderSimulatorWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderSimulatorWorker> logger
) : BackgroundService
{
    private static readonly string[] s_products =
    [
        "Mechanical Keyboard",
        "Wireless Mouse",
        "USB-C Hub",
        "Monitor Stand",
        "Webcam HD",
        "Noise-Cancelling Headphones",
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(3000, stoppingToken);

        logger.LogSimulatorStarted();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                var product = s_products[Random.Shared.Next(s_products.Length)];
                var quantity = Random.Shared.Next(1, 6);

                // 1. Place order via mediator command
                var result = await sender.SendAsync(
                    new PlaceOrderCommand { ProductName = product, Quantity = quantity },
                    stoppingToken
                );

                // 2. Query order status via mediator query
                var status = await sender.QueryAsync(
                    new GetOrderStatusQuery { OrderId = result.OrderId },
                    stoppingToken
                );

                logger.LogOrderStatus(result.OrderId, status.Status);

                var correlationId = Guid.NewGuid();

                // 3. Publish to bus — FulfillmentService will pick this up, saga will track it

                await messageBus.PublishAsync(
                    new OrderShippedEvent
                    {
                        OrderId = result.OrderId,
                        TrackingNumber = $"TRACK-{Random.Shared.Next(1000, 9999)}",
                    },
                    stoppingToken
                );
                await messageBus.PublishAsync(
                    new OrderPlacedEvent
                    {
                        OrderId = result.OrderId,
                        ProductName = product,
                        Quantity = quantity,
                        CorrelationId = correlationId,
                    },
                    stoppingToken
                );
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogSimulationError(ex);
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}

internal static partial class Logs
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Order simulator started")]
    public static partial void LogSimulatorStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} status: {Status}")]
    public static partial void LogOrderStatus(this ILogger logger, string orderId, string status);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in order simulation")]
    public static partial void LogSimulationError(this ILogger logger, Exception ex);
}
