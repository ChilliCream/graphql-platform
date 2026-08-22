using System.Diagnostics;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class ProcessInfoProvider : IProcessInfoProvider
{
    /// <summary>
    /// Tolerance for comparing a freshly read process start time against one
    /// persisted and reparsed from the database. Both derive from the same
    /// OS clock, but the storage round trip (formatting, then
    /// <see cref="DateTimeOffset.Parse(string)"/>) can shift the value by a
    /// small fraction of a second.
    /// </summary>
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
