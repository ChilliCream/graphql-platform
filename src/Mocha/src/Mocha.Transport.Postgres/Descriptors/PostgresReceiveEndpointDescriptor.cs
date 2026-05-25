namespace Mocha.Transport.Postgres;

internal sealed class PostgresReceiveEndpointDescriptor
    : ReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
    , IPostgresReceiveEndpointDescriptor
{
    internal PostgresReceiveEndpointDescriptor(IMessagingConfigurationContext discoveryContext, string name)
        : base(discoveryContext)
    {
        Configuration = new PostgresReceiveEndpointConfiguration { Name = name, QueueName = name };
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler
    {
        base.Handler<THandler>();

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Handler(Type handlerType)
    {
        base.Handler(handlerType);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Consumer(Type consumerType)
    {
        base.Consumer(consumerType);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer
    {
        base.Consumer<TConsumer>();

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind)
    {
        base.Kind(kind);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency)
    {
        base.MaxConcurrency(maxConcurrency);

        return this;
    }

    /// <inheritdoc />
    public IPostgresReceiveEndpointDescriptor Queue(string name)
    {
        Configuration.QueueName = name;

        return this;
    }

    /// <inheritdoc />
    public IPostgresReceiveEndpointDescriptor MaxBatchSize(int size)
    {
        Configuration.MaxBatchSize = size;

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor FaultEndpoint(string name)
    {
        base.FaultEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor SkippedEndpoint(string name)
    {
        base.SkippedEndpoint(name);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseReceive(configuration, before: before, after: after);

        return this;
    }

    public PostgresReceiveEndpointConfiguration CreateConfiguration()
    {
        return Configuration;
    }

    public static PostgresReceiveEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
