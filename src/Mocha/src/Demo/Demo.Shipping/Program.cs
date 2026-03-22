using Demo.Contracts.Events;
using Demo.Shipping.Commands;
using Demo.Shipping.Data;
using Demo.Shipping.Handlers;
using Demo.Shipping.Queries;
using Microsoft.EntityFrameworkCore;
using Mocha;
using Mocha.EntityFrameworkCore;
using Mocha.Mediator;
using Mocha.Inbox;
using Mocha.Outbox;
using Mocha.Transport.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Database
builder.AddNpgsqlDbContext<ShippingDbContext>("shipping-db");

// RabbitMQ
builder.AddRabbitMQClient("rabbitmq", x => x.DisableTracing = true);

// Mocha.Mediator
builder.Services.AddMediator()
    .AddShipping()
    .UseEntityFrameworkTransactions<ShippingDbContext>();

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
    {
        p.UsePostgresOutbox();
        p.UsePostgresInbox();
    })
    .AddRabbitMQ();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShippingDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// REST API Endpoints
app.MapGet("/", () => "Shipping Service");

// Shipments
app.MapGet("/api/shipments", async (ISender sender) =>
    await sender.QueryAsync(new GetShipmentsQuery()));

app.MapGet("/api/shipments/{id:guid}", async (Guid id, ISender sender) =>
    await sender.QueryAsync(new GetShipmentByIdQuery(id)) is { } shipment
        ? Results.Ok(shipment)
        : Results.NotFound());

app.MapGet("/api/shipments/order/{orderId:guid}", async (Guid orderId, ISender sender) =>
    await sender.QueryAsync(new GetShipmentByOrderIdQuery(orderId)) is { } shipment
        ? Results.Ok(shipment)
        : Results.NotFound());

// Ship a shipment
app.MapPost("/api/shipments/{id:guid}/ship", async (Guid id, ShipShipmentRequest request, ISender sender) =>
{
    var result = await sender.SendAsync(
        new ShipShipmentCommand(id, request.Carrier, request.EstimatedDays));

    if (!result.Success)
    {
        return result.Error == "Shipment not found"
            ? Results.NotFound(result.Error)
            : Results.BadRequest(result.Error);
    }

    return Results.Ok(result.Shipment);
});

// Return Shipments
app.MapGet("/api/returns", async (ISender sender) =>
    await sender.QueryAsync(new GetReturnShipmentsQuery()));

app.MapGet("/api/returns/{id:guid}", async (Guid id, ISender sender) =>
    await sender.QueryAsync(new GetReturnShipmentByIdQuery(id)) is { } returnShipment
        ? Results.Ok(returnShipment)
        : Results.NotFound());

app.MapGet("/api/returns/order/{orderId:guid}", async (Guid orderId, ISender sender) =>
    await sender.QueryAsync(new GetReturnShipmentByOrderIdQuery(orderId)) is { } returnShipment
        ? Results.Ok(returnShipment)
        : Results.NotFound());

// Receive return package
app.MapPost("/api/returns/{id:guid}/receive", async (Guid id, ISender sender) =>
{
    var result = await sender.SendAsync(new ReceiveReturnPackageCommand(id));

    if (!result.Success)
    {
        return result.Error == "Return shipment not found"
            ? Results.NotFound(result.Error)
            : Results.BadRequest(result.Error);
    }

    return Results.Ok(result.ReturnShipment);
});

app.Run();

public record ShipShipmentRequest(string Carrier, int EstimatedDays = 5);
