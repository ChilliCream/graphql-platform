// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Resources.AspNetCore@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run CustomConsumer.cs

using Microsoft.Extensions.DependencyInjection;
using Mocha;
using Mocha.Resources;
using Mocha.Resources.AspNetCore;
using Mocha.Transport.InMemory;

var builder = WebApplication.CreateBuilder(args);

var bus = builder.Services.AddMessageBus();

// Register the consumer type in DI and configure it with a custom name
// and explicit inbound route via the low-level ConfigureMessageBus API.
bus.Services.AddScoped<OrderFulfillmentConsumer>();
bus.ConfigureMessageBus(b =>
{
    var mb = (MessageBusBuilder)b;
    mb.AddHandler<OrderFulfillmentConsumer>(descriptor =>
    {
        descriptor
            .Name("order-fulfillment")
            .AddRoute(route =>
                route
                    .MessageType(typeof(OrderPlaced))
                    .Kind(InboundRouteKind.Subscribe));
    });
});

bus.AddInMemory();

builder.Services.AddMochaMessageBusResources();

var app = builder.Build();

app.MapGet("/orders", async (IMessageBus messageBus) =>
{
    await messageBus.PublishAsync(new OrderPlaced(
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

// IConsumer<T> gives full access to the consume context - message payload,
// envelope metadata, headers, correlation IDs, and the service provider.
// Use ConfigureMessageBus with AddHandler to assign a custom consumer name
// and configure explicit inbound routes.
public class OrderFulfillmentConsumer(ILogger<OrderFulfillmentConsumer> logger)
    : IConsumer<OrderPlaced>
{
    public ValueTask ConsumeAsync(IConsumeContext<OrderPlaced> context)
    {
        var order = context.Message;

        logger.LogInformation(
            "Fulfilling order {OrderId} for customer {CustomerId} (MessageId={MessageId}, CorrelationId={CorrelationId})",
            order.OrderId,
            order.CustomerId,
            context.MessageId,
            context.CorrelationId);

        // Access custom headers set by the publisher
        if (context.Headers.TryGetValue("x-priority", out var priority))
        {
            logger.LogInformation("Priority: {Priority}", priority);
        }

        return ValueTask.CompletedTask;
    }
}
