using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Reads a process's start time as an exact digit string two processes can
/// compare for equality: Linux's own kernel-tick clock from
/// <c>/proc/&lt;pid&gt;/stat</c>, and elsewhere the kernel start timestamp
/// behind <see cref="Process.StartTime"/>.
/// </summary>
internal static class ProcStat
{
    /// <summary>
    /// Returns the pid's start-tick count as the exact digit string the
    /// kernel reports, for comparison against a value another process
    /// recorded, or null when the file is unavailable or malformed.
    /// </summary>
    public static string? ReadStartTicks(int pid) => ReadStartTicks(pid, out _);

    /// <summary>
    /// Same as <see cref="ReadStartTicks(int)"/>, additionally reporting
    /// through <paramref name="permissionDenied"/> whether the null result
    /// came from an access failure reading <c>/proc/&lt;pid&gt;/stat</c>
    /// rather than the pid simply having no such file (no process, or the
    /// content did not parse): a caller that must distinguish "cannot tell"
    /// from "provably gone" needs this, a plain lookup does not.
    /// </summary>
    public static string? ReadStartTicks(int pid, out bool permissionDenied)
    {
        permissionDenied = false;

        if (!OperatingSystem.IsLinux())
        {
            return ReadStartTimeTicks(pid, out permissionDenied);
        }

        try
        {
            var content = File.ReadAllText($"/proc/{pid}/stat");

            // comm (the second field) is parenthesized and can itself
            // contain spaces or parens, so every field position is only
            // safe counted from the LAST closing paren, not from the start
            // of the line: ppid is the 2nd field after it, start ticks the
            // 20th.
            var afterComm = content.LastIndexOf(')');

            if (afterComm < 0)
            {
                return null;
            }

            var fields = content[(afterComm + 1)..].Split(
                ' ', StringSplitOptions.RemoveEmptyEntries);

            return fields.Length >= 20 ? fields[19] : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            permissionDenied = true;
            return null;
        }
    }

    /// <summary>
    /// The pid's start time in <see cref="DateTime.Ticks"/>, for platforms
    /// with no procfs. Unlike Linux, where .NET estimates this from boot
    /// time, the value is the fixed timestamp the kernel recorded, so two
    /// readers of the same live pid agree exactly.
    /// </summary>
    private static string? ReadStartTimeTicks(int pid, out bool permissionDenied)
    {
        permissionDenied = false;

        try
        {
            using var process = Process.GetProcessById(pid);

            return process.StartTime.Ticks.ToString(CultureInfo.InvariantCulture);
        }
        catch (ArgumentException)
        {
            // No process carries this pid.
            return null;
        }
        catch (InvalidOperationException)
        {
            // The process exited between the lookup and the read.
            return null;
        }
        catch (Win32Exception)
        {
            permissionDenied = true;
            return null;
        }
    }
}
