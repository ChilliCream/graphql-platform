using Mocha;
using Mocha.Resources.AspNetCore;
using Mocha.Transport.Postgres;
using PostgresTransport.Contracts.Events;
using PostgresTransport.ShippingService.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Get the Postgres connection string from Aspire-injected configuration
var messagingConnectionString = builder.Configuration.GetConnectionString("messaging-db")
    ?? "Host=localhost;Database=mocha_messaging;Username=postgres;Password=postgres";

// MessageBus with PostgreSQL transport
builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddPostgres(t => t.ConnectionString(messagingConnectionString));

// Resource source diagnostics — exposes the message bus topology as Mocha resources.
builder.Services.AddMochaMessageBusResources();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Shipping Service (Postgres Transport)");

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

app.MapMochaResourceEndpoint();

app.Run();

public record CreateShipmentRequest(Guid OrderId, string Carrier);
