namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A notification reservation for one session, message, and channel.
/// A reservation does not confirm that delivery succeeded.
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
