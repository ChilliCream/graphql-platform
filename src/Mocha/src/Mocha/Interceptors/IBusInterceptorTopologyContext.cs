namespace Mocha;

/// <summary>
/// Provides access to the messaging topology model for a transport during interceptor processing.
/// Interceptors use this context to inspect and modify topology entities such as exchanges, queues, and topics.
/// </summary>
public interface IBusInterceptorTopologyContext
{
    /// <summary>
    /// Gets the messaging transport for which this topology context is provided.
    /// </summary>
    MessagingTransport Transport { get; }

    /// <summary>
    /// Gets the service provider used to resolve dependencies.
    /// </summary>
    IServiceProvider Services { get; }
}
