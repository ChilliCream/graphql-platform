namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Derives the deterministic provisional session id a harness's SessionStart
/// or <c>nitro agent register</c> binds a live row to when no authoritative
/// session id is available but a process generation (host, pid, raw
/// proc-start ticks) is observable. Stable for the same process generation
/// across duplicate resolutions, so a repeated derivation for the same
/// generation, and its matching SessionEnd, always identify the exact same
/// row. Distinguishable from every authoritative session id a harness could
/// ever issue by its fixed prefix, so a later canonical id for the same
/// process generation can be told apart from this one and reconciled onto
/// its row instead of creating a second participant.
/// </summary>
internal static class AgentSessionProvisionalSessionId
{
    private const string Prefix = "provisional:";

    public static string Derive(string harness, string host, int pid, string procStartTicks)
        => $"{Prefix}{harness}:{host}:{pid}:{procStartTicks}";

    public static bool IsProvisional(string sessionId) => sessionId.StartsWith(Prefix, StringComparison.Ordinal);
}
