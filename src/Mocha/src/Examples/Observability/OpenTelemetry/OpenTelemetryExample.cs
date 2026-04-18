// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run OpenTelemetry.cs

using Mocha;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;
using Mocha.Hosting;

// To collect Mocha spans and metrics with the .NET OpenTelemetry SDK, add
// the packages below and configure tracing/metrics as shown in the comments.
//
// dotnet add package OpenTelemetry.Extensions.Hosting
// dotnet add package OpenTelemetry.Instrumentation.AspNetCore
//
// Then configure OpenTelemetry before registering the bus:
//
//   builder.Services
//       .AddOpenTelemetry()
//       .WithTracing(tracing =>
//       {
//           tracing
//               .AddSource("Mocha")            // Subscribe to Mocha spans
//               .AddAspNetCoreInstrumentation();
//       })
//       .WithMetrics(metrics =>
//       {
//           metrics
//               .AddMeter("Mocha")             // Subscribe to Mocha metrics
//               .AddAspNetCoreInstrumentation();
//       });

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    // AddInstrumentation() registers the built-in ActivityMessagingDiagnosticListener.
    // Without this call, Mocha uses a no-op listener with zero overhead.
    // Spans are emitted to the "Mocha" activity source - subscribe via AddSource("Mocha").
    .AddInstrumentation()
    // Register a custom listener alongside the built-in one for application-level telemetry.
    // Multiple listeners compose automatically - no manual aggregation needed.
    .AddDiagnosticEventListener<ConsoleDiagnosticObserver>()
    .AddEventHandler<OrderPlacedHandler>()
    .AddInMemory();

var app = builder.Build();

app.MapGet("/orders", async (IMessageBus bus) =>
{
    var orderId = Guid.NewGuid();

    // Three linked spans are created per message flow:
    //   1. "publish {destination}" - created by dispatch instrumentation
    //   2. "receive {endpoint}"   - created by receive instrumentation
    //   3. "consumer {handler}"   - created as a child of the receive span
    await bus.PublishAsync(
        new OrderPlaced(orderId, "Mechanical Keyboard", 149.99m),
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

// --- Handlers ---

public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger)
    : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        // Log entries written here automatically include TraceId and SpanId
        // when OpenTelemetry logging is configured, enabling log-trace correlation.
        logger.LogInformation(
            "Order received: {OrderId} - {ProductName} for {Amount:C}",
            message.OrderId,
            message.ProductName,
            message.Amount);

        return ValueTask.CompletedTask;
    }
}

// --- Custom diagnostic listener ---

// Extend MessagingDiagnosticEventListener to collect telemetry or integrate with a
// non-OpenTelemetry backend. Override only the methods you care about - the base
// class provides no-op defaults for the rest. Each scope method returns an
// IDisposable whose disposal marks the end of the observed scope.
public sealed class ConsoleDiagnosticObserver : MessagingDiagnosticEventListener
{
    public override IDisposable Dispatch(IDispatchContext context)
    {
        var startTime = DateTimeOffset.UtcNow;
        Console.WriteLine($"[Dispatch] -> {context.DestinationAddress}");

        return new Scope(() =>
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            Console.WriteLine($"[Dispatch] completed in {duration.TotalMilliseconds:F1}ms");
        });
    }

    public override IDisposable Receive(IReceiveContext context)
    {
        Console.WriteLine($"[Receive]  <- {context.Endpoint.Address}");
        return new Scope(() => Console.WriteLine("[Receive]  completed"));
    }

    public override IDisposable Consume(IConsumeContext context)
    {
        Console.WriteLine($"[Consume]  message {context.MessageId}");
        return new Scope(() => Console.WriteLine("[Consume]  completed"));
    }

    public override void DispatchError(IDispatchContext context, Exception exception)
        => Console.WriteLine($"[Dispatch] error: {exception.Message}");

    public override void ReceiveError(IReceiveContext context, Exception exception)
        => Console.WriteLine($"[Receive]  error: {exception.Message}");

    public override void ConsumeError(IConsumeContext context, Exception exception)
        => Console.WriteLine($"[Consume]  error: {exception.Message}");

    private sealed class Scope(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
