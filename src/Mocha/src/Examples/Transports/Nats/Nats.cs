// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Transport.Nats@1.0.0-preview.*
// $ dotnet run Nats.cs
//
// Requires a NATS server with JetStream enabled:
// $ docker run -p 4222:4222 nats:2.12-alpine -js

using System.Threading.Channels;
using Mocha;
using Mocha.Transport.Nats;
using NATS.Client.Core;

var builder = WebApplication.CreateBuilder(args);

// Register the NATS connection. In production, use Aspire's builder.AddNatsClient("nats"),
// which reads from configuration and provides health checks and dashboard integration.
builder.Services.AddSingleton<INatsConnection>(_ => new NatsConnection(new NatsOpts
{
    Url = "nats://localhost:4222",

    // NATS.Net drops messages when a subscriber falls behind. Request/reply responses arrive on a
    // core subscription, so leaving the default in place can silently lose them.
    SubPendingChannelFullMode = BoundedChannelFullMode.Wait
}));

var bus = builder.Services.AddMessageBus();

// Scopes durable consumer names, and would otherwise fall back to the entry assembly name.
bus.ConfigureMessageBus(mocha => mocha.Host(host => host.ServiceName("order-service")));

bus.AddEventHandler<OrderPlacedHandler>()
    .AddNats(nats =>
    {
        // Names the convention stream ORDER_SERVICE. Defaults to the host name above, so this call is
        // only needed when the stream should be named something else.
        nats.StreamName("order-service");

        // Naming the endpoint keeps the durable name off the handler type name. MaxConcurrency
        // bounds both parallel handling and how many messages are buffered locally.
        nats.Endpoint("order-processing")
            .Handler<OrderPlacedHandler>()
            .MaxConcurrency(10);

        // Settings the endpoint API does not expose live on the consumer declaration. Naming the
        // durable the endpoint already derives folds the two together rather than replacing it.
        nats.DeclareConsumer("order-processing")
            .AckWait(TimeSpan.FromSeconds(30))
            .MaxDeliver(5)
            .Backoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5))
            // Extends the acknowledgement deadline while a slow handler is still working, instead
            // of letting the message be redelivered underneath it.
            .AckProgressEvery(TimeSpan.FromSeconds(10));
    });

var app = builder.Build();

app.MapGet("/orders", async (IMessageBus messageBus) =>
{
    var orderId = Guid.NewGuid();

    await messageBus.PublishAsync(
        new OrderPlaced(orderId, "Wireless Headphones", 299.99m),
        CancellationToken.None);

    return Results.Ok(new { OrderId = orderId, Status = "Published" });
});

app.Run();

// --- Domain ---

public sealed record OrderPlaced(Guid OrderId, string ProductName, decimal Amount);

// --- Handlers ---

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
