namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The result of successfully claiming a <c>mail_wake_batches</c> row:
/// its immutable id, the outbox generation it claimed, and the frozen set of
/// full session generations it was materialized against. See
/// <see cref="IMailWakeBatchStore.TryClaimAsync"/>.
/// </summary>
internal sealed record MailWakeBatchClaim(
    string BatchId, long ClaimedGeneration, IReadOnlyList<AgentSessionGeneration> Targets);
