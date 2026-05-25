namespace Mocha.Transport.Postgres;

/// <summary>
/// Fluent interface for configuring a PostgreSQL subscription (topic-to-queue binding).
/// </summary>
public interface IPostgresSubscriptionDescriptor : IMessagingDescriptor<PostgresSubscriptionConfiguration>
{
    /// <summary>
    /// Sets whether the subscription should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresSubscriptionDescriptor AutoProvision(bool autoProvision = true);
}
