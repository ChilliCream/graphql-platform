namespace Mocha.Transport.Postgres;

/// <summary>
/// Descriptor implementation for configuring a PostgreSQL queue.
/// </summary>
internal sealed class PostgresQueueDescriptor
    : MessagingDescriptorBase<PostgresQueueConfiguration>
        , IPostgresQueueDescriptor
{
    public PostgresQueueDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new PostgresQueueConfiguration { Name = name };
    }

    /// <inheritdoc />
    protected internal override PostgresQueueConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IPostgresQueueDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor AutoDelete(bool autoDelete = true)
    {
        Configuration.AutoDelete = autoDelete;
        return this;
    }

    /// <inheritdoc />
    public IPostgresQueueDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <summary>
    /// Creates the final queue configuration.
    /// </summary>
    /// <returns>The configured queue configuration.</returns>
    public PostgresQueueConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new queue descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The queue name.</param>
    /// <returns>A new queue descriptor.</returns>
    public static PostgresQueueDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
