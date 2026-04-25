// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Resources.AspNetCore@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run QuickStart.cs

using Mocha;
using Mocha.Resources;
using Mocha.Resources.AspNetCore;
using Mocha.Transport.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();

builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedHandler>()
    .AddInMemory();

builder.Services.AddMochaMessageBusResources();

var app = builder.Build();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.MapGet("/orders", async (IMessageBus bus) =>
{
    var orderPlaced = new OrderPlaced(
        OrderId: Guid.NewGuid(),
        ProductName: "Mechanical Keyboard",
        Amount: 149.99m);

    await bus.PublishAsync(orderPlaced, CancellationToken.None);

    return Results.Ok(new { orderPlaced.OrderId, Status = "Published" });
});

Console.WriteLine("Listening for orders on http://localhost:5000/orders");

if (app.Environment.IsDevelopment())
{
    app.MapMochaResourceEndpoint();
}

app.Run();

public sealed record OrderPlaced(
    Guid OrderId,
    string ProductName,
    decimal Amount);

public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger)
    : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order received: {OrderId} - {ProductName} for {Amount:C}",
            message.OrderId,
            message.ProductName,
            message.Amount);

        return ValueTask.CompletedTask;
    }
}
