namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The <c>mail_wake_targets.status</c> values, matching the table's CHECK
/// constraint. <see cref="Delivered"/>, <see cref="Satisfied"/>,
/// <see cref="Delegated"/>, and <see cref="Skipped"/> are successful terminal
/// statuses. <see cref="Pending"/> is unresolved and <see cref="Failed"/> is a
/// terminal failure.
/// </summary>
internal static class MailWakeTargetStatus
{
    /// <summary>
    /// The row's status until something durably resolves it: still
    /// materialized, not yet dispatched, or dispatched but offered to a
    /// handoff (busy, cooldown, capacity, or Claude access-denied) that has
    /// not yet been accepted or settled.
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// The transport call itself succeeded: the digest was actually written
    /// to the target's live endpoint.
    /// </summary>
    public const string Delivered = "delivered";

    /// <summary>
    /// No transport was needed: the mail that triggered this wake was
    /// already read by the time this batch dispatched.
    /// </summary>
    public const string Satisfied = "satisfied";

    /// <summary>
    /// Responsibility for this one target was durably handed to another
    /// owner (a dashboard leader) and accepted.
    /// </summary>
    public const string Delegated = "delegated";

    /// <summary>
    /// This target was deliberately not attempted (currently unused by the
    /// direct-first dispatcher; reserved for a future caller-directed skip).
    /// </summary>
    public const string Skipped = "skipped";

    /// <summary>
    /// The target could not be reached: the frozen generation had already
    /// disappeared, its endpoint kind carries no transport, its transport
    /// call ended in an unaccepted terminal failure, or it was never
    /// dispatched at all because this batch lost its own claim first.
    /// </summary>
    public const string Failed = "failed";
}
