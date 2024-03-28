using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Diagnostic;

public interface IFusionDiagnosticEvents
{
    /// <summary>
    /// Called when starting to execute a federated GraphQL request.
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
    /// Called when an exception occurred during the execution of a QueryPlan.
    /// </summary>
    /// <param name="exception">
    /// The unhandled exception that occurred while executing a QueryPlan.
    /// </param>
    void QueryPlanExecutionError(Exception exception);

    /// <summary>
    /// Called when an exception occurred during the execution of a Resolve QueryPlan node.
    /// </summary>
    /// <param name="exception">
    /// The exception that occurred while executing a Resolve QueryPlan node.
    /// </param>
    void ResolveError(Exception exception);

    /// <summary>
    /// Called when an exception occurred during the execution of a ResolveByKeyBatch QueryPlan node.
    /// </summary>
    /// <param name="exception">
    /// The exception that occurred while executing a ResolveByKeyBatch QueryPlan node.
    /// </param>
    void ResolveByKeyBatchError(Exception exception);
}
