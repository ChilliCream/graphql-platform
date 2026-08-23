namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The <c>agent_sessions.harness</c> values, matching the table's CHECK
/// constraint.
/// </summary>
internal static class AgentSessionHarness
{
    public const string ClaudeCode = "claude-code";
    public const string Codex = "codex";
    public const string Copilot = "copilot";
}

/// <summary>
/// The <c>agent_sessions.binding_kind</c> values, matching the table's CHECK
/// constraint.
/// </summary>
internal static class AgentSessionBindingKind
{
    public const string None = "none";
    public const string Env = "env";
    public const string Explicit = "explicit";
}

/// <summary>
/// The <c>agent_sessions.endpoint_kind</c> values, matching the table's
/// CHECK constraint.
/// </summary>
internal static class AgentSessionEndpointKind
{
    public const string ClaudePeer = "claude-peer";
    public const string CodexThread = "codex-thread";
    public const string CopilotExtension = "copilot-extension";
    public const string None = "none";
}

/// <summary>
/// The <c>agent_sessions.last_ping_result</c> values, matching the table's
/// CHECK constraint.
/// </summary>
internal static class AgentPingResult
{
    public const string Ok = "ok";
    public const string SpawnFailed = "spawn-failed";
    public const string EndpointGone = "endpoint-gone";
    public const string Timeout = "timeout";
    public const string CapacityDropped = "capacity-dropped";
    public const string Error = "error";

    /// <summary>
    /// The endpoint is recorded but the notifier has no transport for its
    /// kind (<c>claude-peer</c>, currently). Distinct from
    /// <see cref="AgentSessionEndpointKind.None"/>, which means there is no
    /// endpoint to attempt at all.
    /// </summary>
    public const string Unsupported = "unsupported";
}

/// <summary>
/// The observable presence states <see cref="IAgentSessionRegistry.ListAsync"/>
/// computes for a surviving row: <c>online</c> (current instance, an
/// endpoint is registered), <c>unreachable</c> (current instance, no
/// endpoint), or <c>remote</c> (recorded by a different Nitro instance id,
/// never reaped or pinged from here). A dead-generation row on the current
/// instance never reaches this projection: it is reaped on read instead of
/// being reported as offline.
/// </summary>
internal static class AgentSessionState
{
    public const string Online = "online";
    public const string Unreachable = "unreachable";
    public const string Remote = "remote";
}
