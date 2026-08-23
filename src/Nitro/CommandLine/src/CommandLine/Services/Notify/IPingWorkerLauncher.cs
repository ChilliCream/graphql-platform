using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Spawns the detached <c>nitro agent ping-worker</c> child process a ping
/// attempt hands off to once its cooldown claim and lease are secured. The
/// notifier's own process (a one-shot mail command) exits almost
/// immediately after spawning, so the child must survive independently of
/// it: on Linux this means a new session (<c>setsid</c>) immune to the
/// SIGHUP a terminating terminal session would otherwise deliver to every
/// process still in its foreground process group (verified in
/// perles-net-k3j.3's spike).
/// </summary>
internal interface IPingWorkerLauncher
{
    /// <summary>
    /// Launches the worker with <paramref name="workerArgs"/> appended to
    /// <paramref name="descriptor"/>'s own launch command. Never throws: a
    /// missing <c>setsid</c>, a permission error, or any other spawn-time
    /// failure returns false so the caller can record it as
    /// <c>spawn-failed</c> instead of losing the caller's own exit code to
    /// it.
    /// </summary>
    bool TryLaunch(LaunchDescriptor descriptor, IReadOnlyList<string> workerArgs);
}
