using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Provides the built-in receive middleware configurations that form the default receive pipeline.
/// </summary>
public static class ReceiveMiddlewares
{
    /// <summary>
    /// The transport-level circuit breaker middleware configuration.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration TransportCircuitBreaker =
        TransportCircuitBreakerMiddleware.Create();

    /// <summary>
    /// The concurrency limiter middleware configuration that throttles concurrent message processing.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration ConcurrencyLimiter = ConcurrencyLimiterMiddleware.Create();

    /// <summary>
    /// The instrumentation middleware configuration that emits telemetry for receive operations.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Instrumentation = ReceiveInstrumentationMiddleware.Create();

    /// <summary>
    /// The circuit breaker middleware configuration that stops message processing after repeated failures.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration CircuitBreaker = ReceiveCircuitBreakerMiddleware.Create();

    /// <summary>
    /// The dead-letter middleware configuration that routes unprocessable messages to a dead-letter queue.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration DeadLetter = ReceiveDeadLetterMiddleware.Create();

    /// <summary>
    /// The fault middleware configuration that handles message processing faults.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Fault = ReceiveFaultMiddleware.Create();

    /// <summary>
    /// The expiry middleware configuration that discards messages past their expiration time.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Expiry = ReceiveExpiryMiddleware.Create();

    /// <summary>
    /// The message type selection middleware configuration that resolves the CLR message type from the envelope.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration MessageTypeSelection =
        MessageTypeSelectionMiddleware.Create();

    /// <summary>
    /// The redelivery middleware configuration that reschedules failed messages for later delivery.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Redelivery = ReceiveRedeliveryMiddleware.Create();

    /// <summary>
    /// The routing middleware configuration that dispatches messages to the appropriate consumer.
    /// </summary>
    public static readonly ReceiveMiddlewareConfiguration Routing = RoutingMiddleware.Create();
}
