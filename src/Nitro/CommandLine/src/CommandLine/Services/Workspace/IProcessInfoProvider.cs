namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Reads process liveness from the OS, the way the session lifecycle
/// establishes and checks a generation: a pid alone is not a stable
/// identity (the OS recycles pids), so every check pairs it with the
/// process's absolute start time.
/// </summary>
internal interface IProcessInfoProvider
{
    /// <summary>
    /// Returns the given pid's absolute start time (UTC), or null when no
    /// process with that pid is currently running.
    /// </summary>
    DateTimeOffset? GetStartTime(int pid);

    /// <summary>
    /// True when a process with the given pid is currently running and its
    /// start time matches <paramref name="expectedStart"/>. A pid that is
    /// running but started at a different time belongs to a different
    /// generation (the OS reused the pid) and is not considered alive for
    /// that generation.
    /// </summary>
    bool IsAlive(int pid, DateTimeOffset expectedStart);
}
