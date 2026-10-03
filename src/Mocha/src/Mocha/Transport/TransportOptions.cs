namespace Mocha;

/// <summary>
/// Mutable transport-level configuration options for content type, circuit breaker and shutdown settings.
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

    /// <summary>
    /// Gets or sets the options that bound how long stopping this transport takes.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if the value is <see langword="null"/>.</exception>
    public TransportShutdownOptions Shutdown
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = new();

    IReadOnlyTransportCircuitBreakerOptions IReadOnlyTransportOptions.CircuitBreaker => CircuitBreaker;

    IReadOnlyTransportShutdownOptions IReadOnlyTransportOptions.Shutdown => Shutdown;
}
