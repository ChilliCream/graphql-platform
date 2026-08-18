using ChilliCream.Nitro;
using Mocha;
using Mocha.Transport.AzureServiceBus;
using AzureServiceBusTransport.Contracts.Commands;
using AzureServiceBusTransport.Contracts.Events;
using AzureServiceBusTransport.ShippingService.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureServiceBusClient("messaging");
builder.Services.AddNitro().AddMocha();

var administrationConnectionString = builder.Configuration.GetConnectionString("messaging-administration");

builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .Host(h => h.InstanceId(Guid.Parse("00000000-0000-0000-0000-000000000002")))
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddRequestHandler<PrepareShipmentRequestHandler>()
    .AddAzureServiceBus(t =>
    {
        // Aspire does not register the emulator's separate administration client.
        if (administrationConnectionString is not null)
        {
            t.AdministrationConnectionString(administrationConnectionString);
        }

        // Receive PrepareShipmentCommand from the order fulfillment saga
        t.DeclareQueue("prepare-shipment");
        t.Endpoint("prepare-shipment-ep")
            .Queue("prepare-shipment")
            .Handler<PrepareShipmentRequestHandler>();
    });

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Shipping Service (Azure Service Bus Transport)");

// Ship an order - publishes OrderShippedEvent
app.MapPost(
    "/api/shipments",
    async (CreateShipmentRequest request, IMessageBus messageBus) =>
    {
        var trackingNumber = $"TRK-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

        await messageBus.PublishAsync(
            new OrderShippedEvent
            {
                OrderId = request.OrderId,
                TrackingNumber = trackingNumber,
                Carrier = request.Carrier,
                ShippedAt = DateTimeOffset.UtcNow
            },
            CancellationToken.None);

        return Results.Ok(new
        {
            request.OrderId,
            trackingNumber,
            request.Carrier,
            status = "Shipped"
        });
    });

app.Run();

public record CreateShipmentRequest(Guid OrderId, string Carrier);
