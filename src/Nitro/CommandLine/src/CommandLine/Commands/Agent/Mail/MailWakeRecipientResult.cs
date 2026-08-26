using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

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
