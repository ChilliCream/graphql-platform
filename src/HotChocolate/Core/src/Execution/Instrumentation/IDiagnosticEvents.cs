using System;
using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation
{
    /// <summary>
    /// Diagnostic events that can be triggered by the execution engine.
    /// </summary>
    /// <seealso cref="IDiagnosticEventListener"/>
    public interface IDiagnosticEvents
    {
        /// <summary>
        /// Called when starting to execute a request.
        /// </summary>
        /// <param name="context">
        /// The request context encapsulates all GraphQL-specific information about an
        /// individual GraphQL request.
        /// </param>
        /// <returns>
        /// A scope that will be disposed when the execution has finished.
        /// </returns>
        IActivityScope ExecuteRequest(IRequestContext context);

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
        IActivityScope ParseDocument(IRequestContext context);

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
        IActivityScope ValidateDocument(IRequestContext context);

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
        /// Called when starting to resolve a field value.
        /// </summary>
        /// <remarks>
        /// <see cref="IDiagnosticEventListener.EnableResolveFieldValue"/> must be true if
        /// a listener implements this method to ensure that it is called.
        /// </remarks>
        /// <param name="context">
        /// The middleware context encapsulates all resolver-specific information about the
        /// execution of an individual field selection.
        /// </param>
        /// <returns>
        /// A scope that will be disposed when the field resolution has finished.
        /// </returns>
        IActivityScope ResolveFieldValue(IMiddlewareContext context);

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
        /// Called when starting to run an execution engine task.
        /// </summary>
        /// <remarks>
        /// <see cref="IDiagnosticEventListener.EnableResolveFieldValue"/> must be true if
        /// a listener implements this method to ensure that it is called.
        /// </remarks>
        /// <param name="task">
        /// Execution engine tasks are things like executing a DataLoader.
        /// </param>
        /// <returns>
        /// A scope that will be disposed when the task has finished.
        /// </returns>
        IActivityScope RunTask(IExecutionTask task);

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
        /// Called when a subscription was created.
        /// </summary>
        /// <param name="subscription">
        /// The subscription object.
        /// </param>
        /// <returns>
        /// A scope that will be disposed when the subscription has completed.
        /// </returns>
        IActivityScope ExecuteSubscription(ISubscription subscription);

        /// <summary>
        /// Called when an event was raised and a new subscription result is being produced.
        /// </summary>
        /// <param name="context">
        /// The subscription event context.
        /// </param>
        /// <returns>
        /// A scope that will be disposed when the subscription event execution has completed.
        /// </returns>
        IActivityScope OnSubscriptionEvent(SubscriptionEventContext context);

        /// <summary>
        /// Called when a result for a specific subscription event was produced.
        /// </summary>
        /// <param name="context">
        /// The subscription event context.
        /// </param>
        /// <param name="result">
        /// The subscription result that is being written to the response stream.
        /// </param>
        void SubscriptionEventResult(SubscriptionEventContext context, IQueryResult result);

        /// <summary>
        /// Called when an error occured while producing the subscription event result.
        /// </summary>
        /// <param name="context">
        /// The subscription event context.
        /// </param>
        /// <param name="exception">
        /// The exception that occured.
        /// </param>
        void SubscriptionEventError(SubscriptionEventContext context, Exception exception);

        /// <summary>
        /// Called when an error occured while producing the subscription event result.
        /// </summary>
        /// <param name="subscription">
        /// The subscription object.
        /// </param>
        /// <param name="exception">
        /// The exception that occured.
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
        /// Called when the document for a persisted query has been read from storage.
        /// </summary>
        /// <param name="context"></param>
        void RetrievedDocumentFromStorage(IRequestContext context);

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
        /// During execution we allow components like the DataLoader or schema stitching to
        /// defer execution of data resolvers to be executed in batches. If the execution engine
        /// has nothing to execute anymore these batches will be dispatched for execution.
        /// </summary>
        /// <param name="context">
        /// The request context encapsulates all GraphQL-specific information about an
        /// individual GraphQL request.
        /// </param>
        void BatchDispatched(IRequestContext context);

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
}
