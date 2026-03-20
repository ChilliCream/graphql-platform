namespace Mocha.Transport.Postgres;

/// <summary>
/// Fluent interface for configuring a PostgreSQL topic.
/// </summary>
public interface IPostgresTopicDescriptor : IMessagingDescriptor<PostgresTopicConfiguration>
{
    /// <summary>
    /// Sets the name of the topic.
    /// </summary>
    /// <param name="name">The topic name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresTopicDescriptor Name(string name);

    /// <summary>
    /// Sets whether the topic should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresTopicDescriptor AutoProvision(bool autoProvision = true);
}
