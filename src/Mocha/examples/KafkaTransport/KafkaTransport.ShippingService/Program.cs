using Mocha;
using Mocha.Hosting;
using Mocha.Transport.Kafka;
using KafkaTransport.Contracts.Events;
using KafkaTransport.ShippingService.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Get the Kafka bootstrap servers from Aspire-injected configuration
var bootstrapServers = (builder.Configuration.GetConnectionString("kafka")
    ?? "localhost:9092")
    .Replace("localhost", "127.0.0.1");

// MessageBus with Kafka transport
builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddKafka(t =>
    {
        t.BootstrapServers(bootstrapServers);
        t.AutoProvision(true);
    });

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Shipping Service (Kafka Transport)");

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

app.MapMessageBusDeveloperTopology();

app.Run();

public record CreateShipmentRequest(Guid OrderId, string Carrier);
