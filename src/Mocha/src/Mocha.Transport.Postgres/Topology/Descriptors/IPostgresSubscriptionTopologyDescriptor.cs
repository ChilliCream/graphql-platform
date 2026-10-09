namespace Mocha.Transport.Postgres;

/// <summary>
/// Fluent interface for configuring a PostgreSQL subscription (topic-to-queue binding).
/// </summary>
public interface IPostgresSubscriptionTopologyDescriptor : IMessagingDescriptor<PostgresSubscriptionConfiguration>
{
    /// <summary>
    /// Sets whether the subscription should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresSubscriptionTopologyDescriptor AutoProvision(bool autoProvision = true);
}
