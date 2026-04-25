// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Resources.AspNetCore@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run UnitOfWork.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha;
using Mocha.Resources;
using Mocha.Resources.AspNetCore;
using Mocha.Transport.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<OrderRepository>();

var messageBus = builder.Services.AddMessageBus();

messageBus.ConfigureMessageBus(bus =>
{
    // Wrap every handler in a unit-of-work: commit on success, roll back on failure.
    bus.UseConsume(UnitOfWorkConsumerMiddleware.Create());
});

messageBus
    .AddEventHandler<OrderPlacedHandler>()
    .AddInMemory();

builder.Services.AddMochaMessageBusResources();

var app = builder.Build();

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

public class OrderPlacedHandler(
    OrderRepository repository,
    ILogger<OrderPlacedHandler> logger)
    : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        await repository.SaveOrderAsync(message.OrderId, message.ProductName, message.Amount);

        logger.LogInformation(
            "Order saved: {OrderId} - {ProductName} for {Amount:C}",
            message.OrderId,
            message.ProductName,
            message.Amount);
    }
}

// A simple scoped repository that tracks pending writes within a unit of work.
public sealed class OrderRepository
{
    private readonly ILogger<OrderRepository> _logger;
    private readonly List<(Guid OrderId, string ProductName, decimal Amount)> _pending = [];

    public OrderRepository(ILogger<OrderRepository> logger)
    {
        _logger = logger;
    }

    public ValueTask SaveOrderAsync(Guid orderId, string productName, decimal amount)
    {
        _pending.Add((orderId, productName, amount));
        return ValueTask.CompletedTask;
    }

    public ValueTask CommitAsync()
    {
        _logger.LogInformation(
            "Committed {Count} order(s) to the database",
            _pending.Count);
        _pending.Clear();
        return ValueTask.CompletedTask;
    }

    public ValueTask RollbackAsync()
    {
        _logger.LogWarning(
            "Rolling back {Count} pending order(s)",
            _pending.Count);
        _pending.Clear();
        return ValueTask.CompletedTask;
    }
}

// Consumer middleware that wraps handler execution in a unit of work.
// On success the repository is committed; on any exception it is rolled back and the
// exception is re-thrown so the fault middleware can route the message to an error endpoint.
internal sealed class UnitOfWorkConsumerMiddleware
{
    public async ValueTask InvokeAsync(
        IConsumeContext context,
        ConsumerDelegate next)
    {
        var repository = context.Services.GetRequiredService<OrderRepository>();

        try
        {
            await next(context);
            await repository.CommitAsync();
        }
        catch
        {
            await repository.RollbackAsync();
            throw;
        }
    }

    public static ConsumerMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var middleware = new UnitOfWorkConsumerMiddleware();
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "UnitOfWork");
}
