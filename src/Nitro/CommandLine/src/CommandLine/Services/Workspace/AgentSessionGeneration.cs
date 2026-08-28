namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The generation identity every <c>agent_sessions</c> lifecycle mutation
/// predicates on: <c>(harness, session_id)</c> addresses the row and
/// <see cref="Host"/> pins it to the machine that owns it. The harness
/// names its own session in every hook event it sends, so nothing about a
/// session is inferred from the process tree.
/// </summary>
internal sealed record AgentSessionGeneration(string Harness, string SessionId, string Host);
