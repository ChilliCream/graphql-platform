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

    /// <summary>
    /// This process's own observable process scope (PID/boot namespace
    /// visibility), the same value a session row's writer records as
    /// <c>process_scope</c>. Empty when the platform or environment exposes
    /// no such signal.
    /// </summary>
    string GetProcessScope();

    /// <summary>
    /// Classifies the process at <paramref name="pid"/>, expected to have
    /// started at <paramref name="expectedStart"/>, as one of the
    /// <see cref="ProcessObservationResult"/> values. Compares this
    /// process's own <see cref="GetProcessScope"/> against <paramref
    /// name="recordedProcessScope"/> (the scope the row's writer recorded)
    /// first: a positive mismatch between two known scopes, or a failure
    /// reading the target process, both classify as <c>unobservable</c>
    /// rather than <c>dead</c>. Either scope being unknown falls back to a
    /// direct liveness check.
    /// </summary>
    string Observe(int pid, DateTimeOffset expectedStart, string recordedProcessScope);
}
