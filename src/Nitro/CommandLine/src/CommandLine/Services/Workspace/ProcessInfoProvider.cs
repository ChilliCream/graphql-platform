using System.Diagnostics;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class ProcessInfoProvider : IProcessInfoProvider
{
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

        // Exact equality, matching every agent_sessions predicate: the
        // round trip through SQLite TEXT storage and DateTimeOffset.Parse
        // is lossless (Microsoft.Data.Sqlite formats with an explicit
        // offset, verified empirically), so a fuzzy tolerance here would
        // only widen the pid-reuse window proc_start exists to close,
        // without correcting for anything that actually loses precision.
        return actualStart == expectedStart;
    }
}
