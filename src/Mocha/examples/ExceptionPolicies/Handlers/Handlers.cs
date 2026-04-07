using ExceptionPolicies.Exceptions;
using ExceptionPolicies.Messages;
using Mocha;

namespace ExceptionPolicies.Handlers;

/// <summary>
/// Simulates a flaky payment gateway that succeeds after 3 failures.
/// Policy: Retry 5x with exponential backoff.
/// </summary>
public class ProcessPaymentHandler : IEventHandler<ProcessPayment>
{
    private static int _attempts;

    public ValueTask HandleAsync(ProcessPayment message, CancellationToken cancellationToken)
    {
        var attempt = Interlocked.Increment(ref _attempts);
        Console.WriteLine($"[Payment] Attempt {attempt} for order {message.OrderId}");

        if (attempt <= 3)
        {
            throw new PaymentGatewayException("Gateway timeout");
        }

        Console.WriteLine($"[Payment] Successfully processed order {message.OrderId}");
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Receives a message with an invalid payload.
/// Policy: DeadLetter immediately  the message is permanently bad.
/// </summary>
public class ValidateOrderHandler : IEventHandler<ValidateOrder>
{
    public ValueTask HandleAsync(ValidateOrder message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Validate] Validating order {message.OrderId}");
        throw new MessageValidationException($"Order {message.OrderId} has invalid schema");
    }
}

/// <summary>
/// Detects a duplicate message that was already processed.
/// Policy: Discard silently  no retry, no dead-letter.
/// </summary>
public class DeduplicateMessageHandler : IEventHandler<DeduplicateMessage>
{
    public ValueTask HandleAsync(DeduplicateMessage message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Dedup] Message {message.MessageId} already processed");
        throw new DuplicateMessageException($"Message {message.MessageId} is a duplicate");
    }
}

/// <summary>
/// Calls an external API that is completely down.
/// Policy: Retry 5x aggressively, then redeliver with increasing intervals, then dead-letter.
/// </summary>
public class CallExternalApiHandler : IEventHandler<CallExternalApi>
{
    private static int _attempts;

    public ValueTask HandleAsync(CallExternalApi message, CancellationToken cancellationToken)
    {
        var attempt = Interlocked.Increment(ref _attempts);
        Console.WriteLine($"[ExternalApi] Attempt {attempt} for {message.Url}");
        throw new ExternalServiceUnavailableException($"Service at {message.Url} is down");
    }
}

/// <summary>
/// Service with an expired auth token.
/// Policy: Redeliver only (skip retry)  immediate retry won't help.
/// </summary>
public class RefreshAuthTokenHandler : IEventHandler<RefreshAuthToken>
{
    private static int _attempts;

    public ValueTask HandleAsync(RefreshAuthToken message, CancellationToken cancellationToken)
    {
        var attempt = Interlocked.Increment(ref _attempts);
        Console.WriteLine($"[Auth] Attempt {attempt} for service {message.Service}");
        throw new AuthTokenExpiredException($"Token for {message.Service} expired");
    }
}

/// <summary>
/// Transient database failure during batch processing.
/// Policy: Retry 3x quickly, then escalate to redelivery.
/// </summary>
public class ProcessBatchHandler : IEventHandler<ProcessBatch>
{
    private static int _attempts;

    public ValueTask HandleAsync(ProcessBatch message, CancellationToken cancellationToken)
    {
        var attempt = Interlocked.Increment(ref _attempts);
        Console.WriteLine($"[Batch] Attempt {attempt} for batch {message.BatchId}");

        if (attempt <= 4)
        {
            throw new TransientDatabaseException("Connection pool exhausted");
        }

        Console.WriteLine($"[Batch] Successfully processed batch {message.BatchId}");
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Ingests telemetry data from a device, encountering various HTTP errors.
/// Policy: Conditional  different behavior for 404, 429, and 503 status codes.
/// </summary>
public class IngestTelemetryHandler : IEventHandler<IngestTelemetry>
{
    private static int _attempts;

    public ValueTask HandleAsync(IngestTelemetry message, CancellationToken cancellationToken)
    {
        var attempt = Interlocked.Increment(ref _attempts);
        Console.WriteLine($"[Telemetry] Attempt {attempt} for device {message.DeviceId}");

        // Rotate through different HTTP errors to demonstrate conditional policies
        throw (attempt % 3) switch
        {
            0 => new HttpServiceException("Not Found", 404),
            1 => new HttpServiceException("Too Many Requests", 429),
            _ => new HttpServiceException("Service Unavailable", 503)
        };
    }
}

/// <summary>
/// Handles a corrupt/unparseable message.
/// Policy: Retry once (in case of transient parse issue), then dead-letter immediately.
/// </summary>
public class HandlePoisonMessageHandler : IEventHandler<HandlePoisonMessage>
{
    private static int _attempts;

    public ValueTask HandleAsync(HandlePoisonMessage message, CancellationToken cancellationToken)
    {
        var attempt = Interlocked.Increment(ref _attempts);
        Console.WriteLine($"[Poison] Attempt {attempt} for data: {message.Data}");
        throw new PoisonMessageException("Payload cannot be deserialized");
    }
}
