namespace Mocha.Transport.Postgres;

/// <summary>
/// Descriptor implementation for configuring a PostgreSQL topic.
/// </summary>
internal sealed class PostgresTopicTopologyDescriptor
    : MessagingDescriptorBase<PostgresTopicConfiguration>
    , IPostgresTopicTopologyDescriptor
{
    public PostgresTopicTopologyDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new PostgresTopicConfiguration { Name = name, Origin = TopologyOrigin.Declared };
    }

    /// <inheritdoc />
    protected internal override PostgresTopicConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IPostgresTopicTopologyDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public IPostgresTopicTopologyDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <summary>
    /// Creates the final topic configuration.
    /// </summary>
    /// <returns>The configured topic configuration.</returns>
    public PostgresTopicConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new topic descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The topic name.</param>
    /// <returns>A new topic descriptor.</returns>
    public static PostgresTopicTopologyDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
