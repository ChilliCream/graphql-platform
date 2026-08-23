using System.Diagnostics;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class ProcessInfoProvider : IProcessInfoProvider
{
    // On Linux, .NET derives Process.StartTime from an estimated boot time,
    // so two processes reading the StartTime of the SAME live pid can get
    // values that differ by sub-millisecond jitter (measured ~0.9ms across 6
    // processes on this machine). The SQLite round trip itself is lossless
    // (Microsoft.Data.Sqlite formats with an explicit offset, verified
    // empirically), but that doesn't remove the OS-side cross-process
    // non-determinism, so a tolerance is still required here. Keep this at
    // 2s: the measured jitter is small, but boot-time estimates can drift
    // further under clock adjustment, so don't shrink this without new
    // evidence.
    private static readonly TimeSpan StartTimeTolerance = TimeSpan.FromSeconds(2);

    public DateTimeOffset? GetStartTime(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);

            return process.StartTime.ToUniversalTime();
        }
        catch (ArgumentException)
        {
            // No process with this pid.
            return null;
        }
        catch (InvalidOperationException)
        {
            // The process exited between the lookup and reading StartTime.
            return null;
        }
    }

    public bool IsAlive(int pid, DateTimeOffset expectedStart)
    {
        var actualStart = GetStartTime(pid);

        return actualStart is not null && (actualStart.Value - expectedStart).Duration() <= StartTimeTolerance;
    }
}
