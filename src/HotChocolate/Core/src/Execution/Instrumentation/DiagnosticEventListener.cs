using System;

namespace HotChocolate.Execution.Instrumentation;

[Obsolete("Use ExecutionDiagnosticEventListener")]
public class DiagnosticEventListener
    : ExecutionDiagnosticEventListener
    , IDiagnosticEventListener
{

}
