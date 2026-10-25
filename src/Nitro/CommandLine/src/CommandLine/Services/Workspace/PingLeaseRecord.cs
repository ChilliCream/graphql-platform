namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A ping attempt's lease on one of the four workspace concurrency slots.
/// </summary>
internal sealed record PingLeaseRecord
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the ping_leases table.
    /// </summary>
    public const string Columns =
        "slot AS Slot, attempt_id AS AttemptId, acquired_at AS AcquiredAt, expires_at AS ExpiresAt";

    /// <summary>
    /// Between 1 and 4 inclusive, the workspace-wide cap on outstanding ping
    /// children.
    /// </summary>
    public required int Slot { get; init; }

    public required string AttemptId { get; init; }
    public required DateTimeOffset AcquiredAt { get; init; }

    /// <summary>
    /// The time at which the lease expires and its slot becomes available for another attempt.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
