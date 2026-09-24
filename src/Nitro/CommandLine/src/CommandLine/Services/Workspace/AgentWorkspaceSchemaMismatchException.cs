namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Reports a workspace schema version that is older than the version required by the CLI.
/// </summary>
internal sealed class AgentWorkspaceSchemaMismatchException(string message) : ExitException(message);
