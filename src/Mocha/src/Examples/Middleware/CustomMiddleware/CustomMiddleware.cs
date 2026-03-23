// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run CustomMiddleware.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;
using Mocha.Hosting;

var builder = WebApplication.CreateBuilder(args);

var messageBus = builder.Services.AddMessageBus();

messageBus.ConfigureMessageBus(bus =>
{
    // Prepend tenant dispatch middleware so every outgoing message carries the tenant header.
    bus.PrependDispatch(TenantDispatchMiddleware.Create("acme"));

    // Insert logging receive middleware after ReceiveInstrumentation so telemetry spans are
    // already open when our middleware runs.
    bus.AppendReceive("ReceiveInstrumentation", LoggingReceiveMiddleware.Create());
});

messageBus
    .AddEventHandler<OrderPlacedHandler>()
    .AddInMemory();

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
    app.MapMessageBusDeveloperTopology();
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

// Dispatch middleware: stamps every outgoing message with a tenant identifier.
internal sealed class TenantDispatchMiddleware
{
    private readonly string _tenantId;

    public TenantDispatchMiddleware(string tenantId)
    {
        _tenantId = tenantId;
    }

    public async ValueTask InvokeAsync(
        IDispatchContext context,
        DispatchDelegate next)
    {
        context.Headers.Set("x-tenant", _tenantId);

        await next(context);
    }

    public static DispatchMiddlewareConfiguration Create(string tenantId)
        => new(
            (context, next) =>
            {
                var middleware = new TenantDispatchMiddleware(tenantId);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "TenantDispatch");
}

// Receive middleware: logs every incoming message before and after it is processed.
internal sealed class LoggingReceiveMiddleware(
    ILogger<LoggingReceiveMiddleware> logger)
{
    public async ValueTask InvokeAsync(
        IReceiveContext context,
        ReceiveDelegate next)
    {
        logger.LogInformation(
            "Receiving {MessageId}",
            context.MessageId);

        await next(context);

        logger.LogInformation(
            "Finished {MessageId}",
            context.MessageId);
    }

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var logger = context.Services
                    .GetRequiredService<ILogger<LoggingReceiveMiddleware>>();
                var middleware = new LoggingReceiveMiddleware(logger);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Logging");
}
