namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A Claude session's id, working directory, peer name, and version as recorded in its session file.
/// </summary>
internal sealed record ClaudeSessionFile(string SessionId, string Cwd, string Name, string Version);
