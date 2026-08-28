namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A row of <c>ping_leases</c>: one of the four fixed concurrency slots a
/// ping child holds while running. Acquisition is an atomic
/// insert-or-steal-expired claim; release deletes by (slot, attempt_id). See
/// <see cref="IPingLeaseStore"/>.
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
    /// The ping child's hard timeout is strictly shorter than this, so an
    /// expired lease can never be stolen while its child still runs.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
