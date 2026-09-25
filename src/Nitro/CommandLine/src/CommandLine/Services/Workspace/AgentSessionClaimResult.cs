namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The session after a claim, whether its binding changed, and the prior binding
/// used by the claim transition.
/// </summary>
internal sealed record AgentSessionClaimResult(
    AgentSessionRecord Session, bool Changed, string PreviousBindingKind, string? PreviousAgentName);
