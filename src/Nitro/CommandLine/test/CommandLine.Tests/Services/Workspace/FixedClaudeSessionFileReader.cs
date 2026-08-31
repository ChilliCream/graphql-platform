using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// An <see cref="IClaudeSessionFileReader"/> returning a fixed session,
/// standing in for a real session file read. Returns null by default, the
/// same as a session id no file on this machine carries.
/// </summary>
internal sealed class FixedClaudeSessionFileReader(ClaudeSessionFile? session = null)
    : IClaudeSessionFileReader
{
    public ClaudeSessionFile? Find(string sessionId) => session;
}
