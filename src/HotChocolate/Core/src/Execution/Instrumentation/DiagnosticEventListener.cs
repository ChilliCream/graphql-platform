using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation
{
    /// <summary>
    /// This class can be used as a base class for <see cref="IDiagnosticEventListener"/>
    /// implementations, so that they only have to override the methods they
    /// are interested in instead of having to provide implementations for all of them.
    /// </summary>
    public class DiagnosticEventListener : IDiagnosticEventListener
    {
        protected DiagnosticEventListener()
        {
        }

        public virtual bool EnableResolveFieldValue => false;

        /// <summary>
        /// A no-op <see cref="IActivityScope"/> that can be returned from
        /// event methods that are not interested in when the scope is disposed.
        /// </summary>
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

        private sealed class EmptyActivityScope : IActivityScope
        {
            public void Dispose()
            {
            }
        }
    }
}
