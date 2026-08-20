namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The well-known task states. Custom lowercase states are accepted and
/// round-tripped verbatim.
/// </summary>
internal static class TaskStates
{
    public const string Open = "open";
    public const string InProgress = "in_progress";
    public const string Blocked = "blocked";
    public const string Deferred = "deferred";
    public const string Closed = "closed";
    public const string Tombstone = "tombstone";

    /// <summary>
    /// A terminal state ends the task's lifecycle and releases anything
    /// blocked on it.
    /// </summary>
    public static bool IsTerminal(string state)
        => state is Closed or Tombstone;

    public static string Normalize(string state)
        => state.Trim().ToLowerInvariant().Replace('-', '_');
}
