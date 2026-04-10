// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run CircuitBreaker.cs

using Mocha;
using Mocha.Transport.InMemory;
using Mocha.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    // Circuit breaker stops processing messages when the failure rate exceeds
    // FailureRatio during a SamplingDuration window. After BreakDuration elapses,
    // one test message is allowed through to check if the handler has recovered.
    .AddCircuitBreaker(opts =>
    {
        opts.FailureRatio = 0.5;          // Open when 50%+ of messages fail
        opts.MinimumThroughput = 5;       // Evaluate only after 5 messages
        opts.SamplingDuration = TimeSpan.FromSeconds(10);
        opts.BreakDuration = TimeSpan.FromSeconds(30);
    })
    // Concurrency limiter gates how many messages run through the pipeline in
    // parallel. Tune this based on your handler's resource requirements.
    .AddConcurrencyLimiter(opts => opts.MaxConcurrency = 10)
    .AddEventHandler<OrderPlacedHandler>()
    .AddInMemory();

var app = builder.Build();

app.MapGet("/orders", async (IMessageBus bus) =>
{
    var orderId = Guid.NewGuid();

    await bus.PublishAsync(
        new OrderPlaced(orderId, "Wireless Headphones", 299.99m),
        CancellationToken.None);

    return Results.Ok(new { OrderId = orderId, Status = "Published" });
});

// Publish a price quote with a 5-minute expiry.
// If the message sits in the queue longer than 5 minutes, the expiry middleware
// drops it before any deserialization or handler work runs.
app.MapGet("/quotes", async (IMessageBus bus) =>
{
    await bus.PublishAsync(
        new PriceQuote("MSFT", 425.30m),
        new PublishOptions
        {
            ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(5)
        },
        CancellationToken.None);

    return Results.Ok(new { Status = "Quote published with 5-minute expiry" });
});

if (app.Environment.IsDevelopment())
{
    app.MapMessageBusDeveloperTopology();
}

app.Run();

// --- Domain ---

public sealed record OrderPlaced(Guid OrderId, string ProductName, decimal Amount);

public sealed record PriceQuote(string Ticker, decimal Price);

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
