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
    public new IPostgresDispatchEndpointDescriptor UseDispatch(DispatchMiddlewareConfiguration configuration)
    {
        base.UseDispatch(configuration);
        return this;
    }

    /// <inheritdoc />>
    public new IPostgresDispatchEndpointDescriptor AppendDispatch(
        string after,
        DispatchMiddlewareConfiguration configuration)
    {
        base.AppendDispatch(after, configuration);
        return this;
    }

    /// <inheritdoc />>
    public new IPostgresDispatchEndpointDescriptor PrependDispatch(
        string before,
        DispatchMiddlewareConfiguration configuration)
    {
        base.PrependDispatch(before, configuration);
        return this;
    }

    /// <inheritdoc />>
    public PostgresDispatchEndpointConfiguration CreateConfiguration() => Configuration;

    public static PostgresDispatchEndpointDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
