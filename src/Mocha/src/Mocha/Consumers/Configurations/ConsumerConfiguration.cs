namespace Mocha;

/// <summary>
/// Holds the resolved configuration for a consumer, including its name, inbound routes, and
/// consumer-scoped middleware pipeline.
/// </summary>
public class ConsumerConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets or sets the logical name of the consumer.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the list of inbound route configurations that determine which message types
    /// this consumer handles.
    /// </summary>
    public List<InboundRouteConfiguration> Routes { get; set; } = [];

    /// <summary>
    /// Gets or sets the consumer-scoped middleware configurations executed during message
    /// consumption.
    /// </summary>
    public List<ConsumerMiddlewareConfiguration> ConsumerMiddlewares { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of pipeline modifiers that can reorder or replace consumer middleware
    /// at build time.
    /// </summary>
    public List<Action<List<ConsumerMiddlewareConfiguration>>> ConsumerPipelineModifiers { get; set; } = [];
}
