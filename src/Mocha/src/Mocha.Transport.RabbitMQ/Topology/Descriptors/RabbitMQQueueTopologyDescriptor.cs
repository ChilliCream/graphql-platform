namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Descriptor implementation for configuring a RabbitMQ queue.
/// </summary>
internal sealed class RabbitMQQueueTopologyDescriptor
    : MessagingDescriptorBase<RabbitMQQueueConfiguration>
    , IRabbitMQQueueTopologyDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQQueueTopologyDescriptor"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The initial queue name.</param>
    public RabbitMQQueueTopologyDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new RabbitMQQueueConfiguration { Name = name, Origin = TopologyOrigin.Declared };
    }

    /// <inheritdoc />
    protected internal override RabbitMQQueueConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IRabbitMQQueueTopologyDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueTopologyDescriptor Durable(bool durable = true)
    {
        Configuration.Durable = durable;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueTopologyDescriptor Exclusive(bool exclusive = true)
    {
        Configuration.Exclusive = exclusive;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueTopologyDescriptor AutoDelete(bool autoDelete = true)
    {
        Configuration.AutoDelete = autoDelete;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueTopologyDescriptor WithArgument(string key, object value)
    {
        Configuration.Arguments ??= new Dictionary<string, object>();
        Configuration.Arguments[key] = value;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQQueueTopologyDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <summary>
    /// Creates the final queue configuration.
    /// </summary>
    /// <returns>The configured queue configuration.</returns>
    public RabbitMQQueueConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new queue descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The queue name.</param>
    /// <returns>A new queue descriptor.</returns>
    public static RabbitMQQueueTopologyDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
