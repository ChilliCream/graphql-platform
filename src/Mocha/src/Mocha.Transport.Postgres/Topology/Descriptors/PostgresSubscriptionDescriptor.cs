namespace Mocha.Transport.Postgres;

/// <summary>
/// Descriptor implementation for configuring a PostgreSQL subscription (topic-to-queue binding).
/// </summary>
internal sealed class PostgresSubscriptionDescriptor
    : MessagingDescriptorBase<PostgresSubscriptionConfiguration>
    , IPostgresSubscriptionDescriptor
{
    public PostgresSubscriptionDescriptor(
        IMessagingConfigurationContext context,
        string source,
        string destination) : base(context)
    {
        Configuration = new PostgresSubscriptionConfiguration { Source = source, Destination = destination };
    }

    /// <inheritdoc />
    protected internal override PostgresSubscriptionConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IPostgresSubscriptionDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <summary>
    /// Creates the final subscription configuration.
    /// </summary>
    /// <returns>The configured subscription configuration.</returns>
    public PostgresSubscriptionConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new subscription descriptor with the specified source and destination.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="source">The source topic name.</param>
    /// <param name="destination">The destination queue name.</param>
    /// <returns>A new subscription descriptor.</returns>
    public static PostgresSubscriptionDescriptor New(
        IMessagingConfigurationContext context,
        string source,
        string destination)
        => new(context, source, destination);
}
