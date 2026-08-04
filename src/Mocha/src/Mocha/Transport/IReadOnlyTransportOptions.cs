namespace Mocha;

/// <summary>
/// Provides read-only access to transport-level configuration options.
/// </summary>
public interface IReadOnlyTransportOptions
{
    /// <summary>
    /// Gets the default content type for message serialization on this transport, or <c>null</c> to use the system default.
    /// </summary>
    MessageContentType? DefaultContentType { get; }

    /// <summary>
    /// Gets the transport-level circuit breaker options.
    /// </summary>
    IReadOnlyTransportCircuitBreakerOptions CircuitBreaker { get; }
}
