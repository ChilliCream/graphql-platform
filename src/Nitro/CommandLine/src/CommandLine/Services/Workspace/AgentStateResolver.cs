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
    /// The width of the clock-aligned window that groups agents by last-seen time for ordering.
    /// </summary>
    public static readonly TimeSpan LastSeenWindow = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Floors <paramref name="value"/> to the start of its <see cref="LastSeenWindow"/>, aligned
    /// to the UTC clock (for example 10:00, 10:05, 10:10) rather than to <paramref name="value"/>
    /// itself.
    /// </summary>
    public static DateTimeOffset LastSeenWindowStart(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        var windowTicks = LastSeenWindow.Ticks;
        var flooredTicks = utc.Ticks - (utc.Ticks % windowTicks);
        return new DateTimeOffset(flooredTicks, TimeSpan.Zero);
    }

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
