namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Execs a previously-configured foreign <c>notify</c> program after this
/// CLI's own notify work runs (install-flow contract: "foreign notify is
/// wrapped, ours execs it after our work, preserving argv/stdin/cwd/exit
/// code; if our handler fails, the foreign program still runs"). A true
/// exec-replace is a POSIX-only syscall .NET does not expose portably, so
/// this spawns the foreign program as a child with the identical single
/// argv payload, inherited stdio (no redirection, so both stdin and stdout
/// pass through unmodified) and this process's own unchanged cwd, then waits
/// for it - observably equivalent for a notify program, which is a
/// fire-and-forget leaf process, not an interactive one.
/// </summary>
internal interface ICodexForeignNotifyRunner
{
    /// <summary>
    /// Runs <paramref name="foreignArgv"/> (argv[0] is the foreign program
    /// itself) with <paramref name="payloadJson"/> appended as its one
    /// argument, and returns its exit code, or null when the foreign program
    /// could not be spawned at all (fail-open: a missing or unexecutable
    /// foreign program must not be allowed to break OUR own already-completed
    /// notify work, and there is no meaningful "foreign exit code" to
    /// propagate for a program that never ran).
    /// </summary>
    Task<int?> RunAsync(IReadOnlyList<string> foreignArgv, string payloadJson, CancellationToken cancellationToken);
}
