using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Diagnostic;

/// <summary>
/// This class can be used as a base class for <see cref="IFusionDiagnosticEventListener"/>
/// implementations, so that they only have to override the methods they
/// are interested in instead of having to provide implementations for all of them.
/// </summary>
public class FusionDiagnosticEventListener : IFusionDiagnosticEventListener
{
    /// <summary>
    /// A no-op activity scope that can be returned from
    /// event methods that are not interested in when the scope is disposed.
    /// </summary>
    protected static IDisposable EmptyScope { get; } = new EmptyActivityScope();

    /// <inheritdoc />
    public virtual IDisposable ExecuteFederatedQuery(IRequestContext context)
        => EmptyScope;

    /// <inheritdoc />
    public virtual void QueryPlanExecutionError(Exception exception)
    {
    }

    /// <inheritdoc />
    public virtual void ResolveError(Exception exception)
    {
    }

    /// <inheritdoc />
    public virtual void ResolveByKeyBatchError(Exception exception)
    {
    }

    private sealed class EmptyActivityScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
