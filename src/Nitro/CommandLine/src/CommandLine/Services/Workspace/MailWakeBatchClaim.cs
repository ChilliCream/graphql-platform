namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The claimed batch id, captured outbox generation, and target session identities.
/// </summary>
internal sealed record MailWakeBatchClaim(
    string BatchId, long ClaimedGeneration, IReadOnlyList<AgentSessionGeneration> Targets);
