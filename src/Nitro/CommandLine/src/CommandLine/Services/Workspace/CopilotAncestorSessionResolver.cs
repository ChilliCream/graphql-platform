using System.Runtime.InteropServices;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class CopilotAncestorSessionResolver(
    Func<int, int?>? parentPidReader = null,
    Func<int, string?>? commReader = null) : ICopilotAncestorSessionResolver
{
    /// <summary>
    /// Bounds the ancestor walk so a pathological /proc (a cycle, or a very
    /// deep process tree) cannot loop forever. Same bound as
    /// <see cref="CodexAncestorSessionResolver"/>.
    /// </summary>
    private const int MaxAncestorHops = 64;

    private const string CopilotProcessName = "copilot";

    private readonly Func<int, int?> _parentPidReader = parentPidReader ?? ReadParentPid;
    private readonly Func<int, string?> _commReader = commReader ?? ReadComm;

    public CopilotAncestorSession? Resolve()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return null;
        }

        var pid = Environment.ProcessId;

        for (var hop = 0; hop < MaxAncestorHops; hop++)
        {
            var parentPid = _parentPidReader(pid);

            if (parentPid is null or <= 1)
            {
                return null;
            }

            var comm = _commReader(parentPid.Value);

            if (string.Equals(comm, CopilotProcessName, StringComparison.Ordinal))
            {
                return new CopilotAncestorSession(parentPid.Value);
            }

            pid = parentPid.Value;
        }

        return null;
    }

    /// <summary>
    /// Reads the parent pid of <paramref name="pid"/> from
    /// <c>/proc/&lt;pid&gt;/status</c>. Any failure (the process already
    /// exited, permission denied) just ends the walk, the documented
    /// always-available fallback for self-identification being env binding
    /// at SessionStart.
    /// </summary>
    private static int? ReadParentPid(int pid)
    {
        try
        {
            foreach (var line in File.ReadLines($"/proc/{pid}/status"))
            {
                if (line.StartsWith("PPid:", StringComparison.Ordinal))
                {
                    var value = line["PPid:".Length..].Trim();

                    return int.TryParse(value, out var parsed) ? parsed : null;
                }
            }
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        return null;
    }

    /// <summary>
    /// Reads <paramref name="pid"/>'s executable name (<c>comm</c>, trimmed
    /// of its trailing newline) from <c>/proc/&lt;pid&gt;/comm</c>.
    /// </summary>
    private static string? ReadComm(int pid)
    {
        try
        {
            var path = $"/proc/{pid}/comm";

            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
