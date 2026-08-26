using System.Runtime.InteropServices;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class CodexAncestorSessionResolver(
    Func<int, int?>? parentPidReader = null,
    Func<int, string?>? commReader = null) : ICodexAncestorSessionResolver
{
    /// <summary>
    /// Bounds the ancestor walk so a pathological /proc (a cycle, or a very
    /// deep process tree) cannot loop forever. Same bound as
    /// <see cref="ClaudeAncestorSessionResolver"/>.
    /// </summary>
    private const int MaxAncestorHops = 64;

    private const string CodexProcessName = "codex";

    private readonly Func<int, int?> _parentPidReader = parentPidReader ?? ProcessAncestry.GetParentPid;
    private readonly Func<int, string?> _commReader = commReader ?? ProcessAncestry.GetProcessName;

    public CodexAncestorSession? Resolve()
    {
        var pid = Environment.ProcessId;

        for (var hop = 0; hop < MaxAncestorHops; hop++)
        {
            var parentPid = _parentPidReader(pid);

            if (parentPid is null or <= 1)
            {
                return null;
            }

            var comm = _commReader(parentPid.Value);

            if (string.Equals(comm, CodexProcessName, StringComparison.Ordinal))
            {
                return new CodexAncestorSession(parentPid.Value);
            }

            pid = parentPid.Value;
        }

        return null;
    }
}
