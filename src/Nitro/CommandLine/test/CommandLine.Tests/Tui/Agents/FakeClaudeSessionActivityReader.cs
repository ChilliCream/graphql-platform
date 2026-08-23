using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// An <see cref="IClaudeSessionActivityReader"/> returning a fixed status per
/// pid, standing in for a real <c>~/.claude/sessions/&lt;pid&gt;.json</c>
/// read in tests.
/// </summary>
internal sealed class FakeClaudeSessionActivityReader : IClaudeSessionActivityReader
{
    public Dictionary<int, string> StatusByPid { get; } = [];

    public string? GetStatus(int pid, string sessionId)
        => StatusByPid.GetValueOrDefault(pid);
}
