namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Descriptor implementation for configuring a RabbitMQ exchange.
/// </summary>
internal sealed class RabbitMQExchangeTopologyDescriptor
    : MessagingDescriptorBase<RabbitMQExchangeConfiguration>
    , IRabbitMQExchangeTopologyDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQExchangeTopologyDescriptor"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The initial exchange name.</param>
    public RabbitMQExchangeTopologyDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new RabbitMQExchangeConfiguration { Name = name, Origin = TopologyOrigin.Declared };
    }

    /// <inheritdoc />
    protected internal override RabbitMQExchangeConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IRabbitMQExchangeTopologyDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeTopologyDescriptor Type(string type)
    {
        Configuration.Type = type;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeTopologyDescriptor Durable(bool durable = true)
    {
        Configuration.Durable = durable;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeTopologyDescriptor AutoDelete(bool autoDelete = true)
    {
        Configuration.AutoDelete = autoDelete;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeTopologyDescriptor WithArgument(string key, object value)
    {
        Configuration.Arguments ??= new Dictionary<string, object>();
        Configuration.Arguments[key] = value;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeTopologyDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <summary>
    /// Creates the final exchange configuration.
    /// </summary>
    /// <returns>The configured exchange configuration.</returns>
    public RabbitMQExchangeConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new exchange descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The exchange name.</param>
    /// <returns>A new exchange descriptor.</returns>
    public static RabbitMQExchangeTopologyDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
