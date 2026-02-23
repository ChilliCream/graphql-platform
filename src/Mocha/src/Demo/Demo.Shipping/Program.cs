using Demo.Contracts.Events;
using Demo.Shipping.Data;
using Demo.Shipping.Entities;
using Demo.Shipping.Handlers;
using Microsoft.EntityFrameworkCore;
using Mocha;
using Mocha.EntityFrameworkCore;
using Mocha.Outbox;
using Mocha.Transport.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Database
builder.AddNpgsqlDbContext<ShippingDbContext>("shipping-db");

// RabbitMQ
builder.AddRabbitMQClient("rabbitmq", x => x.DisableTracing = true);

// MessageBus
builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    // Event handlers
    .AddEventHandler<PaymentCompletedEventHandler>()
    // Request handlers
    .AddRequestHandler<GetShipmentStatusRequestHandler>()
    .AddRequestHandler<CreateReturnLabelCommandHandler>()
    .AddEntityFramework<ShippingDbContext>(p =>
        p.AddPostgresOutbox())
    .AddRabbitMQ();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShippingDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Start message bus runtime
var runtime = (MessagingRuntime)app.Services.GetRequiredService<IMessagingRuntime>();
await runtime.StartAsync(CancellationToken.None);

// REST API Endpoints
app.MapGet("/", () => "Shipping Service");

// Shipments
app.MapGet("/api/shipments", async (ShippingDbContext db) => await db.Shipments.Include(s => s.Items).ToListAsync());

app.MapGet(
    "/api/shipments/{id:guid}",
    async (Guid id, ShippingDbContext db) =>
        await db.Shipments.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == id) is { } shipment
            ? Results.Ok(shipment)
            : Results.NotFound());

app.MapGet(
    "/api/shipments/order/{orderId:guid}",
    async (Guid orderId, ShippingDbContext db) =>
        await db.Shipments.Include(s => s.Items).FirstOrDefaultAsync(s => s.OrderId == orderId) is { } shipment
            ? Results.Ok(shipment)
            : Results.NotFound());

// Ship a shipment - triggers ShipmentShippedEvent
app.MapPost(
    "/api/shipments/{id:guid}/ship",
    async (Guid id, ShipShipmentRequest request, ShippingDbContext db, IMessageBus messageBus) =>
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id);
        if (shipment is null)
            return Results.NotFound("Shipment not found");

        if (shipment.Status == ShipmentStatus.Shipped)
            return Results.BadRequest("Shipment already shipped");

        shipment.Status = ShipmentStatus.Shipped;
        shipment.Carrier = request.Carrier;
        shipment.ShippedAt = DateTimeOffset.UtcNow;
        shipment.EstimatedDelivery = DateTimeOffset.UtcNow.AddDays(request.EstimatedDays);
        await db.SaveChangesAsync();

        // Publish ShipmentShippedEvent
        await messageBus.PublishAsync(
            new ShipmentShippedEvent
            {
                ShipmentId = shipment.Id,
                OrderId = shipment.OrderId,
                TrackingNumber = shipment.TrackingNumber!,
                Carrier = shipment.Carrier,
                ShippedAt = shipment.ShippedAt.Value,
                EstimatedDelivery = shipment.EstimatedDelivery.Value
            },
            CancellationToken.None);

        return Results.Ok(shipment);
    });

// Return Shipments
app.MapGet("/api/returns", async (ShippingDbContext db) => await db.ReturnShipments.ToListAsync());

app.MapGet(
    "/api/returns/{id:guid}",
    async (Guid id, ShippingDbContext db) =>
        await db.ReturnShipments.FirstOrDefaultAsync(r => r.Id == id) is { } returnShipment
            ? Results.Ok(returnShipment)
            : Results.NotFound());

app.MapGet(
    "/api/returns/order/{orderId:guid}",
    async (Guid orderId, ShippingDbContext db) =>
        await db.ReturnShipments.FirstOrDefaultAsync(r => r.OrderId == orderId) is { } returnShipment
            ? Results.Ok(returnShipment)
            : Results.NotFound());

// Simulate return package received - triggers ReturnPackageReceivedEvent for saga
app.MapPost(
    "/api/returns/{id:guid}/receive",
    async (Guid id, ShippingDbContext db, IMessageBus messageBus, ILogger<Program> logger) =>
    {
        var returnShipment = await db.ReturnShipments.FirstOrDefaultAsync(r => r.Id == id);
        if (returnShipment is null)
            return Results.NotFound("Return shipment not found");

        if (returnShipment.Status == ReturnShipmentStatus.Received)
            return Results.BadRequest("Return package already received");

        returnShipment.Status = ReturnShipmentStatus.Received;
        returnShipment.ReceivedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Return package {ReturnId} received, publishing ReturnPackageReceivedEvent",
            returnShipment.Id);

        // Publish ReturnPackageReceivedEvent to start saga for inspection/refund
        await messageBus.PublishAsync(
            new ReturnPackageReceivedEvent
            {
                ReturnId = returnShipment.Id,
                OrderId = returnShipment.OrderId,
                TrackingNumber = returnShipment.TrackingNumber!,
                ReceivedAt = returnShipment.ReceivedAt.Value,
                // Include order details for saga processing
                ProductId = returnShipment.ProductId,
                Quantity = returnShipment.Quantity,
                Amount = returnShipment.Amount,
                CustomerId = returnShipment.CustomerId,
                Reason = returnShipment.Reason
            },
            CancellationToken.None);

        return Results.Ok(returnShipment);
    });

app.Run();

public record ShipShipmentRequest(string Carrier, int EstimatedDays = 5);
