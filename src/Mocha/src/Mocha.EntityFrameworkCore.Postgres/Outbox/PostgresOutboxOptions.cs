namespace Mocha.Outbox;

/// <summary>
/// Options of the Postgres outbox, named by the DbContext type's full name.
/// </summary>
public sealed class PostgresOutboxOptions
{
    /// <summary>
    /// Gets or sets whether a publish or send through the outbox requires an active database or ambient
    /// transaction and throws an <see cref="InvalidOperationException"/> without one. The default is <c>true</c>.
    /// </summary>
    public bool RequireTransaction { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of messages dispatched concurrently, which must be positive and defaults to one.
    /// Values greater than one allow dispatches to complete out of order.
    /// </summary>
    public int MaxConcurrentMessages { get; set; } = 1;

    /// <summary>
    /// Gets or sets the longest time a single dispatch may take before it is cancelled and the message is
    /// released for a later attempt that does not count towards <see cref="MaxAttempts"/>. The default is
    /// 30 seconds, and <see cref="Timeout.InfiniteTimeSpan"/> switches the timeout off.
    /// </summary>
    public TimeSpan DispatchTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the number of failed dispatch attempts after which a message is no longer retried and
    /// stays in the outbox table. The value must be at least one and defaults to 10.
    /// </summary>
    public int MaxAttempts { get; set; } = 10;

    /// <summary>
    /// Gets or sets the upper bound of the delay before a failed message is dispatched again, which doubles
    /// with every failed attempt starting at one second. The value must be positive and defaults to 10 minutes.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(10);
}
