namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Descriptor implementation for configuring a RabbitMQ binding.
/// </summary>
internal sealed class RabbitMQBindingDescriptor
    : MessagingDescriptorBase<RabbitMQBindingConfiguration>
    , IRabbitMQBindingDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQBindingDescriptor"/> class.
    /// </summary>
    public RabbitMQBindingDescriptor(IMessagingConfigurationContext context, string source, string destination)
        : base(context)
    {
        Configuration = new RabbitMQBindingConfiguration { Source = source, Destination = destination };
    }

    /// <inheritdoc />
    protected internal override RabbitMQBindingConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IRabbitMQBindingDescriptor ToQueue(string queueOrExchangeName)
    {
        Configuration.Destination = queueOrExchangeName;
        Configuration.DestinationKind = RabbitMQDestinationKind.Queue;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQBindingDescriptor ToExchange(string exchangeName)
    {
        Configuration.Destination = exchangeName;
        Configuration.DestinationKind = RabbitMQDestinationKind.Exchange;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQBindingDescriptor RoutingKey(string routingKey)
    {
        Configuration.RoutingKey = routingKey;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQBindingDescriptor WithArgument(string key, object value)
    {
        Configuration.Arguments ??= new Dictionary<string, object>();
        Configuration.Arguments[key] = value;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQBindingDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <summary>
    /// Creates the final binding configuration.
    /// </summary>
    /// <returns>The configured binding configuration.</returns>
    public RabbitMQBindingConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new binding descriptor with the specified source and destination.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="source">The source exchange name.</param>
    /// <param name="destination">The destination queue or exchange name.</param>
    /// <returns>A new binding descriptor.</returns>
    public static RabbitMQBindingDescriptor New(
        IMessagingConfigurationContext context,
        string source,
        string destination)
        => new(context, source, destination);
}
