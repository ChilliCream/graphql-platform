using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

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
