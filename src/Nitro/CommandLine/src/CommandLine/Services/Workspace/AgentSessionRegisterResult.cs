namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The outcome of registering a session identity: the
/// durable identity and the live participant row as they stand after the
/// call, whether the participant's binding or role actually changed, and the
/// binding this session carried immediately before the call.
/// </summary>
internal sealed record AgentSessionRegisterResult(
    AgentRecord Agent, AgentSessionRecord Session, bool Changed, string PreviousBindingKind, string? PreviousAgentName);
