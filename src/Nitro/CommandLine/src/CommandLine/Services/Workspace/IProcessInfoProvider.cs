namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Reads process liveness from the OS, the way the session lifecycle
/// establishes and checks a generation: a pid alone is not a stable
/// identity (the OS recycles pids), so every check pairs it with the
/// process's raw kernel start-tick count (see <see cref="ProcStat"/>).
/// </summary>
internal interface IProcessInfoProvider
{
    /// <summary>
    /// Returns the given pid's raw start-tick count as the exact digit
    /// string the kernel reports (see <see cref="ProcStat.ReadStartTicks(int)"/>),
    /// or null when no process with that pid is currently running or its
    /// start ticks cannot be read.
    /// </summary>
    string? GetStartTicks(int pid);

    /// <summary>
    /// True when a process with the given pid is currently running and its
    /// raw start ticks equal <paramref name="expectedStartTicks"/> exactly.
    /// A pid that is running but started with different ticks belongs to a
    /// different generation (the OS reused the pid) and is not considered
    /// alive for that generation.
    /// </summary>
    bool IsAlive(int pid, string expectedStartTicks);

    /// <summary>
    /// This process's own observable process scope (PID/boot namespace
    /// visibility), the same value a session row's writer records as
    /// <c>process_scope</c>. Empty when the platform or environment exposes
    /// no such signal.
    /// </summary>
    string GetProcessScope();

    /// <summary>
    /// True when this process's own <see cref="GetProcessScope"/> and
    /// <paramref name="recordedProcessScope"/> (the scope a row's writer
    /// recorded) are NOT a positive, known mismatch. False only when both
    /// are non-blank and differ - definite proof this reader cannot trust
    /// its own view of the pid a row with that recorded scope refers to.
    /// Either side being blank (unknown) returns true, the same fall-through
    /// to a direct liveness check every caller of this predicate relies on.
    /// The single source both <see cref="Observe"/> and the session registry
    /// predicate their own scope checks on.
    /// </summary>
    bool CanObserveScope(string recordedProcessScope);

    /// <summary>
    /// Classifies the process at <paramref name="pid"/> as one of the
    /// <see cref="ProcessObservationResult"/> values. Checks
    /// <see cref="CanObserveScope"/> against <paramref
    /// name="recordedProcessScope"/> first: a positive mismatch, or a
    /// failure reading the target process, both classify as
    /// <c>unobservable</c> rather than <c>dead</c>. When <paramref
    /// name="legacy"/> is false (the normal case), <paramref
    /// name="expectedProcStart"/> is compared against the pid's current raw
    /// start ticks by exact string equality, no tolerance. When <paramref
    /// name="legacy"/> is true - a row migrated from before proc_start
    /// carried raw ticks, not yet rewritten by its own next SessionStart -
    /// <paramref name="expectedProcStart"/> is parsed as the legacy
    /// DateTimeOffset format instead and compared against the pid's current
    /// wall-clock start time with a small tolerance, the pre-migration rule.
    /// </summary>
    string Observe(int pid, string expectedProcStart, bool legacy, string recordedProcessScope);
}
