using Mocha.Features;

namespace Mocha.Transport.Postgres;

internal sealed class PostgresReceiveEndpointDescriptor
    : ReceiveEndpointDescriptor<PostgresReceiveEndpointConfiguration>
    , IPostgresReceiveEndpointDescriptor
{
    internal PostgresReceiveEndpointDescriptor(IMessagingConfigurationContext discoveryContext, string name)
        : base(discoveryContext)
    {
        Configuration = new PostgresReceiveEndpointConfiguration
        {
            Name = name,
            QueueName = name,
            BindMode = MessagingBindMode.Implicit
        };
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
    public new IPostgresReceiveEndpointDescriptor Receives<TMessage>()
    {
        base.Receives<TMessage>();

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor Receives(Type messageType)
    {
        base.Receives(messageType);

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor BindImplicitly()
    {
        base.BindImplicitly();

        return this;
    }

    /// <inheritdoc />
    public new IPostgresReceiveEndpointDescriptor BindExplicitly()
    {
        base.BindExplicitly();

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
    public IPostgresReceiveEndpointDescriptor FaultEndpoint(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri)
        {
            throw new ArgumentException("The endpoint address must be an absolute URI.", nameof(address));
        }

        var feature = Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.Address = address;
        feature.IsDisabled = false;

        return this;
    }

    /// <inheritdoc />
    public IPostgresReceiveEndpointDescriptor DisableFaultEndpoint()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>();
        feature.Address = null;
        feature.IsDisabled = true;

        return this;
    }

    /// <inheritdoc />
    public IPostgresReceiveEndpointDescriptor SkippedEndpoint(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri)
        {
            throw new ArgumentException("The endpoint address must be an absolute URI.", nameof(address));
        }

        var feature = Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.Address = address;
        feature.IsDisabled = false;

        return this;
    }

    /// <inheritdoc />
    public IPostgresReceiveEndpointDescriptor DisableSkippedEndpoint()
    {
        var feature = Configuration.Features.GetOrSet<ReceiveSkippedEndpointFeature>();
        feature.Address = null;
        feature.IsDisabled = true;

        return this;
    }

    /// <inheritdoc />
    public IPostgresReceiveEndpointDescriptor MaxBatchSize(int size)
    {
        Configuration.MaxBatchSize = size;

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
