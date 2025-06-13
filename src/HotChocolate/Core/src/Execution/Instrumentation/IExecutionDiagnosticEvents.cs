using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation;

/// <summary>
/// Diagnostic events that can be triggered by the execution engine.
/// </summary>
/// <seealso cref="IExecutionDiagnosticEventListener"/>
public interface IExecutionDiagnosticEvents : ICoreExecutionDiagnosticEvents
{
    /// <summary>
    /// Called when starting to analyze the operation cost.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed of when the analyzer has finished.
    /// </returns>
    IDisposable AnalyzeOperationCost(RequestContext context);

    /// <summary>
    /// Called within <seealso cref="AnalyzeOperationCost"/> scope and
    /// reports the outcome of the analyzer.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <param name="fieldCost">
    /// The execution cost of the operation.
    /// </param>
    /// <param name="typeCost">
    /// The data cost of the operation.
    /// </param>
    void OperationCost(RequestContext context, double fieldCost, double typeCost);

    /// <summary>
    /// Called when starting to compile the GraphQL operation from the syntax tree.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed of when the execution has finished.
    /// </returns>
    IDisposable CompileOperation(RequestContext context);

    /// <summary>
    /// Called within the execute operation scope if the result is a streamed result.
    /// The ExecuteStream scope will run longer then the ExecuteOperation scope.
    /// The ExecuteOperation scope is completed once the initial operation is executed.
    /// All deferred elements will be executed and delivered within the ExecuteStream scope.
    /// </summary>
    /// <param name="operation">
    /// The operation that is being streamed.
    /// </param>
    /// <returns>
    /// A scope that will be disposed of when the execution has finished.
    /// </returns>
    IDisposable ExecuteStream(IOperation operation);

    /// <summary>
    /// Called when starting to execute a deferred part an operation
    /// within the ExecuteStream scope or within the
    /// ExecuteSubscription scope.
    /// </summary>
    /// <returns>
    /// A scope that will be disposed of when the execution has finished.
    /// </returns>
    IDisposable ExecuteDeferredTask();

    /// <summary>
    /// Called when starting to resolve a field value.
    /// </summary>
    /// <remarks>
    /// <see cref="IExecutionDiagnosticEventListener.EnableResolveFieldValue"/> must be true if
    /// a listener implements this method to ensure that it is called.
    /// </remarks>
    /// <param name="context">
    /// The middleware context encapsulates all resolver-specific information about the
    /// execution of an individual field selection.
    /// </param>
    /// <returns>
    /// A scope that will be disposed of when the field resolution has finished.
    /// </returns>
    IDisposable ResolveFieldValue(IMiddlewareContext context);

    /// <summary>
    /// Called when starting to run an execution engine task.
    /// </summary>
    /// <remarks>
    /// <see cref="IExecutionDiagnosticEventListener.EnableResolveFieldValue"/> must be true if
    /// a listener implements this method to ensure that it is called.
    /// </remarks>
    /// <param name="task">
    /// Execution engine tasks are things like executing a DataLoader.
    /// </param>
    /// <returns>
    /// A scope that will be disposed of when the task has finished.
    /// </returns>
    IDisposable RunTask(IExecutionTask task);

    /// <summary>
    /// This event is called when the request execution pipeline scales
    /// the task processors up or down.
    /// </summary>
    /// <param name="context">
    /// The request that is being executed.
    /// </param>
    void StartProcessing(RequestContext context);

    /// <summary>
    /// This event is called when the request execution pipeline scales
    /// the task processors up or down.
    /// </summary>
    /// <param name="context">
    /// The request that is being executed.
    /// </param>
    void StopProcessing(RequestContext context);

    /// <summary>
    /// A compiled operation was added to the operation cache.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    void AddedOperationToCache(RequestContext context);

    /// <summary>
    /// A compiled operation was retrieved from the operation cache.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    void RetrievedOperationFromCache(RequestContext context);

    /// <summary>
    /// During execution, we allow components like the DataLoader to defer execution of data
    /// resolvers to be executed in batches. If the execution engine has nothing to execute anymore
    /// these batches will be dispatched for execution.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    IDisposable DispatchBatch(RequestContext context);
}
