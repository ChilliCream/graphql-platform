namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A row of <c>session_deliveries</c>: an at-most-once claim that a given
/// message was delivered to a given session over a given channel
/// (<c>digest</c>, <c>gate</c>, or <c>ping</c>). The composite primary key
/// includes the channel deliberately, so a digest lost to a crash between
/// reserve and emit does not suppress the gate or a ping for the same
/// message. No command reads or writes this type yet; it exists so the hook
/// adapters and ping notifier landing in a later bead have a column-matched
/// row type to build on.
/// </summary>
internal sealed record SessionDeliveryRecord
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the session_deliveries table.
    /// </summary>
    public const string Columns =
        "harness AS Harness, session_id AS SessionId, message_id AS MessageId, "
        + "channel AS Channel, delivered_at AS DeliveredAt";

    public required string Harness { get; init; }
    public required string SessionId { get; init; }
    public required string MessageId { get; init; }

    /// <summary>
    /// One of <c>"digest"</c>, <c>"gate"</c>, or <c>"ping"</c>.
    /// </summary>
    public required string Channel { get; init; }

    public required DateTimeOffset DeliveredAt { get; init; }
}
