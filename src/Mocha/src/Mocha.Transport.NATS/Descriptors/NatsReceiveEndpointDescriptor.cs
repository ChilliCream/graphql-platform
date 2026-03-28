namespace Mocha.Transport.NATS;

/// <summary>
/// Descriptor for configuring a NATS receive endpoint that consumes messages from a specific subject.
/// </summary>
internal sealed class NatsReceiveEndpointDescriptor
    : ReceiveEndpointDescriptor<NatsReceiveEndpointConfiguration>
    , INatsReceiveEndpointDescriptor
{
    private NatsReceiveEndpointDescriptor(IMessagingConfigurationContext discoveryContext, string name)
        : base(discoveryContext)
    {
        Configuration = new NatsReceiveEndpointConfiguration
        {
            Name = name,
            SubjectName = name,
            ConsumerName = name
        };
    }

    /// <inheritdoc />
    public new INatsReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        base.Handler<THandler>();

        return this;
    }

    public new INatsReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        base.Consumer<TConsumer>();

        return this;
    }

    /// <inheritdoc />
    public new INatsReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind)
    {
        base.Kind(kind);

        return this;
    }

    /// <inheritdoc />
    public new INatsReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency)
    {
        base.MaxConcurrency(maxConcurrency);

        return this;
    }

    /// <inheritdoc />
    public INatsReceiveEndpointDescriptor Subject(string name)
    {
        Configuration.SubjectName = name;

        return this;
    }

    /// <inheritdoc />
    public INatsReceiveEndpointDescriptor ConsumerName(string name)
    {
        Configuration.ConsumerName = name;

        return this;
    }

    /// <inheritdoc />
    public INatsReceiveEndpointDescriptor MaxPrefetch(int maxPrefetch)
    {
        Configuration.MaxPrefetch = maxPrefetch;

        return this;
    }

    /// <inheritdoc />
    public new INatsReceiveEndpointDescriptor FaultEndpoint(string name)
    {
        base.FaultEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public new INatsReceiveEndpointDescriptor SkippedEndpoint(string name)
    {
        base.SkippedEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public new INatsReceiveEndpointDescriptor UseReceive(ReceiveMiddlewareConfiguration configuration)
    {
        base.UseReceive(configuration);

        return this;
    }

    /// <inheritdoc />
    public new INatsReceiveEndpointDescriptor AppendReceive(
        string after,
        ReceiveMiddlewareConfiguration configuration)
    {
        base.AppendReceive(after, configuration);

        return this;
    }

    /// <inheritdoc />
    public new INatsReceiveEndpointDescriptor PrependReceive(
        string before,
        ReceiveMiddlewareConfiguration configuration)
    {
        base.PrependReceive(before, configuration);

        return this;
    }

    /// <summary>
    /// Builds the final receive endpoint configuration from the accumulated settings.
    /// </summary>
    /// <returns>The configured <see cref="NatsReceiveEndpointConfiguration"/>.</returns>
    public NatsReceiveEndpointConfiguration CreateConfiguration()
    {
        return Configuration;
    }

    /// <summary>
    /// Creates a new receive endpoint descriptor with the specified name, which also serves as the default subject and consumer name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The endpoint name and default subject name.</param>
    /// <returns>A new receive endpoint descriptor.</returns>
    public static NatsReceiveEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
