namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Descriptor implementation for configuring a RabbitMQ exchange.
/// </summary>
internal sealed class RabbitMQExchangeDescriptor
    : MessagingDescriptorBase<RabbitMQExchangeConfiguration>
    , IRabbitMQExchangeDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQExchangeDescriptor"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The initial exchange name.</param>
    public RabbitMQExchangeDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new RabbitMQExchangeConfiguration { Name = name };
    }

    /// <inheritdoc />
    protected internal override RabbitMQExchangeConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IRabbitMQExchangeDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeDescriptor Type(string type)
    {
        Configuration.Type = type;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeDescriptor Durable(bool durable = true)
    {
        Configuration.Durable = durable;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeDescriptor AutoDelete(bool autoDelete = true)
    {
        Configuration.AutoDelete = autoDelete;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeDescriptor WithArgument(string key, object value)
    {
        Configuration.Arguments ??= new Dictionary<string, object>();
        Configuration.Arguments[key] = value;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeDescriptor AutoProvision(bool autoProvision = true)
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
    public static RabbitMQExchangeDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
