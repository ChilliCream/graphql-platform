namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Thrown when the workspace database on disk carries a schema version this
/// CLI does not use, so `nitro agent init` has to migrate it before anything
/// else can run. A distinct type from <see cref="ExitException"/> so callers
/// can catch it specifically.
/// </summary>
internal sealed class AgentWorkspaceSchemaMismatchException(string message) : ExitException(message);
