// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Resources.AspNetCore@1.0.0-preview.*
// #:package Mocha.Transport.RabbitMQ@1.0.0-preview.*
// $ dotnet run RabbitMQ.cs

using Mocha;
using Mocha.Transport.RabbitMQ;
using RabbitMQ.Client;
using Mocha.Resources.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register the RabbitMQ connection factory.
// In production, use Aspire's builder.AddRabbitMQClient("rabbitmq") which reads
// from configuration and provides health checks and dashboard integration.
builder.Services.AddSingleton<IConnectionFactory>(_ =>
    new ConnectionFactory
    {
        HostName = "localhost",
        Port = 5672,
        VirtualHost = "/",
        UserName = "guest",
        Password = "guest"
    });

builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedHandler>()
    .AddRabbitMQ(transport =>
    {
        // BindHandlersExplicitly() means you control exactly which handlers
        // go to which endpoints. Auto-discovery is disabled.
        transport.BindHandlersExplicitly();

        // Configure a receive endpoint with a quorum queue for production use.
        // Quorum queues replicate across nodes via Raft consensus and are the
        // recommended queue type since RabbitMQ 4.0 (classic mirrored queues removed).
        transport.Endpoint("order-processing")
            .Queue("orders.processing")
            // MaxPrefetch controls how many unacknowledged messages RabbitMQ
            // delivers at once. For quorum queues, avoid prefetch=1 as it
            // starves consumers while acknowledgements flow through consensus.
            .MaxPrefetch(50)
            // MaxConcurrency controls how many messages are processed in parallel.
            .MaxConcurrency(10)
            .Handler<OrderPlacedHandler>();

        // Declare a quorum queue explicitly with durable flag.
        // Quorum queues require durable=true - non-durable quorum queues are not supported.
        transport.DeclareQueue("orders.processing")
            .Durable()
            .AutoProvision()
            .QueueType("quorum")
            // QuorumInitialGroupSize sets the number of replicas (min 3 for production).
            .QuorumInitialGroupSize(3)
            // MaxDeliveryLimit dead-letters messages that exceed the delivery attempt limit.
            .MaxDeliveryLimit(5);

        // Declare a fanout exchange for order events.
        transport.DeclareExchange("order-events")
            .Type(RabbitMQExchangeType.Fanout)
            .Durable()
            .AutoProvision();

        // Declare a dead-letter exchange for faulted messages.
        transport.DeclareExchange("order-events.dlx")
            .Type(RabbitMQExchangeType.Fanout)
            .Durable()
            .AutoProvision();

        // Declare a dead-letter queue bound to the dead-letter exchange.
        transport.DeclareQueue("orders.processing.dlq")
            .Durable()
            .AutoProvision()
            .QueueType("quorum");

        // Bind the dead-letter exchange to the dead-letter queue.
        transport.DeclareBinding("order-events.dlx", "orders.processing.dlq")
            .AutoProvision();

        // Bind the order-events exchange to the order-processing queue.
        transport.DeclareBinding("order-events", "orders.processing")
            .AutoProvision();
    });

builder.Services.AddMochaMessageBusResources();

var app = builder.Build();

app.MapGet("/orders", async (IMessageBus bus) =>
{
    var orderId = Guid.NewGuid();

    await bus.PublishAsync(
        new OrderPlaced(orderId, "Wireless Headphones", 299.99m),
        CancellationToken.None);

    return Results.Ok(new { OrderId = orderId, Status = "Published" });
});

if (app.Environment.IsDevelopment())
{
    app.MapMochaResourceEndpoint();
}

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
