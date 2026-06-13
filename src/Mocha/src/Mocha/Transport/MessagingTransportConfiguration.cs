namespace Mocha;

/// <summary>
/// Base configuration for a messaging transport, specifying name, schema, endpoints, middleware, and transport options.
/// </summary>
public abstract class MessagingTransportConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets or sets the transport name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the URI scheme used by this transport (e.g., "rabbitmq", "inmemory").
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets the consumer binding mode that controls how consumers are mapped to receive endpoints.
    /// </summary>
    public ConsumerBindingMode ConsumerBindingMode { get; set; } = ConsumerBindingMode.Implicit;

    /// <summary>
    /// Gets or sets a value indicating whether auto-binding is enabled at the transport scope.
    /// A tri-state value: null (unset, defaults to true), true (enabled), or false (disabled).
    /// Auto-binding generates convention binds for consumed message types that reach this queue.
    /// </summary>
    public bool? AutoBind { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the default transport for routing when multiple transports are registered.
    /// In multi-transport applications, exactly one transport must be marked as the default;
    /// the default transport is used as the fallback destination when no explicit transport is specified.
    /// If no transport is marked as default in a multi-transport setup, or if a scheme-qualified destination claims ambiguously, the build will fail.
    /// Single-transport applications do not require this flag to be set.
    /// </summary>
    public bool IsDefaultTransport { get; set; }

    /// <summary>
    /// Gets or sets the transport-specific conventions.
    /// </summary>
    public List<IConvention> Conventions { get; set; } = [];

    /// <summary>
    /// Gets or sets the receive endpoint configurations for this transport.
    /// </summary>
    public List<ReceiveEndpointConfiguration> ReceiveEndpoints { get; set; } = [];

    /// <summary>
    /// Gets or sets the dispatch endpoint configurations for this transport.
    /// </summary>
    public List<DispatchEndpointConfiguration> DispatchEndpoints { get; set; } = [];

    /// <summary>
    /// Gets or sets the dispatch middleware configurations for this transport.
    /// </summary>
    public List<DispatchMiddlewareConfiguration> DispatchMiddlewares { get; set; } = [];

    /// <summary>
    /// Gets or sets the modifiers for the dispatch middleware pipeline.
    /// </summary>
    public List<Action<List<DispatchMiddlewareConfiguration>>> DispatchPipelineModifiers { get; set; } = [];

    /// <summary>
    /// Gets or sets the receive middleware configurations for this transport.
    /// </summary>
    public List<ReceiveMiddlewareConfiguration> ReceiveMiddlewares { get; set; } = [];

    /// <summary>
    /// Gets or sets the modifiers for the receive middleware pipeline.
    /// </summary>
    public List<Action<List<ReceiveMiddlewareConfiguration>>> ReceivePipelineModifiers { get; set; } = [];

    /// <summary>
    /// Gets or sets the transport-level options including content type and circuit breaker settings.
    /// </summary>
    public TransportOptions Options { get; set; } = new();
}
