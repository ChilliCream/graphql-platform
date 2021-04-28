using System;
using System.Collections.Generic;
using HotChocolate.Execution.Processing;
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

        /// <inheritdoc />
        public virtual bool EnableResolveFieldValue => false;

        /// <summary>
        /// A no-op <see cref="IActivityScope"/> that can be returned from
        /// event methods that are not interested in when the scope is disposed.
        /// </summary>
        protected IActivityScope EmptyScope { get; } = new EmptyActivityScope();

        /// <inheritdoc />
        public virtual IActivityScope ExecuteRequest(IRequestContext context) => EmptyScope;

        /// <inheritdoc />
        public virtual void RequestError(IRequestContext context, Exception exception)
        {
        }

        /// <inheritdoc />
        public virtual IActivityScope ParseDocument(IRequestContext context) => EmptyScope;

        /// <inheritdoc />
        public virtual void SyntaxError(IRequestContext context, IError error)
        {
        }

        /// <inheritdoc />
        public virtual IActivityScope ValidateDocument(IRequestContext context) => EmptyScope;

        /// <inheritdoc />
        public virtual void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors)
        {
        }

        /// <inheritdoc />
        public virtual IActivityScope ResolveFieldValue(IMiddlewareContext context) => EmptyScope;

        /// <inheritdoc />
        public virtual void ResolverError(IMiddlewareContext context, IError error)
        {
        }

        /// <inheritdoc />
        public virtual IActivityScope RunTask(IExecutionTask task) => EmptyScope;

        /// <inheritdoc />
        public virtual void TaskError(IExecutionTask task, IError error)
        {
        }

        /// <inheritdoc />
        public virtual IActivityScope ExecuteSubscription(
            ISubscription subscription) =>
            EmptyScope;

        /// <inheritdoc />
        public virtual IActivityScope OnSubscriptionEvent(
            SubscriptionEventContext context) =>
            EmptyScope;

        /// <inheritdoc />
        public virtual void SubscriptionEventResult(
            SubscriptionEventContext context,
            IQueryResult result)
        {
        }

        /// <inheritdoc />
        public virtual void SubscriptionEventError(
            SubscriptionEventContext context,
            Exception exception)
        {
        }

        /// <inheritdoc />
        public virtual void SubscriptionTransportError(
            ISubscription subscription,
            Exception exception)
        {
        }

        /// <inheritdoc />
        public virtual void AddedDocumentToCache(IRequestContext context)
        {
        }

        /// <inheritdoc />
        public virtual void RetrievedDocumentFromCache(IRequestContext context)
        {
        }

        /// <inheritdoc />
        public virtual void RetrievedDocumentFromStorage(IRequestContext context)
        {
        }

        /// <inheritdoc />
        public virtual void AddedOperationToCache(IRequestContext context)
        {
        }

        /// <inheritdoc />
        public virtual void RetrievedOperationFromCache(IRequestContext context)
        {
        }

        /// <inheritdoc />
        public virtual void BatchDispatched(IRequestContext context)
        {
        }

        /// <inheritdoc />
        public virtual void ExecutorCreated(string name, IRequestExecutor executor)
        {
        }

        /// <inheritdoc />
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
