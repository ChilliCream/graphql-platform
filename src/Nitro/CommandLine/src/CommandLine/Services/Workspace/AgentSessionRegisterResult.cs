namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The agent and session after registration, whether the binding or role changed,
/// and the prior binding reported by the registration operation.
/// </summary>
internal sealed record AgentSessionRegisterResult(
    AgentRecord Agent, AgentSessionRecord Session, bool Changed, string PreviousBindingKind, string? PreviousAgentName);
