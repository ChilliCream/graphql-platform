namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The Copilot CLI ancestor process this hook invocation was launched under:
/// just the pid. Like <see cref="CodexAncestorSession"/> (and unlike
/// <see cref="ClaudeAncestorSession"/>), Copilot has no live per-pid session
/// registry file to read, and no ancestor "peer name" is needed either. The
/// Copilot endpoint comes from an extension, not the ancestor process.
/// </summary>
internal sealed record CopilotAncestorSession(int Pid);
