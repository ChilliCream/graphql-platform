namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Configuration collected by <see cref="IRabbitMQQueueDescriptor"/>.
/// </summary>
public sealed class RabbitMQQueueDescriptorConfiguration : MessagingConfiguration
{
    internal RabbitMQQueueDescriptorConfiguration(string name)
    {
        Name = name;
        Queue = new RabbitMQQueueConfiguration
        {
            Name = name,
            Origin = TopologyOrigin.Declared
        };
    }

    /// <summary>
    /// Gets or sets the queue name, which also serves as the default receive endpoint name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets the backing queue topology configuration.
    /// </summary>
    public RabbitMQQueueConfiguration Queue { get; }

    /// <summary>
    /// Gets the consumer identity types explicitly bound to this queue.
    /// </summary>
    public List<Type> ConsumerIdentities { get; } = [];

    /// <summary>
    /// Gets the message types received by this queue.
    /// </summary>
    public List<Type> ReceivedMessageTypes { get; } = [];

    /// <summary>
    /// Gets or sets the queue-scoped bind mode.
    /// </summary>
    public MessagingBindMode? BindMode { get; set; }

    /// <summary>
    /// Gets or sets the receive endpoint kind when this queue materializes an endpoint.
    /// </summary>
    public ReceiveEndpointKind? Kind { get; set; }

    /// <summary>
    /// Gets or sets the receive endpoint maximum concurrency.
    /// </summary>
    public int? MaxConcurrency { get; set; }

    /// <summary>
    /// Gets or sets the RabbitMQ prefetch limit.
    /// </summary>
    public ushort? MaxPrefetch { get; set; }

    /// <summary>
    /// Gets or sets whether this queue's receive endpoint is temporary, scoping its infrastructure
    /// to the lifetime of the consuming process.
    /// </summary>
    public bool IsTemporary { get; set; }

    /// <summary>
    /// Gets the receive middleware configurations applied when this queue materializes an endpoint.
    /// </summary>
    public List<ReceiveMiddlewareConfiguration> ReceiveMiddlewares { get; } = [];

    /// <summary>
    /// Gets the receive pipeline modifiers applied when this queue materializes an endpoint.
    /// </summary>
    public List<Action<List<ReceiveMiddlewareConfiguration>>> ReceivePipelineModifiers { get; } = [];

    /// <summary>
    /// Gets the source exchange bindings declared from this queue descriptor.
    /// </summary>
    public List<RabbitMQQueueSourceBindingConfiguration> SourceBindings { get; } = [];
}
