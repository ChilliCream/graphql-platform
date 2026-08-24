using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed partial class ProcessInfoProvider(
    Func<int, string?>? startTicksReader = null) : IProcessInfoProvider
{
    // Legacy-only: rows migrated from schema v5, whose recorded proc_start
    // is still the old DateTimeOffset text a pre-v6 writer captured, fall
    // back to this wall-clock tolerance until their own next SessionStart
    // rewrites them with raw ticks. On Linux, .NET derives Process.StartTime
    // from an estimated boot time, so two processes reading the StartTime of
    // the SAME live pid can get values that differ by sub-millisecond
    // jitter (measured ~0.9ms across 6 processes on this machine), and boot-
    // time estimates can drift further under clock adjustment - hence a
    // tolerance rather than exact equality for this path only. A v6 row's
    // raw start ticks never uses this constant: it compares by exact string
    // equality instead (see <see cref="Observe"/>).
    private static readonly TimeSpan LegacyStartTimeTolerance = TimeSpan.FromSeconds(2);

    private readonly Func<int, string?> _startTicksReader = startTicksReader ?? (pid => ProcStat.ReadStartTicks(pid));

    public string? GetStartTicks(int pid) => _startTicksReader(pid);

    public bool IsAlive(int pid, string expectedStartTicks) => _startTicksReader(pid) == expectedStartTicks;

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

    public bool CanObserveScope(string recordedProcessScope)
    {
        var readerScope = GetProcessScope();

        // A positive mismatch between two KNOWN scopes: definite proof this
        // reader cannot trust its own view of the target pid. Either side
        // being unknown (blank) falls through as observable instead, so a
        // platform or row with no scope signal at all keeps today's
        // behavior.
        return !(readerScope.Length > 0 && recordedProcessScope.Length > 0 && readerScope != recordedProcessScope);
    }

    public string Observe(int pid, string expectedProcStart, bool legacy, string recordedProcessScope)
    {
        if (!CanObserveScope(recordedProcessScope))
        {
            return ProcessObservationResult.Unobservable;
        }

        if (legacy)
        {
            var actualStart = TryGetStartTime(pid, out var permissionDenied);

            if (permissionDenied)
            {
                return ProcessObservationResult.Unobservable;
            }

            var expectedStart = DateTimeOffset.Parse(expectedProcStart, CultureInfo.InvariantCulture);
            var alive = actualStart is not null
                && (actualStart.Value - expectedStart).Duration() <= LegacyStartTimeTolerance;

            return alive ? ProcessObservationResult.Alive : ProcessObservationResult.Dead;
        }

        var actualTicks = _startTicksReader(pid);

        return actualTicks is not null && actualTicks == expectedProcStart
            ? ProcessObservationResult.Alive
            : ProcessObservationResult.Dead;
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
