// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run EventPubSub.cs

using Mocha;
using Mocha.Transport.InMemory;
using Mocha.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddEventHandler<BillingHandler>()
    .AddEventHandler<NotificationHandler>()
    .AddInMemory();

var app = builder.Build();

app.MapGet("/orders", async (IMessageBus bus) =>
{
    await bus.PublishAsync(new OrderPlacedEvent
    {
        OrderId = Guid.NewGuid(),
        CustomerId = "customer-42",
        TotalAmount = 149.99m,
        CreatedAt = DateTimeOffset.UtcNow
    }, CancellationToken.None);

    return Results.Ok();
});

Console.WriteLine("Listening on http://localhost:5000/orders");

if (app.Environment.IsDevelopment())
{
    app.MapMessageBusDeveloperTopology();
}

app.Run();

public sealed record OrderPlacedEvent
{
    public required Guid OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed record PaymentCompletedEvent
{
    public required Guid PaymentId { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string PaymentMethod { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
}

// Handler 1: Create an invoice when an order is placed, then publish a downstream event
public class BillingHandler(IMessageBus messageBus, ILogger<BillingHandler> logger)
    : IEventHandler<OrderPlacedEvent>
{
    public async ValueTask HandleAsync(
        OrderPlacedEvent message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating invoice for order {OrderId}, amount {Amount}",
            message.OrderId,
            message.TotalAmount);

        // Create invoice logic here

        // Publish a downstream event
        await messageBus.PublishAsync(
            new PaymentCompletedEvent
            {
                PaymentId = Guid.NewGuid(),
                OrderId = message.OrderId,
                Amount = message.TotalAmount,
                PaymentMethod = "CreditCard",
                ProcessedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);
    }
}

// Handler 2: Send a notification when an order is placed
public class NotificationHandler(ILogger<NotificationHandler> logger)
    : IEventHandler<OrderPlacedEvent>
{
    public async ValueTask HandleAsync(
        OrderPlacedEvent message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Sending confirmation to customer {CustomerId} for order {OrderId}",
            message.CustomerId,
            message.OrderId);

        // Send email/SMS logic here
        await Task.CompletedTask;
    }
}
