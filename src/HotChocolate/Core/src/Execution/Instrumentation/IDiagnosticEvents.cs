using System;

namespace HotChocolate.Execution.Instrumentation;

/// <summary>
/// Diagnostic events that can be triggered by the execution engine.
/// </summary>
/// <seealso cref="IDiagnosticEventListener"/>
[Obsolete("Use IExecutionDiagnosticEvents")]
public interface IDiagnosticEvents : IExecutionDiagnosticEvents
{

}
