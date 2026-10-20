namespace HotChocolate.Execution.Instrumentation;

/// <summary>
/// Marks an <see cref="ICoreExecutionDiagnosticEvents"/> implementation that observes nothing,
/// so the scopes it creates have no effect.
/// </summary>
internal interface INoopExecutionDiagnosticEvents : ICoreExecutionDiagnosticEvents;
