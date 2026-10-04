namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Wake target and receipt statuses. Delivered, satisfied, delegated, and skipped
/// are successful terminal statuses; pending is unresolved and failed is unsuccessful.
/// </summary>
internal static class MailWakeTargetStatus
{
    /// <summary>
    /// The target is unresolved, including work offered for a later attempt.
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// The endpoint accepted the wake attempt or observes messages directly through the database.
    /// </summary>
    public const string Delivered = "delivered";

    /// <summary>
    /// No transport was needed because the actor had no unread, unarchived mail at dispatch time.
    /// </summary>
    public const string Satisfied = "satisfied";

    /// <summary>
    /// Responsibility for this one target was durably handed to another
    /// owner (a dashboard leader) and accepted.
    /// </summary>
    public const string Delegated = "delegated";

    /// <summary>
    /// No wake attempt was required, including when an actor has no eligible target.
    /// </summary>
    public const string Skipped = "skipped";

    /// <summary>
    /// The target was unavailable, unsupported, or its transport attempt failed.
    /// </summary>
    public const string Failed = "failed";
}
