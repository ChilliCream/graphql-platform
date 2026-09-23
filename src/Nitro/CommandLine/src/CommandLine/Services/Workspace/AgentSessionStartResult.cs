namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The action <see cref="IAgentStore.StartSessionAsync"/> took for a harness session.
/// </summary>
internal enum AgentSessionStartKind
{
    /// <summary>
    /// A new agent was minted for a harness session id not seen before.
    /// </summary>
    Minted,

    /// <summary>
    /// An existing, non-deleted agent row was reused for a known harness session id.
    /// </summary>
    Reused,

    /// <summary>
    /// The harness session id belongs to a deleted agent; nothing was written.
    /// </summary>
    Ignored
}

/// <summary>
/// The result of <see cref="IAgentStore.StartSessionAsync"/>: which action was taken, and
/// the resulting row, or null when the session was ignored.
/// </summary>
internal sealed record AgentSessionStartResult
{
    public required AgentSessionStartKind Kind { get; init; }

    /// <summary>
    /// Null only when <see cref="Kind"/> is <see cref="AgentSessionStartKind.Ignored"/>.
    /// </summary>
    public AgentRow? Row { get; init; }

    public static AgentSessionStartResult Minted(AgentRow row) =>
        new() { Kind = AgentSessionStartKind.Minted, Row = row };

    public static AgentSessionStartResult Reused(AgentRow row) =>
        new() { Kind = AgentSessionStartKind.Reused, Row = row };

    public static AgentSessionStartResult Ignored { get; } =
        new() { Kind = AgentSessionStartKind.Ignored, Row = null };
}
