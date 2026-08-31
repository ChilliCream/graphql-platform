using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// One target's final, durably-observed disposition within an actor-wake
/// batch: the exact frozen session generation it addressed, its lattice
/// <see cref="Status"/> (a <see cref="MailWakeTargetStatus"/> value),
/// its target-qualified offered/accepted generations, and a bounded
/// diagnostic. Mirrors the corresponding <c>mail_wake_targets</c> row,
/// except when this dispatcher's own attempt lost its batch fence before
/// recording an outcome, in which case <see cref="Status"/> stays
/// <see cref="MailWakeTargetStatus.Pending"/> without asserting what a
/// newer owner may since have written.
/// </summary>
internal sealed record ActorWakeTargetReceipt(
    AgentSessionGeneration Target,
    string Status,
    long? OfferedGeneration,
    long? AcceptedGeneration,
    string? LastError);
