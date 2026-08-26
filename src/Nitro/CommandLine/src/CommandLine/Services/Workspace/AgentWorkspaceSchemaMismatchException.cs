namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Thrown when the workspace database on disk carries a schema version this
/// CLI does not use, so `nitro agent init` has to migrate it before anything
/// else can run. A distinct type rather than a plain <see cref="ExitException"/>
/// so the turn-boundary hooks can tell this apart from the transient
/// failures they fail open on: every hook in the session stays silently
/// inert until someone migrates, which is worth saying out loud once rather
/// than swallowing.
/// </summary>
internal sealed class AgentWorkspaceSchemaMismatchException(string message) : ExitException(message);
