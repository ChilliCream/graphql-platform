using System;
using System.Collections.Generic;
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
        /// <param name="context"></param>
        /// <returns>A scope that will be disposed when the execution has finished.</returns>
        IActivityScope ExecuteRequest(IRequestContext context);

        /// <summary>
        /// Called at the end of the execution if an exception occurred at some point,
        /// including unhandled exceptions when resolving fields.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="exception">The last exception that occurred.</param>
        void RequestError(IRequestContext context, Exception exception);

        /// <summary>
        /// Called when starting to parse a document.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>A scope that will be disposed when parsing has finished.</returns>
        IActivityScope ParseDocument(IRequestContext context);

        /// <summary>
        /// Called if a syntax error is detected in a document during parsing.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="error"></param>
        void SyntaxError(IRequestContext context, IError error);

        /// <summary>
        /// Called when starting to validate a document.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>A scope that will be disposed when the validation has finished.</returns>
        IActivityScope ValidateDocument(IRequestContext context);

        /// <summary>
        /// Called if there are any document validation errors.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="errors"></param>
        void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors);

        /// <summary>
        /// Called when starting to resolve a field value.
        /// </summary>
        /// <remarks>
        /// <see cref="IDiagnosticEventListener.EnableResolveFieldValue"/> must be true if
        /// a listener implements this method to ensure that it is called.
        /// </remarks>
        /// <param name="context"></param>
        /// <returns>A scope that will be disposed when the field resolution has finished.</returns>
        IActivityScope ResolveFieldValue(IMiddlewareContext context);

        /// <summary>
        /// Called for any errors during field resolution, including unhandled exceptions.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="error"></param>
        void ResolverError(IMiddlewareContext context, IError error);

        IActivityScope RunTask(IExecutionTask task);

        void TaskError(IExecutionTask task, IError error);

        void AddedDocumentToCache(IRequestContext context);

        void RetrievedDocumentFromCache(IRequestContext context);

        /// <summary>
        /// Called when the document for a persisted query has been read from storage.
        /// </summary>
        /// <param name="context"></param>
        void RetrievedDocumentFromStorage(IRequestContext context);

        void AddedOperationToCache(IRequestContext context);

        void RetrievedOperationFromCache(IRequestContext context);

        void BatchDispatched(IRequestContext context);

        void ExecutorCreated(string name, IRequestExecutor executor);

        void ExecutorEvicted(string name, IRequestExecutor executor);
    }
}
