namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Descriptor for configuring a RabbitMQ dispatch endpoint that targets a queue or exchange for outbound message delivery.
/// </summary>
internal sealed class RabbitMQDispatchEndpointDescriptor
    : DispatchEndpointDescriptor<RabbitMQDispatchEndpointConfiguration>
    , IRabbitMQDispatchEndpointDescriptor
{
    private RabbitMQDispatchEndpointDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new RabbitMQDispatchEndpointConfiguration { Name = name, ExchangeName = name };
    }

    protected internal override RabbitMQDispatchEndpointConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IRabbitMQDispatchEndpointDescriptor ToQueue(string name)
    {
        Configuration.QueueName = name;
        Configuration.ExchangeName = null;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQDispatchEndpointDescriptor ToExchange(string name)
    {
        Configuration.QueueName = null;
        Configuration.ExchangeName = name;
        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQDispatchEndpointDescriptor Send<TMessage>()
    {
        base.Send<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQDispatchEndpointDescriptor Publish<TMessage>()
    {
        base.Publish<TMessage>();
        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQDispatchEndpointDescriptor UseDispatch(DispatchMiddlewareConfiguration configuration)
    {
        base.UseDispatch(configuration);
        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQDispatchEndpointDescriptor AppendDispatch(
        string after,
        DispatchMiddlewareConfiguration configuration)
    {
        base.AppendDispatch(after, configuration);
        return this;
    }

    /// <inheritdoc />
    public new IRabbitMQDispatchEndpointDescriptor PrependDispatch(
        string before,
        DispatchMiddlewareConfiguration configuration)
    {
        base.PrependDispatch(before, configuration);
        return this;
    }

    /// <summary>
    /// Builds the final dispatch endpoint configuration from the accumulated settings.
    /// </summary>
    /// <returns>The configured <see cref="RabbitMQDispatchEndpointConfiguration"/>.</returns>
    public RabbitMQDispatchEndpointConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new dispatch endpoint descriptor with the specified name, defaulting to an exchange destination.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The endpoint name, also used as the default exchange name.</param>
    /// <returns>A new dispatch endpoint descriptor.</returns>
    public static RabbitMQDispatchEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
