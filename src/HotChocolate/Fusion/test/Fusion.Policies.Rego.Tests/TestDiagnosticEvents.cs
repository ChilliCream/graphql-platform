using HotChocolate.Fusion.Diagnostics;

namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// A diagnostic events listener that records every reported update and compilation error for
/// tests to assert against.
/// </summary>
internal sealed class TestDiagnosticEvents : FusionExecutionDiagnosticEventListener
{
    public List<Exception> UpdateErrors { get; } = [];

    public List<(string PolicyName, Exception Error)> CompilationErrors { get; } = [];

    public override void PolicyUpdateError(Exception error) => UpdateErrors.Add(error);

    public override void PolicyCompilationError(string policyName, Exception error)
        => CompilationErrors.Add((policyName, error));
}
