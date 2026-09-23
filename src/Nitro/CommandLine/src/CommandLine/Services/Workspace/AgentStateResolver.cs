namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Derives an agent's <see cref="AgentState"/> from its row, at read or render time.
/// </summary>
internal static class AgentStateResolver
{
    /// <summary>
    /// The window after <see cref="AgentRow.LastSeenAt"/> during which an agent counts as online.
    /// </summary>
    public static readonly TimeSpan OnlineWindow = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Resolves the state of <paramref name="row"/> as of <paramref name="now"/>.
    /// </summary>
    public static AgentState Resolve(AgentRow row, DateTimeOffset now)
    {
        if (row.DeletedAt is not null)
        {
            return AgentState.Offline;
        }

        if (row.EndedAt is not null)
        {
            return AgentState.Offline;
        }

        if (now - row.LastSeenAt > OnlineWindow)
        {
            return AgentState.Offline;
        }

        if (row.EndpointKind == AgentSessionEndpointKind.None)
        {
            return AgentState.Unreachable;
        }

        return AgentState.Online;
    }
}
