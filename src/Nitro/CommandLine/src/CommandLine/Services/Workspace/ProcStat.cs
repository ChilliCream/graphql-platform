namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Reads a process's raw start-tick count from <c>/proc/&lt;pid&gt;/stat</c>,
/// Linux's own kernel-tick clock for a process's start time, distinct from
/// and more precise than .NET's estimated <see cref="System.Diagnostics.Process.StartTime"/>.
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
}
