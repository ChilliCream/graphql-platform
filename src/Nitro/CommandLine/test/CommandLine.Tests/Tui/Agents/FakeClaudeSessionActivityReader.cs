using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// An <see cref="IClaudeSessionActivityReader"/> returning a fixed status per
/// session id, standing in for a real session file read in tests.
/// </summary>
internal sealed class FakeClaudeSessionActivityReader : IClaudeSessionActivityReader
{
    public Dictionary<string, string> StatusBySessionId { get; } = [];

    public string? GetStatus(string sessionId)
        => StatusBySessionId.GetValueOrDefault(sessionId);
}
