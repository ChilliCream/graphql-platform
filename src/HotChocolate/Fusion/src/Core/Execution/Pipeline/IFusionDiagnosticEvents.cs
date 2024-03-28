using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Pipeline;

public interface IFusionDiagnosticEvents
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the execution has finished.
    /// </returns>
    IDisposable ExecuteFederatedQuery(IRequestContext context);

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="exception"></param>
    void QueryPlanExecutionError(Exception exception);
}

public interface IFusionDiagnosticEventListener : IFusionDiagnosticEvents
{

}

/// <summary>
/// TODO
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

    private sealed class EmptyActivityScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
