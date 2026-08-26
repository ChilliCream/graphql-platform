using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

/// <summary>
/// One target session's actor-wake outcome within a recipient's batch: its
/// harness and session id, its lattice status (a <see cref="MailWakeTargetStatus"/>
/// value), and its last attempt when the target did not settle cleanly.
/// </summary>
internal sealed record MailWakeTargetResult
{
    public required string Harness { get; init; }
    public required string SessionId { get; init; }
    public required string Status { get; init; }
    public MailWakeAttemptResult? LastAttempt { get; init; }

    public static MailWakeTargetResult Create(ActorWakeTargetReceipt receipt) => new()
    {
        Harness = receipt.Target.Harness,
        SessionId = receipt.Target.SessionId,
        Status = receipt.Status,
        LastAttempt = MailWakeAttemptResult.Create(receipt.LastError)
    };
}

/// <summary>
/// A bounded, stable machine <see cref="Reason"/> and a bounded human
/// <see cref="Detail"/> for one wake target or recipient outcome. Never a raw
/// exception message or subprocess stderr.
/// </summary>
internal sealed record MailWakeAttemptResult
{
    public required string Reason { get; init; }
    public required string Detail { get; init; }

    public static MailWakeAttemptResult? Create(string? reason) =>
        reason is null ? null : new MailWakeAttemptResult { Reason = reason, Detail = MailWakeReasonText.Describe(reason) };
}

/// <summary>
/// One recipient's actor-wake outcome for a sent or replied message: its
/// aggregate lattice status derived from every session it addressed (a
/// <see cref="MailWakeTargetStatus"/> value),
/// the wake generation this message committed for it (null when the send
/// used <see cref="MailWakePolicy.Skip"/>), and its target sessions in
/// deterministic order.
/// </summary>
internal sealed record MailWakeRecipientResult
{
    public required string Actor { get; init; }
    public required string Status { get; init; }
    public long? WakeGeneration { get; init; }
    public MailWakeAttemptResult? LastAttempt { get; init; }
    public required IReadOnlyList<MailWakeTargetResult> Targets { get; init; }

    public static MailWakeRecipientResult Skipped(string actor) => new()
    {
        Actor = actor,
        Status = MailWakeTargetStatus.Skipped,
        WakeGeneration = null,
        LastAttempt = null,
        Targets = []
    };

    public static MailWakeRecipientResult Create(MailWakeReceipt receipt, MailWakeObservation observation)
    {
        var targets = observation.Targets
            .OrderBy(t => t.Target.Harness, StringComparer.Ordinal)
            .ThenBy(t => t.Target.SessionId, StringComparer.Ordinal)
            .Select(MailWakeTargetResult.Create)
            .ToArray();

        var attemptSource = targets.FirstOrDefault(t => t.Status is MailWakeTargetStatus.Pending or MailWakeTargetStatus.Failed)
            ?? targets.FirstOrDefault(t => t.LastAttempt is not null);

        var lastAttempt = attemptSource?.LastAttempt
            ?? (observation.Status == MailWakeTargetStatus.Failed && targets.Length == 0
                ? MailWakeAttemptResult.Create("no-live-session")
                : null);

        return new MailWakeRecipientResult
        {
            Actor = receipt.Actor,
            Status = observation.Status,
            WakeGeneration = receipt.Generation,
            LastAttempt = lastAttempt,
            Targets = targets
        };
    }
}

/// <summary>
/// The full actor-wake notification outcome for a sent or replied message:
/// the aggregate <see cref="Status"/> across every recipient (the same
/// lattice <see cref="WakeReceiptAggregator.Aggregate"/> derives target
/// status from), whether delivery remains outstanding for any recipient, and
/// every recipient's own outcome in deterministic order.
/// </summary>
internal sealed record MailNotificationResult
{
    public required string Status { get; init; }
    public required bool DeliveryPending { get; init; }
    public required IReadOnlyList<MailWakeRecipientResult> Recipients { get; init; }
}

/// <summary>
/// Maps a bounded machine wake reason to a bounded human-readable detail.
/// Every value is a stable, safe description; never a raw exception message
/// or subprocess stderr.
/// </summary>
internal static class MailWakeReasonText
{
    public static string Describe(string reason) => reason switch
    {
        "no-live-session" => "No live session was claimed for this recipient.",
        "session-gone" => "The session ended before the wake could be attempted.",
        "no-endpoint" => "The session has no endpoint the wake could reach.",
        "unsupported" or "Unsupported" => "The session's endpoint does not support the automatic wake.",
        "busy" => "The session's transport was already busy with another attempt.",
        "capacity-dropped" or "CapacityDropped" => "No wake transport capacity was available.",
        "access-denied" or "AccessDenied" => "Access to the local Claude endpoint was denied.",
        "mail-already-read" => "The recipient had already read the mail before the wake ran.",
        "EndpointGone" => "The session's endpoint disappeared before the wake could be attempted.",
        "InvalidAuth" => "The session's endpoint rejected the wake's authentication.",
        "Timeout" => "The wake attempt did not complete before its deadline.",
        "TransportError" => "The wake attempt failed to reach the session's endpoint.",
        _ => $"The wake attempt did not complete ({reason})."
    };
}
