using Demo.Catalog.Data;
using Demo.Catalog.Entities;
using Demo.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Mocha;
using Mocha.Mediator;

namespace Demo.Catalog.Commands;

public record PlaceOrderCommand(
    Guid ProductId,
    int Quantity,
    string CustomerId,
    string ShippingAddress) : ICommand<PlaceOrderResult>;

public record PlaceOrderResult(bool Success, OrderRecord? Order = null, string? Error = null);

public class PlaceOrderCommandHandler(CatalogDbContext db, IMessageBus messageBus)
    : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public async ValueTask<PlaceOrderResult> HandleAsync(
        PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var executionStrategy = db.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            var product = await db.Products.FindAsync(command.ProductId);
            if (product is null)
            {
                return new PlaceOrderResult(false, Error: "Product not found");
            }

            if (product.StockQuantity < command.Quantity)
            {
                return new PlaceOrderResult(false, Error: "Insufficient stock");
            }

            var order = new OrderRecord
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = command.Quantity,
                CustomerId = command.CustomerId,
                ShippingAddress = command.ShippingAddress,
                TotalAmount = product.Price * command.Quantity,
                Status = OrderStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            await messageBus.PublishAsync(
                new OrderPlacedEvent
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = order.Quantity,
                    UnitPrice = product.Price,
                    TotalAmount = order.TotalAmount,
                    CustomerId = order.CustomerId,
                    ShippingAddress = order.ShippingAddress,
                    CreatedAt = order.CreatedAt
                },
                CancellationToken.None);

            await transaction.CommitAsync();

            return new PlaceOrderResult(true, order);
        });
    }
}
