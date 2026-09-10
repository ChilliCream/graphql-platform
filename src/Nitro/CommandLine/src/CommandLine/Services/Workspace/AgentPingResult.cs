namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

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
    /// kind. Distinct from
    /// <see cref="AgentSessionEndpointKind.None"/>, which means there is no
    /// endpoint to attempt at all.
    /// </summary>
    public const string Unsupported = "unsupported";
}
