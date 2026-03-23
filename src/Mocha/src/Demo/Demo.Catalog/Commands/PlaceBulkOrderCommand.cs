using Demo.Contracts.Events;
using Microsoft.Extensions.Logging;
using Mocha;
using Mocha.Mediator;

namespace Demo.Catalog.Commands;

public record PlaceBulkOrderCommand(int Count) : ICommand<BulkOrderResult>;

public record BulkOrderResult(int Dispatched, long ElapsedMs);

public class PlaceBulkOrderCommandHandler(IMessageBus messageBus, ILogger<PlaceBulkOrderCommandHandler> logger)
    : ICommandHandler<PlaceBulkOrderCommand, BulkOrderResult>
{
    public async ValueTask<BulkOrderResult> HandleAsync(
        PlaceBulkOrderCommand command, CancellationToken cancellationToken)
    {
        var count = command.Count is > 0 ? command.Count : 2000;

        var products = new[]
        {
            ("Wireless Headphones", 299.99m),
            ("Mechanical Keyboard", 149.99m),
            ("Clean Code", 39.99m)
        };

        logger.LogInformation("Dispatching {Count} BulkOrderEvents", count);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (var i = 0; i < count; i++)
        {
            var (name, price) = products[i % products.Length];
            var qty = (i % 5) + 1;

            await messageBus.PublishAsync(
                new BulkOrderEvent
                {
                    OrderId = Guid.NewGuid(),
                    ProductName = name,
                    Quantity = qty,
                    UnitPrice = price,
                    TotalAmount = price * qty,
                    CustomerId = $"bulk-customer-{i:D5}",
                    CreatedAt = DateTimeOffset.UtcNow
                },
                cancellationToken);
        }

        sw.Stop();
        logger.LogInformation("Dispatched {Count} BulkOrderEvents in {Elapsed}ms", count, sw.ElapsedMilliseconds);

        return new BulkOrderResult(count, sw.ElapsedMilliseconds);
    }
}
