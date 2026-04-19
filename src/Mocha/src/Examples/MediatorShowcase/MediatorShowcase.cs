using MediatorShowcase;
using Mocha.Mediator;

var builder = WebApplication.CreateBuilder(args);

// Register the Mocha Mediator with source-generated handlers.
builder.Services.AddMediator()
    .AddMediatorShowcase()
    .Use(LoggingMiddleware.Create())
    .Use(PlaceOrderValidationMiddleware.Create())
    .Use(PlaceOrderAuditMiddleware.Create())
    .Use(ExceptionHandlingMiddleware.Create())
    .AddInstrumentation();

var app = builder.Build();

// ──────────────────────────────────────────────────
// Commands
// ──────────────────────────────────────────────────

// Void command - no return value
app.MapPost("/api/products", async (CreateProductRequest req, ISender sender) =>
{
    await sender.SendAsync(new CreateProductCommand(req.Name, req.Price));
    return Results.Created();
});

// Command with response
app.MapPost("/api/orders", async (PlaceOrderRequest req, ISender sender) =>
{
    var result = await sender.SendAsync(
        new PlaceOrderCommand(req.ProductName, req.Quantity));
    return Results.Ok(result);
});

// ──────────────────────────────────────────────────
// Queries
// ──────────────────────────────────────────────────

app.MapGet("/api/products", async (ISender sender) =>
    await sender.QueryAsync(new GetProductsQuery()));

app.MapGet("/api/products/{id:guid}", async (Guid id, ISender sender) =>
    await sender.QueryAsync(new GetProductByIdQuery(id)) is { } product
        ? Results.Ok(product)
        : Results.NotFound());

// ──────────────────────────────────────────────────
// Notifications
// ──────────────────────────────────────────────────

app.MapPost("/api/notifications/order-shipped", async (Guid orderId, IPublisher publisher) =>
{
    await publisher.PublishAsync(new OrderShippedNotification(orderId));
    return Results.Ok("Notification published");
});

// ──────────────────────────────────────────────────
// Exception handling demo
// ──────────────────────────────────────────────────

app.MapGet("/api/demo/exception", async (ISender sender) =>
{
    var result = await sender.SendAsync(new RiskyCommand());
    return Results.Ok(result);
});

app.Run();

// ──────────────────────────────────────────────────
// Request/Response DTOs
// ──────────────────────────────────────────────────

public record CreateProductRequest(string Name, decimal Price);
public record PlaceOrderRequest(string ProductName, int Quantity);

// ──────────────────────────────────────────────────
// Messages
// ──────────────────────────────────────────────────

namespace MediatorShowcase
{
    // -- Commands --

    public sealed record CreateProductCommand(string Name, decimal Price) : ICommand;

    public sealed record PlaceOrderCommand(string ProductName, int Quantity) : ICommand<OrderResult>;

    public sealed record OrderResult(Guid OrderId, string Status, decimal Total);

    public sealed record RiskyCommand : ICommand<string>;

    // -- Queries --

    public sealed record GetProductsQuery : IQuery<IReadOnlyList<ProductDto>>;

    public sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductDto?>;

    public sealed record ProductDto(Guid Id, string Name, decimal Price);

    // -- Notifications --

    public sealed record OrderShippedNotification(Guid OrderId) : INotification;
}
