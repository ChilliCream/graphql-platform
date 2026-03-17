namespace Mocha;

/// <summary>
/// Holds the resolved configuration for a receive endpoint, including its consumer bindings, error handling, and receive-scoped middleware pipeline.
/// </summary>
public class ReceiveEndpointConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets or sets the logical name of the receive endpoint.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the kind of receive endpoint, controlling how inbound messages are accepted.
    /// </summary>
    public ReceiveEndpointKind Kind { get; set; } = ReceiveEndpointKind.Default;

    /// <summary>
    /// Gets or sets the URI of the error/fault endpoint where failed messages are forwarded.
    /// </summary>
    public Uri? ErrorEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the URI of the endpoint where skipped messages are forwarded.
    /// </summary>
    public Uri? SkippedEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the list of consumer identity types explicitly bound to this endpoint.
    /// </summary>
    public List<Type> ConsumerIdentities { get; set; } = [];

    /// <summary>
    /// Gets or sets whether this is a temporary (auto-delete) endpoint.
    /// </summary>
    public bool IsTemporary { get; set; }

    /// <summary>
    /// Gets or sets whether the transport should automatically provision infrastructure for this endpoint.
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages that can be processed concurrently on this endpoint.
    /// </summary>
    public int? MaxConcurrency { get; set; }

    /// <summary>
    /// Gets or sets the receive-scoped middleware configurations executed during message reception.
    /// </summary>
    public List<ReceiveMiddlewareConfiguration> ReceiveMiddlewares { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of pipeline modifiers that can reorder or replace receive middleware at build time.
    /// </summary>
    public List<Action<List<ReceiveMiddlewareConfiguration>>> ReceivePipelineModifiers { get; set; } = [];

    public static class Defaults
    {
        /// <summary>
        /// The default maximum concurrency for receive endpoints, set to the number of available processors.
        /// </summary>
        public static int MaxConcurrency = Environment.ProcessorCount * 2;
    }
}
