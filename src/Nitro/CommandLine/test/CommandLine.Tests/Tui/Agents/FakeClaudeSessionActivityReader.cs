using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// An in-memory <see cref="IClaudeSessionActivityReader"/> returning the configured status
/// for a session id, or null when no status is configured.
/// </summary>
internal sealed class FakeClaudeSessionActivityReader : IClaudeSessionActivityReader
{
    public Dictionary<string, string> StatusBySessionId { get; } = [];

    public string? GetStatus(string sessionId)
        => StatusBySessionId.GetValueOrDefault(sessionId);
}
