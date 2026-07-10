using Mocha.Features;

namespace Mocha.Transport.Postgres.Features;

/// <summary>
/// Pooled feature that carries the PostgreSQL message item through the receive middleware pipeline,
/// enabling parsing and acknowledgement middleware to access the raw message data.
/// </summary>
public sealed class PostgresReceiveFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the PostgreSQL message item containing the raw message data read from the database.
    /// </summary>
    public PostgresMessageItem MessageItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transport message identifier used for acknowledgement (delete/release).
    /// </summary>
    public Guid TransportMessageId { get; set; }

    /// <inheritdoc />
    public void Initialize(object state)
    {
        MessageItem = null!;
        TransportMessageId = Guid.Empty;
    }

    /// <inheritdoc />
    public void Reset()
    {
        MessageItem = null!;
        TransportMessageId = Guid.Empty;
    }
}
