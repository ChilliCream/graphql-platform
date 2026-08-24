using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed partial class ProcessInfoProvider : IProcessInfoProvider
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

    public DateTimeOffset? GetStartTime(int pid) => TryGetStartTime(pid, out _);

    public bool IsAlive(int pid, DateTimeOffset expectedStart)
    {
        var actualStart = GetStartTime(pid);

        return actualStart is not null && (actualStart.Value - expectedStart).Duration() <= StartTimeTolerance;
    }

    public string GetProcessScope()
    {
        if (!OperatingSystem.IsLinux())
        {
            return string.Empty;
        }

        try
        {
            var linkTarget = File.ResolveLinkTarget("/proc/self/ns/pid", returnFinalTarget: false)?.FullName ?? "";
            var match = PidNamespacePattern().Match(linkTarget);

            return match.Success ? $"pidns:{match.Groups[1].Value}" : string.Empty;
        }
        catch (IOException)
        {
            return string.Empty;
        }
        catch (UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }

    public string Observe(int pid, DateTimeOffset expectedStart, string recordedProcessScope)
    {
        var readerScope = GetProcessScope();

        // A positive mismatch between two KNOWN scopes: definite proof this
        // reader cannot trust its own view of the target pid. Either side
        // being unknown (blank) falls through to the direct liveness check
        // below instead, so a platform or row with no scope signal at all
        // keeps today's behavior.
        if (readerScope.Length > 0 && recordedProcessScope.Length > 0 && readerScope != recordedProcessScope)
        {
            return ProcessObservationResult.Unobservable;
        }

        var actualStart = TryGetStartTime(pid, out var permissionDenied);

        if (permissionDenied)
        {
            return ProcessObservationResult.Unobservable;
        }

        var alive = actualStart is not null && (actualStart.Value - expectedStart).Duration() <= StartTimeTolerance;

        return alive ? ProcessObservationResult.Alive : ProcessObservationResult.Dead;
    }

    private static DateTimeOffset? TryGetStartTime(int pid, out bool permissionDenied)
    {
        permissionDenied = false;

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
        catch (UnauthorizedAccessException)
        {
            permissionDenied = true;
            return null;
        }
        catch (Win32Exception)
        {
            permissionDenied = true;
            return null;
        }
    }

    [GeneratedRegex(@"pid:\[(\d+)\]")]
    private static partial Regex PidNamespacePattern();
}
