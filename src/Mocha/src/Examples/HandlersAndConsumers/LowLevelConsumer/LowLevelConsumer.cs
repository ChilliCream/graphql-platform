// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Resources.AspNetCore@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run LowLevelConsumer.cs

using Mocha;
using Mocha.Transport.InMemory;
using Mocha.Resources.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddConsumer<OrderAuditConsumer>()
    .AddInMemory();

builder.Services.AddMochaMessageBusResources();

var app = builder.Build();

app.MapGet("/orders", async (IMessageBus bus) =>
{
    await bus.PublishAsync(new OrderPlaced(
        OrderId: Guid.NewGuid(),
        CustomerId: "customer-42",
        TotalAmount: 149.99m),
        CancellationToken.None);

    return Results.Ok("Published");
});

Console.WriteLine("GET http://localhost:5000/orders to publish an order");

if (app.Environment.IsDevelopment())
{
    app.MapMochaResourceEndpoint();
}

app.Run();

public sealed record OrderPlaced(
    Guid OrderId,
    string CustomerId,
    decimal TotalAmount);

public class OrderAuditConsumer(ILogger<OrderAuditConsumer> logger)
    : IConsumer<OrderPlaced>
{
    public ValueTask ConsumeAsync(IConsumeContext<OrderPlaced> context)
    {
        var order = context.Message;

        logger.LogInformation(
            "Audit: OrderId={OrderId} MessageId={MessageId} CorrelationId={CorrelationId} Source={Source}",
            order.OrderId,
            context.MessageId,
            context.CorrelationId,
            context.SourceAddress);

        if (context.Headers.TryGetValue("x-tenant", out var tenant))
        {
            logger.LogInformation("Tenant: {Tenant}", tenant);
        }

        return ValueTask.CompletedTask;
    }
}
