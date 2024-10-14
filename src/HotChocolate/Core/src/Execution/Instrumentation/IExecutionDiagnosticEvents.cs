using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation;

/// <summary>
/// Diagnostic events that can be triggered by the execution engine.
/// </summary>
/// <seealso cref="IExecutionDiagnosticEventListener"/>
public interface IExecutionDiagnosticEvents
{
    /// <summary>
    /// Called when starting to execute a GraphQL request with the <see cref="IRequestExecutor"/>.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the execution has finished.
    /// </returns>
    IDisposable ExecuteRequest(IRequestContext context);

    /// <summary>
    /// Called at the end of the execution if an exception occurred at some point,
    /// including unhandled exceptions when resolving fields.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <param name="exception">
    /// The last exception that occurred.
    /// </param>
    void RequestError(IRequestContext context, Exception exception);

    /// <summary>
    /// Called when starting to parse a document.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when parsing has finished.
    /// </returns>
    IDisposable ParseDocument(IRequestContext context);

    /// <summary>
    /// Called if a syntax error is detected in a document during parsing.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <param name="error">
    /// The GraphQL syntax error.
    /// </param>
    void SyntaxError(IRequestContext context, IError error);

    /// <summary>
    /// Called when starting to validate a document.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the validation has finished.
    /// </returns>
    IDisposable ValidateDocument(IRequestContext context);

    /// <summary>
    /// Called if there are any document validation errors.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <param name="errors">
    /// The GraphQL validation errors.
    /// </param>
    void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors);

    /// <summary>
    /// Called when starting to analyze the operation cost.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the analyzer has finished.
    /// </returns>
    IDisposable AnalyzeOperationCost(IRequestContext context);

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
    void OperationCost(IRequestContext context, double fieldCost, double typeCost);

    /// <summary>
    /// Called when starting to coerce variables for a request.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the execution has finished.
    /// </returns>
    IDisposable CoerceVariables(IRequestContext context);

    /// <summary>
    /// Called when starting to compile the GraphQL operation from the syntax tree.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the execution has finished.
    /// </returns>
    IDisposable CompileOperation(IRequestContext context);

    /// <summary>
    /// Called when starting to execute the GraphQL operation and its resolvers.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the execution has finished.
    /// </returns>
    IDisposable ExecuteOperation(IRequestContext context);

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
    /// A scope that will be disposed when the execution has finished.
    /// </returns>
    IDisposable ExecuteStream(IOperation operation);

    /// <summary>
    /// Called when starting to execute a deferred part an operation
    /// within the ExecuteStream scope or within the
    /// ExecuteSubscription scope.
    /// </summary>
    /// <returns>
    /// A scope that will be disposed when the execution has finished.
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
    /// A scope that will be disposed when the field resolution has finished.
    /// </returns>
    IDisposable ResolveFieldValue(IMiddlewareContext context);

    /// <summary>
    /// Called for any errors during field resolution, including unhandled exceptions.
    /// </summary>
    /// <param name="context">
    /// The middleware context encapsulates all resolver-specific information about the
    /// execution of an individual field selection.
    /// </param>
    /// <param name="error">
    /// The error object.
    /// </param>
    void ResolverError(IMiddlewareContext context, IError error);

    /// <summary>
    /// Called for field errors that do NOT occur within the resolver task.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <param name="selection">
    /// The selection that is affected by the error.
    /// </param>
    /// <param name="error">
    /// The error object.
    /// </param>
    /// <remarks>
    /// Some field level errors are handled after the resolver was completed and this
    /// are handled in the request scope.
    /// </remarks>
    void ResolverError(IRequestContext context, ISelection selection, IError error);

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
    /// A scope that will be disposed when the task has finished.
    /// </returns>
    IDisposable RunTask(IExecutionTask task);

    /// <summary>
    /// Called for any errors reported on a <see cref="IExecutionTaskContext"/>
    /// during task execution.
    /// </summary>
    /// <param name="task">
    /// Execution engine tasks are things like executing a DataLoader.
    /// </param>
    /// <param name="error">
    /// The error that occurred while running the execution task.
    /// </param>
    void TaskError(IExecutionTask task, IError error);

    /// <summary>
    /// This event is called when the request execution pipeline scales
    /// the task processors up or down.
    /// </summary>
    /// <param name="context">
    /// The request that is being executed.
    /// </param>
    void StartProcessing(IRequestContext context);

    /// <summary>
    /// This event is called when the request execution pipeline scales
    /// the task processors up or down.
    /// </summary>
    /// <param name="context">
    /// The request that is being executed.
    /// </param>
    void StopProcessing(IRequestContext context);

    /// <summary>
    /// Called when a subscription was created.
    /// </summary>
    /// <param name="subscription">
    /// The subscription object.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the subscription has completed.
    /// </returns>
    IDisposable ExecuteSubscription(ISubscription subscription);

    /// <summary>
    /// Called when an event was raised and a new subscription result is being produced.
    /// </summary>
    /// <param name="context">
    /// The subscription event context.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the subscription event execution has completed.
    /// </returns>
    IDisposable OnSubscriptionEvent(SubscriptionEventContext context);

    /// <summary>
    /// Called when a result for a specific subscription event was produced.
    /// </summary>
    /// <param name="context">
    /// The subscription event context.
    /// </param>
    /// <param name="result">
    /// The subscription result that is being written to the response stream.
    /// </param>
    void SubscriptionEventResult(SubscriptionEventContext context, IOperationResult result);

    /// <summary>
    /// Called when an error occurred while producing the subscription event result.
    /// </summary>
    /// <param name="context">
    /// The subscription event context.
    /// </param>
    /// <param name="exception">
    /// The exception that occurred.
    /// </param>
    void SubscriptionEventError(SubscriptionEventContext context, Exception exception);

    /// <summary>
    /// Called when an error occurred while producing the subscription event result.
    /// </summary>
    /// <param name="subscription">
    /// The subscription object.
    /// </param>
    /// <param name="exception">
    /// The exception that occurred.
    /// </param>
    void SubscriptionEventError(ISubscription subscription, Exception exception);

    /// <summary>
    /// Called when an error occurred while producing the subscription event result.
    /// </summary>
    /// <param name="subscription">
    /// The subscription object.
    /// </param>
    /// <param name="exception">
    /// The exception that occurred.
    /// </param>
    void SubscriptionTransportError(ISubscription subscription, Exception exception);

    /// <summary>
    /// A GraphQL request document was added to the document cache.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    void AddedDocumentToCache(IRequestContext context);

    /// <summary>
    /// A GraphQL request document was retrieved from the document cache.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    void RetrievedDocumentFromCache(IRequestContext context);

    /// <summary>
    /// Called when the document for a persisted operation has been read from storage.
    /// </summary>
    /// <param name="context"></param>
    void RetrievedDocumentFromStorage(IRequestContext context);

    /// <summary>
    /// Called when the document for a persisted operation could not be found in the
    /// operation document storage.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information
    /// about an individual GraphQL request.
    /// </param>
    /// <param name="documentId">
    /// The document id that was not found in the storage.
    /// </param>
    void DocumentNotFoundInStorage(IRequestContext context, OperationDocumentId documentId);

    /// <summary>
    /// A compiled operation was added to the operation cache.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    void AddedOperationToCache(IRequestContext context);

    /// <summary>
    /// A compiled operation was retrieved from the operation cache.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    void RetrievedOperationFromCache(IRequestContext context);

    /// <summary>
    /// During execution we allow components like the DataLoader to defer execution of data
    /// resolvers to be executed in batches. If the execution engine has nothing to execute anymore
    /// these batches will be dispatched for execution.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    IDisposable DispatchBatch(IRequestContext context);

    /// <summary>
    /// A GraphQL request executor was created and is now able to execute GraphQL requests.
    /// </summary>
    /// <param name="name">The name of the GraphQL schema.</param>
    /// <param name="executor">The GraphQL request executor.</param>
    void ExecutorCreated(string name, IRequestExecutor executor);

    /// <summary>
    /// A GraphQL request executor was evicted and will be removed from memory.
    /// </summary>
    /// <param name="name">The name of the GraphQL schema.</param>
    /// <param name="executor">The GraphQL request executor.</param>
    void ExecutorEvicted(string name, IRequestExecutor executor);
}
