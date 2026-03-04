using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation;

/// <summary>
/// Provides diagnostic events that can be triggered by the GraphQL execution engine.
/// These events allow monitoring and instrumentation of various execution phases
/// including operation compilation, field resolution, task execution, and subscription handling.
/// </summary>
/// <seealso cref="IExecutionDiagnosticEventListener"/>
public interface IExecutionDiagnosticEvents : ICoreExecutionDiagnosticEvents
{
    /// <summary>
    /// Called when a subscription event is raised and a new subscription result is being produced.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <param name="subscriptionId">
    /// An internal identifier for the subscription instance.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the subscription event execution has completed.
    /// </returns>
    IDisposable OnSubscriptionEvent(RequestContext context, ulong subscriptionId);

    /// <summary>
    /// Called when an error occurs while producing a subscription event result.
    /// </summary>
    /// <param name="context">
    /// The request context for the subscription event.
    /// </param>
    /// <param name="subscriptionId">
    /// An internal identifier for the subscription instance.
    /// </param>
    /// <param name="exception">
    /// The exception that occurred during event processing.
    /// </param>
    void SubscriptionEventError(RequestContext context, ulong subscriptionId, Exception exception);

    /// <summary>
    /// Called when starting to analyze the operation cost for query complexity analysis.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the cost analysis has finished.
    /// </returns>
    IDisposable AnalyzeOperationCost(RequestContext context);

    /// <summary>
    /// Called within the <see cref="AnalyzeOperationCost"/> scope to report
    /// the outcome of the cost analysis.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <param name="fieldCost">
    /// The execution cost of the operation based on field complexity.
    /// </param>
    /// <param name="typeCost">
    /// The data cost of the operation based on type complexity.
    /// </param>
    void OperationCost(RequestContext context, double fieldCost, double typeCost);

    /// <summary>
    /// Called when starting to compile the GraphQL operation from the parsed syntax tree.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the operation compilation has finished.
    /// </returns>
    IDisposable CompileOperation(RequestContext context);

    /// <summary>
    /// Called when starting to resolve a field value.
    /// </summary>
    /// <remarks>
    /// <see cref="IExecutionDiagnosticEventListener.EnableResolveFieldValue"/> must be true
    /// for a listener to receive this event.
    /// </remarks>
    /// <param name="context">
    /// The middleware context encapsulates all resolver-specific information about the
    /// execution of an individual field selection.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the field resolution has finished.
    /// </returns>
    IDisposable ResolveFieldValue(IMiddlewareContext context);

    /// <summary>
    /// Called for any errors during field resolution, including unhandled exceptions
    /// that occur within the resolver execution.
    /// </summary>
    /// <param name="context">
    /// The middleware context encapsulates all resolver-specific information about the
    /// execution of an individual field selection.
    /// </param>
    /// <param name="error">
    /// The error that occurred during field resolution.
    /// </param>
    void ResolverError(IMiddlewareContext context, IError error);

    /// <summary>
    /// Called when starting to execute an execution engine task.
    /// </summary>
    /// <remarks>
    /// <see cref="IExecutionDiagnosticEventListener.EnableResolveFieldValue"/> must be true
    /// for a listener to receive this event.
    /// </remarks>
    /// <param name="task">
    /// The execution task being run, such as DataLoader batch execution or other
    /// background processing tasks.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the task execution has finished.
    /// </returns>
    IDisposable RunTask(IExecutionTask task);

    /// <summary>
    /// Called for any errors reported on an <see cref="IExecutionTaskContext"/>
    /// during task execution.
    /// </summary>
    /// <param name="task">
    /// The execution task that encountered an error, such as DataLoader batch execution
    /// or other background processing tasks.
    /// </param>
    /// <param name="error">
    /// The error that occurred while running the execution task.
    /// </param>
    void TaskError(IExecutionTask task, IError error);

    /// <summary>
    /// Called when the request execution pipeline starts processing tasks,
    /// typically when scaling task processors up.
    /// </summary>
    /// <param name="context">
    /// The request context for the request being executed.
    /// </param>
    void StartProcessing(RequestContext context);

    /// <summary>
    /// Called when the request execution pipeline stops processing tasks,
    /// typically when scaling task processors down or completing execution.
    /// </summary>
    /// <param name="context">
    /// The request context for the request being executed.
    /// </param>
    void StopProcessing(RequestContext context);

    /// <summary>
    /// Called when a compiled operation is added to the operation cache.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    void AddedOperationToCache(RequestContext context);

    /// <summary>
    /// Called when a compiled operation is retrieved from the operation cache.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    void RetrievedOperationFromCache(RequestContext context);
}
