using Mocha.Mediator;

namespace MediatorShowcase;

// ──────────────────────────────────────────────────
// Command Handlers
// ──────────────────────────────────────────────────

/// <summary>
/// Handles a void command (no return value).
/// </summary>
public sealed class CreateProductCommandHandler(ILogger<CreateProductCommandHandler> logger)
    : ICommandHandler<CreateProductCommand>
{
    public ValueTask HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Product created: {Name} at {Price:C}", command.Name, command.Price);
        return ValueTask.CompletedTask;
    }
}
public record CreateProductCommand2(string Name, decimal Price) : ICommand;
public sealed class CreateProductCommandHandler2(ILogger<CreateProductCommandHandler> logger)
    : ICommandHandler<CreateProductCommand2>
{
    public ValueTask HandleAsync(CreateProductCommand2 command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Product created: {Name} at {Price:C}", command.Name, command.Price);
        return ValueTask.CompletedTask;
    }
}

public record CreateProductCommand3(string Name, decimal Price) : ICommand;
public sealed class CreateProductCommandHandler3(ILogger<CreateProductCommandHandler> logger)
    : ICommandHandler<CreateProductCommand3>
{
    public ValueTask HandleAsync(CreateProductCommand3 command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Product created: {Name} at {Price:C}", command.Name, command.Price);
        return ValueTask.CompletedTask;
    }
}

public record CreateProductCommand4(string Name, decimal Price) : ICommand;
public sealed class CreateProductCommandHandler4(ILogger<CreateProductCommandHandler> logger)
    : ICommandHandler<CreateProductCommand4>
{
    public ValueTask HandleAsync(CreateProductCommand4 command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Product created: {Name} at {Price:C}", command.Name, command.Price);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Handles a command that returns a response.
/// </summary>
public sealed class PlaceOrderCommandHandler(ILogger<PlaceOrderCommandHandler> logger)
    : ICommandHandler<PlaceOrderCommand, OrderResult>
{
    public ValueTask<OrderResult> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        var total = command.Quantity * 29.99m;

        logger.LogInformation(
            "Order {OrderId} placed: {Quantity}x {ProductName} = {Total:C}",
            orderId, command.Quantity, command.ProductName, total);

        return new ValueTask<OrderResult>(new OrderResult(orderId, "Confirmed", total));
    }
}

/// <summary>
/// A command handler that always throws, demonstrating exception handling.
/// </summary>
public sealed class RiskyCommandHandler : ICommandHandler<RiskyCommand, string>
{
    public ValueTask<string> HandleAsync(RiskyCommand command, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Something went wrong processing the risky command!");
    }
}

// ──────────────────────────────────────────────────
// Query Handlers
// ──────────────────────────────────────────────────

public sealed class GetProductsQueryHandler : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private static readonly IReadOnlyList<ProductDto> Products =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Mechanical Keyboard", 149.99m),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Ergonomic Mouse", 79.99m),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "4K Monitor", 499.99m)
    ];

    public ValueTask<IReadOnlyList<ProductDto>> HandleAsync(
        GetProductsQuery query, CancellationToken cancellationToken)
        => new(Products);
}

public sealed class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductDto?>
{
    public async ValueTask<ProductDto?> HandleAsync(
        GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var products = await new GetProductsQueryHandler()
            .HandleAsync(new GetProductsQuery(), cancellationToken);
        return products.FirstOrDefault(p => p.Id == query.Id);
    }
}

// ──────────────────────────────────────────────────
// Notification Handlers (multiple handlers per notification)
// ──────────────────────────────────────────────────

/// <summary>
/// First handler: sends an email when an order is shipped.
/// </summary>
public sealed class OrderShippedEmailHandler(ILogger<OrderShippedEmailHandler> logger)
    : INotificationHandler<OrderShippedNotification>
{
    public ValueTask HandleAsync(OrderShippedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("[Email] Order {OrderId} shipped — email sent to customer", notification.OrderId);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Second handler: updates analytics when an order is shipped.
/// </summary>
public sealed class OrderShippedAnalyticsHandler(ILogger<OrderShippedAnalyticsHandler> logger)
    : INotificationHandler<OrderShippedNotification>
{
    public ValueTask HandleAsync(OrderShippedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("[Analytics] Order {OrderId} shipped — metrics recorded", notification.OrderId);
        return ValueTask.CompletedTask;
    }
}
