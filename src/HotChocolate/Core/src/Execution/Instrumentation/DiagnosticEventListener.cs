using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation
{
    public class DiagnosticEventListener : IDiagnosticEventListener
    {
        protected DiagnosticEventListener()
        {
        }

        public virtual bool EnableResolveFieldValue => false;

        protected IActivityScope EmptyScope { get; } = new EmptyActivityScope();

        public virtual IActivityScope ExecuteRequest(IRequestContext context) => EmptyScope;

        public virtual void RequestError(IRequestContext context, Exception exception)
        {
        }

        public virtual IActivityScope ParseDocument(IRequestContext context) => EmptyScope;

        public virtual void SyntaxError(IRequestContext context, IError error)
        {
        }

        public virtual IActivityScope ValidateDocument(IRequestContext context) => EmptyScope;

        public virtual void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors)
        {
        }

        public virtual IActivityScope ResolveFieldValue(IMiddlewareContext context) => EmptyScope;

        public virtual void ResolverError(IMiddlewareContext context, IError error)
        {
        }

        public virtual IActivityScope RunTask(IExecutionTask task) => EmptyScope;

        public virtual void TaskError(IExecutionTask task, IError error)
        {
        }

        public virtual void AddedDocumentToCache(IRequestContext context)
        {
        }

        public virtual void RetrievedDocumentFromCache(IRequestContext context)
        {
        }

        public virtual void RetrievedDocumentFromStorage(IRequestContext context)
        {
        }

        public virtual void AddedOperationToCache(IRequestContext context)
        {
        }

        public virtual void RetrievedOperationFromCache(IRequestContext context)
        {
        }

        public virtual void BatchDispatched(IRequestContext context)
        {
        }

        public virtual void ExecutorCreated(string name, IRequestExecutor executor)
        {
        }

        public virtual void ExecutorEvicted(string name, IRequestExecutor executor)
        {
        }

        private class EmptyActivityScope : IActivityScope
        {
            public void Dispose()
            {
            }
        }
    }
}
