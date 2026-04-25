// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Resources.AspNetCore@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run BatchHandler.cs

using Mocha;
using Mocha.Transport.InMemory;
using Mocha.Resources.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddBatchHandler<OrderPlacedBatchHandler>(opts =>
    {
        opts.MaxBatchSize = 50;
        opts.BatchTimeout = TimeSpan.FromSeconds(10);
    })
    .AddInMemory();

builder.Services.AddMochaMessageBusResources();

var app = builder.Build();

app.MapGet("/orders/{count:int}", async (int count, IMessageBus bus) =>
{
    for (var i = 0; i < count; i++)
    {
        await bus.PublishAsync(new OrderPlaced(
            OrderId: Guid.NewGuid(),
            CustomerId: $"customer-{i}",
            TotalAmount: 99.99m),
            CancellationToken.None);
    }

    return Results.Ok(new { Published = count });
});

Console.WriteLine("POST to http://localhost:5000/orders/{count} to publish a batch");

if (app.Environment.IsDevelopment())
{
    app.MapMochaResourceEndpoint();
}

app.Run();

public sealed record OrderPlaced(
    Guid OrderId,
    string CustomerId,
    decimal TotalAmount);

public class OrderPlacedBatchHandler(ILogger<OrderPlacedBatchHandler> logger)
    : IBatchEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(
        IMessageBatch<OrderPlaced> batch,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing batch of {Count} orders (reason: {Mode})",
            batch.Count,
            batch.CompletionMode);

        var totalRevenue = 0m;
        foreach (var order in batch)
        {
            totalRevenue += order.TotalAmount;
        }

        logger.LogInformation(
            "Batch complete: {Count} orders, {Total:C} total revenue",
            batch.Count,
            totalRevenue);

        return ValueTask.CompletedTask;
    }
}
