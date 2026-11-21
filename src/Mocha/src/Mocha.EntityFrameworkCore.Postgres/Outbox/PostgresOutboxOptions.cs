namespace Mocha.Outbox;

/// <summary>
/// Options of the Postgres outbox, named by the DbContext type's full name.
/// </summary>
public sealed class PostgresOutboxOptions
{
    /// <summary>
    /// Gets or sets how long an idle worker waits before checking the database for messages to send
    /// when no notification arrives. The default is 10 seconds.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets whether a publish or send through the outbox requires an active database or ambient
    /// transaction and throws an <see cref="InvalidOperationException"/> without one. The default is <c>true</c>.
    /// </summary>
    public bool RequireTransaction { get; set; } = true;
}
