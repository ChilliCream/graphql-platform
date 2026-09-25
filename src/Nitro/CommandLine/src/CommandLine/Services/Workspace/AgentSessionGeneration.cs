namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Identifies a harness session and its owning Nitro instance.
/// <see cref="Host"/> is the Nitro instance id.
/// </summary>
internal sealed record AgentSessionGeneration(string Harness, string SessionId, string Host);
