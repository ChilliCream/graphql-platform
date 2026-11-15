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
}
