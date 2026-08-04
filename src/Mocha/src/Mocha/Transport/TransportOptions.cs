namespace Mocha;

/// <summary>
/// Mutable transport-level configuration options for content type and circuit breaker settings.
/// </summary>
public class TransportOptions : IReadOnlyTransportOptions
{
    /// <summary>
    /// Gets or sets the default content type for message serialization on this transport.
    /// </summary>
    public MessageContentType? DefaultContentType { get; set; }

    /// <summary>
    /// Transport circuit breaker options <see cref="TransportCircuitBreakerMiddleware"/>.
    /// </summary>
    public TransportCircuitBreakerOptions CircuitBreaker { get; set; } = new();

    IReadOnlyTransportCircuitBreakerOptions IReadOnlyTransportOptions.CircuitBreaker => CircuitBreaker;
}
