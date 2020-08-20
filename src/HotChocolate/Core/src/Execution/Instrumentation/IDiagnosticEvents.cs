using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation
{
    public interface IDiagnosticEvents
    {
        IActivityScope ExecuteRequest(IRequestContext context);

        void RequestError(IRequestContext context, Exception exception);

        IActivityScope ParseDocument(IRequestContext context);

        void SyntaxError(IRequestContext context, IError error);

        IActivityScope ValidateDocument(IRequestContext context);

        void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors);

        IActivityScope ResolveFieldValue(IMiddlewareContext context);

        void ResolverError(IMiddlewareContext context, IError error);

        IActivityScope RunTask(IExecutionTask task);

        void TaskError(IExecutionTask task, IError error);

        void AddedDocumentToCache(IRequestContext context);

        void RetrievedDocumentFromCache(IRequestContext context);

        void RetrievedDocumentFromStorage(IRequestContext context);

        void AddedOperationToCache(IRequestContext context);

        void RetrievedOperationFromCache(IRequestContext context);

        void BatchDispatched(IRequestContext context);

        void ExecutorCreated(string name, IRequestExecutor executor);

        void ExecutorEvicted(string name, IRequestExecutor executor);
    }
}
