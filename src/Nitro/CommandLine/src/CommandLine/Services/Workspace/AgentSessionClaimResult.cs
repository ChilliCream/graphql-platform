namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The outcome of <see cref="IAgentSessionRegistry.ClaimAsync"/> or
/// <see cref="IAgentSessionRegistry.SelfClaimAsync"/>: the row as it stands
/// after the call, whether the claim transition actually changed anything
/// (an <c>explicit(A) -&gt; explicit(A)</c> re-claim is a no-op), and the
/// binding this session carried immediately before the call.
/// </summary>
internal sealed record AgentSessionClaimResult(
    AgentSessionRecord Session, bool Changed, string PreviousBindingKind, string? PreviousAgentName);
