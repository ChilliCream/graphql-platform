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
    public const string Archived = "archived";

    /// <summary>
    /// The maximum number of closed tasks kept in the closed state. When a
    /// close pushes the closed count above this, the oldest closed tasks
    /// (by closed_at, tie-break id) move to <see cref="Archived"/> until
    /// exactly this many remain.
    /// </summary>
    public const int ClosedTaskCap = 100;

    /// <summary>
    /// A terminal state ends the task's lifecycle and releases anything
    /// blocked on it.
    /// </summary>
    public static bool IsTerminal(string state)
        => state is Closed or Tombstone or Archived;

    public static string Normalize(string state)
        => state.Trim().ToLowerInvariant().Replace('-', '_');
}
