namespace Mocha.Transport.Postgres;

internal sealed class PostgresDispatchEndpointDescriptor
    : DispatchEndpointDescriptor<PostgresDispatchEndpointConfiguration>
    , IPostgresDispatchEndpointDescriptor
{
    private PostgresDispatchEndpointDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new PostgresDispatchEndpointConfiguration { Name = name, TopicName = name };
    }

    /// <inheritdoc />>
    protected internal override PostgresDispatchEndpointConfiguration Configuration { get; protected set; }

    /// <inheritdoc />>
    public IPostgresDispatchEndpointDescriptor ToQueue(string name)
    {
        Configuration.QueueName = name;
        Configuration.TopicName = null;
        return this;
    }

    /// <inheritdoc />>
    public IPostgresDispatchEndpointDescriptor ToTopic(string name)
    {
        Configuration.QueueName = null;
        Configuration.TopicName = name;
        return this;
    }

    /// <inheritdoc />>
    public new IPostgresDispatchEndpointDescriptor Send<TMessage>()
    {
        base.Send<TMessage>();
        return this;
    }

    /// <inheritdoc />>
    public new IPostgresDispatchEndpointDescriptor Publish<TMessage>()
    {
        base.Publish<TMessage>();
        return this;
    }

    /// <inheritdoc />>
    public new IPostgresDispatchEndpointDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null)
    {
        base.UseDispatch(configuration, before: before, after: after);
        return this;
    }

    /// <inheritdoc />>
    public PostgresDispatchEndpointConfiguration CreateConfiguration() => Configuration;

    public static PostgresDispatchEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
