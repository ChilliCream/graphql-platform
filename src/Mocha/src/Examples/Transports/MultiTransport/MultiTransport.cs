// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run MultiTransport.cs

using Mocha;
using Mocha.Transport.InMemory;
using Mocha.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedHandler>()
    .AddEventHandler<AuditHandler>()
    // First InMemory transport — the default transport for most handlers.
    // Handlers are discovered and bound automatically using default conventions.
    .AddInMemory(transport =>
    {
        transport.IsDefaultTransport(); // All unrouted handlers go here
        transport.Name("primary");
    })
    // Second InMemory transport — dedicated to the audit pipeline.
    // BindHandlersExplicitly() means only handlers explicitly assigned via
    // .Handler<T>() on an endpoint descriptor are bound to this transport.
    .AddInMemory(transport =>
    {
        transport.Name("audit");
        transport.BindHandlersExplicitly();

        // Route AuditHandler exclusively to this transport's endpoint.
        // OrderPlacedHandler is NOT bound here — it runs on the primary transport.
        transport.Endpoint("audit-queue")
            .Handler<AuditHandler>();
    });

var app = builder.Build();

app.MapGet("/orders", async (IMessageBus bus) =>
{
    var orderId = Guid.NewGuid();

    // Publish routes through the default (primary) transport to OrderPlacedHandler.
    await bus.PublishAsync(
        new OrderPlaced(orderId, "Mechanical Keyboard", 149.99m),
        CancellationToken.None);

    // AuditEvent routes through the audit transport to AuditHandler.
    await bus.PublishAsync(
        new AuditEvent(orderId, "OrderPlaced"),
        CancellationToken.None);

    return Results.Ok(new { OrderId = orderId, Status = "Published" });
});

if (app.Environment.IsDevelopment())
{
    app.MapMessageBusDeveloperTopology();
}

app.Run();

// --- Domain ---

public sealed record OrderPlaced(Guid OrderId, string ProductName, decimal Amount);

public sealed record AuditEvent(Guid CorrelationId, string EventName);

// --- Handlers ---

public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger)
    : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[primary] Order {OrderId} — {ProductName} for {Amount:C}",
            message.OrderId,
            message.ProductName,
            message.Amount);

        return ValueTask.CompletedTask;
    }
}

// AuditHandler is bound exclusively to the audit transport.
// It receives AuditEvent messages published to that transport.
public class AuditHandler(ILogger<AuditHandler> logger)
    : IEventHandler<AuditEvent>
{
    public ValueTask HandleAsync(
        AuditEvent message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[audit] Audit event: {EventName} for correlation {CorrelationId}",
            message.EventName,
            message.CorrelationId);

        return ValueTask.CompletedTask;
    }
}
