using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// In-memory <see cref="IClaudeSessionActivityReader"/> stub returning a fixed status per session id.
/// </summary>
internal sealed class FakeClaudeSessionActivityReader : IClaudeSessionActivityReader
{
    public Dictionary<string, string> StatusBySessionId { get; } = [];

    public string? GetStatus(string sessionId)
        => StatusBySessionId.GetValueOrDefault(sessionId);
}
